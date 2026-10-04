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
- Intended code licensing/attribution: LGPL-3.0-or-later for the Cursory-derived library; NumPy subset BSD-3-Clause; PCG implementation MIT; CPython math behavior PSF-2. Existing notices are incomplete (base GPL text, separate upstream notices, and source-replacement/build instructions remain outstanding). Do not publish or apply the global MIT author/copyright metadata to this assembly.

## Implementation and validation status

**Algorithm parity work for ticket 03 is complete:** final and intermediate
fixtures, readable stage/RNG decomposition, and Windows/Linux Release test
results are committed and pass. External publication/redistribution remains
blocked separately on unresolved dataset provenance and incomplete licensing
artifacts; this is not represented as legal clearance.

The library provides candidate ranking and weighted selection, morph, knots,
jitter, timing resampling/interpolation, PCG64/SeedSequence seeding, integer
sampling and normal ziggurat tables. It preserves integer-path jitter-normal
truncation behavior. The .NET port currently matches 160 trajectory cases emitted by the pinned
cursory-js commit at `16fff97fab05bb6b0c6753b2dc136a7692634cec`, within the
existing absolute `1e-9` pixel tolerance; timings and point counts are exact.
The trajectory fixtures cover 160 varied fixed-seed cases, endpoints, frequency,
frequency randomizer and directness, using the pinned 2,356-record dataset. Exact
PCG64 raw words/doubles, ziggurat draws (with fixed-seed assertions that both
normal tails are reached), and bounded-integer vectors are checked; a separate
2,000,000,000-bound fixed-seed vector plus raw-word replay proves integer
rejection and post-rejection stream alignment. Tests
also cover 2,000 deterministic invariants, integer-path normal truncation,
numeric edge guards, and 1,000 concurrent generations while hashing every
shared dataset value before and after.

A separate three-seed, instrumented reference fixture records candidate ranking
order/weights/selection, chosen recording, pre/post-resampling jitter/knots/morph,
normalized and sampled timing, interpolation, and final transforms. The tool
copies the pinned source into a temporary development directory, injects
trace-only hooks there, builds and removes that copy; it never modifies the
upstream checkout. .NET tests compare the complete intermediate trace, with
`1e-9` coordinate tolerance (`1e-12` for weights) and exact selected recording,
timing and integer RNG outcomes. Candidate score ties have an explicit stable
ascending dataset-index order.

Numeric helpers include compensated CPython-compatible `hypot`, NumPy-style
pairwise sum for rescaling, and compensated `log1p` for ziggurat tails. Endpoint
displacement overflow and invalid/absurd options are rejected, and sampling is
guarded at 100,000 points. Generation is decomposed into internal selection,
geometry transforms, timing/resampling and RNG components; the public behavior
and all 160 final trajectory fixtures remain unchanged.

Fixture updates from the pinned JS checkout:

```sh
export CURSORY_JS_DIST=<pinned-checkout>/dist/index.js
node tools/cursory-reference/generate-js-fixtures.cjs
export CURSORY_JS_CHECKOUT=<pinned-checkout>
node tools/cursory-reference/generate-js-stage-fixtures.cjs
```

The reference checkout must be at the commit above. Both committed fixture sets
are consumed by ordinary .NET tests; Node/TypeScript remain maintenance-only.
Review source commit, dataset hash, manifest hashes and the complete fixture diff
before accepting regenerated data.

Cross-platform validation (Release; 15 tests): Windows 10.0.19045 x64 with
.NET SDK 8.0.319 passed; WSL2 Ubuntu 20.04.4 x64 with
`/home/rodrigo/.dotnet-spybrowser` SDK 8.0.319 passed against a fresh copy
excluding `.git`, `bin` and `obj`. Both
runs use the same fixtures and tolerances. These are local results, not a promise
of bit-identical floating point behavior on every CPU/runtime.

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

`SpyBrowser.Playwright` now references this preview through an experimental opt-in mouse adapter. The pure library remains Playwright-independent; upstream parity and dataset-rights review are still release gates.
