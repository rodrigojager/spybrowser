# SpyBrowser

SpyBrowser is a distributable .NET browser runtime for authorized RPA, testing,
and automation. It keeps automation code on the official
`Microsoft.Playwright` interfaces while adding stable identities, persistent
profiles, cross-process isolation, GPU consistency diagnostics, and optional
human-paced input.

The current `0.2.0-beta.1` runtime uses installed Chrome/Edge, Playwright
Chromium, or a caller-provided Chromium executable. A native source-patched
Chromium is deliberately a separate engine concern; the public API already has
an `IBrowserExecutableProvider` seam for it, but this repository does not claim
that stock Chrome contains proprietary or equivalent C++ patches.

## What is ready

- Official `IBrowser`, `IBrowserContext`, `IPage`, `IFrame`, `ILocator`,
  `IElementHandle`, `IMouse`, and `IKeyboard` interfaces.
- Transitive transparent humanization for standard click, double-click, hover,
  fill/type, press, clear, mouse, keyboard, and wheel calls.
- Raw Playwright escape hatches for exact unmodified behavior.
- Persistent, disposable-context, and browser-only launch modes.
- One persistent profile per identity, with an operating-system lease preventing
  two processes from corrupting the same profile.
- Round-robin `IdentityRotationPool` allocation that skips profiles already in use.
- Unlimited concurrent identities, subject only to machine resources.
- Locale, timezone, viewport, screen, proxy, WebRTC policy, browser selection,
  and optional custom executable per identity.
- WebGL1/WebGL2/WebGPU/canvas diagnostics and configurable GPU consistency gates.
- Windows and Linux support from the same `net8.0` packages.
- A source-compatible migration package for the CloakBrowser launch API used by
  RpaBlockly.
- CLI, NuGet metadata, symbols, license notices, CI compatibility matrix, and
  tag-driven release workflow.

## Install

Reference the native SpyBrowser API:

```xml
<PackageReference Include="SpyBrowser.Playwright" Version="0.2.0-beta.1" />
```

For existing code that starts with `using CloakBrowser;`, replace the original
package reference with:

```xml
<PackageReference Include="SpyBrowser.Compatibility.CloakBrowser" Version="0.2.0-beta.1" />
```

Do not reference both packages that provide `CloakBrowser.dll` in the same
application. The compatibility package is a replacement assembly, not an add-on
to the vendor package.

## Playwright-first quick start

```csharp
using Microsoft.Playwright;
using SpyBrowser.Playwright;

await using var session = await SpyBrowserLauncher.LaunchPersistentContextAsync(new()
{
    IdentityId = "cliente-a",
    Headless = false,
    Humanize = true
});

IPage page = session.Pages.FirstOrDefault() ?? await session.NewPageAsync();
await page.GotoAsync("https://example.com");
await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();

IPage rawPage = PlaywrightHumanizer.Unwrap(page);
```

The RPA code continues to compile against Playwright. Non-interaction methods
delegate verbatim. Interaction calls with advanced non-default Playwright
options also fall back to the raw implementation so their exact semantics are
preserved.

## Launch modes

| Method | Ownership | Typical use |
|---|---|---|
| `LaunchPersistentContextAsync` | profile + context | stable returning identity, cookies/history/storage across runs |
| `LaunchContextAsync` | browser + disposable context | isolated job with identity settings but no persisted browser profile |
| `LaunchBrowserAsync` | browser | code that must create several contexts/pages itself |
| `LaunchAsync` | alias of persistent mode | backward-compatible SpyBrowser entry point |

Different identities may run concurrently in one process, different processes,
VMs, or containers. The restriction is per profile directory: one writer at a
time. Containers are an operational isolation choice, not a requirement for
multiple browser instances.

Rotate across a fixed pool of stable identities without creating a new
fingerprint on every launch:

```csharp
var pool = new IdentityRotationPool(["cliente-a", "cliente-b", "cliente-c"]);
await using var session = await pool.LaunchNextAsync(id => new()
{
    IdentityId = id,
    Headless = true,
    Humanize = true
});
```

The pool skips leased profiles and throws `IdentityPoolExhaustedException` only
when every configured identity is active. This works across processes because
availability is ultimately decided by each profile's operating-system lease.

## CloakBrowser migration facade

The following existing RpaBlockly-style code compiles after changing only the
package reference:

```csharp
using CloakBrowser;

await using var browser = await CloakLauncher.LaunchAsync(new LaunchOptions
{
    Headless = true,
    Locale = "pt-BR",
    BrowserVersion = "146.0.7680.177.5",
    Humanize = true
});

Microsoft.Playwright.IBrowser raw = browser.RawBrowser;
var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");
```

