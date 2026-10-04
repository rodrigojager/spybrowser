using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class DirectWrappedHelpersTests
{
    [BrowserTheory]
    [InlineData(HumanizationCompatibilityMode.Legacy)]
    [InlineData(HumanizationCompatibilityMode.PlaywrightCompatible)]
    public async Task Every_direct_helper_accepts_wrapped_arguments_without_nested_input_leases(HumanizationCompatibilityMode mode)
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync();
        var raw = await context.NewPageAsync();
        await raw.SetContentAsync("<button id='button'>go</button><input id='field'><script>window.clicks=0;button.onclick=()=>clicks++;</script>");
        var options = new HumanInteractionOptions { CompatibilityMode = mode };
        var wrapped = new PlaywrightHumanizer(options).Wrap(raw);
        var helpers = new HumanActions(options);
        var button = wrapped.Locator("#button");
        var field = wrapped.Locator("#field");
        // Test-only bounds expose reentrant leases; production deadlines are unchanged.
        await helpers.MoveAsync(wrapped, 20, 20).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.ClickAsync(button).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.DoubleClickAsync(button).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(3, await raw.EvaluateAsync<int>("clicks"));
        await helpers.HoverAsync(button).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.ClickAsync(wrapped, 20, 20).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.ClickAsync(wrapped, 20, 20, doubleClick: true).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.PressAsync(field, "ArrowLeft").WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.TypeAsync(field, "wrapped-direct").WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("wrapped-direct", await raw.Locator("#field").InputValueAsync());
        await field.FocusAsync();
        await helpers.TypeFocusedAsync(wrapped, "!").WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.PressFocusedAsync(wrapped, "End").WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.ScrollAsync(wrapped, 10).WaitAsync(TimeSpan.FromSeconds(10));
        await helpers.ScrollAsync(wrapped, 10, 10).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("wrapped-direct!", await raw.Locator("#field").InputValueAsync());
        Assert.False(raw.IsClosed);
    }

    [BrowserFact]
    public async Task Direct_cursory_helpers_and_distinct_humanizers_share_confirmed_raw_page_cursor_state()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync();
        var raw = await context.NewPageAsync();
        await raw.SetContentAsync("<script>window.moves=[];addEventListener('mousemove', e=>moves.push([e.clientX,e.clientY]));</script>");
        var options = new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            RandomSeed = 912,
            MouseMinimumDurationMilliseconds = 250,
            MouseMaximumDurationMilliseconds = 250,
            CursoryMovementDeadlineMilliseconds = 10_000
        };
        var first = new PlaywrightHumanizer(options);
        var second = new PlaywrightHumanizer(options);
        await new HumanActions(options).MoveAsync(first.Wrap(raw), 10, 10);
        await raw.EvaluateAsync("moves=[]");
        await second.Wrap(raw).Mouse.MoveAsync(400, 200);
        Assert.True(await raw.EvaluateAsync<int>("moves.length") > 1, "Recognized direct movement was lost and the second scope used an unknown-position anchor.");
        first.InvalidateMousePosition(raw);
        await raw.EvaluateAsync("moves=[]");
        await second.Wrap(raw).Mouse.MoveAsync(50, 50);
        Assert.Equal(1, await raw.EvaluateAsync<int>("moves.length"));
    }
}
