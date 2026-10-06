import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tarfile
import sys
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).parents[1] / 'build_candidate_feed.py'
spec = importlib.util.spec_from_file_location('candidate_feed', SCRIPT)
feed = importlib.util.module_from_spec(spec)
spec.loader.exec_module(feed)


class CandidateFeedAuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name)
        self.commit = 'a' * 40
        self.mapping = {'documents': {'/_/*': f'{feed.RAW_SOURCE_URL}/{self.commit}/*'}}
        self.file = self.source / 'src' / 'Example.cs'
        self.file.parent.mkdir(parents=True)
        self.file.write_bytes(b'class Example {}\n')
        self.document = {'path': '_/src/Example.cs',
                         'hashAlgorithm': '8829d00f-11b8-4213-878b-770e8597ac16',
                         'checksum': hashlib.sha256(self.file.read_bytes()).hexdigest()}
        self.good = [{'file': 'Example.pdb', 'sourceLink': [json.dumps(self.mapping)],
                      'documents': [self.document]}]

    def test_archive_source_commit_preserves_git_blob_with_autocrlf_enabled(self):
        scratch = Path(__file__).parents[3] / '.scratch'
        scratch.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=scratch, prefix='candidate-feed-git-') as temp:
            repository = Path(temp)
            def git(*args):
                return subprocess.run(['git', *args], cwd=repository, check=True,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout

            git('init', '--quiet')
            git('config', 'core.autocrlf', 'true')
            git('config', 'user.name', 'Candidate Feed Test')
            git('config', 'user.email', 'candidate-feed@example.invalid')
            source = b'class Example {\n    // preserve LF bytes\n}\n'
            (repository / 'Example.cs').write_bytes(source)
            git('add', 'Example.cs')
            git('commit', '--quiet', '-m', 'test source')
            commit = git('rev-parse', 'HEAD').decode().strip()
            blob = git('show', f'{commit}:Example.cs')
            self.assertEqual(blob, source)

            default_archive = git('archive', commit)
            with tarfile.open(fileobj=io.BytesIO(default_archive), mode='r:') as archive:
                self.assertEqual(archive.extractfile('Example.cs').read(),
                                 source.replace(b'\n', b'\r\n'))

            fixed_archive = feed.archive_source_commit(repository, commit)
            with tarfile.open(fileobj=io.BytesIO(fixed_archive), mode='r:') as archive:
                self.assertEqual(archive.extractfile('Example.cs').read(), blob)

    def test_run_timeout_captures_output_and_terminates_owned_process(self):
        log = self.source / 'timeout.log'
        code = 'import time; print("started", flush=True); time.sleep(30)'
        with self.assertRaisesRegex(RuntimeError, 'timed out'):
            feed.run([sys.executable, '-c', code], self.source, log, timeout=0.2)
        contents = log.read_text(encoding='utf-8')
        self.assertIn('started', contents)
        self.assertIn('TIMEOUT after 0.2s', contents)

    def test_nuspec_metadata_requires_full_commit_and_expected_identity(self):
        output = self.source / 'feed'
        output.mkdir()
        commit = self.commit
        for name in feed.PACKAGES:
            package = output / f'{name}.nupkg'
            recorded_commit = 'b' * 40 if name == 'SpyBrowser.Cursory' else commit
            nuspec = f'''<package><metadata><id>{name}</id><version>9.1.0</version>
              <repository type="git" url="{feed.REPOSITORY}.git" commit="{recorded_commit}" />
              </metadata></package>'''
            with zipfile.ZipFile(package, 'w') as archive:
                archive.writestr(f'{name}.nuspec', nuspec)
        with self.assertRaisesRegex(RuntimeError, 'repository commit does not match'):
            feed.audit_nuget_metadata(output, commit, '9.1.0')

    def test_good_source_link_and_source_checksum_pass(self):
        result = feed.validate_pdb_audit(self.good, self.source, self.commit, self.mapping)
        self.assertEqual(result['sourceControlledDocumentsVerified'], 1)
        self.assertEqual(result['sourceLinkCdiGuid'], feed.SOURCELINK_KIND)

    def test_missing_source_link_cdi_fails_closed(self):
        with self.assertRaisesRegex(RuntimeError, 'expected exactly one SourceLink CDI'):
            feed.validate_pdb_audit([{'file': 'x.pdb', 'sourceLink': [],
                                      'documents': [self.document]}], self.source,
                                    self.commit, self.mapping)

    def test_wrong_commit_mapping_fails_closed(self):
        wrong_commit = 'b' * 40
        bad = [{'file': 'x.pdb', 'sourceLink': [json.dumps({'documents': {
            '/_/*': f'{feed.REPOSITORY}/{wrong_commit}/*'}})], 'documents': [self.document]}]
        with self.assertRaisesRegex(RuntimeError, 'canonical raw-source'):
            feed.validate_pdb_audit(bad, self.source, self.commit, self.mapping)

    def test_github_html_source_route_rejected_even_as_caller_expectation(self):
        html_map = {'documents': {'/_/*': f'{feed.REPOSITORY}/{self.commit}/*'}}
        audit = [{'file': 'x.pdb', 'sourceLink': [json.dumps(html_map)],
                  'documents': [self.document]}]
        with self.assertRaisesRegex(RuntimeError, 'Caller-supplied.*canonical raw-source'):
            feed.validate_pdb_audit(audit, self.source, self.commit, html_map)

    def test_wrong_mapping_scope_and_wildcard_suffix_rejected(self):
        for documents in (
            {'/src/*': f'{feed.RAW_SOURCE_URL}/{self.commit}/*'},
            {'/_/*': f'{feed.RAW_SOURCE_URL}/{self.commit}/src/*'},
            {'/_/*': f'{feed.RAW_SOURCE_URL}/{self.commit}//*'},
        ):
            with self.subTest(documents=documents):
                bad_mapping = {'documents': documents}
                audit = [{'file': 'x.pdb', 'sourceLink': [json.dumps(bad_mapping)],
                          'documents': [self.document]}]
                with self.assertRaisesRegex(RuntimeError, 'Caller-supplied.*canonical raw-source'):
                    feed.validate_pdb_audit(audit, self.source, self.commit, bad_mapping)

    def test_valid_raw_mapping_reports_full_commit_source_prefix(self):
        result = feed.validate_pdb_audit(self.good, self.source, self.commit, self.mapping)
        self.assertEqual(result['sourceLinkUrlPrefix'],
                         f'{feed.RAW_SOURCE_URL}/{self.commit}/')
        self.assertEqual(result['sourceLinkUrlTemplate'],
                         f'{feed.RAW_SOURCE_URL}/{self.commit}/*')
        self.assertEqual(result['sourceLinkMappingKey'], '/_/*')
        self.assertFalse(result['remoteSourceAccessVerified'])

    def test_document_path_escape_fails_closed(self):
        bad_document = dict(self.document, path='_/../secrets.cs')
        with self.assertRaisesRegex(RuntimeError, 'unsafe PDB document path'):
            feed.validate_pdb_audit([{'file': 'x.pdb', 'sourceLink': [json.dumps(self.mapping)],
                                      'documents': [bad_document]}], self.source,
                                    self.commit, self.mapping)

    def test_document_checksum_mismatch_fails_closed(self):
        bad_document = dict(self.document, checksum='0' * 64)
        with self.assertRaisesRegex(RuntimeError, 'source checksum mismatch'):
            feed.validate_pdb_audit([{'file': 'x.pdb', 'sourceLink': [json.dumps(self.mapping)],
                                      'documents': [bad_document]}], self.source,
                                    self.commit, self.mapping)

    def test_source_document_missing_from_archive_fails_closed(self):
        bad_document = dict(self.document, path='_/src/Missing.cs')
        with self.assertRaisesRegex(RuntimeError, 'absent from source archive'):
            feed.validate_pdb_audit([{'file': 'x.pdb', 'sourceLink': [json.dumps(self.mapping)],
                                      'documents': [bad_document]}], self.source,
                                    self.commit, self.mapping)

    def test_no_source_documents_fails_closed(self):
        generated = dict(self.document, path='_/src/obj/generated.g.cs')
        with self.assertRaisesRegex(RuntimeError, 'No source-controlled'):
            feed.validate_pdb_audit([{'file': 'x.pdb', 'sourceLink': [json.dumps(self.mapping)],
                                      'documents': [generated]}], self.source,
                                    self.commit, self.mapping)


if __name__ == '__main__':
    unittest.main()
