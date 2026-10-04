using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

/// <summary>Regression evidence for remaining Playwright-compatible input contracts.</summary>
public sealed class RemainingInputContractTests
{
    [BrowserFact]
    public async Task Compatible_double_click_preserves_native_detail_and_dblclick_event()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button id='target'>target</button><script>window.trace=[];target.addEventListener('click',e=>trace.push(['click',e.detail]));target.addEventListener('dblclick',e=>trace.push(['dblclick',e.detail]))</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        }).Wrap(page);

        await wrapped.Locator("#target").DblClickAsync();

        var trace = await page.EvaluateAsync<string[][]>("window.trace.map(([name,detail])=>[name,String(detail)])");
        Assert.Equal(new[] { "click", "click", "dblclick" }, trace.Select(events => events[0]));
        Assert.Equal(new[] { "1", "2", "2" }, trace.Select(events => events[1]));
    }

    [BrowserFact]
    public async Task Compatible_typing_delegates_multirune_grapheme_to_native_before_any_paced_prefix()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='field'><script>window.trace=[];field.addEventListener('input',e=>trace.push(e.data))</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            KeyMinimumDelayMilliseconds = 50,
            KeyMaximumDelayMilliseconds = 50
        }).Wrap(page);

        await wrapped.Locator("#field").PressSequentiallyAsync("e\u0301");

        Assert.Equal("e\u0301", await page.Locator("#field").InputValueAsync());
        Assert.Equal(new[] { "e", "\u0301" }, await page.EvaluateAsync<string[]>("window.trace"));
    }

    [BrowserFact]
    public async Task Native_default_timeout_on_compatible_fill_preserves_playwright_exception_category()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var rawPage = await browser.NewPageAsync();
        var wrappedPage = await browser.NewPageAsync();
        await rawPage.SetContentAsync("<input id='never' style='display:none'>");
        await wrappedPage.SetContentAsync("<input id='never' style='display:none'>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        }).Wrap(wrappedPage);
        rawPage.SetDefaultTimeout(100);
        wrapped.SetDefaultTimeout(100);

        var rawFailure = await Record.ExceptionAsync(() => rawPage.Locator("#never").FillAsync("value"));
        var wrappedFailure = await Record.ExceptionAsync(() => wrapped.Locator("#never").FillAsync("value"));

        Assert.NotNull(rawFailure);
        Assert.NotNull(wrappedFailure);
        Assert.IsType(rawFailure!.GetType(), wrappedFailure);
        Assert.False(rawPage.IsClosed);
        Assert.False(wrappedPage.IsClosed);
        Assert.Contains("Timeout", rawFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timeout", wrappedFailure.Message, StringComparison.OrdinalIgnoreCase);
    }
}
