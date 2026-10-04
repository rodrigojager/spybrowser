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
truncation behavior. The implementation is **not yet upstream-parity complete**:
its differential trajectory fixture test currently demonstrates a mismatch
(trajectory point count 3 vs 5 for the first case). In particular, exact NumPy
numeric compatibility and broad intermediate-stage vectors are not complete;
`Math.Sqrt(x*x+y*y)` and the tail `Math.Log(1-u)` are not the CPython/NumPy
compatibility implementations. No parity claim is made and acceptance of ticket
03 remains unmet.

Fixtures under `tests/SpyBrowser.Cursory.Tests/Fixtures/parity.json` are emitted
by the pinned cursory-js `generateTrajectory` using the same data package, not
reused Python-dataset fixtures. The upstream JS suite was run locally with
`npm ci && npm test` and passed 34 tests. To regenerate the compact local
fixture, build the pinned upstream checkout and call its `dist/cursory.js`
`generateTrajectory` for explicit seeded cases; update the source commit/hash
metadata whenever either pin changes. The checked-in .NET differential test is
expected to remain red until the known divergence is fixed; do not hide it or
loosen its 1e-9 pixel tolerance.

## Reproducible commands

```sh
dotnet test tests/SpyBrowser.Cursory.Tests
dotnet pack src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release
```

Package inspection should confirm `PackageLicenseExpression=LGPL-3.0-or-later`,
embedded gzip, NOTICE, full license text and source-correspondence information.
Before external distribution, a legal review must address the dataset's
independent terms/rights, and a release must provide complete corresponding
source for the exact DLL (including this port and build instructions).

This preview is deliberately not referenced by `SpyBrowser.Playwright`.
