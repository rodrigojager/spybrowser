#!/usr/bin/env python3
"""Build a source-bound LOCAL verification feed from an exact Git archive. Never publish."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tarfile
import tempfile


def run(args, cwd, log):
    result = subprocess.run(args, cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    with log.open('a', encoding='utf-8') as stream:
        stream.write('$ ' + ' '.join(map(str, args)) + '\n' + result.stdout + '\n')
    if result.returncode:
        raise RuntimeError(f'Command failed ({result.returncode}); see {log}')
    return result.stdout.strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--final', action='store_true', help='Only after the parent has independently accepted the complete source candidate')
    args = parser.parse_args()
    repo = args.repository.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any(output.glob('*.nupkg')):
        parser.error('Output already contains packages; use a new immutable feed directory')
    log = output / 'build.log'
    git = ['git', '-c', f'safe.directory={repo.as_posix()}', '-C', str(repo)]
    commit = run(git + ['rev-parse', '--verify', args.source_commit + '^{commit}'], repo, log)
    archive = subprocess.run(git + ['archive', commit], check=True, stdout=subprocess.PIPE).stdout
    source_hash = hashlib.sha256(archive).hexdigest()
    with tempfile.TemporaryDirectory(prefix='spybrowser-source-bound-feed-') as temp:
        source = Path(temp) / 'source'
        source.mkdir()
        with tarfile.open(fileobj=io.BytesIO(archive), mode='r:') as tree:
            tree.extractall(source, filter='data')
        sdk = run(['dotnet', '--version'], source, log)
        for name in ('SpyBrowser.Core', 'SpyBrowser.Cursory', 'SpyBrowser.Playwright', 'SpyBrowser.Cli'):
            run(['dotnet', 'pack', str(source / 'src' / name / (name + '.csproj')),
                 '-c', 'Release', '-o', str(output), '--include-symbols',
                 '-p:SymbolPackageFormat=snupkg', '-p:ContinuousIntegrationBuild=true',
                 '-p:Version=' + args.version, '-p:RepositoryCommit=' + commit,
                 '-p:RepositoryUrl=https://github.com/rodrigojager/spybrowser.git',
                 '-p:MicrosoftPlaywrightVersion=1.61.0'], source, log)
    hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(output.glob('*.nupkg'))}
    symbols = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(output.glob('*.snupkg'))}
    manifest = {'schemaVersion': 1, 'candidateVersion': args.version, 'sourceCommit': commit,
                'declaredFinalCommit': commit if args.final else None, 'isFinal': args.final,
                'sourceArchiveSha256': source_hash, 'sdk': sdk, 'playwrightVersion': '1.61.0',
                'packages': hashes, 'symbols': symbols, 'externalPublicationAllowed': False}
    (output / 'feed-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()
