using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class WrapperFactoryTests
{
    [Fact]
    public async Task Public_browser_factories_events_frames_and_popups_share_wrappers()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"), "1", StringComparison.Ordinal)) return;
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("wrapper-factory");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temp.Path,
            Headless = true, RunGpuProbe = false, Humanize = true,
            GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false
        });

        IBrowserContext? announced = null;
        handle.Browser.Context += (_, context) => announced = context;
        var supplied = new BrowserNewContextOptions { IgnoreHTTPSErrors = true, Locale = "en-US" };
        var context = await handle.Browser.NewContextAsync(supplied);
        Assert.Same(context, announced);
        Assert.Equal("en-US", supplied.Locale);
        Assert.Same(context, handle.Browser.Contexts.Single());

        IPage? announcedPage = null;
        context.Page += (_, page) => announcedPage = page;
        var page = await context.NewPageAsync();
        Assert.Same(page, announcedPage);
        Assert.Same(context, page.Context);
        Assert.Equal("en-US", await page.EvaluateAsync<string>("navigator.language"));
        await page.SetContentAsync("<iframe id='child' srcdoc=\"<button id='go'>go</button>\"></iframe><button id='open' onclick=\"window.open('about:blank')\">open</button>");
        var frameButton = page.FrameLocator("#child").Locator("#go");
        await frameButton.ClickAsync();
        Assert.Same(page, frameButton.Page);

        IPage? popupEventPage = null;
        page.Popup += (_, popup) => popupEventPage = popup;
        var popup = await page.RunAndWaitForPopupAsync(async () => await page.Locator("#open").ClickAsync());
        Assert.Same(popup, popupEventPage);
        Assert.Contains(context.Pages, item => ReferenceEquals(item, popup));
        var popupWait = page.WaitForPopupAsync();
        await page.Locator("#open").ClickAsync();
        Assert.NotSame(popup, await popupWait);

        await context.CloseAsync();
        Assert.Empty(handle.Browser.Contexts);
    }

    [Fact]
    public async Task Humanize_disabled_browser_factory_returns_raw_playwright_objects()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"), "1", StringComparison.Ordinal)) return;
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("wrapper-raw");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temp.Path,
            Headless = true, RunGpuProbe = false, Humanize = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false
        });
        var page = await handle.Browser.NewPageAsync();
        Assert.Same(page, PlaywrightHumanizer.Unwrap(page));
        Assert.Equal(identity.Locale, await page.EvaluateAsync<string>("navigator.language"));
        await page.Context.CloseAsync();
    }
}