The facade covers the launch/handle contract, common launch/context options,
proxy shapes, persistent contexts, transparent humanization, and the common
`CloakBrowser.Human.HumanPage` operations. `BrowserVersion`, `ReleaseChannel`,
and `LicenseKey` are accepted for source compatibility but never download or
unlock CloakBrowser binaries. `GeoIp = true` fails explicitly; set a coherent
proxy, locale, and timezone instead of silently calling an external service.

This is source compatibility for migration, not a promise of binary or
behavioral parity with every public/internal type ever shipped by CloakBrowser.
See [compatibility details](docs/compatibility.md).

## Identity provisioning and CLI

```powershell
dotnet tool install --global SpyBrowser.Cli --version 0.2.0-beta.1

spybrowser identity create cliente-a `
  --name "Cliente A" `
  --locale pt-BR `
  --timezone America/Sao_Paulo `
  --gpu require-hardware

spybrowser doctor
spybrowser probe cliente-a --headless --humanize
spybrowser open cliente-a --url https://example.com --humanize
spybrowser version
```

Use `SPYBROWSER_HOME` or `--root` to change the identity root. Proxy credentials
are referenced through environment-variable names and are not serialized into
identity manifests.

## Build and validate

```powershell
cd C:\Users\Rodrigo\Desktop\SpyBrowser
dotnet restore SpyBrowser.sln -p:MicrosoftPlaywrightVersion=1.61.0
dotnet build SpyBrowser.sln -c Release --no-restore -p:MicrosoftPlaywrightVersion=1.61.0
dotnet test SpyBrowser.sln -c Release --no-build -p:MicrosoftPlaywrightVersion=1.61.0
```

Run the real installed-Chrome tests:

```powershell
$env:SPYBROWSER_RUN_BROWSER_TESTS = '1'
dotnet test tests\SpyBrowser.Tests\SpyBrowser.Tests.csproj -c Release `
  --filter FullyQualifiedName~BrowserIntegrationTests
Remove-Item Env:SPYBROWSER_RUN_BROWSER_TESTS
```

The package baseline is Playwright 1.61.0. CI builds and exercises real Chrome
on Windows and Linux against both that baseline and the latest stable
`Microsoft.Playwright` version resolved at run time.

## GPU and fingerprint limits

The default is to prefer coherent native values. The optional experimental
WebGL mask changes reported strings only; it cannot change rendered pixels,
timing, WebGPU, TLS, or native-function introspection. It is therefore disabled
unless explicitly configured. Snapshot comparisons retain normalized renderer
categories, not raw GPU adapter, renderer, or device strings.

Xvfb provides a display, not a GPU. Hardware rendering in Linux/container
deployments needs a real render device (commonly `/dev/dri`) or NVIDIA GPU
runtime exposure. A stable profile plus real Chrome and real hardware is more
coherent than claiming a GPU that the renderer cannot reproduce.

No browser can guarantee zero CAPTCHA challenges. IP reputation, account
history, request volume, site policy, and server-side risk models remain outside
the browser API. SpyBrowser does not include CAPTCHA solvers or access-control
bypass logic.

## Repository

```text
src/SpyBrowser.Core                        identity, storage, validation, leases
src/SpyBrowser.Playwright                  launcher, decorators, diagnostics
src/SpyBrowser.Compatibility.CloakBrowser  migration facade (CloakBrowser.dll)
src/SpyBrowser.Cli                         global .NET tool
tests/SpyBrowser.Tests                     unit and real-browser tests
docs/                                      architecture, compatibility, distribution
```

See [architecture](docs/architecture.md),
[distribution](docs/distribution.md), and
[the RpaBlockly integration contract](docs/rpablockly-integration.md).

## License and responsible use

SpyBrowser's integration code is MIT licensed. The separately linked
`SpyBrowser.Cursory` DLL is LGPL-3.0-or-later and retains its upstream notices
and corresponding source; see [Cursory packaging](docs/cursory-packaging.md).
Chrome/Edge and Playwright retain their own licenses and are not relicensed by
SpyBrowser; see `THIRD_PARTY_NOTICES.md`.
Redistribution rights for the bundled Cursory trajectory dataset and its
upstream-derived materials are **unverified**; repository and package metadata
are not legal clearance. Until an independent rights review explicitly clears
distribution, do not publish or push SpyBrowser packages containing that data,
and do not treat a successful build or installed-package test as permission to
redistribute it. The external-distribution gate must fail closed.

Use SpyBrowser only for systems and data you are authorized to automate. Site
terms, rate limits, access controls, and applicable laws still apply.
