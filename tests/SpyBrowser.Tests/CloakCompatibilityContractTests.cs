using CloakBrowser;
using CloakBrowser.Human;
using Microsoft.Playwright;

namespace SpyBrowser.Tests;

public sealed class CloakCompatibilityContractTests
{
    [Fact]
    public void Facade_keeps_the_source_shape_used_by_rpablockly()
    {
        var options = new LaunchOptions
        {
            Headless = true,
            Locale = "pt-BR",
            BrowserVersion = "146.0.7680.177.5"
        };

        Assert.True(options.Headless);
        Assert.Equal("pt-BR", options.Locale);
        Assert.Equal("CloakBrowser", typeof(CloakLauncher).Assembly.GetName().Name);
        Assert.Equal(typeof(Task<CloakBrowserHandle>),
            typeof(CloakLauncher).GetMethod(nameof(CloakLauncher.LaunchAsync))!.ReturnType);
        Assert.Equal(typeof(IBrowser), typeof(CloakBrowserHandle).GetProperty("RawBrowser")!.PropertyType);
    }

    [Fact]
    public void Common_cloak_options_remain_source_compatible()
    {
        var options = new LaunchContextOptions
        {
            Proxy = new ProxySettings
            {
                Server = "http://127.0.0.1:8080",
                Username = "user",
                Password = "secret"
            },
            Args = ["--disable-features=Translate"],
            StealthArgs = true,
            Humanize = true,
            HumanPreset = HumanPreset.Careful,
            HumanConfig = new Dictionary<string, object> { ["typing_delay"] = 80d },
            Viewport = (1280, 800),
            ColorScheme = "dark",
            StorageStatePath = "state.json"
        };

        Assert.IsType<ProxySettings>(options.Proxy);
        Assert.Equal((1280, 800), options.Viewport);
        Assert.Equal(HumanPreset.Careful, options.HumanPreset);
    }
}
