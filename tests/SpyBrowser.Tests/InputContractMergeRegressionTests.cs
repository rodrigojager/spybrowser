using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class InputContractMergeRegressionTests
{
    [BrowserFact]
    public async Task Serialized_native_select_options_preserves_typed_result_and_explicit_options()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = BrowserTestSettings.Headless,
            Channel = BrowserTestSettings.Channel
        });
        var raw = await browser.NewPageAsync();
        await raw.SetContentAsync("<select id='value'><option value='one'>One</option><option value='two'>Two</option></select>");
        var page = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        }).Wrap(raw);
        var options = new LocatorSelectOptionOptions { Timeout = 3_000 };
        var selected = await page.Locator("#value").SelectOptionAsync("one", options);
        Assert.Equal(new[] { "one" }, selected);
        Assert.Equal(3_000f, options.Timeout);
        selected = await page.SelectOptionAsync("#value", "two", new PageSelectOptionOptions { Timeout = 3_000 });
        Assert.Equal(new[] { "two" }, selected);
        Assert.Equal("two", await raw.Locator("#value").InputValueAsync());
    }
}
