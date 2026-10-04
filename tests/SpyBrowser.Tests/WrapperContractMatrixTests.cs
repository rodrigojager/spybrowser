using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

/// <summary>Integration matrix for the public wrapper contracts; all pages are served from loopback.</summary>
public sealed class WrapperContractMatrixTests
{
    [BrowserFact]
    public async Task Configured_factories_apply_identity_precedence_without_mutating_options_and_allow_concurrency()
    {
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("wrapper-matrix-factory") with
        {
            Locale = "fr-FR", TimezoneId = "Europe/Paris",
            Viewport = new ViewportIdentity { Width = 913, Height = 617, ScreenWidth = 1100, ScreenHeight = 900, DeviceScaleFactor = 1.25f },
            Browser = new BrowserIdentitySettings { Engine = BrowserEngine.Chromium, Channel = null, UserAgent = "SpyBrowser-Matrix-Default/1" }
        };
        BrowserNewContextOptions? contextCallbackOptions = null;
        BrowserNewPageOptions? pageCallbackOptions = null;
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temp.Path,
            Headless = true, RunGpuProbe = false, Humanize = true,
            DefaultTimeoutMilliseconds = 1350, DefaultNavigationTimeoutMilliseconds = 2460,
            GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false,
            ConfigureContext = options => { options.Locale = "de-DE"; options.TimezoneId = "UTC"; contextCallbackOptions = options; },
            ConfigurePage = options => { options.Locale = "it-IT"; options.TimezoneId = "UTC"; pageCallbackOptions = options; }
        });

        var supplied = new BrowserNewContextOptions
        {
            Locale = "en-GB", TimezoneId = "America/New_York", ViewportSize = new ViewportSize { Width = 777, Height = 555 },
            ScreenSize = new ScreenSize { Width = 888, Height = 666 }, DeviceScaleFactor = 1.5f,
            UserAgent = "SpyBrowser-Matrix-Explicit/2", IgnoreHTTPSErrors = true, AcceptDownloads = false
        };
        var before = (Locale: supplied.Locale, Timezone: supplied.TimezoneId, Width: supplied.ViewportSize!.Width,
            Height: supplied.ViewportSize.Height, ScreenWidth: supplied.ScreenSize!.Width, ScreenHeight: supplied.ScreenSize.Height,
            Scale: supplied.DeviceScaleFactor, Agent: supplied.UserAgent, Ignore: supplied.IgnoreHTTPSErrors, Downloads: supplied.AcceptDownloads);
        var context = await handle.Browser.NewContextAsync(supplied);
        Assert.Equal(before, (supplied.Locale, supplied.TimezoneId, supplied.ViewportSize!.Width, supplied.ViewportSize.Height,
            supplied.ScreenSize!.Width, supplied.ScreenSize.Height, supplied.DeviceScaleFactor, supplied.UserAgent, supplied.IgnoreHTTPSErrors, supplied.AcceptDownloads));
        Assert.Equal("de-DE", contextCallbackOptions!.Locale); // callback wins over caller options
        Assert.True(contextCallbackOptions.IgnoreHTTPSErrors);
        Assert.False(contextCallbackOptions.AcceptDownloads);
        var page = await context.NewPageAsync();
        Assert.Equal("de-DE", await page.EvaluateAsync<string>("navigator.language"));
        Assert.Equal("UTC", await page.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone"));
        Assert.Equal("SpyBrowser-Matrix-Explicit/2", await page.EvaluateAsync<string>("navigator.userAgent"));
        Assert.Equal(777, await page.EvaluateAsync<int>("innerWidth"));
        Assert.Equal(555, await page.EvaluateAsync<int>("innerHeight"));
        Assert.Equal(1.5, await page.EvaluateAsync<double>("devicePixelRatio"));
        var timeoutClock = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(() => page.Locator("#never-created").WaitForAsync());
        Assert.InRange(timeoutClock.ElapsedMilliseconds, 900, 3500); // configured default action timeout, not a hard-coded message

        var pageOptions = new BrowserNewPageOptions
        {
            Locale = "es-ES", TimezoneId = "Asia/Tokyo", ViewportSize = new ViewportSize { Width = 811, Height = 611 },
            ScreenSize = new ScreenSize { Width = 900, Height = 700 }, DeviceScaleFactor = 1.25f,
            UserAgent = "SpyBrowser-Matrix-Page/3", AcceptDownloads = false
        };
        var standalone = await handle.Browser.NewPageAsync(pageOptions);
        Assert.Equal("it-IT", pageCallbackOptions!.Locale);
        Assert.Equal("it-IT", await standalone.EvaluateAsync<string>("navigator.language"));
        Assert.Equal("UTC", await standalone.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone"));
        Assert.Equal("SpyBrowser-Matrix-Page/3", await standalone.EvaluateAsync<string>("navigator.userAgent"));
        Assert.Equal(811, await standalone.EvaluateAsync<int>("innerWidth"));
        Assert.Equal(1.25, await standalone.EvaluateAsync<double>("devicePixelRatio"));
        Assert.Equal("es-ES", pageOptions.Locale); // callbacks and defaults operate on clones

        var leftTask = handle.Browser.NewContextAsync(new BrowserNewContextOptions { Locale = "nl-NL" });
        var rightTask = handle.NewContextAsync(new BrowserNewContextOptions { Locale = "sv-SE" });
        var pair = await Task.WhenAll(leftTask, rightTask);
        Assert.Equal("de-DE", await (await pair[0].NewPageAsync()).EvaluateAsync<string>("navigator.language"));
        Assert.Equal("de-DE", await (await pair[1].NewPageAsync()).EvaluateAsync<string>("navigator.language"));
        await Task.WhenAll(pair.Select(c => c.CloseAsync()));
        await context.CloseAsync();
        await standalone.Context.CloseAsync();
        await handle.DisposeAsync();
        await handle.DisposeAsync();
    }

    [BrowserFact]
    public async Task Frame_collections_payloads_diagnostics_and_weak_lifetimes_follow_contract()
    {
        using var site = new WrapperContractSite();
        using var peerSite = new WrapperContractSite();
        site.PeerUrl = peerSite.Url;
        await site.StartAsync();
        await peerSite.StartAsync();
        using var temp = new TemporaryDirectory();
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-matrix-frame", true));
        var context = await handle.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(site.Url + "frames");

        var wrappedRawPage = PlaywrightHumanizer.Unwrap(page);
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true, CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible });
        var diagnosticPage = humanizer.Wrap(wrappedRawPage);
        var diagnosticContext = humanizer.Wrap(PlaywrightHumanizer.Unwrap(context));
        var action = diagnosticPage.FrameLocator("#outer").FrameLocator("#inner").Locator("#policy-action");
        await action.ClickAsync();
        Assert.Equal("done", await action.GetAttributeAsync("data-result"));
        var report = humanizer.GetDiagnosticsSnapshot();
        Assert.Contains(report!.Records, call => call.Method == "ClickAsync" && call.Reason == "humanization.preparation-or-paced-action");

        var dto = new PayloadDto { LocatorLike = action, Value = "untouched" };
        Assert.Same(dto, humanizer.Wrap(dto));
        Assert.Same(action, dto.LocatorLike); // arbitrary user payload is not recursively rewritten
        var returned = await diagnosticPage.EvaluateAsync<string>("() => JSON.stringify({nested:{value:1}})");
        Assert.Equal("{\"nested\":{\"value\":1}}", returned);
        Assert.Same(diagnosticPage.MainFrame, diagnosticPage.MainFrame);
        Assert.Same(diagnosticPage, action.Page);
        Assert.Same(diagnosticContext, diagnosticPage.Context);
        var all = await diagnosticPage.FrameLocator("#outer").FrameLocator("#inner").Locator(".item").AllAsync();
        Assert.All(all, locator => Assert.Same(diagnosticPage, locator.Page));
        Assert.Equal("done", await page.FrameLocator("#outer").FrameLocator("#inner").Locator("#policy-action").GetAttributeAsync("data-result"));

        var weakRefs = await CreateAndCloseDisposableContextAsync(handle, site.Url + "frames");
        ForceBoundedCollection();
        Assert.False(weakRefs.Context.IsAlive, "Closed context remained rooted after all local strong references left scope.");
        Assert.False(weakRefs.Page.IsAlive, "Closed page remained rooted after all local strong references left scope.");
        await context.CloseAsync();

        await using var rawMode = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-matrix-frame-raw", false));
        var rawContext = await rawMode.Browser.NewContextAsync();
        var rawPage = await rawContext.NewPageAsync();
        await rawPage.GotoAsync(site.Url + "frames");
        var rawAction = rawPage.FrameLocator("#outer").FrameLocator("#inner").Locator("#policy-action");
        Assert.Same(rawAction, PlaywrightHumanizer.Unwrap(rawAction));
        await rawAction.ClickAsync();
        Assert.Equal("done", await rawAction.GetAttributeAsync("data-result"));
        await rawContext.CloseAsync();
    }

    [BrowserFact]
    public async Task Context_and_popup_events_are_snapshots_reentrant_and_preserve_wait_contract()
    {
        using var site = new WrapperContractSite();
        await site.StartAsync();
        using var temp = new TemporaryDirectory();
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-matrix-events", true));
        var browserCalls = 0;
        EventHandler<IBrowserContext> browserHandler = (sender, args) =>
        {
            Assert.Same(handle.Browser, sender);
            Assert.Contains(handle.Browser.Contexts, candidate => ReferenceEquals(candidate, args));
            browserCalls++;
        };
        handle.Browser.Context += browserHandler;
        handle.Browser.Context += browserHandler;
        var context = await handle.Browser.NewContextAsync();
        Assert.Equal(2, browserCalls);
        var collectionSnapshot = handle.Browser.Contexts;
        Assert.IsAssignableFrom<IReadOnlyList<IBrowserContext>>(collectionSnapshot);
        if (collectionSnapshot is IList<IBrowserContext> mutableSnapshot && !mutableSnapshot.IsReadOnly)
            mutableSnapshot.Clear();
        Assert.Contains(handle.Browser.Contexts, candidate => ReferenceEquals(candidate, context));

        var page = await context.NewPageAsync();
        await page.GotoAsync(site.Url + "opener");
        var pageCallbacks = 0;
        IPage? popupFromEvent = null;
        EventHandler<IPage> popupHandler = (sender, popup) =>
        {
            Assert.Same(page, sender);
            Assert.Contains(context.Pages, candidate => ReferenceEquals(candidate, popup));
            popupFromEvent = popup;
            _ = context.Pages.Count; // re-entry demonstrates user callbacks run outside registry locks
            Interlocked.Increment(ref pageCallbacks);
        };
        page.Popup += popupHandler;
        page.Popup += popupHandler;
        var popup = await page.RunAndWaitForPopupAsync(() => page.Locator("#open-work").ClickAsync());
        Assert.Equal(2, pageCallbacks);
        Assert.Same(popup, popupFromEvent);
        Assert.Contains(context.Pages, candidate => ReferenceEquals(candidate, popup));
        await popup.Locator("#popup-action").ClickAsync();
        Assert.Equal("acted", await popup.Locator("#popup-action").GetAttributeAsync("data-result"));
        page.Popup -= popupHandler; // .NET event removal removes the last duplicate registration
        var waiting = page.WaitForPopupAsync(new PageWaitForPopupOptions { Timeout = 175 });
        var wrappedError = await Assert.ThrowsAsync<TimeoutException>(() => waiting);
        Assert.False(string.IsNullOrWhiteSpace(wrappedError.Message));
        page.Popup -= popupHandler;
        var rawPage = PlaywrightHumanizer.Unwrap(page);
        var rawError = await Assert.ThrowsAsync<TimeoutException>(() => rawPage.WaitForPopupAsync(new PageWaitForPopupOptions { Timeout = 175 }));
        Assert.Equal(wrappedError.GetType(), rawError.GetType());
        Assert.Contains("Timeout", wrappedError.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timeout", rawError.Message, StringComparison.OrdinalIgnoreCase);
        await popup.CloseAsync();
        Assert.DoesNotContain(context.Pages, candidate => ReferenceEquals(candidate, popup));

        var immediatelyClosed = new TaskCompletionSource<IPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IPage> observeClosingPage = (_, observed) => immediatelyClosed.TrySetResult(observed);
        context.Page += observeClosingPage;
        await page.EvaluateAsync("() => { const child = window.open('/popup'); setTimeout(() => child.close(), 0); }");
        var racingPopup = await immediatelyClosed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(context, racingPopup.Context);
        await Task.Delay(100);
        if (racingPopup.IsClosed) Assert.DoesNotContain(context.Pages, candidate => ReferenceEquals(candidate, racingPopup));
        context.Page -= observeClosingPage;

        handle.Browser.Context -= browserHandler;
        handle.Browser.Context -= browserHandler;
        await context.CloseAsync();
        Assert.DoesNotContain(handle.Browser.Contexts, candidate => ReferenceEquals(candidate, context));
        await handle.DisposeAsync();
        await handle.DisposeAsync();
    }

    [BrowserFact]
    public async Task Wrapped_known_playwright_locator_argument_inside_options_is_unwrapped()
    {
        using var temp = new TemporaryDirectory();
        using var site = new WrapperContractSite();
        await site.StartAsync();
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-matrix-argument", true));
        var context = await handle.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(site.Url + "frames");
        var humanizer = new PlaywrightHumanizer();
        var wrapped = humanizer.Wrap(PlaywrightHumanizer.Unwrap(page));
        var knownLocatorArgument = wrapped.FrameLocator("#outer").FrameLocator("#inner").Locator("#policy-action");
        var filtered = wrapped.Locator("body").Locator("button", new LocatorLocatorOptions { Has = knownLocatorArgument });
        Assert.Equal("policy-action", await filtered.GetAttributeAsync("id"));
        await context.CloseAsync();
    }

    [BrowserFact]
    public async Task Failed_context_preparation_preserves_callback_exception_and_publishes_nothing()
    {
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("wrapper-matrix-failure");
        var failure = new InvalidOperationException("configured-context sentinel");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temp.Path,
            Headless = true, RunGpuProbe = false, Humanize = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false,
            ConfigureContext = _ => throw failure
        });
        var announcements = 0;
        handle.Browser.Context += (_, _) => announcements++;
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => handle.Browser.NewContextAsync());
        Assert.Same(failure, caught);
        Assert.Equal(0, announcements);
        Assert.Empty(handle.Browser.Contexts);
    }

    private static SpyBrowserLaunchOptions Options(string root, string id, bool humanize) => new()
    {
        IdentityId = id, IdentityOverride = BrowserIdentity.Create(id) with
        {
            Browser = new BrowserIdentitySettings { Engine = BrowserEngine.Chromium, Channel = null }
        }, IdentitiesRoot = root, Headless = true, RunGpuProbe = false, Humanize = humanize,
        HumanInteraction = new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseMinimumDurationMilliseconds = 0, MouseMaximumDurationMilliseconds = 0,
            ClickHoldMinimumMilliseconds = 0, ClickHoldMaximumMilliseconds = 0,
            ThinkingPauseProbability = 0, ThinkingPauseMinimumMilliseconds = 0, ThinkingPauseMaximumMilliseconds = 0
        }, GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Context, WeakReference Page)> CreateAndCloseDisposableContextAsync(SpyBrowserBrowserHandle handle, string url)
    {
        var context = await handle.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(url);
        var contextReference = new WeakReference(context);
        var pageReference = new WeakReference(page);
        await context.CloseAsync();
        return (contextReference, pageReference);
    }

    private static void ForceBoundedCollection()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(25);
        }
    }

    private sealed class PayloadDto
    {
        public object? LocatorLike { get; init; }
        public string Value { get; init; } = string.Empty;
    }
}
