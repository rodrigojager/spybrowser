# Corresponding Cursory source packaging fix

**Result:** the source payload now has its own self-contained build/repack path,
and a clean package built from the pinned compiler reproduces every compared
runtime method body, type/member entry and resource. This changes package
metadata/source assets only: no Cursory `.cs` file, compile include, trajectory
algorithm, RNG, numeric assertion, runtime default or dataset was edited. The
existing `4d8a18c` feed was not modified; outputs here are a local candidate,
not a release or publication.

## Fix

`src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj` pins C# 12, SDK output
version defaults (`0.2.0-beta.2` unless the build forwards the package version),
.NET runtime framework `8.0.22`, and deterministic source path mapping to
`/_/src/SpyBrowser.Cursory`. It conditionally adds SourceLink GitHub 8.0.0 as a
private build dependency in CI and embeds repository metadata/informational
revision without hard-coding this commit. Packaging now carries the full
project-relative LGPL/GPL texts, third-party licenses, NOTICE and README next
to the extracted source project, as well as a strict SDK 8.0.319 `global.json`
and build/modify guide. The guide reference is conditional: repacking the
extracted tree resolves its included guide rather than reaching outside the
package/repository.

## Reproducibility validation

Built the candidate from this checkout with `dotnet pack` under SDK 8.0.319,
.NET runtime 8.0.22, and the full source revision forwarded as both
`RepositoryCommit` and `SourceRevisionId`. The package contains 41 entries,
including all 25 `source/` entries and all seven LGPL/GPL/third-party license
files. The NuGet dependency group has no runtime dependencies. The packaged
recording resource SHA-256 is the unchanged
`1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`.

Extracted only `source/` to `D:/Temp/cursory-source-standalone-fix/`, outside
the repository, changed cwd into `source/` (so its strict SDK pin applies),
then successfully built and repacked the project. The build had zero errors;
when SourceLink was enabled in that extracted tree it emitted the expected
warnings that Git metadata/mapping cannot be recovered from an archive. This
is why the release PDB mapping is produced by the exact source Git checkout,
not fabricated by the extracted standalone rebuild.

The actual candidate `.snupkg` PDB has one SourceLink CDI whose JSON maps
`/_/*` to the versioned raw URL for full revision
`4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`. All 14 PDB source documents that
exist as source files in the package match their PDB SHA-256 checksums (14/14).
The source-link audit helper is `artifacts/goal/corresponding-source-packaging-fix/Program.cs`.

Using the existing BCL IL comparer, compared the candidate's packaged DLL to a
fresh Release build made from only its extracted corresponding source with SDK
8.0.319, C# 12, exact forwarded package version, repository URL and full
revision. **23/23 types, 249/249 inventory entries; zero differing methods,
members or resources.** `CreateRandomSeed` now matches byte-for-byte; the prior
72-vs-62-byte difference is not addressed by any RNG change. The assembly MVID
is expected to differ between independent compilations. The semantic compile
projection also stays unchanged: 11 SDK-default C# source files, no new
`Compile` include/remove, and the same embedded resource declaration. The
Cursory test project passed all 37 tests (0 failed, 0 skipped).

## Evidence and limitations

- Candidate `.nupkg` SHA-256:
  `e1e06b5e5469e21eb7798e70290bb9b407a201aab9a869e246bbf003f1d976ab`
- Candidate `.snupkg` SHA-256:
  `21c80aedb7187a298cc79a29802154bef6c2350261dec8dbc7b4393851b91dd4`
- Packaged DLL SHA-256:
  `3c1e6548fc0cb6f6bce8f0e86e66ba0dd0dd4897b6b69da87a3ea1c3af1f71c5`
- Full structured result and validation inputs:
  [`corresponding-source-packaging-fix.json`](corresponding-source-packaging-fix.json)
- IL comparison tool is reused from
  `artifacts/goal/final-source-rebuild-correspondence/IlCompare.csproj`;
  outputs were checked against the actual candidate and standalone rebuild.

The source/license accessibility defect and compiler-selection ambiguity are
fixed for the package payload/rebuild workflow, and bytecode correspondence was
re-established for this local candidate. This is not an external publication
approval. Rights/terms for the SapiMouse / MouseSynthesizer recordings, and
authorship/right status for Vinyzu, Jake Writer and SpyBrowser, remain
UNVERIFIED. The previous feed stays untouched; no release-feed mutation or
commit was made. The separate candidate-feed tool was not edited as part of
this task; concurrent peer changes to that file were observed in the shared
working tree.
