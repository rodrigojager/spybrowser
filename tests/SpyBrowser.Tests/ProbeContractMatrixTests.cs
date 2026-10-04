using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class ProbeContractMatrixTests
{
    [Theory]
    [InlineData("context")]
    [InlineData("persistent")]
    [InlineData("browser-only")]
    public void Missing_optional_surfaces_are_incomplete_not_identity_conflicts(string mode)
    {
        var identity = BrowserIdentity.Create($"probe-{mode}");
        var expected = ConsistencyExpectations.FromIdentity(identity);
        var diagnostics = new BrowserSurfaceDiagnostics
        {
            WebGl1 = new WebGlSurfaceDiagnostics { Available = false },
            WebGl2 = new WebGlSurfaceDiagnostics { Available = false },
            WebGpu = new WebGpuSurfaceDiagnostics { Available = false, Error = "secure context required" }
        };

        var report = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware, diagnostics, expected);

        Assert.Contains(report.Findings, finding => finding.Code == "gpu.webgl-unavailable" && finding.Severity == ConsistencySeverity.Warning);
        Assert.DoesNotContain(report.Findings, finding => finding.Code.Contains("identity.", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Findings, finding => finding.Code.Contains("webgpu", StringComparison.Ordinal));
    }

    [Fact]
    public void Effective_callback_expectations_do_not_report_stale_manifest_values_as_conflicts()
    {
        var identity = BrowserIdentity.Create("effective-probe");
        var actual = new BrowserSurfaceDiagnostics
        {
            TimezoneId = "UTC",
            Languages = ["en-GB", "fr-FR"],
            Screen = new ScreenSurfaceDiagnostics
            {
                Width = 1600, Height = 900, ViewportWidth = 1280, ViewportHeight = 720, DevicePixelRatio = 1.25
            },
            WebGl1 = new WebGlSurfaceDiagnostics { Available = true },
            WebGl2 = new WebGlSurfaceDiagnostics { Available = true }
        };
        var effective = new ConsistencyExpectations
        {
            TimezoneId = "Etc/UTC", Locale = "en-GB", ScreenWidth = 1600, ScreenHeight = 900,
            ViewportWidth = 1280, ViewportHeight = 720, DeviceScaleFactor = 1.25
        };

        var report = IdentityConsistencyValidator.Validate(identity, GpuPolicy.AllowSoftware, actual, effective);
        Assert.DoesNotContain(report.Findings, finding => finding.Severity == ConsistencySeverity.Error);
        Assert.DoesNotContain(report.Findings, finding => finding.Code.StartsWith("identity.", StringComparison.Ordinal));
    }
}
