namespace SpyBrowser.Playwright.Diagnostics;

/// <summary>Runtime versions are supplied by the caller; this type does not depend on unfinished launcher integrations.</summary>
public sealed record RuntimeVersionRecord
{
    public string SpyBrowser { get; init; } = "unknown";
    public string Playwright { get; init; } = "unknown";
    public string BrowserFamily { get; init; } = "unknown";
    public string BrowserVersion { get; init; } = "unknown";
    public string Algorithm { get; init; } = "unknown";
    public string Dataset { get; init; } = "unknown";
}
