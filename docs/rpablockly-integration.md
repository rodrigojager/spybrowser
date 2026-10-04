# RpaBlockly integration contract

The current RpaBlockly checkout is not modified by this repository. SpyBrowser
can be introduced as another browser selection when that integration is desired.

## Lowest-change migration from the current Cloak path

RpaBlockly currently imports `CloakBrowser`, calls
`CloakLauncher.LaunchAsync`, reads `CloakBrowserHandle.RawBrowser`, and pins
`BrowserVersion`. `SpyBrowser.Compatibility.CloakBrowser` deliberately emits an
assembly named `CloakBrowser.dll` with that source shape.

The smallest first integration is therefore:

```xml
<!-- remove -->
<PackageReference Include="CloakBrowser" Version="0.5.2" />

<!-- add -->
<PackageReference Include="SpyBrowser.Compatibility.CloakBrowser"
                  Version="0.2.0-beta.1" />
```

The existing `using CloakBrowser;` and launcher code can remain. The old
`BrowserVersion` value is accepted but no longer selects/downloads a vendor
binary; Chrome, Edge, or an explicitly supplied executable is used.

## Native SpyBrowser selection

For the full identity model, add a value named `spybrowser` while retaining all
existing browser values. Suggested runtime fields:

```csharp
string? SpyBrowserIdentityId;
string? SpyBrowserHome;
bool SpyBrowserHumanize;
GpuPolicy? SpyBrowserGpuPolicyOverride;
```

The launcher can return an existing context as well as a browser:

```csharp
public IBrowser? Browser { get; }
public IBrowserContext? ExistingContext { get; }
```

The runner then uses the persistent identity context when present:

```csharp
var browserContext = session.ExistingContext
    ?? await session.Browser!.NewContextAsync(existingOptions);
```

`RpaContext.Page` remains `Microsoft.Playwright.IPage`; existing blocks and new
application-specific blocks keep ordinary Playwright code. Set `Humanize = true`
at launch to pace supported calls transparently, or unwrap a specific object for
raw behavior. `Humanize = false` still applies identity defaults on factory-made
contexts/pages while exposing raw Playwright objects, matching the existing
RpaBlockly expectation. `RawBrowser` bypasses those configured factories; prefer
`SpyBrowserBrowserHandle.Browser` (or the handle's own factory methods) for
normal creation. The native `IBrowser` and handle creation paths share one
factory and one wrapper cache, so contexts/pages observed via lists and supported
events preserve reference identity.

## Process model

Many identities may run concurrently. A single persistent identity/profile is
exclusive to one worker until disposal. A stock and custom Playwright driver
must also live in separate worker processes because driver selection is
process-wide. Docker is optional; ordinary processes, services, VMs, and
containers all use the same API.

## Recommended regression gate

1. Keep all old browser selections unchanged.
2. Run one representative flow through stock Playwright and SpyBrowser.
3. Assert the same page/locator/block outputs.
4. Run two distinct identities concurrently.
5. Assert a second launch of the same profile fails clearly.
6. Exercise both Windows and Linux workers.
