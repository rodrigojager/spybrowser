using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class LifecycleBrowserTests
{
    [Fact]
    public async Task Concurrent_raw_and_decorated_clicks_are_serialized_without_duplicates()
    {
        await using var browser = await LaunchChromiumAsync();
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button id='a' style='position:fixed;left:0;top:0;width:100px;height:100px'>a</button><button id='b'>b</button><script>window.log=[];for(const id of ['a','b'])document.getElementById(id).onclick=()=>log.push(id)</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible }).Wrap(page);

        await Task.WhenAll(wrapped.Locator("#a").ClickAsync(), wrapped.Mouse.ClickAsync(2, 2));
        Assert.Equal(2, await page.EvaluateAsync<int>("log.length"));
    }

    [Fact]
    public async Task Independent_pages_can_accept_input_in_parallel()
    {
        await using var browser = await LaunchChromiumAsync();
        var first = await browser.NewPageAsync();
        var second = await browser.NewPageAsync();
        await Task.WhenAll(first.SetContentAsync("<button id='b'>1</button>"), second.SetContentAsync("<button id='b'>2</button>"));
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible });
        var a = humanizer.Wrap(first);
        var b = humanizer.Wrap(second);
        await Task.WhenAll(a.Locator("#b").ClickAsync(), b.Locator("#b").ClickAsync());
        Assert.False(first.IsClosed);
        Assert.False(second.IsClosed);
    }

    [Fact]
    public async Task Typing_deadline_closes_the_page_instead_of_abandoning_an_input_task()
    {
        await using var browser = await LaunchChromiumAsync();
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='value'>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.Legacy,
            TypingDeadlineMilliseconds = 100,
            KeyMinimumDelayMilliseconds = 250,
            KeyMaximumDelayMilliseconds = 250,
            ThinkingPauseProbability = 0
        }).Wrap(page);
        await Assert.ThrowsAnyAsync<Exception>(() => wrapped.Locator("#value").FillAsync("deadline"));
        Assert.True(page.IsClosed);
    }

    [Fact]
    public async Task Closing_page_during_humanized_typing_stops_the_inflight_input()
    {
        await using var browser = await LaunchChromiumAsync();
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='value'>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.Legacy,
            KeyMinimumDelayMilliseconds = 80,
            KeyMaximumDelayMilliseconds = 80,
            ThinkingPauseProbability = 0
        }).Wrap(page);
        var typing = wrapped.Locator("#value").FillAsync(new string('x', 200));
        await Task.Delay(120);
        await page.CloseAsync();
        await Assert.ThrowsAnyAsync<Exception>(async () => await typing);
        Assert.True(page.IsClosed);
    }

    private static async Task<IBrowser> LaunchChromiumAsync()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        try { return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }); }
        catch { playwright.Dispose(); throw; }
    }
}
