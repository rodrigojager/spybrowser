# Distribution and release

## Artifacts

The solution produces four independent packages:

| Package | Purpose |
|---|---|
| `SpyBrowser.Core` | identity manifests, validation, storage, and leases |
| `SpyBrowser.Playwright` | launch modes, humanization, GPU diagnostics |
| `SpyBrowser.Compatibility.CloakBrowser` | migration assembly named `CloakBrowser.dll` |
| `SpyBrowser.Cli` | global .NET tool named `spybrowser` |

All packages target .NET 8 and can be consumed by .NET 8 or newer compatible
applications. NuGet packages include README, MIT metadata, third-party notices,
XML documentation, symbols, deterministic builds, and repository metadata.

The library packages are small and depend on `Microsoft.Playwright` normally.
The global CLI tool package is currently about 205 MB because .NET tool packing
must carry the Playwright Node driver/runtime payload inside the tool. It does
not contain a Chrome/Edge browser binary.

## Local package build

```powershell
dotnet restore SpyBrowser.sln -p:MicrosoftPlaywrightVersion=1.61.0
dotnet build SpyBrowser.sln -c Release --no-restore -p:MicrosoftPlaywrightVersion=1.61.0

dotnet pack src/SpyBrowser.Core/SpyBrowser.Core.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Playwright/SpyBrowser.Playwright.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Compatibility.CloakBrowser/SpyBrowser.Compatibility.CloakBrowser.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Cli/SpyBrowser.Cli.csproj -c Release --no-build -o artifacts/packages
```

Applications can point a private feed or local `NuGet.config` at
`artifacts/packages`. Tags matching `v*` run the release workflow, attach all
packages to a GitHub release, and publish to NuGet when `NUGET_API_KEY` exists.

## Browser delivery

The SDK does not redistribute Google Chrome or Microsoft Edge. Deployments may:

1. install the stable Chrome/Edge channel with the operating system;
2. install Playwright Chromium using Playwright's documented installer;
3. provide an executable path directly; or
4. implement `IBrowserExecutableProvider` for an independently licensed,
   downloaded, verified, and updated browser engine.

An executable provider is an integration seam, not proof that a binary is
stealth-patched. A future native fork needs its own source repository, patch
queue, reproducible Windows/Linux builders, artifact signing, update manifest,
security response, and browser-license review.

## Scaling

SpyBrowser imposes no license key or global instance counter. Capacity is bound
by CPU, memory, file descriptors, and browser resources. Each persistent profile
has one exclusive writer; provision one profile per concurrent identity. Use
separate worker processes when selecting different Playwright drivers. Docker is
useful for Linux deployment/isolation but is not required for concurrency.

`IdentityRotationPool` allocates the next available stable profile and reports
pool exhaustion explicitly. It has no server or license dependency; profile
leases remain the cross-process source of truth.

## Release checklist

1. Build and test the Playwright baseline and current stable version.
2. Run installed-Chrome integration tests on Windows and Linux.
3. Pack all four artifacts and inspect dependencies/content.
4. Install the CLI from the produced local package and run `version`/`help`.
5. Rebuild a Cloak-migration sample against the compatibility package.
6. Confirm license and third-party notices.
7. Tag only after the version/changelog match the artifacts.
