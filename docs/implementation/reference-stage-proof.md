# Pinned Cursory reference-stage proof

**Result:** fresh pinned-JavaScript regeneration and current .NET differential verification passed on Windows and WSL/Linux. This is evidence for the ticket-03 parity slice only; it does not certify the whole ticket or native-Cursory goal.

## Provenance and source coherency

- Workspace: `D:/SpyBrowser`, HEAD `7be0292fa4080e9b6fbb02bffac3608bf67c8299`.
- Reference: `cursory-js` 2.0.1, commit `16fff97fab05bb6b0c6753b2dc136a7692634cec`, clean checkout at `C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-cursory-js-review`.
- Dataset SHA-256 (JS source, JS dist copy, and native embedded gzip): `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`.
- Pinned checkout was clean before/after generation; `npm ls --depth=0` matched installed package versions (TypeScript 5.9.3); Node v26.1.0, npm 11.12.0. Build output retained identical dist/resource hashes. Temporary instrumentation was made in a copied checkout and removed; the pinned source checkout was not modified.
- Native generator inputs were clean in the workspace, and their source hashes are recorded in `artifacts/goal/reference-stage-proof/manifest.json`. No product, test-source, fixture-source, or generator source was changed. Existing unrelated workspace untracked files were left alone.
- Pinned JS dataset contains 2,356 recordings, versus Python's 2,357; the selected JS snapshot excludes recording `2281`. No Python-derived expected values were used.

## Regeneration and byte comparison

Used `tools/cursory-reference/generate-js-fixtures.cjs` and `generate-js-stage-fixtures.cjs`, with only their output paths redirected in temporary copies under `C:/Users/Rodrigo/AppData/Local/Temp/reference-stage-proof-7be0292/`. The final generator emitted 160 cases. The stage generator instrumented a temporary JS source copy and emitted 3 stage cases. Generated JSON was normalized to LF and compared byte-for-byte; checked-in test fixtures were not overwritten.

- `artifacts/goal/reference-stage-proof/reference-fixtures/parity.json`: 961,337 bytes; SHA-256 `bd1b706830ec0719a3e44b95d54c2f6fe928966a8891d5de5b9869cc842fe1ac`; byte-identical to current checked-in final fixtures.
- `artifacts/goal/reference-stage-proof/reference-fixtures/stage-parity.json`: 61,156 bytes; SHA-256 `47ab441cbf8806985054d60daebbfcf939ac5007c87527ceda43366f9dde29c0`; byte-identical to current checked-in intermediate fixtures.
- Both generated files identify the pinned source commit and dataset hash. Actual hashes also match the checked-in upstream manifest.

**Hash subject resolved by repository history:** the parent read exact Git blobs for `tests/SpyBrowser.Cursory.Tests/Fixtures/parity.json`. At `2275ffe` it contains 160 cases and 658,363 bytes, SHA-256 `f48dc331139c212d9c2d1407afc4a7de784a7787995522213dacb80e59db38ba`. The later fixture-completion commit `1132b2e` expanded those 160 cases to 961,337 bytes, SHA-256 `bd1b706830ec0719a3e44b95d54c2f6fe928966a8891d5de5b9869cc842fe1ac`; this is also the exact current blob and independently regenerated output. Thus `f48dc331…` was a historical fixture revision, not the current expected hash or an aggregate. No fixture, assertion, tolerance or hash was rewritten to conceal the difference. Current manifest and current generated/checked-in bytes match.

## Differential test results

Built the current test project in isolated `artifacts/goal/reference-stage-proof/{windows,linux}` directories. After each build, copied only the independently generated JSON into that build's `bin/.../Fixtures` output (not the tracked fixtures), then ran the compiled tests with `--no-build` against those copies.

| Environment | SDK | Generated-fixture tests | Logs |
|---|---:|---:|---|
| Windows x64 | 8.0.319 | 36 passed, 0 failed, 0 skipped | `artifacts/goal/reference-stage-proof/windows/generated-reference-tests.log` |
| WSL Ubuntu x64 | 8.0.319 | 36 passed, 0 failed, 0 skipped | `artifacts/goal/reference-stage-proof/linux/generated-reference-tests.log` |

Fresh build-and-test logs and the Linux SDK record are also retained in the same directory. The **36** is the .NET test count, not the reference sample count: tests exercise **160** final trajectory cases and **3** intermediate traces. Existing assertions retain coordinate tolerance `1e-9` pixels, exact timings/RNG outcomes, and `1e-12` stage-selection weight tolerance; no tolerance was changed.

The intermediate-stage assertions compare selection/ranking, recording selection, transforms, sampled timing, interpolation and final results. Logs establish the current test suite passed on both named environments; they do not claim all CPU/runtime combinations or prove unrelated acceptance criteria.

## Reproduction commands

Run from the assigned workspace and pinned checkout; preserve the tracked fixture files:

```sh
cd /D/SpyBrowser
# The maintenance scripts normally target checked-in fixtures. For this proof,
# temporary copies redirected those output paths to the external temp directory.
cd /C/Users/Rodrigo/AppData/Local/Temp/spybrowser-cursory-js-review
CURSORY_JS_DIST=C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-cursory-js-review/dist/index.js REFERENCE_OUTPUT=C:/Users/Rodrigo/AppData/Local/Temp/reference-stage-proof-7be0292/parity.json node C:/Users/Rodrigo/AppData/Local/Temp/reference-stage-proof-7be0292/generate-js-fixtures.cjs.cjs
CURSORY_JS_CHECKOUT=C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-cursory-js-review REFERENCE_OUTPUT=C:/Users/Rodrigo/AppData/Local/Temp/reference-stage-proof-7be0292/stage-parity.json node C:/Users/Rodrigo/AppData/Local/Temp/reference-stage-proof-7be0292/generate-js-stage-fixtures.cjs.cjs
```

The temporary final-generator copy likewise set `REFERENCE_OUTPUT` to the external `parity.json`. This command transcript describes this run; for a clean recreation, make temporary copies by changing only the respective output-target declaration and use a new external temp directory. Verify the checkout HEAD, clean state and gzip hash before generation.

Build/test commands used each OS's separate artifacts output path:

```sh
cd /D/SpyBrowser
dotnet test tests/SpyBrowser.Cursory.Tests/SpyBrowser.Cursory.Tests.csproj -c Release --artifacts-path D:/SpyBrowser/artifacts/goal/reference-stage-proof/windows --logger 'console;verbosity=minimal'
dotnet test tests/SpyBrowser.Cursory.Tests/SpyBrowser.Cursory.Tests.csproj -c Release --artifacts-path D:/SpyBrowser/artifacts/goal/reference-stage-proof/windows --no-build --logger 'console;verbosity=minimal'
```

WSL used `PATH=/home/rodrigo/.dotnet-spybrowser:/usr/bin:/bin`, `cd /mnt/d/SpyBrowser`, the same test project, and `/mnt/d/SpyBrowser/artifacts/goal/reference-stage-proof/linux` as `--artifacts-path`. For both second runs, the build-output fixtures had first been copied from the independently generated files. Each command completed within the 240-second command bound.

Full hashes, environment details and source-coherency metadata: `artifacts/goal/reference-stage-proof/manifest.json`.
