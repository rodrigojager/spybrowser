using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class MissingWebGLBrowserTests
{
    [BrowserFact]
    public async Task Disabled_WebGL_is_live_unverified_warning_not_hardware_failure_and_leaves_user_pages_untouched()
    {
        using var temporary = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("missing-webgl-live") with
        {
            Gpu = new GpuIdentitySettings { Policy = GpuPolicy.RequireHardware }
        };
        await using var handle = await SpyBrowserLauncher.LaunchContextAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = temporary.Path,
            Headless = true,
            RunGpuProbe = true,
            // Chromium flags cause the real browser to expose neither WebGL context.
            ExtraArguments = ["--disable-webgl", "--disable-webgl2"]
        });

        Assert.NotNull(handle.Diagnostics);
        Assert.False(handle.Diagnostics!.WebGl1.Available);
        Assert.False(handle.Diagnostics.WebGl2.Available);
        Assert.Contains(handle.Consistency.Findings, finding =>
            finding.Code == "gpu.webgl-unavailable" && finding.Severity == ConsistencySeverity.Warning);
        Assert.DoesNotContain(handle.Consistency.Findings, finding => finding.Code == "gpu.hardware-required");
        Assert.False(handle.Consistency.HasErrors);
        Assert.Empty(handle.Pages);
        Assert.Empty(handle.RawContext.Pages);
    }
}
