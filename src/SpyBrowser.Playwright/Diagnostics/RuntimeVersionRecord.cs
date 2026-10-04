namespace SpyBrowser.Playwright.Diagnostics;

/// <summary>Runtime versions are supplied by the caller; this type does not depend on unfinished launcher integrations.</summary>
public sealed record RuntimeVersionRecord
{
    public string SpyBrowser { get; init; } = "unknown";
    public string Playwright { get; init; } = "unknown";
    public string PlaywrightVersionSource { get; init; } = "unknown";
    public string BrowserFamily { get; init; } = "unknown";
    public string BrowserFamilySource { get; init; } = "unknown";
    public string? BrowserChannel { get; init; }
    public string BrowserVersion { get; init; } = "unknown";
    public string BrowserVersionSource { get; init; } = "unknown";
    public string Algorithm { get; init; } = "unknown";
    public string Dataset { get; init; } = "unknown";
}
