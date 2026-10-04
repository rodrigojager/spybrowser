using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>Immutable launch facts; browser version comes from Playwright's running browser process, never the User-Agent.</summary>
public sealed record BrowserRuntimeProvenance(
    string BrowserFamily,
    string? BrowserChannel,
    string BrowserVersion,
    string BrowserVersionSource,
    string BrowserFamilySource,
    string Algorithm,
    string Dataset)
{
    /// <summary>Playwright assembly version used by the running process.</summary>
    public string PlaywrightVersion { get; init; } =
        typeof(Microsoft.Playwright.IPage).Assembly.GetName().Version?.ToString() ?? "unknown";

    public string PlaywrightVersionSource => "playwright.assembly.version";
    internal static BrowserRuntimeProvenance Create(BrowserIdentity identity, string? channel,
        string browserVersion, HumanInteractionOptions? humanization)
    {
        var family = channel?.Trim().ToLowerInvariant() switch
        {
            "msedge" => "edge",
            "chrome" => "chrome",
            _ => identity.Browser.Engine switch
            {
                BrowserEngine.Edge => "edge",
                BrowserEngine.Chrome => "chrome",
                BrowserEngine.Chromium or BrowserEngine.CustomChromium => "chromium",
                _ => "unknown"
            }
        };
        var algorithm = humanization is null ? "none" : humanization.MouseAlgorithm == MouseTrajectoryAlgorithm.Cursory
            ? "cursory" : "bezier:legacy-v1";
        var dataset = algorithm == "cursory"
            ? "cursory-js-16fff97fab05bb6b0c6753b2dc136a7692634cec:2356"
            : "none";
        return new BrowserRuntimeProvenance(family, channel,
            string.IsNullOrWhiteSpace(browserVersion) ? "unavailable" : browserVersion,
            "playwright.browser.version", "playwright.launch-configuration", algorithm, dataset);
    }
}
