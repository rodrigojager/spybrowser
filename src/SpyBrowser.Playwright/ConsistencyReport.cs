using System.Globalization;
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

/// <summary>Values actually applied to the browser context after caller callbacks.</summary>
public sealed record ConsistencyExpectations
{
    public string? Locale { get; init; }
    public string? TimezoneId { get; init; }
    public int? ViewportWidth { get; init; }
    public int? ViewportHeight { get; init; }
    public int? ScreenWidth { get; init; }
    public int? ScreenHeight { get; init; }
    public double? DeviceScaleFactor { get; init; }
    public string? UserAgent { get; init; }
    public string? BrowserFamily { get; init; }
    public string? Platform { get; init; }

    public static ConsistencyExpectations FromIdentity(BrowserIdentity identity) => new()
    {
        Locale = identity.Locale,
        TimezoneId = identity.TimezoneId,
        ViewportWidth = identity.Viewport.Width,
        ViewportHeight = identity.Viewport.Height,
        ScreenWidth = identity.Viewport.ScreenWidth,
        ScreenHeight = identity.Viewport.ScreenHeight,
        DeviceScaleFactor = identity.Viewport.DeviceScaleFactor,
        UserAgent = identity.Browser.UserAgent,
        BrowserFamily = identity.Browser.Engine switch
        {
            BrowserEngine.Edge => "edge",
            BrowserEngine.Chrome => "chrome",
            BrowserEngine.Chromium or BrowserEngine.CustomChromium => "chromium",
            _ => null
        },
        Platform = identity.Navigator.Platform
    };
}

public static class IdentityConsistencyValidator
{
    public static ConsistencyReport Validate(
        BrowserIdentity identity,
        GpuPolicy effectiveGpuPolicy,
        BrowserSurfaceDiagnostics diagnostics) =>
        Validate(identity, effectiveGpuPolicy, diagnostics, ConsistencyExpectations.FromIdentity(identity));

