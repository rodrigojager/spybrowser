# Final pure-package BCL and historical proof — 4d8a18c

**Result: PASS for the scoped pure-package and historical DOM/API slices.** This report binds fresh consumers to the actual immutable local final feed; it does not edit or repack producer artifacts. Machine-readable hashes, sample output, API snapshots, projections, and detailed checks are in [final-native-package-proof.json](final-native-package-proof.json).

## Exact package provenance and BCL execution

The producer manifest declares `isFinal=true`; `sourceCommit` and `declaredFinalCommit` both equal `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`. The declared source archive SHA-256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4` was recomputed. All nupkg and snupkg actual SHA-256 values match producer declarations. Cursory package SHA-256 is `595df25eb646defe3c66fa1505c2f517345476e26ca46333f1f9a473d5b8363e`; its assembly hash is `3a93b3535312098328c5e817e5dad9430c9fbd8dcfba3fa8377f82e5df69c978`; embedded dataset SHA-256 is `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`.

A clean isolated `net8.0` package-only consumer restored solely from the local producer feed using SDK 8.0.319; runtime PATH was exactly `C:\Program Files\dotnet`, with no Python or Node. The actual Cursory nuspec has no dependencies; the package has only its Cursory runtime DLL; the consumer deps graph contains only the demo and Cursory package. Seeded points and timings repeated exactly; requested endpoints, finite outputs, zero-based monotonic timing and positive elapsed duration passed (534 ms observed); eight concurrent generations passed; invalid input was rejected; packaged dataset hash was unchanged before/after.

No process sampling was done: there is no general no-child-process claim. The exact packaged Cursory source was checked and contains zero `Process.Start`, `ProcessStartInfo`, or `System.Diagnostics.Process` references. Its source payload preserves stackalloc in `NumericCompat`/`Pcg64` and bounded top-5 selection; exact file hashes are in the JSON.

## Benchmark — fresh process per row

Protocol: one cold generation, 20 warmups, 500 timed samples per process. No timing threshold was used.

| Run | Cold ms | p50 ms | p95 ms | Mean allocated bytes/generation |
|---|---:|---:|---:|---:|
| 1 | 337.114 | 6.250 | 10.227 | 2,160,426 |
| 2 | 329.971 | 6.053 | 8.942 | 2,160,426 |
| 3 | 345.798 | 8.516 | 13.200 | 2,160,426 |
| **Median** | **337.114** | **6.250** | **10.227** | **2,160,426** |

The 10 ms p95 figure is an investigation budget, not a gate or guarantee. These are machine-local descriptive observations and may be affected by concurrent browser/CPU work.

## Fresh historical association

The historical runner restored and used byte-exact projections of final candidate packages and the pinned `e217359` baseline packages; projections carry separate manifests and leave producer feeds untouched. Actual DOM observations passed **28/28**: 7 each for baseline Bézier, candidate Bézier, candidate Cursory, and candidate off. All were chronological and within 1 CSS px of requested endpoints across short/subpixel, long, hover/click, slow transport, and independent-page cases. The old baseline RNG is unseedable; this is not paired-trajectory evidence. DOM timings are descriptive.

Fresh reflection compared all 37 historical exported types against the installed candidate: zero removed members or shape changes; 13 additive types and 74 additive public members; historical `HumanInteractionOptions` defaults retained. Raw snapshots and their hashes are preserved.

## Existing final-source runtime evidence (separate scope)

These are already-retained runs bound to the same final source commit, not reruns by the package demo. Windows required Playwright 1.61 passed 177 product tests plus 37 Cursory tests. Linux required161, reviewed163, and Chrome161 each passed 177 product plus 37 Cursory tests: 642/642 combined, zero skips. The 37-test Cursory run includes exact pinned upstream/intermediate fixtures, RNG vectors, and bounded top-5/full-sort tie parity. Windows reviewed163 is **not** all-green: it records 176/177 with one `ProbeIsolationBrowserTests...` timeout; this remains explicitly visible in the evidence. Full artifacts: `artifacts/goal/windows-final-4d8a18c/` and `artifacts/goal/linux-final-4d8a18c/run-status.json`.

No rights clearance, external publication permission, release sign-off, or default promotion is claimed.