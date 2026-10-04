using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

public static class SpyBrowserLauncher
{
    /// <summary>
    /// Backward-compatible entry point. SpyBrowser identities are persistent by
    /// default; use LaunchBrowserAsync or LaunchContextAsync for disposable modes.
    /// </summary>
    public static Task<SpyBrowserSession> LaunchAsync(
        SpyBrowserLaunchOptions options,
        CancellationToken cancellationToken = default) =>
        LaunchPersistentContextAsync(options, cancellationToken);

    public static async Task<SpyBrowserSession> LaunchPersistentContextAsync(
        SpyBrowserLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        var store = new IdentityStore(options.IdentitiesRoot);
        var identity = await ResolveIdentityAsync(options, store, cancellationToken).ConfigureAwait(false);
        var userDataDirectory = string.IsNullOrWhiteSpace(options.UserDataDirectoryOverride)
            ? store.GetProfileDirectory(identity.Id)
            : Path.GetFullPath(options.UserDataDirectoryOverride);
        Directory.CreateDirectory(userDataDirectory);
        var leasePath = string.IsNullOrWhiteSpace(options.UserDataDirectoryOverride)
            ? store.GetLeasePath(identity.Id)
            : userDataDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
              ".spybrowser.lock";
        var lease = await IdentityLease.AcquirePathAsync(identity.Id, leasePath, cancellationToken)
            .ConfigureAwait(false);
        LaunchState? state = null;
        IBrowserContext? context = null;

        try
        {
            state = await CreateStateAsync(options, identity, cancellationToken).ConfigureAwait(false);
            var persistentOptions = CreatePersistentContextOptions(state, options);
            options.ConfigurePersistentContext?.Invoke(persistentOptions);
            context = await state.BrowserType.LaunchPersistentContextAsync(
                userDataDirectory,
                persistentOptions).ConfigureAwait(false);
            var prepared = await PrepareContextAsync(context, state, options, cancellationToken).ConfigureAwait(false);
            return new SpyBrowserSession(
                state.Playwright,
                context,
                identity,
                lease,
                prepared.Diagnostics,
                prepared.Consistency,
                state.Humanizer);
        }
        catch
        {
            try
            {
                if (context is not null)
                {
                    await context.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                state?.Playwright.Dispose();
                await lease.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    public static async Task<SpyBrowserContextHandle> LaunchContextAsync(
        SpyBrowserLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        var store = new IdentityStore(options.IdentitiesRoot);
        var identity = await ResolveIdentityAsync(options, store, cancellationToken).ConfigureAwait(false);
        LaunchState? state = null;
        IBrowser? browser = null;
        IBrowserContext? context = null;

        try
        {
            state = await CreateStateAsync(options, identity, cancellationToken).ConfigureAwait(false);
            var browserOptions = CreateBrowserOptions(state, options);
            options.ConfigureBrowserLaunch?.Invoke(browserOptions);
            browser = await state.BrowserType.LaunchAsync(browserOptions).ConfigureAwait(false);
            var contextOptions = CreateContextOptions(identity);
            options.ConfigureContext?.Invoke(contextOptions);
            context = await browser.NewContextAsync(contextOptions).ConfigureAwait(false);
            var prepared = await PrepareContextAsync(context, state, options, cancellationToken).ConfigureAwait(false);
            return new SpyBrowserContextHandle(
                state.Playwright,
                context,
                browser,
                identity,
                identityLease: null,
                closeBrowser: true,
                prepared.Diagnostics,
                prepared.Consistency,
                state.Humanizer);
        }
        catch
        {
            try
            {
                if (context is not null)
                {
                    await context.CloseAsync().ConfigureAwait(false);
                }

                if (browser is not null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                state?.Playwright.Dispose();
            }

            throw;
        }
    }

    public static async Task<SpyBrowserBrowserHandle> LaunchBrowserAsync(
        SpyBrowserLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        var store = new IdentityStore(options.IdentitiesRoot);
        var identity = await ResolveIdentityAsync(options, store, cancellationToken).ConfigureAwait(false);
        LaunchState? state = null;
        IBrowser? browser = null;

        try
        {
            state = await CreateStateAsync(options, identity, cancellationToken).ConfigureAwait(false);
            var browserOptions = CreateBrowserOptions(state, options);
            options.ConfigureBrowserLaunch?.Invoke(browserOptions);
            browser = await state.BrowserType.LaunchAsync(browserOptions).ConfigureAwait(false);
            var capturedBrowser = browser;
            var capturedState = state;

            async Task<IBrowserContext> CreateConfiguredContextAsync(BrowserNewContextOptions? supplied)
            {
                var contextOptions = supplied is null
                    ? CreateContextOptions(identity)
                    : CloneOptions(supplied);
                ApplyContextDefaults(contextOptions, identity);
                options.ConfigureContext?.Invoke(contextOptions);
                var rawContext = await capturedBrowser.NewContextAsync(contextOptions).ConfigureAwait(false);
                try
                {
                    await ConfigureContextRuntimeAsync(rawContext, capturedState, options).ConfigureAwait(false);
                    return capturedState.Humanizer?.Wrap(rawContext) ?? rawContext;
                }
                catch
                {
                    try { await rawContext.CloseAsync().ConfigureAwait(false); }
                    catch { /* Preserve the preparation failure. */ }
                    throw;
                }
            }

            async Task<IPage> CreateConfiguredPageAsync(BrowserNewPageOptions? supplied)
            {
                var pageOptions = supplied is null
                    ? CreatePageOptions(identity)
                    : CloneOptions(supplied);
                ApplyPageDefaults(pageOptions, identity);
                options.ConfigurePage?.Invoke(pageOptions);
                var rawPage = await capturedBrowser.NewPageAsync(pageOptions).ConfigureAwait(false);
                try
                {
                    await ConfigureContextRuntimeAsync(rawPage.Context, capturedState, options).ConfigureAwait(false);
                    return capturedState.Humanizer?.Wrap(rawPage) ?? rawPage;
                }
                catch
                {
                    try { await rawPage.Context.CloseAsync().ConfigureAwait(false); }
                    catch { /* Preserve the preparation failure. */ }
                    throw;
                }
            }

            return new SpyBrowserBrowserHandle(
                state.Playwright,
                browser,
                identity,
                state.Humanizer,
                CreateConfiguredContextAsync,
                CreateConfiguredPageAsync);
        }
        catch
        {
            try
            {
                if (browser is not null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                state?.Playwright.Dispose();
            }

            throw;
        }
    }

    private static async Task<LaunchState> CreateStateAsync(
        SpyBrowserLaunchOptions options,
        BrowserIdentity identity,
        CancellationToken cancellationToken)
    {
        PlaywrightDriverConfiguration.Configure(options.DriverSearchPath);
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        try
        {
            var effectiveGpuPolicy = options.GpuPolicyOverride ?? identity.Gpu.Policy;
            if (effectiveGpuPolicy == GpuPolicy.ExperimentalMask &&
                (string.IsNullOrWhiteSpace(identity.Gpu.MaskVendor) ||
                 string.IsNullOrWhiteSpace(identity.Gpu.MaskRenderer)))
            {
                throw new InvalidOperationException(
                    "ExperimentalMask requires MaskVendor and MaskRenderer in the identity manifest.");
            }

            var providerPath = options.ExecutableProvider is null
                ? null
                : await options.ExecutableProvider.ResolveExecutableAsync(identity, cancellationToken)
                    .ConfigureAwait(false);
            var executablePath = options.ExecutablePathOverride ?? providerPath ?? identity.Browser.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                executablePath = Path.GetFullPath(executablePath);
                if (!File.Exists(executablePath))
                {
                    throw new FileNotFoundException("The configured browser executable was not found.", executablePath);
                }
            }

            return new LaunchState(
                playwright,
                ResolveBrowserType(playwright, identity.Browser.Engine),
                identity,
                effectiveGpuPolicy,
                ResolveChannel(identity, options.ChannelOverride, executablePath),
                executablePath,
                BrowserArgumentBuilder.Build(identity, effectiveGpuPolicy, options.ExtraArguments),
                options.Humanize ? new PlaywrightHumanizer(options.HumanInteraction) : null);
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

    private static async Task<BrowserIdentity> ResolveIdentityAsync(
        SpyBrowserLaunchOptions options,
        IdentityStore store,
        CancellationToken cancellationToken)
    {
        if (options.IdentityOverride is null)
        {
            return await store.GetAsync(options.IdentityId, cancellationToken).ConfigureAwait(false);
        }

        IdentityValidator.ValidateAndThrow(options.IdentityOverride);
        var requestedId = IdentityId.Normalize(options.IdentityId);
        if (!string.Equals(requestedId, options.IdentityOverride.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "IdentityOverride.Id must match IdentityId.",
                nameof(options));
        }

        return options.IdentityOverride;
    }

    private static BrowserTypeLaunchOptions CreateBrowserOptions(
        LaunchState state,
        SpyBrowserLaunchOptions options) => new()
        {
            Headless = options.Headless,
            Channel = state.Channel,
            ExecutablePath = state.ExecutablePath,
            Args = state.Arguments.ToArray(),
            SlowMo = options.SlowMoMilliseconds,
            Proxy = ProxyConfiguration.Resolve(state.Identity.Network)
        };

    private static BrowserTypeLaunchPersistentContextOptions CreatePersistentContextOptions(
        LaunchState state,
        SpyBrowserLaunchOptions options) => new()
        {
            Headless = options.Headless,
            Channel = state.Channel,
            ExecutablePath = state.ExecutablePath,
            Args = state.Arguments.ToArray(),
            SlowMo = options.SlowMoMilliseconds,
            Locale = state.Identity.Locale,
            TimezoneId = state.Identity.TimezoneId,
            ViewportSize = CreateViewport(state.Identity),
            ScreenSize = CreateScreen(state.Identity),
            DeviceScaleFactor = state.Identity.Viewport.DeviceScaleFactor,
            UserAgent = state.Identity.Browser.UserAgent,
            Proxy = ProxyConfiguration.Resolve(state.Identity.Network),
            AcceptDownloads = true
        };

    private static T CloneOptions<T>(T options) where T : class
    {
        var clone = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (T)clone.Invoke(options, null)!;
    }

    private static BrowserNewContextOptions CreateContextOptions(BrowserIdentity identity)
    {
        var result = new BrowserNewContextOptions();
        ApplyContextDefaults(result, identity);
        return result;
    }

    private static void ApplyContextDefaults(BrowserNewContextOptions options, BrowserIdentity identity)
    {
        options.Locale ??= identity.Locale;
        options.TimezoneId ??= identity.TimezoneId;
        options.ViewportSize ??= CreateViewport(identity);
        options.ScreenSize ??= CreateScreen(identity);
        options.DeviceScaleFactor ??= identity.Viewport.DeviceScaleFactor;
        options.UserAgent ??= identity.Browser.UserAgent;
        options.AcceptDownloads ??= true;
    }

    private static BrowserNewPageOptions CreatePageOptions(BrowserIdentity identity)
    {
        var result = new BrowserNewPageOptions();
        ApplyPageDefaults(result, identity);
        return result;
    }

    private static void ApplyPageDefaults(BrowserNewPageOptions options, BrowserIdentity identity)
    {
        options.Locale ??= identity.Locale;
        options.TimezoneId ??= identity.TimezoneId;
        options.ViewportSize ??= CreateViewport(identity);
        options.ScreenSize ??= CreateScreen(identity);
        options.DeviceScaleFactor ??= identity.Viewport.DeviceScaleFactor;
        options.UserAgent ??= identity.Browser.UserAgent;
        options.AcceptDownloads ??= true;
    }

    private static ViewportSize CreateViewport(BrowserIdentity identity) => new()
    {
        Width = identity.Viewport.Width,
        Height = identity.Viewport.Height
    };

    private static ScreenSize CreateScreen(BrowserIdentity identity) => new()
    {
        Width = identity.Viewport.ScreenWidth,
        Height = identity.Viewport.ScreenHeight
    };

    private static async Task<PreparedContext> PrepareContextAsync(
        IBrowserContext context,
        LaunchState state,
        SpyBrowserLaunchOptions options,
        CancellationToken cancellationToken)
    {
        await ConfigureContextRuntimeAsync(context, state, options).ConfigureAwait(false);
        BrowserSurfaceDiagnostics? diagnostics = null;
        var shouldProbe = options.RunGpuProbe ||
            state.GpuPolicy is GpuPolicy.RequireHardware or GpuPolicy.ExperimentalMask;
        if (shouldProbe)
        {
            // Never navigate or reuse an application/restored tab for diagnostics.
            var probePage = await context.NewPageAsync().ConfigureAwait(false);
            try
            {
                probePage.SetDefaultTimeout(10_000);
                diagnostics = await GpuProbe.RunAsync(probePage, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try { await probePage.CloseAsync().ConfigureAwait(false); }
                catch { /* Keep probe/cancellation failures primary; context teardown owns final cleanup. */ }
            }
        }

        var consistency = diagnostics is null
            ? new ConsistencyReport(Array.Empty<ConsistencyFinding>())
            : IdentityConsistencyValidator.Validate(state.Identity, state.GpuPolicy, diagnostics);

        if (options.FailOnConsistencyErrors && consistency.HasErrors)
        {
            throw new InvalidOperationException(
                "SpyBrowser identity consistency validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, consistency.Findings
                    .Where(finding => finding.Severity == ConsistencySeverity.Error)
                    .Select(finding => $"- [{finding.Code}] {finding.Message}")));
        }

        return new PreparedContext(diagnostics, consistency);
    }

    private static async Task ConfigureContextRuntimeAsync(
        IBrowserContext context,
        LaunchState state,
        SpyBrowserLaunchOptions options)
    {
        context.SetDefaultTimeout(options.DefaultTimeoutMilliseconds);
        context.SetDefaultNavigationTimeout(options.DefaultNavigationTimeoutMilliseconds);
        var initScript = IdentityInitScriptBuilder.Build(state.Identity, state.GpuPolicy);
        if (!string.IsNullOrWhiteSpace(initScript))
        {
            await context.AddInitScriptAsync(initScript).ConfigureAwait(false);
        }
    }

    private static IBrowserType ResolveBrowserType(IPlaywright playwright, BrowserEngine engine) =>
        engine switch
        {
            BrowserEngine.Chrome => playwright.Chromium,
            BrowserEngine.Chromium => playwright.Chromium,
            BrowserEngine.Edge => playwright.Chromium,
            BrowserEngine.CustomChromium => playwright.Chromium,
            _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "Unknown browser engine.")
        };

    private static string? ResolveChannel(
        BrowserIdentity identity,
        string? channelOverride,
        string? executablePath)
    {
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(channelOverride))
        {
            return channelOverride;
        }

        return identity.Browser.Engine switch
        {
            BrowserEngine.Chrome => identity.Browser.Channel ?? "chrome",
            BrowserEngine.Edge => identity.Browser.Channel ?? "msedge",
            BrowserEngine.Chromium or BrowserEngine.CustomChromium => null,
            _ => identity.Browser.Channel
        };
    }

    private static void ValidateOptions(SpyBrowserLaunchOptions options)
    {
        _ = IdentityId.Normalize(options.IdentityId);
        if (options.DefaultTimeoutMilliseconds is < 1 or > 3_600_000)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultTimeoutMilliseconds));
        }

        if (options.DefaultNavigationTimeoutMilliseconds is < 1 or > 3_600_000)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultNavigationTimeoutMilliseconds));
        }

        if (options.ExtraArguments.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Extra browser arguments cannot contain empty values.", nameof(options));
        }

        if (options.UserDataDirectoryOverride is not null &&
            string.IsNullOrWhiteSpace(options.UserDataDirectoryOverride))
        {
            throw new ArgumentException("The user-data directory override cannot be empty.", nameof(options));
        }
    }

    private sealed record LaunchState(
        IPlaywright Playwright,
        IBrowserType BrowserType,
        BrowserIdentity Identity,
        GpuPolicy GpuPolicy,
        string? Channel,
        string? ExecutablePath,
        IReadOnlyList<string> Arguments,
        PlaywrightHumanizer? Humanizer);

    private sealed record PreparedContext(
        BrowserSurfaceDiagnostics? Diagnostics,
        ConsistencyReport Consistency);
}