    public static ConsistencyReport Validate(
        BrowserIdentity identity,
        GpuPolicy effectiveGpuPolicy,
        BrowserSurfaceDiagnostics diagnostics,
        ConsistencyExpectations expectations)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(expectations);
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
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Error, "gpu.hardware-required",
                "The identity requires hardware rendering, but a software renderer was detected."));
        }

        if (effectiveGpuPolicy == GpuPolicy.ExperimentalMask)
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "gpu.experimental-mask",
                "The WebGL string mask does not alter pixels, timing, WebGPU, or native function introspection."));
        }

        if (!string.IsNullOrWhiteSpace(expectations.TimezoneId) &&
            !string.IsNullOrWhiteSpace(diagnostics.TimezoneId) &&
            !TimezonesEquivalent(expectations.TimezoneId, diagnostics.TimezoneId))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Error, "identity.timezone-mismatch",
                $"Expected timezone '{expectations.TimezoneId}', observed '{diagnostics.TimezoneId}'."));
        }

        if (!string.IsNullOrWhiteSpace(expectations.Locale) && diagnostics.Languages.Length > 0 &&
            TryNormalizeLanguage(expectations.Locale, out var expectedLocale) &&
            TryNormalizeLanguage(diagnostics.Languages[0], out var observedLocale) &&
            !string.Equals(expectedLocale, observedLocale, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Error, "identity.locale-mismatch",
                $"Expected primary locale '{expectedLocale}', observed '{observedLocale}'."));
        }

        if (expectations.ScreenWidth is int expectedWidth && expectations.ScreenHeight is int expectedHeight &&
            diagnostics.Screen.Width > 0 && diagnostics.Screen.Height > 0 &&
            (diagnostics.Screen.Width != expectedWidth || diagnostics.Screen.Height != expectedHeight))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "identity.screen-mismatch",
                $"Expected screen {expectedWidth}x{expectedHeight}, observed {diagnostics.Screen.Width}x{diagnostics.Screen.Height}."));
        }

        if (expectations.DeviceScaleFactor is double expectedScale && diagnostics.Screen.DevicePixelRatio > 0 &&
            Math.Abs(expectedScale - diagnostics.Screen.DevicePixelRatio) > 0.01)
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "identity.device-scale-mismatch",
                $"Expected device scale factor {expectedScale}, observed DPR {diagnostics.Screen.DevicePixelRatio}."));
        }

        if (expectations.ViewportWidth is int viewportWidth && expectations.ViewportHeight is int viewportHeight &&
            diagnostics.Screen.ViewportWidth > 0 && diagnostics.Screen.ViewportHeight > 0 &&
            (diagnostics.Screen.ViewportWidth != viewportWidth || diagnostics.Screen.ViewportHeight != viewportHeight))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "identity.viewport-mismatch",
                $"Expected viewport {viewportWidth}x{viewportHeight}, observed {diagnostics.Screen.ViewportWidth}x{diagnostics.Screen.ViewportHeight}."));
        }

        AddBrowserFindings(expectations, diagnostics, findings);
        if (identity.Navigator.EnableExperimentalOverrides)
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "navigator.experimental-overrides",
                "Navigator overrides are script-level and remain distinguishable from native browser values."));
        }

        return new ConsistencyReport(findings);
    }

    private static void AddBrowserFindings(
        ConsistencyExpectations expected,
        BrowserSurfaceDiagnostics observed,
        ICollection<ConsistencyFinding> findings)
    {
        if (!string.IsNullOrWhiteSpace(expected.UserAgent) && !string.IsNullOrWhiteSpace(observed.UserAgent) &&
            TryBrowserFamily(expected.UserAgent, out var expectedUaFamily) &&
            TryBrowserFamily(observed.UserAgent, out var actualUaFamily) &&
            !string.Equals(expectedUaFamily, actualUaFamily, StringComparison.Ordinal))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "browser.ua-family-mismatch",
                $"Configured UA indicates '{expectedUaFamily}', observed UA indicates '{actualUaFamily}'."));
        }

        if (!string.IsNullOrWhiteSpace(expected.BrowserFamily) &&
            TryBrowserFamily(expected.BrowserFamily, out var expectedFamily) &&
            TryBrowserFamily(observed.UserAgent, out var uaFamily) &&
            !BrowserFamiliesCompatible(expectedFamily, uaFamily))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "browser.family-ua-mismatch",
                $"Launched browser family '{expectedFamily}' conflicts with the observed UA family '{uaFamily}'."));
        }

        if (!string.IsNullOrWhiteSpace(expected.Platform) && !string.IsNullOrWhiteSpace(observed.Platform) &&
            !PlatformCompatible(expected.Platform, observed.Platform))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "browser.platform-mismatch",
                $"Configured platform '{expected.Platform}' conflicts with observed platform '{observed.Platform}'."));
        }

        if (observed.ClientHintsAvailable && !string.IsNullOrWhiteSpace(observed.ClientHintPlatform) &&
            !string.IsNullOrWhiteSpace(observed.Platform) &&
            !PlatformCompatible(observed.ClientHintPlatform, observed.Platform))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Warning, "browser.client-hints-platform-mismatch",
                "Available Client Hints contradict navigator.platform."));
        }

        if (observed.WebGpu.Available && observed.WebGl1.Available &&
            observed.WebGpu.IsSoftwareAdapter != observed.WebGl1.IsSoftwareRenderer &&
            (observed.WebGpu.IsSoftwareAdapter || observed.WebGl1.IsSoftwareRenderer))
        {
            findings.Add(new ConsistencyFinding(ConsistencySeverity.Information, "gpu.surface-capability-difference",
                "WebGL and WebGPU report different renderer classes; this can be legitimate and is informational."));
        }
    }

    private static bool TryNormalizeLanguage(string value, out string normalized)
    {
        try
        {
            normalized = CultureInfo.GetCultureInfo(value.Replace('_', '-')).Name;
            return normalized.Length > 0;
        }
        catch (CultureNotFoundException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    private static bool TimezonesEquivalent(string expected, string observed)
    {
        if (string.Equals(expected, observed, StringComparison.OrdinalIgnoreCase)) return true;
        if (IsUtc(expected) && IsUtc(observed)) return true;
        try
        {
            var expectedIsIana = expected.Contains('/', StringComparison.Ordinal);
            var observedIsIana = observed.Contains('/', StringComparison.Ordinal);
            if (expectedIsIana == observedIsIana) return false;
            var windowsId = expectedIsIana ? observed : expected;
            var ianaId = expectedIsIana ? expected : observed;
            return TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId, out var converted) &&
                   string.Equals(converted, ianaId, StringComparison.OrdinalIgnoreCase);
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private static bool IsUtc(string value) => value.Equals("UTC", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Etc/GMT", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("GMT", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Coordinated Universal Time", StringComparison.OrdinalIgnoreCase);

    private static bool PlatformCompatible(string left, string right)
    {
        static string Normalize(string value)
        {
            var text = value.ToLowerInvariant();
            if (text.Contains("win", StringComparison.Ordinal)) return "windows";
            if (text.Contains("mac", StringComparison.Ordinal) || text.Contains("darwin", StringComparison.Ordinal)) return "macos";
            if (text.Contains("linux", StringComparison.Ordinal) || text.Contains("x11", StringComparison.Ordinal)) return "linux";
            if (text.Contains("android", StringComparison.Ordinal)) return "android";
            if (text.Contains("iphone", StringComparison.Ordinal) || text.Contains("ipad", StringComparison.Ordinal) || text.Contains("ios", StringComparison.Ordinal)) return "ios";
            return text.Trim();
        }
        return Normalize(left) == Normalize(right);
    }

    private static bool BrowserFamiliesCompatible(string expected, string observed) =>
        string.Equals(expected, observed, StringComparison.Ordinal) ||
        (expected == "chromium" && observed == "chrome");

    private static bool TryBrowserFamily(string value, out string family)
    {
        var text = value.ToLowerInvariant();
        if (text.Contains("edg/", StringComparison.Ordinal) || text == "edge") family = "edge";
        else if (text.Contains("chrome/", StringComparison.Ordinal) || text == "chrome") family = "chrome";
        else if (text.Contains("chromium", StringComparison.Ordinal)) family = "chromium";
        else if (text.Contains("firefox", StringComparison.Ordinal)) family = "firefox";
        else if (text.Contains("safari", StringComparison.Ordinal) && !text.Contains("chrome", StringComparison.Ordinal)) family = "safari";
        else { family = string.Empty; return false; }
        return true;
    }
}
