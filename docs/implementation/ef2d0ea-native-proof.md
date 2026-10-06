# ef2d0ea final package evidence

## Identity and immutable bytes

The reviewed package is source commit `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`, version `0.2.0-beta.2.final.ef2d0ea`, produced at `D:/Temp/spybrowser-feed-final-ef2d0ea/feed-manifest.json` (manifest SHA-256 `689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`). Every available final package copy was hashed against the producer: all four `.nupkg` and four `.snupkg` in the producer and native demo; three consumer package copies in `artifacts/goal/final-feed-ef2d0ea`; and three in the historical candidate feed projection. All hashes match the frozen package hashes in [ef2d0ea-package-proof.json](ef2d0ea-package-proof.json). This report does not substitute older 4d evidence.

Producer package audit records four package IDs/version/full repository commit; portable PDB audit has four PDBs, 71 documents, 58 checked-in sources verified and 13 generated documents excluded. SourceLink's immutable manifest diagnostic key `/_*` is a display-field typo, not a real CDI mapping. Per `ef2d0ea-source-map-report-note.md`, the real canonical mapping is `/_/*` with the full-commit raw GitHub URL template; independent validation checked that mapping for all four PDBs and verified the 58 source checksums. Remote source access was not verified. Correspondence audit compared 432 entries over 23 types with zero differences; this is not whole-PE byte identity.

## NuGet-only consumer

`artifacts/goal/native-package-demo-ef2d0ea/consumer-run.log` records a successful package-only console run. The dependency list contains only `Cursory.PackageDemo` and `SpyBrowser.Cursory`; the consumer is BCL-only. The SDK check was run in the actual workspace CWD and returned `8.0.319`; consumer `global.json` pins 8.0.319 with `latestFeature` roll-forward, prerelease disabled. Runtime was .NET 8.0.22.

The consumer program asserts: equal points and timings for two calls with the same `(start,end,Seed=42,Frequency=60)`; exact requested endpoints; default-seed output has at least two points and exact endpoints; finite point coordinates and timings; equal point/timing counts; timing starts at zero, ends positive and is nondecreasing; NaN and ±infinity start coordinates throw `ArgumentOutOfRangeException`; eight independently seeded concurrent calls complete with correct endpoints; and SHA-256 of the embedded gzip dataset remains `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203` before and after. Observed output reports a repeatable 33-point trajectory and pass. No optional entropy test is claimed. There was no process-table sampling, so no general child-process-absence claim is made.

The existing TRX at `artifacts/goal/cursory-ef2d0ea-results/test-results/cursory-ef2d0ea.trx` reports 37 executed, 37 passed, zero failed/errors/skipped. Source tests constrain parity to 160 pinned upstream fixtures (point coordinates within 1e-9 and timings exact), plus three intermediate-stage fixtures (selection within 1e-12, other stages within 1e-9), pinned hashes/provenance and RNG vectors. Other relevant assertions cover read-only result collections, invalid inputs, seeded repeatability, endpoint/timing invariants and 1,000 concurrent generations while checking the shared dataset hash. This is not a claim of universal browser or whole-product parity.

## Native benchmark (descriptive)

Three already-retained separate process logs were reviewed; experiments were not rerun. Each process measured one cold first generation, performed 20 untimed warmups, then timed 500 samples. Percentiles use sorted nearest-rank `ceil(p*n)-1`; allocated bytes are process GC total-allocation delta divided by 500. Runtime: .NET 8.0.22, Windows 10.0.19045, x64, 16 logical processors.

| Process | Cold ms | Warm p50 ms | Warm p95 ms | Mean allocated bytes |
|---|---:|---:|---:|---:|
| 1 | 503.269 | 7.563 | 11.049 | 2,160,426 |
| 2 | 401.380 | 8.474 | 13.597 | 2,160,426 |
| 3 | 400.063 | 6.283 | 10.106 | 2,160,439 |
| Median | 401.380 | 7.563 | 11.049 | 2,160,426 |

The 10 ms p95 figure is an investigative budget, not a universal pass threshold; two of three observed p95 values exceed it. These measurements are descriptive.

## Historical comparison

Retained runs used baseline `e217359d19a29635f2b3b5ba54664d299fd16d36`, archive SHA-256 `91f7807b1c998d556479e8456d0c256f5c7cd6659ba96e56e148a666aa0ce52f`, and final candidate commit `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`. Four lanes each produced seven completed page observations (28 total). DOM move counts summed from the page records:

| Lane | Per-case/page DOM moves | Total |
|---|---|---:|
| baseline-bezier | 25, 25, 25, 25, 25, 25, 25 | 175 |
| candidate-bezier | 25, 25, 25, 25, 25, 25, 25 | 175 |
| candidate-cursory | 3, 20, 18, 11, 20, 17, 19 | 108 |
| candidate-off | 1, 1, 1, 1, 1, 1, 1 | 7 |

All pages report task, hover and click completed. The run logs specify identical fixture/options, but historical RNG is not seedable: there is no paired trajectory comparison and no causal performance regression claim. Counts are actual recorded DOM move events, not a pass/fail threshold.

API diff: 37 baseline exported types; 50 candidate; zero removals, 13 additions; zero removed members and 74 added members; no changed defaults. All original option defaults are unchanged. Candidate additions default to diagnostics disabled, capacity 256, Bezier algorithm, null random seed, Cursory frequency 60, randomizer 1, directness 0.65, movement deadline 2000 ms, Legacy compatibility, and action/typing deadlines 30000 ms. See `artifacts/goal/historical-ef2d0ea/api/api-diff-summary.json` for the complete definitions.

## Caveats

Dataset rights remain **UNVERIFIED**. External publication is disallowed; no defaults are promoted. No product source, feed, ledger, prior artifact or test was modified or rerun for this report.
