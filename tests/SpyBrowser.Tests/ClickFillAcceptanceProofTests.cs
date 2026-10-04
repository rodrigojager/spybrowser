using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

/// <summary>Additional local-browser proofs for gaps not covered by the existing action matrix.</summary>
public sealed class ClickFillAcceptanceProofTests
{
    [BrowserFact]
    public async Task Compatible_click_re_resolves_a_target_that_moves_during_pointer_preparation()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <button id="target" style="position:absolute;left:80px;top:100px;width:100px;height:50px">move me</button>
            <script>
              window.moves = 0;
              window.clicks = [];
              document.addEventListener('mousemove', () => {
                if (moves === 0) {
                  moves++;
                  target.style.left = '240px';
                }
              });
              target.addEventListener('click', event => clicks.push([event.clientX, event.clientY]));
            </script>
            """);
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        }).Wrap(page);

        await wrapped.Locator("#target").ClickAsync();

        Assert.Equal(1, await page.EvaluateAsync<int>("moves"));
        var clicks = await page.EvaluateAsync<double[][]>("clicks");
        Assert.Single(clicks);
        Assert.InRange(clicks[0][0], 240, 340);
        Assert.InRange(clicks[0][1], 100, 150);
    }
}
