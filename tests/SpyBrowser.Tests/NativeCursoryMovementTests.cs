using Microsoft.Playwright;
using SpyBrowser.Cursory;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class NativeCursoryMovementTests
{
    [BrowserTheory]
    [InlineData(1d)]
    [InlineData(1.25d)]
    [InlineData(1.5d)]
    [InlineData(2d)]
    public async Task Cursory_move_reaches_fractional_endpoint_in_local_dom_at_device_scale(double scale)
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        await using var context = await runtime.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 600, Height = 400 },
            DeviceScaleFactor = (float)scale
        });
        var rawPage = await context.NewPageAsync();
        await rawPage.SetContentAsync("<div id='target' style='width:100vw;height:100vh'></div><script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 100,
            MouseMaximumDurationMilliseconds = 1_500,
            CursoryMovementDeadlineMilliseconds = 2_000
        });
        var page = humanizer.Wrap(rawPage);

        // Unknown cursor: one native endpoint anchor, never an invented viewport-center start.
        await page.Mouse.MoveAsync(30.25f, 40.5f);
        var anchorEvents = await rawPage.EvaluateAsync<int>("moves.length");
        Assert.Equal(1, anchorEvents);

        // An explicit raw Playwright option is preserved and observed before the next generated move.
        await page.Mouse.MoveAsync(80.5f, 95.25f, new MouseMoveOptions { Steps = 1 });
        await page.Mouse.MoveAsync(420.75f, 275.125f);
        var events = await rawPage.EvaluateAsync<double[][]>("moves");

        Assert.Equal(scale, await rawPage.EvaluateAsync<double>("devicePixelRatio"));
        Assert.True(events.Length > anchorEvents + 2);
        // The adapter preserves fractional CSS coordinates; this Playwright/Chromium combination
        // reports integer clientX/clientY values in DOM MouseEvent, independent of device scale.
        Assert.InRange(Math.Abs(420.75 - events[^1][0]), 0, 1);
        Assert.InRange(Math.Abs(275.125 - events[^1][1]), 0, 1);

        await page.Mouse.MoveAsync(0.25f, 0.5f);
        await page.Mouse.MoveAsync(0.75f, 0.875f); // subpixel-distance movement near viewport edges
        events = await rawPage.EvaluateAsync<double[][]>("moves");
        Assert.InRange(Math.Abs(0.75 - events[^1][0]), 0, 1);
        Assert.InRange(Math.Abs(0.875 - events[^1][1]), 0, 1);
        Assert.Contains(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly == typeof(CursoryTrajectoryGenerator).Assembly);
    }

    [BrowserFact]
    public async Task Cursory_unknown_position_and_observed_pressed_button_use_single_native_moves()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory });
        var page = humanizer.Wrap(rawPage);

        await page.Mouse.MoveAsync(10, 11);
        Assert.Equal(1, await rawPage.EvaluateAsync<int>("moves.length"));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(20, 21);
        Assert.Equal(2, await rawPage.EvaluateAsync<int>("moves.length"));
        await page.Mouse.UpAsync();
        await page.Mouse.MoveAsync(100, 120);
        Assert.True(await rawPage.EvaluateAsync<int>("moves.length") > 3);
        Assert.Equal(new[] { 100d, 120d }, (await rawPage.EvaluateAsync<double[][]>("moves"))[^1]);

        await PlaywrightHumanizer.Unwrap(page).Mouse.MoveAsync(15, 16);
        humanizer.InvalidateMousePosition(page);
        var beforeInvalidatedMove = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(30, 40);
        Assert.Equal(beforeInvalidatedMove + 1, await rawPage.EvaluateAsync<int>("moves.length"));
    }

    private sealed class OwnedBrowserRuntime : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        internal IBrowser Browser { get; }

        private OwnedBrowserRuntime(IPlaywright playwright, IBrowser browser)
        {
            _playwright = playwright;
            Browser = browser;
        }

        internal static async Task<OwnedBrowserRuntime> LaunchAsync()
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            try
            {
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                return new OwnedBrowserRuntime(playwright, browser);
            }
            catch
            {
                playwright.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Browser.DisposeAsync();
            _playwright.Dispose();
        }
    }
}
