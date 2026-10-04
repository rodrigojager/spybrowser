# Cursory upstream-reference maintenance

This directory contains maintenance-only tools. The library runtime and ordinary
tests require only .NET; Node/TypeScript are not runtime dependencies.

## Pinned trajectory/RNG fixtures

Reference checkout: `https://github.com/JWriter20/cursory-js`, commit
`16fff97fab05bb6b0c6753b2dc136a7692634cec`, version `2.0.1`; dataset SHA-256
`1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`.
Use that exact checkout and its committed package lock:

```sh
npm ci
npm run build
CURSORY_JS_DIST=/absolute/path/to/cursory-js/dist/index.js node tools/cursory-reference/generate-js-fixtures.cjs
```

On PowerShell, set `$env:CURSORY_JS_DIST` to the absolute `dist/index.js` path
before invoking Node. Check the resulting source/dataset metadata and review all
fixture diffs. The deterministic generator writes 160 full trajectory cases and
PCG64/distribution vectors (128-bit endpoint seeds, ziggurat normals and bounded
integer rejection). A regenerated fixture must still pass the .NET differential
test at `1e-9` absolute pixel tolerance; do not change tolerance to force a pass.

The checked-in generator does not yet produce stage-level features or emit
Windows/Linux comparison reports. Do not infer those criteria are met from the
final-trajectory fixtures. Updating upstream requires a separately reviewed
pin/manifest/notice change and regenerated fixtures.
