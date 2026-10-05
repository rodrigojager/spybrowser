using System.Reflection;
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
        var rawPage = PlaywrightHumanizer.Unwrap(page);
        Assert.False(rawPage.IsClosed);
        Assert.Equal(100, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);

        // Native Fill above already respects the raw context default even without SDK tracking.
        // Capture an SDK-owned stage's real timeout to distinguish the tracking regression.
        var locator = DispatchProxy.Create<ILocator, TypeBudgetRecorder>();
        var recorder = (TypeBudgetRecorder)(object)locator;
        recorder.Page = rawPage;
        var actions = new HumanActions(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            TypingDeadlineMilliseconds = 15_000
        });
        await actions.TypeAsync(locator, "", replaceExisting: false);
        Assert.Equal(1, recorder.TypeCalls);
        Assert.InRange(recorder.Timeout!.Value, 1, 100);
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
        Assert.Equal(750, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);

        var timer = System.Diagnostics.Stopwatch.StartNew();
        var pageFailure = await Record.ExceptionAsync(() => wrappedPage.Locator("#hidden").FillAsync("x"));
        timer.Stop();
        Assert.NotNull(pageFailure);
        Assert.InRange(timer.ElapsedMilliseconds, 600, 2_500);

        timer.Restart();
        wrappedContext.SetDefaultTimeout(100);
        Assert.Equal(750, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);
        var contextFailure = await Record.ExceptionAsync(() => wrappedPage.Locator("#hidden").FillAsync("x"));
        timer.Stop();
        Assert.NotNull(contextFailure);
        Assert.InRange(timer.ElapsedMilliseconds, 600, 2_500);
        Assert.False(rawPage.IsClosed);
    }

    [BrowserFact]
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
        Assert.Equal(0, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);

        await rawPage.SetContentAsync("<input id='field'>");
        await wrappedPage.Locator("#field").FillAsync("unlimited");
        Assert.Equal("unlimited", await rawPage.Locator("#field").InputValueAsync());
    }

    [Fact]
    public void Positive_fractional_timeout_is_never_tracked_as_native_unlimited()
    {
        SharedInputBudgetTests.StubProxy.ContextValue = DispatchProxy.Create<IBrowserContext, SharedInputBudgetTests.StubProxy>();
        var rawPage = DispatchProxy.Create<IPage, SharedInputBudgetTests.StubProxy>();
        var page = new PlaywrightHumanizer().Wrap(rawPage);
        page.SetDefaultTimeout(0.25f);
        Assert.Equal(1, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);
        page.SetDefaultTimeout(100.25f);
        Assert.Equal(101, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);
        page.SetDefaultTimeout(0);
        Assert.Equal(0, PageInputState.For(rawPage).EffectiveDefaultTimeoutMilliseconds);
    }

    public class TypeBudgetRecorder : DispatchProxy
    {
        internal IPage Page = null!;
        internal float? Timeout;
        internal int TypeCalls;
        protected override object? Invoke(MethodInfo? method, object?[]? arguments)
        {
            if (method?.Name == "get_Page") return Page;
            if (method?.Name == nameof(ILocator.PressSequentiallyAsync))
            {
                TypeCalls++;
                Timeout = ((LocatorPressSequentiallyOptions)arguments![1]!).Timeout;
                return Task.CompletedTask;
            }
            throw new NotSupportedException(method?.Name);
        }
    }
}
