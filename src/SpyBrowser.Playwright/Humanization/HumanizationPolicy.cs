namespace SpyBrowser.Playwright.Humanization;

internal static class HumanizationPolicy
{
    public static bool IsPlaywrightCompatible(HumanizationCompatibilityMode mode) =>
        mode == HumanizationCompatibilityMode.PlaywrightCompatible;
}
