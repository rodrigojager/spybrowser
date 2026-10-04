using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class ProbeIsolationBrowserTests
{
    [Fact]
    public async Task Probe_preserves_user_pages_and_concurrent_new_pages_in_all_launch_modes()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"), "1", StringComparison.Ordinal))
            return;

        using var temporary = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("probe-isolation");
        var options = new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = temporary.Path,
            Headless = true,
            Humanize = true,
            RunGpuProbe = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        };

        await using (var persistent = await SpyBrowserLauncher.LaunchPersistentContextAsync(options))
            await VerifyProbeIsolationAsync(persistent);
        await using (var context = await SpyBrowserLauncher.LaunchContextAsync(options))
            await VerifyProbeIsolationAsync(context);
        await using var browser = await SpyBrowserLauncher.LaunchBrowserAsync(options);
        var browserContext = await browser.NewContextAsync();
        try
        {
            var userPage = await browserContext.NewPageAsync();
            await userPage.GotoAsync("data:text/html,<title>browser-only</title>");
            var originalUrl = userPage.Url;
            await GpuProbe.RunAsync(browserContext, TimeSpan.FromSeconds(15));
            Assert.Equal(originalUrl, userPage.Url);
            Assert.Single(browserContext.Pages);
        }
        finally
        {
            await browserContext.CloseAsync();
        }
    }

    [Fact]
    public async Task Headed_probe_does_not_change_the_existing_pages_focus_or_active_element()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_RUN_HEADED_PROBE_TESTS"), "1", StringComparison.Ordinal))
            return;

        using var temporary = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("headed-probe-focus");
        var options = new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = temporary.Path,
            Headless = false,
            Humanize = true,
            RunGpuProbe = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        };

        await using var handle = await SpyBrowserLauncher.LaunchContextAsync(options);
        var page = await handle.NewPageAsync();
        await page.GotoAsync("data:text/html,<input id=focus value=preserve>");
        await page.Locator("#focus").FocusAsync();
        await page.BringToFrontAsync();
        var focusBefore = await page.EvaluateAsync<bool>("() => document.hasFocus()");
        var activeBefore = await page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''");

        await handle.RunGpuProbeAsync(TimeSpan.FromSeconds(15));

        Assert.True(focusBefore, "The headed baseline must have an actually focused browser page.");
        Assert.True(await page.EvaluateAsync<bool>("() => document.hasFocus()"));
        Assert.Equal(activeBefore, await page.EvaluateAsync<string>("() => document.activeElement?.id ?? ''"));
    }

    private static async Task VerifyProbeIsolationAsync(SpyBrowserContextHandle handle)
    {
        var first = await handle.NewPageAsync();
        var second = await handle.NewPageAsync();
        await first.GotoAsync("data:text/html,<title>first</title><input value=keep>");
        await second.GotoAsync("data:text/html,<title>second</title>");
        var pagesBefore = handle.Pages.ToArray();
        var urlsBefore = pagesBefore.Select(page => page.Url).ToArray();
        var inputBefore = await first.Locator("input").InputValueAsync();
        var userPageEvents = new List<IPage>();
        handle.Context.Page += (_, page) => userPageEvents.Add(page);

        var userCreation = handle.NewPageAsync();
        var probe = handle.RunGpuProbeAsync(TimeSpan.FromSeconds(15));
        var userPage = await userCreation;
        await userPage.GotoAsync("data:text/html,<title>concurrent</title>");
        var diagnostics = await probe;

        Assert.NotNull(diagnostics);
        Assert.Equal("keep", await first.Locator("input").InputValueAsync());
        Assert.Equal(inputBefore, await first.Locator("input").InputValueAsync());
        Assert.Equal(urlsBefore[0], pagesBefore[0].Url);
        Assert.Equal(urlsBefore[1], pagesBefore[1].Url);
        Assert.All(pagesBefore, page => Assert.Contains(handle.Pages, current => ReferenceEquals(page, current)));
        Assert.Contains(userPage, handle.Pages);
        Assert.Equal(pagesBefore.Length + 1, handle.Pages.Count);
        Assert.DoesNotContain(userPageEvents, page => !handle.Pages.Any(current => ReferenceEquals(current, page)));

        // Hold native probe creation open, then bypass the wrapped NewPage gate as a
        // browser-created popup would. Its publication must be deferred, not dropped.
        var registry = ProbePageRegistry.For(handle.RawContext);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IPage? probePage = null;
        registry.ProbePageFactory = async () =>
        {
            started.TrySetResult();
            await release.Task;
            probePage = await handle.RawContext.NewPageAsync();
            return probePage;
        };
        var deferredEvents = new List<IPage>();
        handle.Context.Page += (_, page) => deferredEvents.Add(page);
        try
        {
            var rawPopupEvent = new TaskCompletionSource<IPage>(TaskCreationOptions.RunContinuationsAsynchronously);
            void CaptureRawPopup(object? _, IPage page) => rawPopupEvent.TrySetResult(page);
            handle.RawContext.Page += CaptureRawPopup;
            var isolatedProbe = GpuProbe.RunAsync(handle.RawContext, TimeSpan.FromSeconds(15));
            await started.Task;
            await PlaywrightHumanizer.Unwrap(first).EvaluateAsync("() => window.open('about:blank')");
            var popup = await rawPopupEvent.Task.WaitAsync(TimeSpan.FromSeconds(5));
            handle.RawContext.Page -= CaptureRawPopup;
            Assert.Empty(deferredEvents);
            release.SetResult();
            await isolatedProbe;
            Assert.NotNull(probePage);
            Assert.True(registry.IsProbe(probePage));
            Assert.Single(deferredEvents);
            Assert.Same(PlaywrightHumanizer.Unwrap(popup), PlaywrightHumanizer.Unwrap(deferredEvents[0]));
            Assert.Contains(handle.Pages, page => ReferenceEquals(PlaywrightHumanizer.Unwrap(page), popup));
            Assert.DoesNotContain(handle.Pages, page => ReferenceEquals(PlaywrightHumanizer.Unwrap(page), probePage));
        }
        finally
        {
            release.TrySetResult();
            registry.ProbePageFactory = null;
        }

        var failedEvents = new List<IPage>();
        handle.Context.Page += (_, page) => failedEvents.Add(page);
        registry.ProbePageFactory = () => Task.FromException<IPage>(new InvalidOperationException("injected creation failure"));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => GpuProbe.RunAsync(handle.RawContext, TimeSpan.FromSeconds(5)));
            var afterFailure = await handle.NewPageAsync();
            Assert.Contains(afterFailure, failedEvents);
            Assert.Contains(afterFailure, handle.Pages);
        }
        finally
        {
            registry.ProbePageFactory = null;
        }

        var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCancelledCreation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledPage = new TaskCompletionSource<IPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        registry.ProbePageFactory = async () =>
        {
            cancellationStarted.TrySetResult();
            await allowCancelledCreation.Task;
            var page = await handle.RawContext.NewPageAsync();
            cancelledPage.TrySetResult(page);
            return page;
        };
        try
        {
            using var cancellation = new CancellationTokenSource();
            var cancelledProbe = GpuProbe.RunAsync(handle.RawContext, TimeSpan.FromSeconds(15), cancellation.Token);
            await cancellationStarted.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledProbe);
            allowCancelledCreation.SetResult();
            var lateCancelledPage = await cancelledPage.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (var attempt = 0; !lateCancelledPage.IsClosed && attempt < 50; attempt++) await Task.Delay(20);
            Assert.True(lateCancelledPage.IsClosed);
        }
        finally
        {
            allowCancelledCreation.TrySetResult();
            registry.ProbePageFactory = null;
        }

        var lateStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var createLate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var latePage = new TaskCompletionSource<IPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        registry.ProbePageFactory = async () =>
        {
            lateStarted.TrySetResult();
            await createLate.Task;
            var page = await handle.RawContext.NewPageAsync();
            latePage.TrySetResult(page);
            return page;
        };
        try
        {
            var timedOut = GpuProbe.RunAsync(handle.RawContext, TimeSpan.FromMilliseconds(100));
            await lateStarted.Task;
            await Assert.ThrowsAsync<TimeoutException>(() => timedOut);
            createLate.SetResult();
            var late = await latePage.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (var attempt = 0; !late.IsClosed && attempt < 50; attempt++) await Task.Delay(20);
            Assert.True(late.IsClosed);
        }
        finally
        {
            createLate.TrySetResult();
            registry.ProbePageFactory = null;
        }
    }
}
