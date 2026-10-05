using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class PointerObservationTests
{
    [BrowserFact]
    public async Task Compatible_locator_action_leaves_cursor_unknown_when_native_action_may_reposition_it()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<button id='target'>target</button><script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        });
        var page = humanizer.Wrap(rawPage);

        await page.Locator("#target").HoverAsync();
        var beforeBootstrap = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(410, 260);

        Assert.Equal(beforeBootstrap + 1, await rawPage.EvaluateAsync<int>("moves.length"));
        Assert.Equal(new[] { 410d, 260d }, (await rawPage.EvaluateAsync<double[][]>("moves"))[^1]);
    }

    [BrowserFact]
    public async Task Raw_trial_locator_action_does_not_invalidate_a_stationary_pointer()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<button id='target'>target</button><script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 300
        }).Wrap(rawPage);
        await page.Mouse.MoveAsync(20, 25);
        await page.Locator("#target").ClickAsync(new LocatorClickOptions { Trial = true });
        var beforePath = await rawPage.EvaluateAsync<int>("moves.length");

        await page.Mouse.MoveAsync(480, 330);

        Assert.True(await rawPage.EvaluateAsync<int>("moves.length") > beforePath + 1);
    }

    [BrowserFact]
    public async Task Failed_raw_mouse_click_keeps_exact_fault_and_forces_future_bootstrap()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var options = new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 300
        };
        var page = new PlaywrightHumanizer(options).Wrap(rawPage);
        await page.Mouse.MoveAsync(25, 30);
        var expected = new IOException("synthetic partially-dispatched click fault");
        var scope = new HumanizationScope(options);
        object?[] coordinates = [120f, 140f];
        scope.MarkRawMouseCallStarted(rawPage, nameof(IMouse.ClickAsync), coordinates);

        var actual = await Assert.ThrowsAsync<IOException>(() => scope.ObserveRawMouseCallAsync(
            rawPage, nameof(IMouse.ClickAsync), coordinates, Task.FromException(expected)));

        Assert.Same(expected, actual);
        var beforeBootstrap = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(410, 260);
        Assert.Equal(beforeBootstrap + 1, await rawPage.EvaluateAsync<int>("moves.length"));
    }

    [BrowserFact]
    public async Task Compatible_locator_hover_during_drag_uses_one_native_hover_without_releasing_owner_button()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<button id='target'>target</button><script>window.moves=[];window.ups=0;addEventListener('mousemove',e=>moves.push(e.buttons));addEventListener('mouseup',()=>ups++);</script>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        }).Wrap(rawPage);

        await page.Mouse.MoveAsync(15, 20);
        await page.Mouse.DownAsync();
        var beforeHover = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Locator("#target").HoverAsync();

        Assert.Equal(beforeHover + 1, await rawPage.EvaluateAsync<int>("moves.length"));
        Assert.Equal(0, await rawPage.EvaluateAsync<int>("ups"));
        Assert.Equal(1, (await rawPage.EvaluateAsync<int[]>("moves"))[^1]);
    }

    [BrowserFact]
    public async Task Explicit_raw_mouse_click_options_observe_its_endpoint_for_the_next_trajectory()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<script>window.moves=[];addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY]));</script>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 300
        }).Wrap(rawPage);

        await page.Mouse.MoveAsync(25, 30);
        await page.Mouse.ClickAsync(120, 140, new MouseClickOptions { Button = MouseButton.Left });
        var beforePath = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(500, 350);

        var moves = await rawPage.EvaluateAsync<double[][]>("moves");
        Assert.True(moves.Length > beforePath + 1, "A known click endpoint should produce a path, not an unknown-position bootstrap.");
        Assert.Equal(new[] { 500d, 350d }, moves[^1]);
    }

    [BrowserFact]
    public async Task Native_right_click_observes_the_selected_button_up_without_leaving_false_drag_state()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<script>window.moves=[];addEventListener('contextmenu',e=>e.preventDefault());addEventListener('mousemove',e=>moves.push([e.clientX,e.clientY,e.buttons]));</script>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 300
        }).Wrap(rawPage);
        await page.Mouse.MoveAsync(25, 30);
        await page.Mouse.DownAsync(new MouseDownOptions { Button = MouseButton.Right });
        await page.Mouse.ClickAsync(120, 140, new MouseClickOptions { Button = MouseButton.Right });
        var beforePath = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(500, 350);
        var moves = await rawPage.EvaluateAsync<double[][]>("moves");
        Assert.True(moves.Length > beforePath + 1, "A completed native right click must not leave an observed/uncertain right drag active.");
        Assert.All(moves.Skip(beforePath), point => Assert.Equal(0, point[2]));
    }

    [BrowserFact]
    public async Task Successful_native_locator_click_observes_its_up_without_sdk_cleanup_of_the_caller_drag()
    {
        await using var runtime = await OwnedBrowserRuntime.LaunchAsync();
        var rawPage = await runtime.Browser.NewPageAsync();
        await rawPage.SetContentAsync("<button id='target'>target</button><script>window.moves=[];window.ups=0;addEventListener('mousemove',e=>moves.push(e.buttons));addEventListener('mouseup',()=>ups++);</script>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            MouseMinimumDurationMilliseconds = 300,
            MouseMaximumDurationMilliseconds = 300
        }).Wrap(rawPage);
        await page.Mouse.MoveAsync(25, 30);
        await page.Mouse.DownAsync();
        await page.Locator("#target").ClickAsync();
        Assert.Equal(1, await rawPage.EvaluateAsync<int>("ups"));
        await page.Mouse.MoveAsync(410, 260); // Unknown native endpoint is bootstrapped, not guessed.
        var beforePath = await rawPage.EvaluateAsync<int>("moves.length");
        await page.Mouse.MoveAsync(500, 350);
        var moves = await rawPage.EvaluateAsync<int[]>("moves");
        Assert.True(moves.Length > beforePath + 1, "Native click Up should clear the observed left drag without issuing any extra SDK Up.");
        Assert.All(moves.Skip(beforePath), buttons => Assert.Equal(0, buttons));
        Assert.Equal(1, await rawPage.EvaluateAsync<int>("ups"));
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
                return new OwnedBrowserRuntime(playwright, await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = BrowserTestSettings.Headless,
                    Channel = BrowserTestSettings.Channel
                }));
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
