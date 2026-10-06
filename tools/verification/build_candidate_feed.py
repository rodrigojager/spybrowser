#!/usr/bin/env python3
"""Build and audit a source-bound LOCAL verification feed from an exact Git archive. Never publish."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import signal
import tarfile
import tempfile
import zipfile
import xml.etree.ElementTree as ET

SOURCELINK_KIND = "cc110556-a091-4d38-9fec-25ab9a351a6a"
REPOSITORY = "https://github.com/rodrigojager/spybrowser"
RAW_SOURCE_URL = "https://raw.githubusercontent.com/rodrigojager/spybrowser"
PACKAGES = ("SpyBrowser.Core", "SpyBrowser.Cursory", "SpyBrowser.Playwright", "SpyBrowser.Cli")
TIMEOUT = 300


def run(args, cwd, log, timeout=TIMEOUT):
    kwargs = {'cwd': cwd, 'text': True, 'stdout': subprocess.PIPE,
              'stderr': subprocess.STDOUT}
    if os.name == 'nt':
        kwargs['creationflags'] = subprocess.CREATE_NEW_PROCESS_GROUP
    else:
        kwargs['start_new_session'] = True
    process = subprocess.Popen(args, **kwargs)
    try:
        output, _ = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired as error:
        if os.name == 'nt':
            # Kill only this command's process tree, while its leader PID is still alive.
            try:
                subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'],
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                               text=True, timeout=30, check=False)
            except subprocess.TimeoutExpired:
                process.kill()
        else:
            try:
                os.killpg(process.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass
            try:
                process.communicate(timeout=5)
            except subprocess.TimeoutExpired:
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
        output, _ = process.communicate()
        with log.open('a', encoding='utf-8') as stream:
            stream.write('$ ' + ' '.join(map(str, args)) + '\n' + (output or '') +
                         f'\nTIMEOUT after {timeout}s; owned process tree terminated\n')
        raise RuntimeError(f'Command timed out after {timeout}s; see {log}') from error
    with log.open('a', encoding='utf-8') as stream:
        stream.write('$ ' + ' '.join(map(str, args)) + '\n' + (output or '') + '\n')
    if process.returncode:
        raise RuntimeError(f'Command failed ({process.returncode}); see {log}')
    return (output or '').strip()


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def _write_pdb_auditor(root):
    """Compile a BCL-only auditor so CDI and document hashes are read from actual Portable PDBs."""
    project = root / 'pdb-audit'
    project.mkdir()
    (project / 'PdbAudit.csproj').write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
        '<Nullable>enable</Nullable></PropertyGroup></Project>\n', encoding='utf-8')
    (project / 'Program.cs').write_text(r'''using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
var result = new List<object>();
foreach (var path in args) {
  using var stream = File.OpenRead(path);
  using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
  var reader = provider.GetMetadataReader();
  var sourceLink = new List<string>();
  foreach (var handle in reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)) {
    var item = reader.GetCustomDebugInformation(handle);
    var kind = reader.GetGuid(item.Kind).ToString();
    if (kind == "cc110556-a091-4d38-9fec-25ab9a351a6a")
      sourceLink.Add(System.Text.Encoding.UTF8.GetString(reader.GetBlobBytes(item.Value)));
  }
  var docs = reader.Documents.Select(h => {
    var d = reader.GetDocument(h);
    return new { path = reader.GetString(d.Name), hashAlgorithm = reader.GetGuid(d.HashAlgorithm).ToString(),
      checksum = Convert.ToHexString(reader.GetBlobBytes(d.Hash)).ToLowerInvariant() };
  }).ToArray();
  result.Add(new { file = Path.GetFileName(path), sourceLink, documents = docs });
}
Console.WriteLine(JsonSerializer.Serialize(result));
''', encoding='utf-8')
    return project / 'PdbAudit.csproj'


def validate_pdb_audit(audit, source_root, commit, source_link_map):
    if not re.fullmatch(r'[0-9a-f]{40}', commit):
        raise RuntimeError('SourceLink audit requires a full lowercase 40-character source commit')
    expected_mapping = {'documents': {'/_/*': f'{RAW_SOURCE_URL}/{commit}/*'}}
    if source_link_map != expected_mapping:
        raise RuntimeError('Caller-supplied SourceLink mapping is not the canonical raw-source mapping')
    expected_url = f'{RAW_SOURCE_URL}/{commit}/'
    documents_seen = source_documents = generated_documents = 0
    for pdb in audit:
        maps = pdb['sourceLink']
        if len(maps) != 1:
            raise RuntimeError(f"{pdb['file']}: expected exactly one SourceLink CDI, found {len(maps)}")
        try:
            actual_map = json.loads(maps[0])
        except (TypeError, json.JSONDecodeError) as error:
            raise RuntimeError(f"{pdb['file']}: invalid SourceLink CDI JSON") from error
        if actual_map != expected_mapping:
            raise RuntimeError(f"{pdb['file']}: SourceLink CDI mapping is not the canonical raw-source commit mapping")
        docs = pdb['documents']
        if not docs:
            raise RuntimeError(f"{pdb['file']}: no Portable PDB documents")
        for document in docs:
            documents_seen += 1
            path = document['path'].replace('\\\\', '/')
            if path.startswith('/'):
                path = path[1:]
            match = re.fullmatch(r'_/(.+)', path)
            if not match:
                raise RuntimeError(f"{pdb['file']}: non-normalized PDB document path {path!r}")
            relative = PurePosixPath(match.group(1))
            if '..' in relative.parts or relative.is_absolute():
                raise RuntimeError(f"{pdb['file']}: unsafe PDB document path {path!r}")
            url = expected_url + relative.as_posix()
            if '/obj/' in url:
                generated_documents += 1
                continue
            source = source_root.joinpath(*relative.parts)
            if not source.is_file():
                raise RuntimeError(f"{pdb['file']}: mapped document absent from source archive: {relative}")
            if document['hashAlgorithm'] != '8829d00f-11b8-4213-878b-770e8597ac16':
                raise RuntimeError(f"{pdb['file']}: unsupported document checksum algorithm for {relative}")
            if document['checksum'] != sha256(source.read_bytes()):
                raise RuntimeError(f"{pdb['file']}: source checksum mismatch for {relative}")
            source_documents += 1
    if not source_documents:
        raise RuntimeError('No source-controlled PDB documents were checksum-verified')
    return {'pdbCount': len(audit), 'documentCount': documents_seen,
            'sourceControlledDocumentsVerified': source_documents,
            'generatedDocumentsExcluded': generated_documents,
            'sourceLinkCdiGuid': SOURCELINK_KIND, 'sourceLinkUrlPrefix': expected_url,
            'sourceLinkMappingKey': '/_/*', 'sourceLinkUrlTemplate': f'{RAW_SOURCE_URL}/{commit}/*',
            'remoteSourceAccessVerified': False}


def audit_nuget_metadata(output, commit, version):
    packages = sorted(output.glob('*.nupkg'))
    if len(packages) != len(PACKAGES):
        raise RuntimeError(f'Expected four NuGet packages, found {len(packages)}')
    found_ids = set()
    for package in packages:
        with zipfile.ZipFile(package) as archive:
            nuspecs = [name for name in archive.namelist() if name.lower().endswith('.nuspec')]
            if len(nuspecs) != 1:
                raise RuntimeError(f'{package.name}: expected exactly one nuspec, found {len(nuspecs)}')
            root = ET.fromstring(archive.read(nuspecs[0]))
        metadata = next((node for node in root.iter() if node.tag.split('}')[-1] == 'metadata'), None)
        if metadata is None:
            raise RuntimeError(f'{package.name}: nuspec metadata is missing')
        fields = {node.tag.split('}')[-1]: node for node in metadata}
        package_id = fields.get('id').text if fields.get('id') is not None else None
        package_version = fields.get('version').text if fields.get('version') is not None else None
        repository = fields.get('repository')
        if package_id not in PACKAGES or package_id in found_ids:
            raise RuntimeError(f'{package.name}: unexpected or duplicate nuspec package ID {package_id!r}')
        found_ids.add(package_id)
        if package_version != version:
            raise RuntimeError(f'{package.name}: nuspec version {package_version!r} != {version!r}')
        if repository is None or repository.attrib.get('commit') != commit:
            raise RuntimeError(f'{package.name}: nuspec repository commit does not match full source SHA')
        if repository.attrib.get('url', '').rstrip('/').removesuffix('.git') != REPOSITORY:
            raise RuntimeError(f'{package.name}: nuspec repository URL is incorrect')
    if found_ids != set(PACKAGES):
        raise RuntimeError(f'NuGet package set mismatch: {sorted(found_ids)}')
    return {'packageIds': sorted(found_ids), 'repositoryCommitVerified': commit,
            'repositoryUrlVerified': REPOSITORY, 'versionVerified': version}


def audit_symbols(output, source_root, commit, source_link_map, log):
    packages = sorted(output.glob('*.snupkg'))
    if len(packages) != len(PACKAGES):
        raise RuntimeError(f'Expected four symbol packages, found {len(packages)}')
    expected_ids = {name.lower() for name in PACKAGES}
    actual_ids = {p.name.lower().split('.', 2)[0] + '.' + p.name.lower().split('.', 2)[1]
                  for p in packages}
    if actual_ids != expected_ids:
        raise RuntimeError('Symbol package IDs do not match the required four production packages')
    with tempfile.TemporaryDirectory(prefix='spybrowser-pdb-audit-') as temp:
        root = Path(temp)
        pdb_by_name = {}
        for package in packages:
            with zipfile.ZipFile(package) as archive:
                members = [name for name in archive.namelist() if name.lower().endswith('.pdb')]
                package_id = package.name.lower().split('.', 2)[0] + '.' + package.name.lower().split('.', 2)[1]
                package_pdb_ids = {name.lower() for name in PACKAGES} if package_id == 'spybrowser.cli' else {package_id}
                # CLI's symbol package intentionally carries all four production PDBs.
                found_ids = {PurePosixPath(name).stem.lower() for name in members}
                if found_ids != package_pdb_ids or len(members) != len(found_ids):
                    raise RuntimeError(f'{package.name}: expected unique production PDBs {sorted(package_pdb_ids)}, found {sorted(found_ids)}')
                for member in members:
                    name = PurePosixPath(member).name
                    data = archive.read(member)
                    if name in pdb_by_name and pdb_by_name[name] != data:
                        raise RuntimeError(f'{package.name}: conflicting duplicate PDB {name}')
                    pdb_by_name[name] = data
        pdbs = []
        for name, data in sorted(pdb_by_name.items()):
            target = root / name
            target.write_bytes(data)
            pdbs.append(target)
        project = _write_pdb_auditor(root)
        (root / 'global.json').write_text(json.dumps({'sdk': {'version': '8.0.319',
                                                               'rollForward': 'disable'}}) + '\n',
                                          encoding='utf-8')
        audit = json.loads(run(['dotnet', 'run', '--project', str(project), '-c', 'Release',
                                '-p:UseSharedCompilation=false', '-p:NodeReuse=false', '--',
                                *map(str, pdbs)], root, log))
    return validate_pdb_audit(audit, source_root, commit, source_link_map)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--final', action='store_true', help='Only after the parent independently accepts the complete source candidate')
    args = parser.parse_args()
    repo = args.repository.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any(output.glob('*.nupkg')) or any(output.glob('*.snupkg')):
        parser.error('Output already contains packages; use a new immutable feed directory')
    log = output / 'build.log'
    git = ['git', '-c', f'safe.directory={repo.as_posix()}', '-C', str(repo)]
    commit = run(git + ['rev-parse', '--verify', args.source_commit + '^{commit}'], repo, log)
    if not re.fullmatch(r'[0-9a-f]{40}', commit):
        parser.error('Resolved source commit is not a full 40-character SHA')
    archive = subprocess.run(git + ['archive', commit], cwd=repo, stdout=subprocess.PIPE,
                             stderr=subprocess.PIPE, timeout=TIMEOUT, check=True).stdout
    source_hash = sha256(archive)
    with tempfile.TemporaryDirectory(prefix='spybrowser-source-bound-feed-') as temp:
        source = Path(temp) / 'source'
        source.mkdir()
        with tarfile.open(fileobj=io.BytesIO(archive), mode='r:') as tree:
            tree.extractall(source, filter='data')
        rights_manifest = json.loads((source / 'src/SpyBrowser.Cursory/Data/upstream-manifest.json').read_text(encoding='utf-8'))
        rights_review = rights_manifest.get('dataset', {}).get('rightsReview', {})
        dataset_rights = rights_review.get('status', 'UNKNOWN')
        if not isinstance(dataset_rights, str) or not dataset_rights.strip():
            dataset_rights = 'UNKNOWN'
        sdk = run(['dotnet', '--version'], source, log)
        if sdk != '8.0.319':
            raise RuntimeError(f'Requires isolated .NET SDK 8.0.319; resolved {sdk!r}')
        source_link_map = {'documents': {'/_/*': f'{RAW_SOURCE_URL}/{commit}/*'}}
        source_link_file = Path(temp) / 'sourcelink.json'
        source_link_file.write_text(json.dumps(source_link_map, separators=(',', ':')) + '\n', encoding='utf-8')
        for name in PACKAGES:
            run(['dotnet', 'pack', str(source / 'src' / name / (name + '.csproj')),
                 '-c', 'Release', '-o', str(output), '--include-symbols',
                 '-p:SymbolPackageFormat=snupkg', '-p:ContinuousIntegrationBuild=true',
                 '-p:Version=' + args.version, '-p:RepositoryCommit=' + commit,
                 '-p:RepositoryUrl=' + REPOSITORY + '.git', '-p:MicrosoftPlaywrightVersion=1.61.0',
                 '-p:SourceLink=' + str(source_link_file), '-p:PathMap=' + str(source) + '=/_'],
                source, log)
        pdb_audit = audit_symbols(output, source, commit, source_link_map, log)
    nuspec_audit = audit_nuget_metadata(output, commit, args.version)
    hashes = {p.name: sha256(p.read_bytes()) for p in sorted(output.glob('*.nupkg'))}
    symbols = {p.name: sha256(p.read_bytes()) for p in sorted(output.glob('*.snupkg'))}
    if len(hashes) != 4 or len(symbols) != 4:
        raise RuntimeError(f'Expected exactly four .nupkg and .snupkg files; found {len(hashes)} and {len(symbols)}')
    manifest = {'schemaVersion': 1, 'candidateVersion': args.version, 'sourceCommit': commit,
                'declaredFinalCommit': commit if args.final else None, 'isFinal': args.final,
                'sourceArchiveSha256': source_hash, 'sdk': sdk, 'playwrightVersion': '1.61.0',
                'packages': hashes, 'symbols': symbols, 'nuspecAudit': nuspec_audit, 'portablePdbAudit': pdb_audit,
                'externalPublicationAllowed': False, 'datasetRights': dataset_rights}
    (output / 'feed-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()
