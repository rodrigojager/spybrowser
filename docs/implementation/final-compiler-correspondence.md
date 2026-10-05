# Final compiler/source correspondence

**Decision: root cause identified and method-body/resource correspondence is proven for the packaged source when rebuilt with the pinned SDK 8 toolchain.** No feed/package/source archive was changed and no publication/rights conclusion is made.

## Root cause

The extracted package source lives outside the Git archive and has no `global.json`. Running `dotnet --version` in the actual extracted project directory (`D:/Temp/spybrowser-final-correspondence/source/SpyBrowser.Cursory`) resolves **10.0.401**, not the root workspace's 8.0.319. That unpinned toolchain reproducibly emits the rejected 62-byte `CreateRandomSeed` body; an isolated copy with `global.json` pinning SDK **8.0.319**, fresh `obj`/restore, exact 8.0.22 reference pack, and `/p:UseSharedCompilation=false` emits the package's exact 72-byte body.

The portable PDB compilation records and compiler identities substantiate the toolchain split:

| Build | SDK / Roslyn | PDB language / runtime version | Target reference pack | `CreateRandomSeed` |
|---|---|---|---|---|
| Package DLL | SDK provenance reconstructed by comparison; package itself has no PDB | — | — | 72 bytes, SHA-256 `c4c5fc4624d9c5848659c1b4d84be53b3d6f5a3066503272a1dcb7a7a041edb9` |
| Pinned replay | 8.0.319 / Roslyn 4.10.0-3.25064.8 | 12.0 / 8.0.22 | Microsoft.NETCore.App.Ref 8.0.22 | 72 bytes, exact IL match |
| Ambient extracted-cwd replay | 10.0.401 / Roslyn 5.9.0-1.26423.113 | 14.0 (`latest`) / 10.0.12 | Microsoft.NETCore.App.Ref 8.0.31 | 62 bytes, SHA-256 `be4adf4a639c2bf977e59bb7ce5ebc34eac0069c80f6d51ce0bf6425ea8d0a2a` |
| SDK 10 control | 10.0.401 / Roslyn 5.9.0-1.26423.113 | explicitly 12.0 / 10.0.12 | Microsoft.NETCore.App.Ref 8.0.31 | still 62 bytes |

Thus `LangVersion=latest` changes from 12.0 to 14.0 in the ambient build, but setting SDK 10 explicitly to 12.0 does **not** restore the 72-byte body. The demonstrated cause is the unpinned SDK/toolchain generation (compiler and associated reference-pack selection), not a change in `Pcg64.cs` or a claimed random-number fix. This experiment does not separately attribute the SDK 10 difference between Roslyn and its 8.0.31 reference pack; both are toolchain provenance absent from the extracted source. The PDBs record compiler/language/runtime versions; their hashes and decoded values are retained in the artifacts.

## Correspondence results

- Re-extracted package is `SpyBrowser.Cursory.0.2.0-beta.2.final.4d8a18c.nupkg`, SHA-256 `595df25eb646defe3c66fa1505c2f517345476e26ca46333f1f9a473d5b8363e`; packaged DLL SHA-256 remains `3a93b3535312098328c5e817e5dad9430c9fbd8dcfba3fa8377f82e5df69c978`.
- All 11 packaged C# source files compare to commit `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b` with content identical after CRLF/LF normalization. Per-file hashes/statuses are in `artifacts/goal/final-compiler-correspondence/source-byte-comparison.txt`.
- The enhanced reflection/IL comparator checks 23 types and 432 metadata/body/resource entries, including member/type signatures and attributes, constants, generic constraints, parameters, method IL, locals/max stack/init-locals, exception regions and embedded-resource SHA-256s. Pinned SDK 8 comparison: **0 differing entries**. SDK 10 comparison: **exactly 1 difference**, `CreateRandomSeed` (72 vs 62 bytes); the decoded bodies and hashes are retained.
- The DLL file hashes/MVIDs are not byte-identical. The claim established here is complete compared metadata/method-body/resource correspondence, not whole-file reproducibility. Informational version is unchanged; `SourceRevisionId` was not added.
- The source's `bytes[8..]` lowering discrepancy disappears under the pinned producer-era compiler. No behavior-only argument or exhaustive 16-byte input claim is needed to close it.

## Reproduction and scope

Fresh standalone source copies were built outside the repository in `D:/Temp/spybrowser-final-compiler-correspondence-sdk8/` and `...-sdk10/`. Both `dotnet pack` runs used the producer properties (Release, continuous integration, version, full repository commit/URL, MicrosoftPlaywrightVersion 1.61.0, symbols/snupkg); the package-only license/readme files were restored from the immutable package for the pack step. SDK 8 was isolated by its own `global.json` pin to 8.0.319; restore/build outputs were fresh. SDK 10 in its own unpinned-to-8 directory selected 10.0.401. Build/rebuild command output, diagnostic compiler invocations, decoded PDB compilation metadata, compiler/reference binary hashes (`toolchain-evidence.txt`), full comparisons, and source byte audit are under `artifacts/goal/final-compiler-correspondence/`.

The earlier rejected diagnosis remains preserved unchanged at `docs/implementation/final-source-rebuild-correspondence.md` and `artifacts/goal/final-source-rebuild-correspondence/`; its original comparison/logs were not overwritten. The old 37 parity tests were not rerun: product/test sources are unchanged, no assertions were relaxed, and this task only rebuilt and compared the package assembly. No product, feed, ledger, unrelated documentation, source archive, or package bytes were changed; no commit was created. This result establishes source/binary correspondence only and does not grant publication permission or verify upstream rights. SourceLink/PDB mapping remains a separate audit.
