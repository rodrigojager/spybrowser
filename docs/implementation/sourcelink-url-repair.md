# SourceLink raw-source URL repair (pre-freeze)

The earlier `sourcelink-build-fix` candidate embedded `https://github.com/rodrigojager/spybrowser/<sha>/*`, an HTML route rather than a raw source endpoint. Its earlier positive test did not establish the correct source-download route. That prototype feed and its declarations are preserved unchanged; this repair is a separate preliminary candidate only.

`tools/verification/build_candidate_feed.py` now emits the canonical mapping key `/_/*` to `https://raw.githubusercontent.com/rodrigojager/spybrowser/<full-40-character-SHA>/*`. The validator independently derives that canonical expected mapping and rejects incorrect CDI JSON even if the caller supplies the same incorrect expectation. It checks exact map key and wildcard suffix, commit, normalized `/_/` PDB source paths and path traversal, while matching 58 source-controlled PDB document SHA-256 values against the exact archived source. The nuspec repository URL remains `https://github.com/rodrigojager/spybrowser`.

## Validation performed

- `python -m unittest tools.verification.tests.test_candidate_feed -v` — 12 tests passed, including rejection of GitHub HTML URLs supplied both in the PDB CDI and caller expectation, wrong scope, extra/wrong wildcard route, commit mismatch, and path escape.
- `python -m py_compile tools/verification/build_candidate_feed.py tools/verification/tests/test_candidate_feed.py` — passed.
- `python tools/verification/build_candidate_feed.py --repository . --source-commit 4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b --version 0.2.0-beta.2.rawmapcheck --output artifacts/goal/sourcelink-url-repair/candidate-4d8a18c` — passed with SDK 8.0.319, exact archive SHA-256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`, four PDBs, 4/4 canonical raw SourceLink CDI mappings, and 58/58 source document checksums. There were 71 total PDB documents; 13 generated `obj` documents were excluded. Four package and four symbol-package files passed metadata/PDB checks.

The actual CDI JSON in the four locally built PDBs structurally proves the raw-content route and full-SHA file path. This is not a network accessibility check: no remote fetch was attempted or claimed. The candidate is version `0.2.0-beta.2.rawmapcheck`, non-final (`declaredFinalCommit: null`), external publication is disallowed, and dataset rights remain `UNVERIFIED`. The parent should independently review the integrated freeze and build a new final feed; these artifacts do not declare or establish a final feed.

Machine-readable result: [`sourcelink-url-repair.json`](sourcelink-url-repair.json). Candidate manifest and build log: [`artifacts/goal/sourcelink-url-repair/candidate-4d8a18c/`](../../artifacts/goal/sourcelink-url-repair/candidate-4d8a18c/).
