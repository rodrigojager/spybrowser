using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

public enum ConsistencySeverity
{
    Information,
    Warning,
    Error
}

public sealed record ConsistencyFinding(
    ConsistencySeverity Severity,
    string Code,
    string Message);

public sealed record ConsistencyReport(IReadOnlyList<ConsistencyFinding> Findings)
{
    public bool HasErrors => Findings.Any(finding => finding.Severity == ConsistencySeverity.Error);
}

public static class IdentityConsistencyValidator
{
    public static ConsistencyReport Validate(
        BrowserIdentity identity,
        GpuPolicy effectiveGpuPolicy,
        BrowserSurfaceDiagnostics diagnostics)
    {
        var findings = new List<ConsistencyFinding>();
        if (!diagnostics.WebGl1.Available && !diagnostics.WebGl2.Available)
        {
            findings.Add(new ConsistencyFinding(
                effectiveGpuPolicy == GpuPolicy.RequireHardware ? ConsistencySeverity.Error : ConsistencySeverity.Warning,
                "gpu.webgl-unavailable",
                "WebGL1 and WebGL2 are both unavailable."));
        }

        if (effectiveGpuPolicy == GpuPolicy.RequireHardware && diagnostics.UsesSoftwareRendering)
        {
            findings.Add(new ConsistencyFinding(
                ConsistencySeverity.Error,
                "gpu.hardware-required",
                "The identity requires hardware rendering, but a software renderer was detected."));
        }

        if (effectiveGpuPolicy == GpuPolicy.ExperimentalMask)
        {
            findings.Add(new ConsistencyFinding(
                ConsistencySeverity.Warning,
                "gpu.experimental-mask",
                "The WebGL string mask does not alter pixels, timing, WebGPU, or native function introspection."));
        }

        if (!string.Equals(identity.TimezoneId, diagnostics.TimezoneId, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ConsistencyFinding(
                ConsistencySeverity.Error,
                "identity.timezone-mismatch",
                $"Expected timezone '{identity.TimezoneId}', observed '{diagnostics.TimezoneId}'."));
        }

        if (diagnostics.Screen.Width != identity.Viewport.ScreenWidth ||
            diagnostics.Screen.Height != identity.Viewport.ScreenHeight)
        {
            findings.Add(new ConsistencyFinding(
                ConsistencySeverity.Warning,
                "identity.screen-mismatch",
                $"Expected screen {identity.Viewport.ScreenWidth}x{identity.Viewport.ScreenHeight}, " +
                $"observed {diagnostics.Screen.Width}x{diagnostics.Screen.Height}."));
        }

        if (identity.Navigator.EnableExperimentalOverrides)
        {
            findings.Add(new ConsistencyFinding(
                ConsistencySeverity.Warning,
                "navigator.experimental-overrides",
                "Navigator overrides are script-level and remain distinguishable from native browser values."));
        }

        return new ConsistencyReport(findings);
    }
}
