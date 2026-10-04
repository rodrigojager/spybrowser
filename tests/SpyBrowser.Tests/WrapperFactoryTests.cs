using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class WrapperFactoryTests
{
    [BrowserFact]
    public async Task Public_browser_factories_events_frames_and_popups_share_wrappers()
    {
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
        Assert.Same(handle.Browser, context.Browser);
        Assert.Equal("en-US", await page.EvaluateAsync<string>("navigator.language"));
        await page.SetContentAsync("<iframe id='child' srcdoc=\"<iframe id='nested' srcdoc='<button id=go>go</button>'></iframe>\"></iframe><button class='go'>main</button><button id='open' onclick=\"window.open('about:blank')\">open</button>");

        var frameButton = page.FrameLocator("#child").FrameLocator("#nested").Locator("#go");
        await frameButton.ClickAsync();
        Assert.Same(page, frameButton.Page);
        var frames = page.Frames;
        Assert.Same(page.MainFrame, frames[0]);
        Assert.Equal(3, frames.Count);
        Assert.Same(frames[2], page.Frames[2]);
        var locators = await page.Locator(".go").AllAsync();
        Assert.Single(locators);
        await locators[0].ClickAsync();
        var rawLocator = PlaywrightHumanizer.Unwrap(locators[0]);
        Assert.Same(rawLocator, PlaywrightHumanizer.Unwrap(rawLocator));

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

    [BrowserFact]
    public async Task Humanize_disabled_browser_factory_returns_raw_playwright_objects()
    {
        using var temp = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("wrapper-raw");
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temp.Path,
            Headless = true, RunGpuProbe = false, Humanize = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware, FailOnConsistencyErrors = false
        });
        IBrowserContext? announced = null;
        handle.Browser.Context += (_, context) => announced = context;
        var rawContext = await handle.RawBrowser.NewContextAsync();
        Assert.Null(announced); // RawBrowser explicitly bypasses configured event publication.
        await rawContext.CloseAsync();
        var page = await handle.NewPageAsync();
        Assert.Same(page.Context, announced);
        Assert.Same(page, PlaywrightHumanizer.Unwrap(page));
        Assert.Same(page.Context, PlaywrightHumanizer.Unwrap(page.Context));
        Assert.Equal(identity.Locale, await page.EvaluateAsync<string>("navigator.language"));
        await page.Context.CloseAsync();
    }
}
