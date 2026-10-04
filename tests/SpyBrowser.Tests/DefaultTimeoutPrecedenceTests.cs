using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class DefaultTimeoutPrecedenceTests
{
    [BrowserFact]
    public async Task Configured_launch_default_is_used_by_compatible_stages()
    {
        using var temporary = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("default-timeout-launch");
        await using var handle = await SpyBrowserLauncher.LaunchContextAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id, IdentityOverride = identity, IdentitiesRoot = temporary.Path,
            Headless = true, RunGpuProbe = false, Humanize = true,
            DefaultTimeoutMilliseconds = 100, GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false,
            HumanInteraction = new HumanInteractionOptions
            {
                CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
                TypingDeadlineMilliseconds = 15_000
            }
        });

        var page = await handle.NewPageAsync();
        await page.SetContentAsync("<input id='hidden' style='display:none'>");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var failure = await Record.ExceptionAsync(() => page.Locator("#hidden").FillAsync("value"));
        timer.Stop();

        Assert.NotNull(failure);
        Assert.Contains("Timeout", failure!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(timer.ElapsedMilliseconds, 50, 2_000);
        Assert.False(PlaywrightHumanizer.Unwrap(page).IsClosed);
    }

    [BrowserFact]
    public async Task Repeated_context_wrapping_and_timeout_updates_preserve_page_override()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        await using var contextLifetime = context;
        var rawPage = await context.NewPageAsync();
        await rawPage.SetContentAsync("<input id='hidden' style='display:none'>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        });
        var wrappedContext = humanizer.Wrap(context);
        var wrappedPage = humanizer.Wrap(rawPage);

        wrappedPage.SetDefaultTimeout(750);
        wrappedContext.SetDefaultTimeout(1000);
        var contextAgain = humanizer.Wrap(context);
        var pageAgain = humanizer.Wrap(rawPage);
        Assert.Same(wrappedContext, contextAgain);
        Assert.Same(wrappedPage, pageAgain);

        var timer = System.Diagnostics.Stopwatch.StartNew();
        var pageFailure = await Record.ExceptionAsync(() => wrappedPage.Locator("#hidden").FillAsync("x"));
        timer.Stop();
        Assert.NotNull(pageFailure);
        Assert.InRange(timer.ElapsedMilliseconds, 600, 2_500);

        timer.Restart();
        wrappedContext.SetDefaultTimeout(100);
        var contextFailure = await Record.ExceptionAsync(() => wrappedPage.Locator("#hidden").FillAsync("x"));
        timer.Stop();
        Assert.NotNull(contextFailure);
        Assert.InRange(timer.ElapsedMilliseconds, 600, 2_500);
        Assert.False(rawPage.IsClosed);
    }

    [Fact]
    public async Task Page_timeout_zero_remains_native_unlimited_after_context_updates()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        await using var contextLifetime = context;
        var rawPage = await context.NewPageAsync();
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        });
        var wrappedContext = humanizer.Wrap(context);
        var wrappedPage = humanizer.Wrap(rawPage);
        wrappedPage.SetDefaultTimeout(0);
        wrappedContext.SetDefaultTimeout(100);

        await rawPage.SetContentAsync("<input id='field'>");
        await wrappedPage.Locator("#field").FillAsync("unlimited");
        Assert.Equal("unlimited", await rawPage.Locator("#field").InputValueAsync());
    }
}
