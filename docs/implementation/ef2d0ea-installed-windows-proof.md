# Installed Windows proof — ef2d0ea

**Result:** 13/13 technical checks PASS, 0 pending, 0 failed. The overall runner returned its expected exit code **2** because dataset redistribution rights remain blocked; this is not a technical test failure and does not authorize publication.

## Exact candidate and provenance

- Version: `0.2.0-beta.2.final.ef2d0ea`
- Source and declared-final commit: `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`; feed manifest says `isFinal: true`.
- Producer feed: `D:/Temp/spybrowser-feed-final-ef2d0ea`; manifest SHA256: `689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`.
- Producer source archive SHA256: `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`.
- SDK: `8.0.319`; Playwright: `1.61.0` / installed runtime `1.61.0.0`.
- Manifest/build audit verifies 4 PDBs, 58 source-controlled documents out of 71 (13 generated documents excluded), SourceLink map `/_*` to the exact source commit, and no remote source access. These are producer-audit claims; this proof does not claim an independent SourceLink CDI GUID decode.

The exact package and symbol file hashes verified by the runner are recorded in the JSON proof. The full per-entry SHA256 inventory (including packaged source/license/notice bytes), nuspec metadata/hashes, runtime assembly hashes, and candidate/baseline artifact inventory are retained in `artifacts/goal/installed-ef2d0ea-windows-proof/distribution-evidence.json`.

## Installed-package execution

The runner installed and invoked the candidate CLI package offline, then restored and built the consumer using package feeds only. The consumer working directory was outside `D:/SpyBrowser`; the harness has no `ProjectReference`. It used an isolated temporary NuGet cache, preserving the existing global cache, and removed its consumer/profile after execution. CLI output reported the candidate version, Windows 10.0.19045 x64, and Playwright 1.61.0.0.

| Technical check | Result |
|---|---|
| Previous package provenance (baseline `0.1.0-baseline.e217359`) | PASS |
| Installed CLI package, offline version | PASS |
| Installed candidate Cursory path | PASS |
| Installed candidate Bézier path | PASS |
| Installed candidate `off` path | PASS |
| Snapshot save, explicit baseline, compare, concurrency, schema, secrets, discard | PASS |
| OS process termination during active SDK write; baseline preserved | PASS |
| Real Windows NTFS ACL denial | PASS |
| GPU version/context information | PASS |
| Previous-version Bézier rollback | PASS |
| Previous package ignores newer schema-999 snapshot; bytes preserved | PASS |
| Previous-version humanization off | PASS |
| Browser executable and official Playwright driver | PASS |

The active-write test killed a worker after confirming it was in the SDK temporary-file write; it recorded zero successful saves before termination and one orphan temporary file, without claiming startup cleanup/recovery. The permission-denial test used an enforced NTFS ACL and restored it before cleanup. Snapshot/profile evidence contains hashes only; no cookie values or real user data are recorded.

## Gate and artifacts

- External dataset-distribution license: **BLOCKED**; `externalPublicationAllowed: false`.
- `technicalAllPassed: true`; `allPassed: false`; runner exit 2 is expected for that rights gate.
- Detailed machine evidence: `artifacts/goal/installed-ef2d0ea-windows-proof/distribution-evidence.json` (SHA256 `6c7329ab27fc43f945da99595f73c6a40c8708d23544abb9519affa13b96d1e0`).
- Source-bound package producer log: `D:/Temp/spybrowser-feed-final-ef2d0ea/build.log`.
- No package bytes were rebuilt or reused from the older 4d proof, no publication or promotion was performed, and no source or runner code was changed.
