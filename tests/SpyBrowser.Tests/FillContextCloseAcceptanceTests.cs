using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

/// <summary>Directly proves a compatible Fill is interrupted by closing its owning context.</summary>
public sealed class FillContextCloseAcceptanceTests
{
    [BrowserFact]
    public async Task Compatible_fill_waiting_on_disabled_field_fails_when_context_closes_and_settles_original_task()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync();
        var rawPage = await context.NewPageAsync();
        await rawPage.SetContentAsync("<input id='unavailable' disabled value='untouched'><script>window.inputEvents=0;document.querySelector('#unavailable').addEventListener('input',()=>window.inputEvents++)</script>")
            .WaitAsync(TimeSpan.FromSeconds(10));
        var wrappedPage = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        }).Wrap(rawPage);

        var fillStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task FillOriginalAsync()
        {
            fillStarted.TrySetResult();
            await wrappedPage.Locator("#unavailable").FillAsync("should-not-apply", new LocatorFillOptions { Timeout = 0 });
        }

        var originalFill = FillOriginalAsync();
        await fillStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(originalFill.IsCompleted, "Fill must still be waiting on the disabled field before context close.");
        Assert.Equal("untouched", await rawPage.Locator("#unavailable").InputValueAsync().WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(0, await rawPage.EvaluateAsync<int>("window.inputEvents").WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.False(originalFill.IsCompleted, "The setup observation must not let Fill complete.");

        await context.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var failure = await Record.ExceptionAsync(() => originalFill.WaitAsync(TimeSpan.FromSeconds(10)));

        // Playwright's pending native locator command is canceled when the owning context closes;
        // Timeout=0 rules out an action-timeout completion.
        Assert.IsType<TaskCanceledException>(failure);
        Assert.DoesNotContain("Timeout", failure!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(originalFill.IsCompleted, "Awaiting the original Fill task must leave no orphan operation.");
        Assert.True(context.IsClosed);
    }
}
