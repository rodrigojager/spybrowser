namespace SpyBrowser.Playwright;

internal static class PlaywrightDriverConfiguration
{
    private const string DriverSearchPathEnvironment = "PLAYWRIGHT_DRIVER_SEARCH_PATH";
    private static readonly object Sync = new();
    private static string? _configuredPath;

    public static void Configure(string? driverSearchPath)
    {
        if (string.IsNullOrWhiteSpace(driverSearchPath))
        {
            return;
        }

        var resolved = Path.GetFullPath(driverSearchPath);
        if (!Directory.Exists(Path.Combine(resolved, ".playwright")))
        {
            throw new DirectoryNotFoundException(
                $"The Playwright driver root must contain a '.playwright' directory: '{resolved}'.");
        }

        lock (Sync)
        {
            var existing = _configuredPath ?? Environment.GetEnvironmentVariable(DriverSearchPathEnvironment);
            if (!string.IsNullOrWhiteSpace(existing) &&
                !string.Equals(Path.GetFullPath(existing), resolved, PathComparison()))
            {
                throw new InvalidOperationException(
                    "A different Playwright driver is already configured in this process. " +
                    "Run stock and patched drivers in separate worker processes.");
            }

            Environment.SetEnvironmentVariable(DriverSearchPathEnvironment, resolved);
            _configuredPath = resolved;
        }
    }

    private static StringComparison PathComparison() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
