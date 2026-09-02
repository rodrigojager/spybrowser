using System.Globalization;
using System.Text.RegularExpressions;

namespace SpyBrowser.Core;

public static partial class IdentityValidator
{
    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "chrome-beta",
        "chrome-dev",
        "chrome-canary",
        "msedge",
        "msedge-beta",
        "msedge-dev",
        "msedge-canary"
    };

    public static void ValidateAndThrow(BrowserIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var errors = Validate(identity);
        if (errors.Count > 0)
        {
            throw new IdentityValidationException(errors);
        }
    }

    public static IReadOnlyList<string> Validate(BrowserIdentity identity)
    {
        var errors = new List<string>();
        try
        {
            if (!string.Equals(identity.Id, IdentityId.Normalize(identity.Id), StringComparison.Ordinal))
            {
                errors.Add("Id must already be normalized to lowercase.");
            }
        }
        catch (ArgumentException exception)
        {
            errors.Add(exception.Message);
        }

        if (identity.SchemaVersion != BrowserIdentity.CurrentSchemaVersion)
        {
            errors.Add($"Unsupported schema version: {identity.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(identity.DisplayName) || identity.DisplayName.Length > 120)
        {
            errors.Add("DisplayName must contain between 1 and 120 characters.");
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(identity.Locale);
        }
        catch (CultureNotFoundException)
        {
            errors.Add($"Unknown locale: '{identity.Locale}'.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(identity.TimezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            errors.Add($"Unknown timezone: '{identity.TimezoneId}'.");
        }
        catch (InvalidTimeZoneException)
        {
            errors.Add($"Invalid timezone data: '{identity.TimezoneId}'.");
        }

        ValidateViewport(identity.Viewport, errors);
        ValidateBrowser(identity.Browser, errors);
        ValidateNetwork(identity.Network, errors);
        ValidateGpu(identity.Gpu, errors);
        ValidateNavigator(identity.Navigator, errors);
        return errors;
    }

    private static void ValidateViewport(ViewportIdentity viewport, ICollection<string> errors)
    {
        if (viewport.Width is < 320 or > 10_000 || viewport.Height is < 240 or > 10_000)
        {
            errors.Add("Viewport dimensions are outside the supported range.");
        }

        if (viewport.ScreenWidth < viewport.Width || viewport.ScreenHeight < viewport.Height)
        {
            errors.Add("Screen dimensions must be greater than or equal to the viewport.");
        }

        if (viewport.DeviceScaleFactor is < 0.5f or > 4f)
        {
            errors.Add("DeviceScaleFactor must be between 0.5 and 4.");
        }
    }

    private static void ValidateBrowser(BrowserIdentitySettings browser, ICollection<string> errors)
    {
        if (browser.Engine == BrowserEngine.CustomChromium && string.IsNullOrWhiteSpace(browser.ExecutablePath))
        {
            errors.Add("CustomChromium requires ExecutablePath.");
        }

        if (!string.IsNullOrWhiteSpace(browser.Channel) && !SupportedChannels.Contains(browser.Channel))
        {
            errors.Add($"Unsupported browser channel: '{browser.Channel}'.");
        }

        if (browser.UserAgent?.ContainsAnyLineBreak() == true)
        {
            errors.Add("UserAgent cannot contain line breaks.");
        }
    }

    private static void ValidateNetwork(NetworkIdentitySettings network, ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(network.ProxyServer))
        {
            if (!Uri.TryCreate(network.ProxyServer, UriKind.Absolute, out var proxyUri) ||
                proxyUri.Scheme is not ("http" or "https" or "socks5"))
            {
                errors.Add("ProxyServer must be an absolute http, https or socks5 URI.");
            }
            else if (!string.IsNullOrEmpty(proxyUri.UserInfo))
            {
                errors.Add("Proxy credentials must use environment references, not URI user info.");
            }
        }

        ValidateEnvironmentName(network.ProxyUsernameEnvironment, nameof(network.ProxyUsernameEnvironment), errors);
        ValidateEnvironmentName(network.ProxyPasswordEnvironment, nameof(network.ProxyPasswordEnvironment), errors);
    }

    private static void ValidateGpu(GpuIdentitySettings gpu, ICollection<string> errors)
    {
        if (gpu.Policy == GpuPolicy.ExperimentalMask &&
            (string.IsNullOrWhiteSpace(gpu.MaskVendor) || string.IsNullOrWhiteSpace(gpu.MaskRenderer)))
        {
            errors.Add("ExperimentalMask requires MaskVendor and MaskRenderer.");
        }

        if (gpu.MaskVendor?.ContainsAnyLineBreak() == true || gpu.MaskRenderer?.ContainsAnyLineBreak() == true)
        {
            errors.Add("GPU mask values cannot contain line breaks.");
        }
    }

    private static void ValidateNavigator(NavigatorIdentitySettings navigator, ICollection<string> errors)
    {
        if (!navigator.EnableExperimentalOverrides &&
            (navigator.Platform is not null || navigator.HardwareConcurrency is not null || navigator.DeviceMemoryGb is not null))
        {
            errors.Add("Navigator overrides require EnableExperimentalOverrides=true.");
        }

        if (navigator.HardwareConcurrency is < 1 or > 256)
        {
            errors.Add("HardwareConcurrency must be between 1 and 256.");
        }

        if (navigator.DeviceMemoryGb is < 0.25 or > 512)
        {
            errors.Add("DeviceMemoryGb must be between 0.25 and 512.");
        }

        if (navigator.Platform?.ContainsAnyLineBreak() == true)
        {
            errors.Add("Navigator Platform cannot contain line breaks.");
        }
    }

    private static void ValidateEnvironmentName(string? value, string fieldName, ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && !EnvironmentVariableName().IsMatch(value))
        {
            errors.Add($"{fieldName} is not a valid environment variable name.");
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentVariableName();

    private static bool ContainsAnyLineBreak(this string value) =>
        value.Contains('\r') || value.Contains('\n');
}
