# Cursory package build, demo, and corresponding source

`SpyBrowser.Cursory` is a standalone .NET 8 preview, licensed separately as
LGPL-3.0-or-later. Its DLL is not merged into SpyBrowser's MIT assemblies. The pure library is
independent of browser automation; `SpyBrowser.Playwright` consumes it through
an explicit, opt-in trajectory adapter. Package metadata intentionally sets
its own authors, copyright, license and acceptance flag rather than inheriting
the repository-wide MIT author/copyright values.

## Reproduce and verify a local package

From the repository root, using the .NET 8 SDK and local source tree:

```powershell
$ErrorActionPreference = 'Stop'
$feed = (Resolve-Path 'artifacts/packages').Path
New-Item -ItemType Directory -Force $feed | Out-Null
dotnet restore src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj
dotnet build src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release --no-restore
dotnet pack src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release --no-build -o $feed
```

The `.nupkg` contains the library DLL, portable PDB, gzip resource, project
source, matching dataset, project file, provenance manifest, this guide, LGPL
and GPL texts, and third-party notices/license texts. The `.snupkg` contains
symbols/source-link inputs where supported. A package-build manifest must bind
the package and symbol package SHA-256 hashes to the exact source commit; the
verification script writes it after the packaging tree is finalized. Artifacts
are not interchangeable when either hash or commit differs.

## Pure NuGet consumer and no-sidecar check

`tools/Cursory.PackageDemo` references `SpyBrowser.Cursory` only by package ID
and version; it must not be changed to a `ProjectReference`. The test runner
uses a NuGet config containing only the fresh local feed, so it cannot restore
a second copy from nuget.org. It executes generation, endpoint/finite/time
invariants, same-seed repeatability, parallel calls, invalid-input rejection,
embedded-resource SHA-256 and dependency-list checks. No Python, Node, browser,
network access, or data generation is used by the consumer executable.

On Windows run `tools/package-demo/verify.ps1`. It builds from the local feed,
then starts the resulting consumer under a PATH restricted to the .NET host
folder. While the demo runs, it enumerates its process descendants and fails if
the consumer starts a sidecar. The harness itself is an allowed .NET process;
the child-process proof is specifically scoped to the consumer PID. Save its
captured output with the package/hash manifest as validation evidence.

## Corresponding source, modifying, and replacing the library

The matching `.nupkg` includes `source/SpyBrowser.Cursory/` with all C#
implementation files, `Data/trajectories.json.gz`, the upstream manifest and
project file. The package's top-level `LICENSE/` folder carries the complete
LGPL/GPL and third-party license texts. Extract those files and build from the
source project using .NET 8:

```powershell
dotnet build source/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release
```

To modify the library, edit the extracted C# sources and rebuild; to replace it
in an application, install the rebuilt compatible `SpyBrowser.Cursory.dll`
next to the consuming application (or rebuild/repack the package and update the
package reference). Keep the DLL separate; do not merge it into another
assembly. The sample consumer's project shows relinking via an ordinary NuGet
reference. The package does not promise support for single-file, trimming,
AOT, or other bundling modes. This is a practical corresponding-source and
relinking path, not legal advice about every distribution arrangement.

The algorithm source and recorded dataset are provided as matching components;
changing the gzip data requires changing the manifest and its hash/counts and
revalidating. The Python/Cursory reference differs by one recording: the
selected cursory-js snapshot has 2,356 records and omits recording id 2281 from
the 2,357-record Python snapshot. No human data is collected or added here.

## External distribution gate

The source algorithm notices are preserved, but independent rights and terms
for the SapiMouse / MouseSynthesizer recordings remain unverified. External
publication or redistribution of this package (especially the data) is blocked
until that provenance review is resolved. Do not infer clearance from the
upstream project's statement. This package work does not claim legal approval.
