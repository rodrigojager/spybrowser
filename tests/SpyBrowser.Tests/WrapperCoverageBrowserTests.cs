using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class WrapperCoverageBrowserFactAttribute : FactAttribute
{
    public WrapperCoverageBrowserFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"), "1", StringComparison.Ordinal))
            Skip = "Set SPYBROWSER_RUN_BROWSER_TESTS=1 to run browser-backed wrapper coverage.";
    }
}

public sealed class WrapperCoverageBrowserTests
{
    [WrapperCoverageBrowserFact]
    public async Task Nested_cross_origin_frames_collections_and_references_keep_one_wrapper_identity()
    {
        using var originA = new LoopbackSite();
        using var originB = new LoopbackSite();
        originA.PeerUrl = originB.Url;
        await originA.StartAsync();
        await originB.StartAsync();
        using var temp = new TemporaryDirectory();
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-frames", true));
        var context = await handle.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(originA.Url + "root");

        var nestedButton = page.FrameLocator("#outer").FrameLocator("#inner").Locator("#go");
        Assert.NotSame(nestedButton, PlaywrightHumanizer.Unwrap(nestedButton));
        var humanizer = new PlaywrightHumanizer();
        Assert.Same(PlaywrightHumanizer.Unwrap(nestedButton), PlaywrightHumanizer.Unwrap(humanizer.Wrap(nestedButton)));
        Assert.Same(nestedButton, humanizer.Wrap(nestedButton));
        Assert.Same(page, nestedButton.Page);
        Assert.Same(context, page.Context);
        Assert.Same(handle.Browser, context.Browser);
        var frames = page.Frames;
        Assert.Same(page.MainFrame, frames[0]);
        Assert.Same(frames[1], page.Frames[1]);
        Assert.Same(frames[2], page.Frames[2]);
        Assert.Same(frames[0], frames[1].ParentFrame);
        Assert.Same(frames[1], frames[2].ParentFrame);

        await nestedButton.ClickAsync();
        Assert.Equal("yes", await page.FrameLocator("#outer").FrameLocator("#inner").Locator("#go").GetAttributeAsync("data-clicked"));
        var all = await page.FrameLocator("#outer").FrameLocator("#inner").Locator(".item").AllAsync();
        Assert.Equal(2, all.Count);
        Assert.All(all, item => Assert.NotSame(item, PlaywrightHumanizer.Unwrap(item)));
        Assert.All(all, item => Assert.Same(page, item.Page));

        await context.CloseAsync();
    }

