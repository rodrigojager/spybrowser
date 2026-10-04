namespace SpyBrowser.Tests;

/// <summary>Marks a browser integration test skipped unless the browser lane explicitly enables it.</summary>
public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (!BrowserTestSettings.Enabled)
        {
            Skip = "Browser integration lane not enabled (set SPYBROWSER_RUN_BROWSER_TESTS=1).";
        }
    }
}

/// <summary>Marks browser integration theories skipped unless the browser lane enables them.</summary>
public sealed class BrowserTheoryAttribute : TheoryAttribute
{
    public BrowserTheoryAttribute()
    {
        if (!BrowserTestSettings.Enabled)
        {
            Skip = "Browser integration lane not enabled (set SPYBROWSER_RUN_BROWSER_TESTS=1).";
        }
    }
}

internal static class BrowserTestSettings
{
    public static bool Enabled => string.Equals(
        Environment.GetEnvironmentVariable("SPYBROWSER_RUN_BROWSER_TESTS"), "1", StringComparison.Ordinal);

    public static bool Headless => !string.Equals(
        Environment.GetEnvironmentVariable("SPYBROWSER_HEADED"), "1", StringComparison.Ordinal);

    public static string? Channel => Environment.GetEnvironmentVariable("SPYBROWSER_BROWSER_CHANNEL");
}
