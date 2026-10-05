# Final source/binary correspondence diagnosis

**Decision: correspondence remains unproven; reject any claim that the packaged assembly is a verified rebuild of the shipped source.** The package and exact declared Git archive remain untouched. This investigation makes no publication or rights claim.

## Inputs and preserved baseline

- Package: `D:/Temp/spybrowser-feed-final-4d8a18c/SpyBrowser.Cursory.0.2.0-beta.2.final.4d8a18c.nupkg`, SHA-256 `595df25eb646defe3c66fa1505c2f517345476e26ca46333f1f9a473d5b8363e`.
- Packaged runtime DLL SHA-256: `3a93b3535312098328c5e817e5dad9430c9fbd8dcfba3fa8377f82e5df69c978`.
- Declared source: commit `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`; archive SHA-256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`; SDK `8.0.319`.
- Preserved earlier source-build result: DLL SHA-256 `7aaa9e8bfb5dba1303cbec6d30a0286b6e9e1c1ea23910f93cfa0976c967cff9` (not a match). These exact baseline values are retained here and in `docs/implementation/final-package-inspection.*`; neither was rewritten.

## Reproduction and results

The package's actual `source/SpyBrowser.Cursory/` tree (including the C# files and project) was extracted to `D:/Temp/spybrowser-final-correspondence/`. A Release `dotnet pack` was attempted with the official builder's relevant properties: `ContinuousIntegrationBuild=true`, `Version=0.2.0-beta.2.final.4d8a18c`, `RepositoryCommit=4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`, the official RepositoryUrl and MicrosoftPlaywrightVersion, symbols enabled and `snupkg`. Compilation succeeded, but packing stopped because the package's source-only folder intentionally omits the project-relative `LICENSE` file. The failed pack and successful build logs are retained under `artifacts/goal/final-source-rebuild-correspondence/`; no output was written into the producer feed.

An independent metadata/IL/resource comparison was then run against the packaged DLL and the preserved prior 7aaa rebuild. The checker reports 23 types and 249 total inventory entries on both sides. Type/member/field/property/event inventories and embedded-resource hashes match, but **one method's IL does not**:

`SpyBrowser.Cursory.Internal.Pcg64.CreateRandomSeed` — packaged IL SHA-256 `c4c5fc4624d9c5848659c1b4d84be53b3d6f5a3066503272a1dcb7a7a041edb9`, 72 bytes; rebuild IL SHA-256 `be4adf4a639c2bf977e59bb7ce5ebc34eac0069c80f6d51ce0bf6425ea8d0a2a`, 62 bytes. The complete raw IL byte sequences and assembly identities/MVIDs are in `artifacts/goal/final-source-rebuild-correspondence/il-comparison.txt`.

The packaged source's `Pcg64.cs` and the declared commit's `Pcg64.cs` contain the same method text (stackalloc 16 bytes, `RandomNumberGenerator.Fill`, two `BitConverter.ToUInt64` reads, and `UInt128` construction). This does **not** explain the emitted-IL discrepancy or establish behavioral equivalence. Therefore this is not a metadata-only difference: the compared assemblies have a real method-body difference and exact bytecode correspondence is rejected. The source-build PASS and matching resources do not close that gap.

A separate isolated build using the explicit `SourceRevisionId` also produced a different DLL (`9fae4b859fbf4f4c1f03728edf2ef12956f44595691089024c430df2a0fb9fb9`) and generated informational version `0.2.0-beta.2.final.4d8a18c+4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`; the packaged assembly's informational version is `0.2.0-beta.2.final.4d8a18c`. This confirms that setting SourceRevisionId changes generated assembly metadata, but does not resolve the method-body mismatch. That isolated build was not treated as a match.

## Artifacts and scope

`artifacts/goal/final-source-rebuild-correspondence/` contains the IL checker source, full comparison output, successful build log and failed pack log. `D:/Temp/spybrowser-final-correspondence/` contains extracted package source and temporary binaries/logs. No product, package, harness, ledger, or other documentation files were changed; no commit was created. The feed bytes and source archive were only read. **Precise disposition: source text is present and structurally tracks the assembly, but the build-to-binary gap is unresolved due to a concrete `CreateRandomSeed` IL mismatch. Do not mark #52 as correspondence proven; refer this discrepancy to the parent for a source-cut/feed decision.**