    [WrapperCoverageBrowserFact]
    public async Task Popup_events_waits_concurrent_closes_and_disabled_mode_preserve_contract()
    {
        using var site = new LoopbackSite();
        site.PeerUrl = site.Url;
        await site.StartAsync();
        using var temp = new TemporaryDirectory();
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-popups", true));
        var context = await handle.Browser.NewContextAsync();
        IPage? announced = null;
        var pageEventCount = 0;
        EventHandler<IPage> onPage = (_, value) => { announced = value; pageEventCount++; };
        context.Page += onPage;
        context.Page += onPage;
        var opener = await context.NewPageAsync();
        await opener.GotoAsync(site.Url + "opener");
        Assert.Same(opener, announced);
        Assert.Equal(2, pageEventCount);
        Assert.Same(opener, context.Pages.Single());
        context.Page -= onPage;

        IPage? popupEvent = null;
        var popupEventCount = 0;
        EventHandler<IPage> onPopup = (_, value) => { popupEvent = value; popupEventCount++; };
        opener.Popup += onPopup;
        opener.Popup += onPopup;
        var popup = await opener.RunAndWaitForPopupAsync(() => opener.Locator("#open-one").ClickAsync());
        Assert.Same(popup, popupEvent);
        Assert.Equal(2, popupEventCount);
        Assert.Equal(3, pageEventCount);
        Assert.Same(popup, context.Pages.Single(p => ReferenceEquals(p, popup)));
        var waitForSecond = opener.WaitForPopupAsync();
        await opener.Locator("#open-two").ClickAsync();
        var secondPopup = await waitForSecond;
        Assert.Same(secondPopup, popupEvent);
        Assert.Equal(4, popupEventCount);
        Assert.Equal(4, pageEventCount);
        Assert.NotSame(popup, secondPopup);
        opener.Popup -= onPopup;
        opener.Popup -= onPopup;
        context.Page -= onPage;

        // Concurrent popup creation and immediate closure must not alias the two new pages.
        var arrived = new List<IPage>();
        var twoPopups = new TaskCompletionSource<IPage[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IPage> collectPopups = (_, value) =>
        {
            if (ReferenceEquals(value, opener)) return;
            lock (arrived)
            {
                arrived.Add(value);
                if (arrived.Count >= 2) twoPopups.TrySetResult(arrived.Take(2).ToArray());
            }
        };
        context.Page += collectPopups;
        await opener.EvaluateAsync("() => { window.open('/popup?one'); window.open('/popup?two'); }");
        var arrivals = await twoPopups.Task.WaitAsync(TimeSpan.FromSeconds(15));
        context.Page -= collectPopups;
        Assert.Equal(4, popupEventCount);
        Assert.Equal(4, pageEventCount);
        Assert.NotSame(arrivals[0], arrivals[1]);
        Assert.All(arrivals, p => Assert.Contains(context.Pages, candidate => ReferenceEquals(candidate, p)));
        await Task.WhenAll(arrivals.Select(p => p.CloseAsync()));
        Assert.DoesNotContain(context.Pages, p => arrivals.Any(closed => ReferenceEquals(closed, p)));
        await context.CloseAsync();

        await using var rawHandle = await SpyBrowserLauncher.LaunchBrowserAsync(Options(temp.Path, "wrapper-popup-raw", false));
        var rawContext = await rawHandle.Browser.NewContextAsync();
        IPage? rawAnnounced = null;
        rawContext.Page += (_, p) => rawAnnounced = p;
        var rawOpener = await rawContext.NewPageAsync();
        await rawOpener.GotoAsync(site.Url + "opener");
        Assert.Same(rawOpener, rawAnnounced);
        Assert.Same(rawOpener, PlaywrightHumanizer.Unwrap(rawOpener));
        IPage? rawPopupEvent = null;
        rawOpener.Popup += (_, p) => rawPopupEvent = p;
        var rawPopup = await rawOpener.RunAndWaitForPopupAsync(() => rawOpener.Locator("#open-one").ClickAsync());
        Assert.Same(rawPopup, rawPopupEvent);
        Assert.Same(rawPopup, PlaywrightHumanizer.Unwrap(rawPopup));
        Assert.Contains(rawContext.Pages, p => ReferenceEquals(p, rawPopup));
        await rawContext.CloseAsync();
    }

    private static SpyBrowserLaunchOptions Options(string root, string id, bool humanize)
    {
        var executable = Environment.GetEnvironmentVariable("SPYBROWSER_CHROMIUM_EXECUTABLE");
        return new SpyBrowserLaunchOptions
        {
            IdentityId = id,
            IdentityOverride = BrowserIdentity.Create(id),
            IdentitiesRoot = root,
            Headless = true,
            RunGpuProbe = false,
            Humanize = humanize,
            HumanInteraction = new HumanInteractionOptions
            {
                MouseMinimumDurationMilliseconds = 0,
                MouseMaximumDurationMilliseconds = 0,
                ClickHoldMinimumMilliseconds = 0,
                ClickHoldMaximumMilliseconds = 0,
                ThinkingPauseProbability = 0,
                ThinkingPauseMinimumMilliseconds = 0,
                ThinkingPauseMaximumMilliseconds = 0
            },
            ExecutablePathOverride = string.IsNullOrWhiteSpace(executable) ? null : executable,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        };
    }
}
