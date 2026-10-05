# SourceLink build provenance repair

**Implemented and exercised on the frozen source archive; not a final-feed acceptance.** The existing builder archives the chosen Git commit into a temporary clean source tree, where SDK SourceLink Git discovery cannot rely on `.git`. `tools/verification/build_candidate_feed.py` now writes explicit SourceLink JSON for that full resolved SHA and passes it to every production `dotnet pack`. It also passes a deterministic compiler `PathMap` mapping the temporary archive root to `/_/`, avoiding workstation/temp-directory paths in Portable PDB documents. The mapping is derived at runtime, never pinned to `4d8a18c`:

```json
{"documents":{"/_/*":"https://github.com/rodrigojager/spybrowser/<full-commit-sha>/*"}}
```

The builder rejects SDKs other than 8.0.319, adds bounded subprocess timeouts, supplies the full `RepositoryCommit`, and inspects actual `.snupkg` PDBs using a temporary .NET 8 BCL `System.Reflection.Metadata` auditor. It fails closed unless all four production PDBs contain exactly one matching SourceLink CDI record, use normalized source-root paths, and every non-generated document checksum matches the same extracted Git archive. Generated `obj` documents are not treated as mapped source. The CLI symbol package must carry all four production PDBs; duplicate symbols found across packages must be byte-identical. It also validates all four package nuspec IDs, expected version, full `RepositoryCommit`, and repository URL. Manifest/package metadata marks the build non-final unless explicitly opted in, records external publication as disallowed, and records dataset rights as `UNVERIFIED`.

## Verification performed

Commands (from workspace root):

```text
python -m unittest discover -s tools/verification/tests -v
python -m py_compile tools/verification/build_candidate_feed.py
python tools/verification/build_candidate_feed.py --repository . --source-commit 4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b --version 0.2.0-beta.2.sourcelink-candidate --output artifacts/goal/sourcelink-build-fix/candidate-4d8a18c
```

Observed: **8 unit tests passed**, syntax compilation passed, and the real source-bound candidate build passed the PDB audit using SDK **8.0.319**. A subsequent nuspec metadata audit of that exact local candidate feed also passed. Results: 4 production PDBs, 4/4 exact SourceLink CDI mappings, 71 PDB documents, 58/58 source-controlled document SHA-256 checksums equal the exact source-archive files, 13 generated `obj` documents excluded, all four production PDBs included in the CLI `.snupkg`, and four correct package nuspec IDs/version/full commit/repository URL. The local feed's hashes and detailed machine-readable audit are in [`artifacts/goal/sourcelink-build-fix/candidate-4d8a18c/feed-manifest.json`](../../artifacts/goal/sourcelink-build-fix/candidate-4d8a18c/feed-manifest.json). The source archive SHA-256 remains `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`.

`tools/verification/tests/test_candidate_feed.py` exercises matching provenance and fail-closed behavior for missing CDI, wrong commit mapping, invalid path traversal, checksum mismatch, source absent from archive, and an audit containing no source-controlled document.

## Required follow-up and limits

The exercised commit is the frozen `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`, before pending peer-owned integration. This candidate is explicitly **not final** (`declaredFinalCommit: null`) and is only proof that the implemented build configuration produces correct SourceLink in actual symbols. Once integration is frozen, run the same builder on the new full source SHA, then independently verify the resulting candidate feed. Do not edit or replace prior final-feed metadata/packages, do not claim final acceptance, and do not publish. Remote URL accessibility and dataset rights remain unverified.
