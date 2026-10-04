using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Backward-compatible name for an identity-backed persistent context handle.
/// </summary>
public sealed class SpyBrowserSession : SpyBrowserContextHandle
{
    internal SpyBrowserSession(
        IPlaywright playwright,
        IBrowserContext rawContext,
        BrowserIdentity identity,
        IdentityLease identityLease,
        BrowserSurfaceDiagnostics? diagnostics,
        ConsistencyReport consistency,
        ConsistencyExpectations effectiveExpectations,
        GpuPolicy effectiveGpuPolicy,
        PlaywrightHumanizer? humanizer,
        BrowserRuntimeProvenance runtimeProvenance)
        : base(
            playwright,
            rawContext,
            rawContext.Browser,
            identity,
            identityLease,
            closeBrowser: false,
            diagnostics,
            consistency,
            effectiveExpectations,
            effectiveGpuPolicy,
            humanizer,
            runtimeProvenance)
    {
    }
}
