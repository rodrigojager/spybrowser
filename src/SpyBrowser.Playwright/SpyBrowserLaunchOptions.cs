using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

public sealed record SpyBrowserLaunchOptions
{
    public required string IdentityId { get; init; }

    /// <summary>
    /// Supplies an identity in memory. When omitted, the identity is loaded from
    /// IdentityStore. This enables adapters and embedded use without provisioning
    /// a manifest while retaining the same profile layout.
    /// </summary>
    public BrowserIdentity? IdentityOverride { get; init; }

    public string? IdentitiesRoot { get; init; }

    public bool Headless { get; init; }

    /// <summary>
    /// Transparently decorates Playwright pages, locators, frames, mouse, and
    /// keyboard objects with human-paced interaction behavior. Navigation,
    /// evaluation, network, download, and all non-interaction members continue
    /// to delegate directly to Playwright.
    /// </summary>
    public bool Humanize { get; init; }

    public HumanInteractionOptions? HumanInteraction { get; init; }

    public string? ChannelOverride { get; init; }

    public string? ExecutablePathOverride { get; init; }

    /// <summary>
    /// Optional executable provider used by independently packaged browser
    /// engines. An explicit ExecutablePathOverride always takes precedence.
    /// </summary>
    public IBrowserExecutableProvider? ExecutableProvider { get; init; }

    /// <summary>
    /// Overrides the identity-managed profile directory for persistent launches.
    /// The directory receives its own cross-process lease.
    /// </summary>
    public string? UserDataDirectoryOverride { get; init; }

    public GpuPolicy? GpuPolicyOverride { get; init; }

    public IReadOnlyList<string> ExtraArguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Directory containing the .playwright driver directory. A custom path is
    /// process-wide and therefore should be selected before any Playwright object is created.
    /// </summary>
    public string? DriverSearchPath { get; init; }

    public float? SlowMoMilliseconds { get; init; }

    public float DefaultTimeoutMilliseconds { get; init; } = 30_000;

    public float DefaultNavigationTimeoutMilliseconds { get; init; } = 60_000;

    /// <summary>Runs an isolated WebGL/WebGPU surface probe after context setup; opt-in and disabled by default.</summary>
    public bool RunGpuProbe { get; init; }

    public bool FailOnConsistencyErrors { get; init; } = true;

    /// <summary>Final customization hook for the official browser launch options.</summary>
    public Action<BrowserTypeLaunchOptions>? ConfigureBrowserLaunch { get; init; }

    /// <summary>Final customization hook for the official incognito context options.</summary>
    public Action<BrowserNewContextOptions>? ConfigureContext { get; init; }

    /// <summary>Final customization hook for Browser.NewPageAsync options.</summary>
    public Action<BrowserNewPageOptions>? ConfigurePage { get; init; }

    /// <summary>Final customization hook for the official persistent context options.</summary>
    public Action<BrowserTypeLaunchPersistentContextOptions>? ConfigurePersistentContext { get; init; }
}
