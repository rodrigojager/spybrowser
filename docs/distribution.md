# Distribution and release

## Artifacts

The solution builds five package projects. `SpyBrowser.Cursory` is an experimental LGPL package with dataset distribution gated by either an independently verified rights review or a separately recorded operator attestation plus explicit operational approval. The operator-attested route is not independent legal clearance.

| Package | Purpose |
|---|---|
| `SpyBrowser.Core` | identity manifests, validation, storage, and leases |
| `SpyBrowser.Playwright` | launch modes, humanization, GPU diagnostics |
| `SpyBrowser.Compatibility.CloakBrowser` | migration assembly named `CloakBrowser.dll` |
| `SpyBrowser.Cli` | global .NET tool named `spybrowser` |
| `SpyBrowser.Cursory` | standalone native trajectory library; experimental, LGPL-3.0-or-later, external release gated |

All projects target .NET 8 and can be consumed by .NET 8 or newer compatible
applications. The four established release packages use MIT metadata; Cursory
has separate LGPL-3.0-or-later metadata and license/NOTICE. Dataset provenance
and redistribution approval remain a gate before publishing Cursory or a
Playwright release that requires its package. The source records operator
attestation for the exact pinned dataset; this is not independent rights review.
LGPL code, complete license texts, corresponding source and upstream
NOTICE/attribution obligations remain in force.

The library packages are small and depend on `Microsoft.Playwright` normally.
The global CLI tool package is currently about 205 MB because .NET tool packing
must carry the Playwright Node driver/runtime payload inside the tool. It does
not contain a Chrome/Edge browser binary.

## Local package build

```powershell
dotnet restore SpyBrowser.sln -p:MicrosoftPlaywrightVersion=1.61.0
dotnet build SpyBrowser.sln -c Release --no-restore -p:MicrosoftPlaywrightVersion=1.61.0

dotnet pack src/SpyBrowser.Core/SpyBrowser.Core.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Playwright/SpyBrowser.Playwright.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Compatibility.CloakBrowser/SpyBrowser.Compatibility.CloakBrowser.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SpyBrowser.Cli/SpyBrowser.Cli.csproj -c Release --no-build -o artifacts/packages
```

Applications can point a private feed or local `NuGet.config` at
`artifacts/packages`. The release workflow currently accepts only
`v0.2.0-beta.2`, selects SDK 8.0.319 and packs all five package projects as
`0.2.0-beta.2`. Publication still requires
`SPYBROWSER_EXTERNAL_PUBLICATION_APPROVED=true` and an authorized dataset-rights
route. A successful workflow attaches packages to a GitHub release and
publishes to NuGet when `NUGET_API_KEY` exists.

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

## Installed-distribution verification (tickets 23/24)

Use the already-produced final local feed; the verifier does not rebuild candidate packages from source. It creates its consumer and clean NuGet cache under a temporary directory outside the checkout, with package sources restricted to the supplied local feeds. The browser executable/Playwright browser cache is a normal prerequisite and is not confused with a Cursory sidecar.

```sh
python tools/verification/distribution-checks/run.py \
  --repository . \
  --feed artifacts/packages \
  --candidate-version 0.2.0-beta.1 \
  --feed-manifest artifacts/packages/distribution-manifest.json \
  --expected-source-commit <exact-source-commit> \
  --declared-final-commit <exact-final-commit> \
  --dependency-feed "$HOME/.nuget/packages" \
  --output artifacts/distribution-checks \
  --evidence-input artifacts/rpablockly/evidence.json \
  --evidence-input artifacts/benchmarks/report.json
```

The command builds the previous SpyBrowser.Core/Playwright packages from baseline `e217359d19a29635f2b3b5ba54664d299fd16d36` into a distinct previous-version feed. To consume a previously built baseline instead, pass `--previous-feed PATH --previous-version VERSION` (and omit `--repository`). The consumer installs via package references from local feeds, reuses one identity manifest and profile directory while switching candidate Cursory, candidate Bézier, candidate `Humanize=false`, and previous-package rollback; it checks storage-state cookie rehydration through the installed Playwright API, manifest checksum, storage-state checksum and one-click behavior. `Microsoft.Playwright` remains the official package/driver; the verifier does not assert that the driver has no Node process.

Set `SPYBROWSER_BROWSER_EXECUTABLE` to an installed Chrome/Chromium executable if it is not discoverable by Playwright, and provision the normal browser cache before running. The consumer CWD and app are temporary and do not need source-workspace access. `distribution-evidence.json` hashes package/nuspec, runtime DLL, source/license entries and symbol packages, records commands and evidence-input paths, and distinguishes PASS/PENDING/FAIL/BLOCKED. Exit status is nonzero for any pending or blocked item; `allPassed` is never true unless every criterion is PASS.

Snapshot tests reflect the supplied installed artifact. An absent API is PENDING and an incomplete API fails; the harness exercises comparison, concurrency, child-process interruption, cancellation, schema rejection, permission-denial limits and isolated discard without copying SDK implementation into the consumer. This local verification command never publishes. External rights can pass its publication gate through either independent VERIFIED evidence or the explicitly recorded operator-attested route; operator attestation is not independent legal clearance.

## Release checklist

1. Build and test the Playwright baseline and current stable version.
2. Run installed-Chrome integration tests on Windows and Linux.
3. For beta.2, pack all five release projects with SDK 8.0.319 and inspect package/version/source/symbol payload. Cursory uses either auditable independent VERIFIED evidence or the recorded OPERATOR_ATTESTED / APPROVED_BY_OPERATOR route; the latter is an operator assertion, not independent legal clearance.
4. Run the installed-distribution command above against the final feed, baseline rollback package, and shared profile; retain `distribution-evidence.json`.
5. Review LGPL/provenance and the exact dataset scope in `docs/implementation/external-publication-authorization.md`; the recorded operator attestation is not an independent review of a license document. Preserve LGPL code/license/NOTICE obligations.
6. Install the CLI from the produced local package and run `version`/`help`.
7. Rebuild a Cloak-migration sample against the compatibility package without adding it to the RpaBlockly package graph.
8. Associate RpaBlockly, browser-contract, parity and benchmark evidence by exact candidate artifact hashes.
9. Tag only after the version/changelog match the artifacts and external publication has separate operational approval.
