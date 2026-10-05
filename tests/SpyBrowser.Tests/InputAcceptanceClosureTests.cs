using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class InputAcceptanceClosureTests
{
    [BrowserFact]
    public async Task Repeated_wrapped_input_and_helper_lifecycles_close_idempotently_and_release_handles()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var closedHandles = new List<(WeakReference Context, WeakReference Page)>();

        for (var iteration = 0; iteration < 32; iteration++)
        {
            closedHandles.Add(await ExerciseAndCloseAsync(browser, iteration));
        }

        // Collect on a fresh stack after all awaited operation and close state machines have returned.
        await Task.Run(ForceBoundedCollection).ConfigureAwait(false);
        Assert.All(closedHandles, handles =>
        {
            Assert.False(handles.Context.IsAlive, "A closed context remained rooted after repeated wrapped input disposal.");
            Assert.False(handles.Page.IsAlive, "A closed page remained rooted after repeated wrapped input disposal.");
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Context, WeakReference Page)> ExerciseAndCloseAsync(IBrowser browser, int iteration)
    {
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<input id='value'><button id='action'>act</button><output></output><script>action.onclick=()=>document.querySelector('output').textContent='done'</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            KeyMinimumDelayMilliseconds = 0,
            KeyMaximumDelayMilliseconds = 0,
            ClickHoldMinimumMilliseconds = 0,
            ClickHoldMaximumMilliseconds = 0,
            ThinkingPauseProbability = 0
        }).Wrap(page);

        var field = wrapped.Locator("#value");
        await field.PressSequentiallyAsync($"iteration-{iteration}").WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal($"iteration-{iteration}", await page.Locator("#value").InputValueAsync());
        await new HumanActions(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            ClickHoldMinimumMilliseconds = 0,
            ClickHoldMaximumMilliseconds = 0,
            ThinkingPauseProbability = 0
        }).ClickAsync(wrapped.Locator("#action")).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("done", await page.Locator("output").InnerTextAsync());

        var contextReference = new WeakReference(context);
        var pageReference = new WeakReference(page);
        await context.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await context.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(page.IsClosed);
        page = null!;
        context = null!;
        wrapped = null!;
        field = null!;
        return (contextReference, pageReference);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
}
