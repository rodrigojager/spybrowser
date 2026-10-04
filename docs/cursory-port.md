# Native Cursory preview: provenance and parity status

This branch adds `SpyBrowser.Cursory`, a pure .NET 8 assembly; it does not
change Playwright product files or activate Cursory in the browser product.
Runtime dependencies are BCL only. The DLL embeds the cursory-js gzip dataset,
validates its SHA-256 and limits compressed/expanded sizes before parsing, and
uses a thread-safe lazy immutable shared dataset. Generation state is local to a
call.

## Pinned sources and data

- Python Cursory: `5caa9d3477a1ff7b2d413978ea168a93fdc01dfc`.
- cursory-js algorithm/data: `16fff97fab05bb6b0c6753b2dc136a7692634cec`, version 2.0.1.
- Dataset SHA-256: `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`; 637,556 gzip bytes, 2,356 recordings, 112,984 points.
- Python reference dataset has 2,357 recordings; cursory-js removed recording ID `2281`. All checked-in JS reference cases declare the 2,356-dataset hash.
- The upstream cursory-js NOTICE attributes source recordings to SapiMouse (Antal, Fejer, Buza) and Mouse-Synthesizer (MIMIC-LOGICS) and says these are redistributed under the original project license. Independent chain-of-rights and data-specific redistribution terms remain unverified. **External redistribution is blocked**; this is a legal/provenance gate, not a claim of clearance.
- Code licensing/attribution: LGPL-3.0-or-later for the Cursory-derived library; NumPy subset BSD-3-Clause; PCG implementation MIT; CPython math behavior PSF-2. Full notices are in the package. Do not apply the global MIT author/copyright metadata to this assembly.

## Implementation and validation status

The library provides candidate ranking and weighted selection, morph, knots,
jitter, timing resampling/interpolation, PCG64/SeedSequence seeding, integer
sampling and normal ziggurat tables. It preserves integer-path jitter-normal
truncation behavior. The .NET port currently matches 160 trajectory cases emitted by the pinned
cursory-js commit at `16fff97fab05bb6b0c6753b2dc136a7692634cec`, within the
existing absolute `1e-9` pixel tolerance; timings and point counts are exact.
The trajectory fixtures cover varied fixed seeds, endpoints, frequency,
frequency randomizer and directness, and use the pinned 2,356-record dataset.
The fixture also contains exact PCG64 raw words, doubles, ziggurat normal draws
(including rejection paths), bounded-integer vectors (including rejection),
and seeds through 128-bit maximum. Tests cover 2,000 deterministic invariant
cases and concurrent generation. This is evidence for the exercised fixtures,
not a claim of exhaustive cross-platform parity: intermediate-stage fixtures
and a Windows/Linux result matrix remain outstanding.

Numeric helpers now include compensated CPython-compatible `hypot`, NumPy-style
pairwise sum for rescaling, and a compensated `log1p` path for ziggurat tails.
Endpoint displacement overflow and invalid/absurd options are rejected, and
sampling is guarded at 100,000 points. The generator and RNG still need a
readability-focused decomposition into cohesive internal helpers; no architecture
expansion is intended.

To regenerate the trajectory and RNG fixtures from the pinned JS checkout, run
`npm --prefix <pinned-checkout> run build`, then set
`CURSORY_JS_DIST=<pinned-checkout>/dist/index.js` and run
`node tools/cursory-reference/generate-js-fixtures.cjs`. The committed compact
fixture is consumed by .NET tests; Node remains a maintenance-only tool. Review
the dataset hash, source commit, and all diffs before accepting regenerated
fixtures. No tool currently emits intermediate-stage feature snapshots.

## Reproducible commands

```sh
dotnet test tests/SpyBrowser.Cursory.Tests -c Release
dotnet run --project tools/Cursory.Benchmark/Cursory.Benchmark.csproj -c Release
dotnet pack src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release
```

Initial local benchmark (Windows 10.0.19045, x64, .NET 8.0.22, 16 logical
processors; command above; 500 warmed samples): first generation/cold dataset
load 326.575 ms, warmed p50 8.549 ms, p95 11.787 ms, mean managed allocations
2,964,547 bytes/trajectory. This is a baseline only; p95 exceeds the proposed
10 ms investigation target and allocation volume warrants investigation. It is
not a cross-machine guarantee. Linux measurements have not been captured.

Package inspection should confirm `PackageLicenseExpression=LGPL-3.0-or-later`,
embedded gzip, NOTICE, full license text and source-correspondence information.
The repository currently lacks the full GPL-3.0 base text and complete separate
NumPy/PCG/PSF notices requested by the licensing plan; do not publish until
those source-verified notices and source replacement/build instructions are
included. Independent dataset terms/rights remain unverified. No independent
legal clearance is claimed; publication and external redistribution stay blocked
pending those artifacts and review.

This preview is deliberately not referenced by `SpyBrowser.Playwright`.
