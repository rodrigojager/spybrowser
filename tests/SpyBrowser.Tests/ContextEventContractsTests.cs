using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class ContextEventContractsTests
{
    [Fact]
    public async Task Context_events_follow_preparation_and_match_factory_and_collection_objects()
    {
        EnsureBrowserTestsEnabled();
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("context-events-ready");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(
            Options(temp.Path, identity, contextOptions => contextOptions.TimezoneId = "Pacific/Auckland"));

        IBrowserContext? announced = null;
        Task<IPage>? eventPage = null;
        handle.Browser.Context += (_, context) =>
        {
            announced = context;
            eventPage = context.NewPageAsync();
        };
        var context = await handle.Browser.NewContextAsync();
        Assert.Same(context, announced);
        Assert.Contains(handle.Browser.Contexts, candidate => ReferenceEquals(candidate, context));
        var page = await eventPage!;
        Assert.Equal("Pacific/Auckland", await page.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone"));
        await context.CloseAsync();
        Assert.DoesNotContain(handle.Browser.Contexts, candidate => ReferenceEquals(candidate, context));
    }

    [Fact]
    public async Task New_page_factory_announces_once_and_subscription_duplicates_remove_last_occurrence()
    {
        EnsureBrowserTestsEnabled();
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("context-events-pages");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, identity));
        var calls = 0;
        IBrowserContext? announced = null;
        EventHandler<IBrowserContext> handler = (_, context) => { calls++; announced = context; };
        handle.Browser.Context += handler;
        handle.Browser.Context += handler;

        var page = await handle.Browser.NewPageAsync();
        Assert.Equal(2, calls);
        Assert.Same(page.Context, announced);
        handle.Browser.Context -= handler;
        await handle.NewContextAsync();
        Assert.Equal(3, calls);
        handle.Browser.Context -= handler;
        await handle.Browser.NewContextAsync();
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Handler_exception_propagates_once_and_callbacks_can_reenter_browser()
    {
        EnsureBrowserTestsEnabled();
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("context-events-errors");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, identity));
        var calls = 0;
        handle.Browser.Context += (_, _) =>
        {
            calls++;
            _ = handle.Browser.Contexts.Count;
            throw new InvalidOperationException("event failure");
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => handle.Browser.NewContextAsync());
        Assert.Equal("event failure", error.Message);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Closing_browser_clears_managed_context_event_subscribers()
    {
        EnsureBrowserTestsEnabled();
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("context-events-close");
        var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, identity));
        var calls = 0;
        handle.Browser.Context += (_, _) => calls++;
        await handle.DisposeAsync();
        Assert.Equal(0, calls);
    }

    private static SpyBrowserLaunchOptions Options(
        string root,
        BrowserIdentity identity,
        Action<BrowserNewContextOptions>? configureContext = null) => new()
    {
        IdentityId = identity.Id,
        IdentityOverride = identity,
        IdentitiesRoot = root,
        Headless = true,
        RunGpuProbe = false,
        Humanize = true,
        GpuPolicyOverride = GpuPolicy.AllowSoftware,
        FailOnConsistencyErrors = false,
        ConfigureContext = configureContext
    };

    private static void EnsureBrowserTestsEnabled()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"));
    }
}
