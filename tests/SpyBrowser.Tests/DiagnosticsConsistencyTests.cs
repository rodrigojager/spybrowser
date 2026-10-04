using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class DiagnosticsConsistencyTests
{
    [Fact]
    public void UTC_alias_and_windows_iana_timezone_are_equivalent_but_same_offset_zones_are_not()
    {
        var identity = BrowserIdentity.Create("timezone") with { TimezoneId = "Etc/UTC" };
        var utc = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(timezone: "UTC"), ConsistencyExpectations.FromIdentity(identity));
        var sameOffsetDifferentRegion = IdentityConsistencyValidator.Validate(identity with { TimezoneId = "America/New_York" },
            GpuPolicy.AllowSoftware, Diagnostics(timezone: "America/Toronto"),
            ConsistencyExpectations.FromIdentity(identity with { TimezoneId = "America/New_York" }));
        Assert.DoesNotContain(utc.Findings, finding => finding.Code == "identity.timezone-mismatch");
        Assert.Contains(sameOffsetDifferentRegion.Findings, finding => finding.Code == "identity.timezone-mismatch");

        var windowsIana = IdentityConsistencyValidator.Validate(identity with { TimezoneId = "Eastern Standard Time" },
            GpuPolicy.AllowSoftware, Diagnostics(timezone: "America/New_York"),
            new ConsistencyExpectations { TimezoneId = "Eastern Standard Time" });
        Assert.DoesNotContain(windowsIana.Findings, finding => finding.Code == "identity.timezone-mismatch");
    }

    [Fact]
    public void Locale_screen_dpr_and_ua_conflicts_are_reported_without_comparing_version_literals()
    {
        var identity = BrowserIdentity.Create("coherence") with
        {
            Locale = "pt-BR",
            Browser = new BrowserIdentitySettings { Engine = BrowserEngine.Chromium, Channel = null }
        };
        var expected = ConsistencyExpectations.FromIdentity(identity) with
        {
            UserAgent = "Mozilla/5.0 Chrome/124.0.0.0 Safari/537.36",
            BrowserFamily = "chrome",
            ScreenWidth = 1366,
            ScreenHeight = 768,
            ViewportWidth = 1200,
            ViewportHeight = 800,
            DeviceScaleFactor = 2
        };
        var coherent = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(timezone: identity.TimezoneId, languages: ["pt-br", "en-US"], userAgent: "Mozilla/5.0 Chrome/126.0 Safari/537.36"), expected);
        Assert.DoesNotContain(coherent.Findings, finding => finding.Severity == ConsistencySeverity.Error);
        Assert.DoesNotContain(coherent.Findings, finding => finding.Code == "browser.ua-family-mismatch");
        Assert.DoesNotContain(coherent.Findings, finding => finding.Code == "browser.family-ua-mismatch");

        var conflicting = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(timezone: identity.TimezoneId, languages: ["en-US"], userAgent: "Mozilla/5.0 Edg/126.0"), expected);
        Assert.Contains(conflicting.Findings, finding => finding.Code == "identity.locale-mismatch");
        Assert.Contains(conflicting.Findings, finding => finding.Code == "browser.ua-family-mismatch");
        Assert.Contains(conflicting.Findings, finding => finding.Code == "identity.screen-mismatch");
        Assert.Contains(conflicting.Findings, finding => finding.Code == "identity.device-scale-mismatch");
        Assert.Contains(conflicting.Findings, finding => finding.Code == "identity.viewport-mismatch");
    }

    [Fact]
    public void Missing_webgpu_or_client_hints_is_not_a_conflict()
    {
        var identity = BrowserIdentity.Create("missing");
        var report = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            new BrowserSurfaceDiagnostics(), ConsistencyExpectations.FromIdentity(identity));
        Assert.DoesNotContain(report.Findings, finding => finding.Code.Contains("webgpu", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Findings, finding => finding.Code.Contains("client-hints", StringComparison.Ordinal));
        Assert.Contains(report.Findings, finding => finding.Code == "gpu.webgl-unavailable" && finding.Severity == ConsistencySeverity.Warning);
    }

    [Fact]
    public void Surface_probes_are_opt_in_and_configuration_warnings_do_not_fail_consistency()
    {
        Assert.False(new SpyBrowserLaunchOptions { IdentityId = "default-probe" }.RunGpuProbe);
        var identity = BrowserIdentity.Create("config-warning");
        var expectations = ConsistencyExpectations.FromIdentity(identity) with
        {
            BrowserFamily = "edge",
            UserAgent = "Mozilla/5.0 Chrome/128.0"
        };

        var report = IdentityConsistencyValidator.ValidateConfiguration(identity, GpuPolicy.AllowSoftware,
            expectations, new BrowserRuntimeProvenance("edge", "msedge", "128.0", "playwright.browser.version",
                "playwright.launch-configuration", "none", "none"));

        Assert.Contains(report.Findings, finding => finding.Code == "browser.configured-family-ua-mismatch");
        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Configuration_checks_are_probe_free_and_use_actual_runtime_family()
    {
        var identity = BrowserIdentity.Create("configured-runtime");
        var runtime = new BrowserRuntimeProvenance("edge", "msedge", "128.0.0.0",
            "playwright.browser.version", "playwright.launch-configuration", "none", "none");
        var expectations = ConsistencyExpectations.FromIdentity(identity) with
        {
            BrowserFamily = runtime.BrowserFamily,
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) Chrome/128.0 Safari/537.36",
            Platform = "Win32"
        };

        var configured = IdentityConsistencyValidator.ValidateConfiguration(identity,
            GpuPolicy.AllowSoftware, expectations, runtime);
        Assert.Equal(runtime, configured.Runtime);
        Assert.Contains(configured.Findings, finding => finding.Code == "browser.configured-family-ua-mismatch" &&
            finding.Severity == ConsistencySeverity.Warning);
        Assert.Contains(configured.Findings, finding => finding.Code == "browser.configured-platform-ua-mismatch" &&
            finding.Severity == ConsistencySeverity.Warning);
        Assert.False(configured.HasErrors);

        var observed = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(userAgent: "Mozilla/5.0 Chrome/128.0"), expectations, runtime);
        Assert.Contains(observed.Findings, finding => finding.Code == "browser.family-ua-mismatch");
        Assert.Equal("playwright.browser.version", observed.Runtime?.BrowserVersionSource);
    }

    [Fact]
    public void Language_preferences_client_hints_scale_warnings_and_no_viewport_are_conservative()
    {
        var identity = BrowserIdentity.Create("conservative");
        var expected = ConsistencyExpectations.FromIdentity(identity) with
        {
            Locale = "en_US", DeviceScaleFactor = 1.25, ViewportWidth = 1280, ViewportHeight = 720
        };
        var coherent = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(timezone: identity.TimezoneId, languages: ["en-US", "fr-FR"]) with
            {
                ClientHintsAvailable = false,
                Screen = new ScreenSurfaceDiagnostics { DevicePixelRatio = 1.5 }
            }, expected);
        Assert.DoesNotContain(coherent.Findings, finding => finding.Severity == ConsistencySeverity.Error);
        // A configured DPR 1.25 versus observed 1.5 is a real difference. Without
        // observed zoom evidence, report it as a warning rather than hiding it.
        Assert.Contains(coherent.Findings, finding => finding.Code == "identity.device-scale-mismatch" &&
            finding.Severity == ConsistencySeverity.Warning);
        Assert.DoesNotContain(coherent.Findings, finding => finding.Code == "identity.viewport-mismatch");

        var contradiction = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware,
            Diagnostics(timezone: identity.TimezoneId, languages: ["en-US", "fr-FR"]) with
            {
                ClientHintsAvailable = true,
                ClientHintPlatform = "Linux x86_64",
                Platform = "Win32",
                Screen = new ScreenSurfaceDiagnostics { DevicePixelRatio = 2 }
            }, expected);
        Assert.Contains(contradiction.Findings, finding => finding.Code == "browser.client-hints-platform-mismatch");
        Assert.Contains(contradiction.Findings, finding => finding.Code == "identity.device-scale-mismatch" && finding.Severity == ConsistencySeverity.Warning);
        Assert.DoesNotContain(contradiction.Findings, finding => finding.Code == "identity.viewport-mismatch");
    }

    private static BrowserSurfaceDiagnostics Diagnostics(
        string timezone = "Etc/UTC", string[]? languages = null, string userAgent = "Mozilla/5.0 Chrome/126.0") => new()
    {
        TimezoneId = timezone,
        Languages = languages ?? ["pt-BR"],
        UserAgent = userAgent,
        Platform = "Win32",
        Screen = new ScreenSurfaceDiagnostics
        {
            Width = 1920, Height = 1080, ViewportWidth = 1440, ViewportHeight = 1000, DevicePixelRatio = 1
        },
        WebGl1 = new WebGlSurfaceDiagnostics { Available = true },
        WebGl2 = new WebGlSurfaceDiagnostics { Available = true }
    };
}
