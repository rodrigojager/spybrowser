using CloakBrowser.Human;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace CloakBrowser;

public sealed record ProxySettings
{
    public required string Server { get; init; }

    public string? Bypass { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}

public record LaunchOptions
{
    public string IdentityId { get; init; } = "default";

    public string? IdentitiesRoot { get; init; }

    public bool Headless { get; init; } = true;

    public bool Humanize { get; init; }

    public HumanPreset HumanPreset { get; init; } = HumanPreset.Default;

    public HumanInteractionOptions? HumanInteraction { get; init; }

    public string? Locale { get; init; }

    public string? Timezone { get; init; }

    public int ViewportWidth { get; init; } = 1440;

    public int ViewportHeight { get; init; } = 1000;

    public int ScreenWidth { get; init; } = 1920;

    public int ScreenHeight { get; init; } = 1080;

    public float DeviceScaleFactor { get; init; } = 1;

    public string? UserAgent { get; init; }

    public string? Channel { get; init; } = "chrome";

    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Accepts a proxy URL, <see cref="ProxySettings"/>, or Playwright
    /// <see cref="Microsoft.Playwright.Proxy"/>, matching CloakBrowser's public shape.
    /// </summary>
    public object? Proxy { get; init; }

    public List<string> Args { get; init; } = [];

    public bool StealthArgs { get; init; } = true;

    public bool GeoIp { get; init; }

    public IReadOnlyDictionary<string, object>? HumanConfig { get; init; }

    public List<string> ExtensionPaths { get; init; } = [];

    /// <summary>Accepted for source compatibility; SpyBrowser never consumes a Cloak license.</summary>
    public string? LicenseKey { get; init; }

    /// <summary>Accepted for source compatibility; use ExecutablePath or ExecutableProvider to select a binary.</summary>
    public string? BrowserVersion { get; init; }

    /// <summary>Accepted for source compatibility; SpyBrowser has no paid release channel.</summary>
    public string? ReleaseChannel { get; init; }

    public GpuPolicy GpuPolicy { get; init; } = GpuPolicy.Auto;

    public string? GpuVendor { get; init; }

    public string? GpuRenderer { get; init; }

    public bool RunGpuProbe { get; init; }

    public bool FailOnConsistencyErrors { get; init; } = true;

    public float? SlowMoMilliseconds { get; init; }

    public string? DriverSearchPath { get; init; }

    public IBrowserExecutableProvider? ExecutableProvider { get; init; }

    public Action<BrowserTypeLaunchOptions>? ConfigureBrowserLaunch { get; init; }

    public Action<BrowserNewContextOptions>? ConfigureContext { get; init; }

    public Action<BrowserNewPageOptions>? ConfigurePage { get; init; }

    public Action<BrowserTypeLaunchPersistentContextOptions>? ConfigurePersistentContext { get; init; }
}

public sealed record LaunchContextOptions : LaunchOptions
{
    public (int Width, int Height)? Viewport { get; init; }

    public bool NoViewport { get; init; }

    public string? ColorScheme { get; init; }

    public string? StorageStatePath { get; init; }
}
