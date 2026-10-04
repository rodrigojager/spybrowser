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
    }
}
