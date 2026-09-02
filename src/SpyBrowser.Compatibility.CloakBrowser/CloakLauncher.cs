using CloakBrowser.Human;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace CloakBrowser;

/// <summary>
/// Migration facade with CloakBrowser-shaped entry points. It does not contain,
/// download, or license any CloakBrowser binary.
/// </summary>
public static class CloakLauncher
{
    public static async Task<CloakBrowserHandle> LaunchAsync(
        LaunchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new LaunchOptions();
        var translated = Translate(options, userDataDirectory: null);
        var handle = await SpyBrowserLauncher.LaunchBrowserAsync(translated, cancellationToken)
            .ConfigureAwait(false);
        return new CloakBrowserHandle(handle, options.Humanize);
    }

    public static async Task<CloakContextHandle> LaunchContextAsync(
        LaunchContextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new LaunchContextOptions();
        var translated = Translate(options, userDataDirectory: null);
        var handle = await SpyBrowserLauncher.LaunchContextAsync(translated, cancellationToken)
            .ConfigureAwait(false);
        return new CloakContextHandle(handle, options.Humanize);
    }

    public static async Task<CloakContextHandle> LaunchPersistentContextAsync(
        string userDataDirectory,
        LaunchContextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataDirectory);
        options ??= new LaunchContextOptions();
        var translated = Translate(options, Path.GetFullPath(userDataDirectory));
        var handle = await SpyBrowserLauncher.LaunchPersistentContextAsync(translated, cancellationToken)
            .ConfigureAwait(false);
        return new CloakContextHandle(handle, options.Humanize);
    }

    private static SpyBrowserLaunchOptions Translate(LaunchOptions options, string? userDataDirectory)
    {
        var identityId = IdentityId.Normalize(options.IdentityId);
        if (options.GeoIp)
        {
            throw new NotSupportedException(
                "The compatibility facade does not call an external GeoIP service. " +
                "Set Locale and Timezone explicitly so they remain coherent with the proxy.");
        }

        var parsedProxy = ResolveProxy(options.Proxy);
        var engine = ResolveEngine(options);
        var locale = string.IsNullOrWhiteSpace(options.Locale) ? "pt-BR" : options.Locale;
        var timezone = string.IsNullOrWhiteSpace(options.Timezone)
            ? "America/Sao_Paulo"
            : options.Timezone;
        var viewport = options is LaunchContextOptions contextOptions && contextOptions.Viewport is { } requested
            ? requested
            : (options.ViewportWidth, options.ViewportHeight);
        var extraArguments = BuildArguments(options);
        var identity = new BrowserIdentity
        {
            Id = identityId,
            DisplayName = identityId,
            Locale = locale,
            TimezoneId = timezone,
            Viewport = new ViewportIdentity
            {
                Width = viewport.Item1,
                Height = viewport.Item2,
                ScreenWidth = options.ScreenWidth,
                ScreenHeight = options.ScreenHeight,
                DeviceScaleFactor = options.DeviceScaleFactor
            },
            Browser = new BrowserIdentitySettings
            {
                Engine = engine,
                Channel = engine == BrowserEngine.CustomChromium ? null : options.Channel,
                ExecutablePath = options.ExecutablePath,
                UserAgent = options.UserAgent
            },
            Network = new NetworkIdentitySettings
            {
                ProxyServer = parsedProxy?.Server,
                ProxyBypass = parsedProxy?.Bypass,
                DisableNonProxiedWebRtc = true
            },
            Gpu = new GpuIdentitySettings
            {
                Policy = options.GpuPolicy,
                MaskVendor = options.GpuVendor,
                MaskRenderer = options.GpuRenderer
            }
        };

        var humanOptions = options.HumanInteraction ?? ResolveHumanOptions(options);
        return new SpyBrowserLaunchOptions
        {
            IdentityId = identityId,
            IdentityOverride = identity,
            IdentitiesRoot = options.IdentitiesRoot,
            UserDataDirectoryOverride = userDataDirectory,
            Headless = options.Headless,
            Humanize = options.Humanize,
            HumanInteraction = humanOptions,
            ChannelOverride = options.Channel,
            ExecutablePathOverride = options.ExecutablePath,
            ExecutableProvider = options.ExecutableProvider,
            DriverSearchPath = options.DriverSearchPath,
            GpuPolicyOverride = options.GpuPolicy,
            ExtraArguments = extraArguments,
            SlowMoMilliseconds = options.SlowMoMilliseconds,
            RunGpuProbe = options.RunGpuProbe,
            FailOnConsistencyErrors = options.FailOnConsistencyErrors,
            ConfigureBrowserLaunch = launch =>
            {
                if (parsedProxy is not null)
                {
                    launch.Proxy = parsedProxy;
                }

                options.ConfigureBrowserLaunch?.Invoke(launch);
            },
            ConfigureContext = context =>
            {
                ApplyContextCompatibility(context, options as LaunchContextOptions);
                options.ConfigureContext?.Invoke(context);
            },
            ConfigurePage = page =>
            {
                ApplyPageCompatibility(page, options as LaunchContextOptions);
                options.ConfigurePage?.Invoke(page);
            },
            ConfigurePersistentContext = persistent =>
            {
                if (parsedProxy is not null)
                {
                    persistent.Proxy = parsedProxy;
                }

                ApplyPersistentContextCompatibility(persistent, options as LaunchContextOptions);
                options.ConfigurePersistentContext?.Invoke(persistent);
            }
        };
    }

    private static BrowserEngine ResolveEngine(LaunchOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ExecutablePath))
        {
            return BrowserEngine.CustomChromium;
        }

        return options.Channel?.StartsWith("msedge", StringComparison.OrdinalIgnoreCase) == true
            ? BrowserEngine.Edge
            : BrowserEngine.Chrome;
    }

    private static HumanInteractionOptions ResolveHumanOptions(LaunchOptions options)
    {
        var resolved = ResolveHumanPreset(options.HumanPreset);
        if (options.HumanConfig is null)
        {
            return resolved;
        }

        return HumanConfigMapper.Apply(resolved, options.HumanConfig);
    }

    private static HumanInteractionOptions ResolveHumanPreset(HumanPreset preset) => preset switch
    {
        HumanPreset.Default => new HumanInteractionOptions(),
        HumanPreset.Careful => new HumanInteractionOptions
        {
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 900,
            KeyMinimumDelayMilliseconds = 65,
            KeyMaximumDelayMilliseconds = 190,
            ScrollMinimumSteps = 7,
            ScrollMaximumSteps = 18,
            ThinkingPauseProbability = 0.06
        },
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown human preset.")
    };

    private static Proxy? ResolveProxy(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is Proxy playwrightProxy)
        {
            return new Proxy
            {
                Server = NormalizeProxyServer(playwrightProxy.Server),
                Bypass = playwrightProxy.Bypass,
                Username = playwrightProxy.Username,
                Password = playwrightProxy.Password
            };
        }

        if (value is ProxySettings settings)
        {
            return new Proxy
            {
                Server = NormalizeProxyServer(settings.Server),
                Bypass = settings.Bypass,
                Username = settings.Username,
                Password = settings.Password
            };
        }

        if (value is not string proxy ||
            !Uri.TryCreate(proxy, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https" or "socks5"))
        {
            throw new ArgumentException(
                "Proxy must be a proxy URL, ProxySettings, or Microsoft.Playwright.Proxy.",
                nameof(value));
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty };
        return new Proxy
        {
            Server = builder.Uri.GetLeftPart(UriPartial.Authority),
            Username = userInfo.Length > 0 && userInfo[0].Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : null,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };
    }

    private static string NormalizeProxyServer(string server)
    {
        if (!Uri.TryCreate(server, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https" or "socks5") ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "ProxySettings.Server must be an absolute URI without embedded credentials.",
                nameof(server));
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static IReadOnlyList<string> BuildArguments(LaunchOptions options)
    {
        var arguments = new List<string>(options.Args);
        if (options.ExtensionPaths.Count == 0)
        {
            return arguments;
        }

        var paths = options.ExtensionPaths.Select(Path.GetFullPath).ToArray();
        var missing = paths.FirstOrDefault(path => !Directory.Exists(path));
        if (missing is not null)
        {
            throw new DirectoryNotFoundException($"Browser extension directory was not found: {missing}");
        }

        var joined = string.Join(',', paths);
        arguments.Add($"--disable-extensions-except={joined}");
        arguments.Add($"--load-extension={joined}");
        return arguments;
    }

    private static void ApplyContextCompatibility(
        BrowserNewContextOptions target,
        LaunchContextOptions? source)
    {
        if (source is null)
        {
            return;
        }

        target.UserAgent = source.UserAgent ?? target.UserAgent;
        target.ViewportSize = ResolveViewport(source, target.ViewportSize);
        target.ColorScheme = ParseColorScheme(source.ColorScheme) ?? target.ColorScheme;
        target.StorageStatePath = source.StorageStatePath ?? target.StorageStatePath;
    }

    private static void ApplyPageCompatibility(
        BrowserNewPageOptions target,
        LaunchContextOptions? source)
    {
        if (source is null)
        {
            return;
        }

        target.UserAgent = source.UserAgent ?? target.UserAgent;
        target.ViewportSize = ResolveViewport(source, target.ViewportSize);
        target.ColorScheme = ParseColorScheme(source.ColorScheme) ?? target.ColorScheme;
        target.StorageStatePath = source.StorageStatePath ?? target.StorageStatePath;
    }

    private static void ApplyPersistentContextCompatibility(
        BrowserTypeLaunchPersistentContextOptions target,
        LaunchContextOptions? source)
    {
        if (source is null)
        {
            return;
        }

        target.UserAgent = source.UserAgent ?? target.UserAgent;
        target.ViewportSize = ResolveViewport(source, target.ViewportSize);
        target.ColorScheme = ParseColorScheme(source.ColorScheme) ?? target.ColorScheme;
    }

    private static ViewportSize? ResolveViewport(LaunchContextOptions source, ViewportSize? fallback) =>
        source.NoViewport
            ? ViewportSize.NoViewport
            : source.Viewport is { } viewport
                ? new ViewportSize { Width = viewport.Width, Height = viewport.Height }
                : fallback;

    private static ColorScheme? ParseColorScheme(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "light" => Microsoft.Playwright.ColorScheme.Light,
        "dark" => Microsoft.Playwright.ColorScheme.Dark,
        "no-preference" => Microsoft.Playwright.ColorScheme.NoPreference,
        _ => throw new ArgumentException("ColorScheme must be light, dark, or no-preference.", nameof(value))
    };
}
