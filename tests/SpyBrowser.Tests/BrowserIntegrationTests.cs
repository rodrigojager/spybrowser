using SpyBrowser.Core;
using SpyBrowser.Playwright;
using CloakCompatibility = CloakBrowser;
using Xunit.Abstractions;

namespace SpyBrowser.Tests;

public sealed class BrowserIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public BrowserIntegrationTests(ITestOutputHelper output) => _output = output;
    [BrowserFact]
    public async Task Persistent_context_returns_official_playwright_interfaces_when_enabled()
    {

        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        await store.CreateAsync(BrowserIdentity.Create("integration"));
        await using var session = await SpyBrowserLauncher.LaunchAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = "integration",
            IdentitiesRoot = temporary.Path,
            Headless = BrowserTestSettings.Headless,
            ChannelOverride = BrowserTestSettings.Channel,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        });
        ReportVersions(session, _output);
        var page = session.Pages.FirstOrDefault() ?? await session.NewPageAsync();
        await page.SetContentAsync("<button id='ok'>OK</button>");

        Assert.Equal("OK", await page.Locator("#ok").InnerTextAsync());
        Assert.NotNull(session.Context);
    }

    [BrowserFact]
    public async Task Transparent_humanization_preserves_playwright_interfaces_and_interactions()
    {

        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        await store.CreateAsync(BrowserIdentity.Create("humanized"));
        await using var session = await SpyBrowserLauncher.LaunchPersistentContextAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = "humanized",
            IdentitiesRoot = temporary.Path,
            Headless = BrowserTestSettings.Headless,
            ChannelOverride = BrowserTestSettings.Channel,
            Humanize = true,
            RunGpuProbe = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        });
        ReportVersions(session, _output);
        var page = await session.NewPageAsync();
        await page.SetContentAsync("""
            <input id="name">
            <button id="save" onclick="this.dataset.clicked='yes'">Save</button>
            """);

        var input = page.Locator("#name");
        Assert.IsAssignableFrom<Microsoft.Playwright.IPage>(page);
        Assert.IsAssignableFrom<Microsoft.Playwright.ILocator>(input);
        Assert.NotSame(page, PlaywrightHumanizer.Unwrap(page));
        Assert.NotSame(input, PlaywrightHumanizer.Unwrap(input));

        await input.FillAsync("Rodrigo");
        await page.Locator("#save").ClickAsync();

        Assert.Equal("Rodrigo", await input.InputValueAsync());
        Assert.Equal("yes", await page.Locator("#save").GetAttributeAsync("data-clicked"));
    }

    [BrowserFact]
    public async Task Disposable_browser_and_context_launch_modes_work()
    {

        using var temporary = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("launch-modes") with
        {
            Browser = new BrowserIdentitySettings { Engine = BrowserEngine.Chromium, Channel = null }
        };
        var common = new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = temporary.Path,
            Headless = BrowserTestSettings.Headless,
            ChannelOverride = BrowserTestSettings.Channel,
            RunGpuProbe = false,
            FailOnConsistencyErrors = false,
            ConfigureContext = context =>
            {
                context.Locale = "fr-FR";
                context.TimezoneId = "UTC";
            }
        };

        await using (var contextHandle = await SpyBrowserLauncher.LaunchContextAsync(common))
        {
            Assert.Equal("fr-FR", contextHandle.EffectiveExpectations.Locale);
            Assert.Equal("UTC", contextHandle.EffectiveExpectations.TimezoneId);
            var page = await contextHandle.NewPageAsync();
            await page.SetContentAsync("<h1>context</h1>");
            Assert.Equal("context", await page.Locator("h1").InnerTextAsync());
        }

        await using (var browserHandle = await SpyBrowserLauncher.LaunchBrowserAsync(common))
        {
            var page = await browserHandle.NewPageAsync();
            await page.SetContentAsync("<h1>browser</h1>");
            Assert.Equal("browser", await page.Locator("h1").InnerTextAsync());
        }
    }

    [BrowserFact]
    public async Task Cloak_shaped_facade_runs_ordinary_playwright_code()
    {

        using var temporary = new TemporaryDirectory();
        await using var browser = await CloakCompatibility.CloakLauncher.LaunchAsync(
            new CloakCompatibility.LaunchOptions
            {
                IdentityId = "cloak-facade",
                IdentitiesRoot = temporary.Path,
                Headless = true,
                Humanize = false,
                RunGpuProbe = false,
                FailOnConsistencyErrors = false
            });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button>compatible</button>");

        Assert.Equal("compatible", await page.Locator("button").InnerTextAsync());
        Assert.NotNull(browser.RawBrowser);
    }

    [BrowserFact]
    public async Task Identity_pool_rotates_concurrent_sessions_and_reports_exhaustion()
    {

        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        await store.CreateAsync(BrowserIdentity.Create("pool-a"));
        await store.CreateAsync(BrowserIdentity.Create("pool-b"));
        var pool = new IdentityRotationPool(["pool-a", "pool-b"]);

        SpyBrowserSession? first = null;
        SpyBrowserSession? second = null;
        SpyBrowserSession? reused = null;
        try
        {
            SpyBrowserLaunchOptions Options(string id) => new()
            {
                IdentityId = id,
                IdentitiesRoot = temporary.Path,
                Headless = BrowserTestSettings.Headless,
                ChannelOverride = BrowserTestSettings.Channel,
                RunGpuProbe = false,
                GpuPolicyOverride = GpuPolicy.AllowSoftware,
                FailOnConsistencyErrors = false
            };

            first = await pool.LaunchNextAsync(Options);
            second = await pool.LaunchNextAsync(Options);

            Assert.NotEqual(first.Identity.Id, second.Identity.Id);
            var exhausted = await Assert.ThrowsAsync<IdentityPoolExhaustedException>(
                () => pool.LaunchNextAsync(Options));
            Assert.Equal(2, exhausted.BusyIdentityIds.Count);

            await first.DisposeAsync();
            first = null;
            reused = await pool.LaunchNextAsync(Options);
            Assert.Contains(reused.Identity.Id, pool.IdentityIds);
        }
        finally
        {
            if (reused is not null)
            {
                await reused.DisposeAsync();
            }

            if (second is not null)
            {
                await second.DisposeAsync();
            }

            if (first is not null)
            {
                await first.DisposeAsync();
            }
        }
    }

    private static void ReportVersions(SpyBrowser.Playwright.SpyBrowserSession session, ITestOutputHelper output)
    {
        output.WriteLine($"SDK: {Environment.GetEnvironmentVariable("SPYBROWSER_DOTNET_SDK_VERSION") ?? "unknown"}; .NET runtime: {Environment.Version}; OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        output.WriteLine($"Playwright: {typeof(Microsoft.Playwright.IPage).Assembly.GetName().Version}");
        output.WriteLine($"Browser: {session.Browser?.Version ?? "unavailable"}");
    }
}
