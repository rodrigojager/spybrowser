namespace SpyBrowser.Core;

public static class SpyBrowserPaths
{
    public const string HomeEnvironmentVariable = "SPYBROWSER_HOME";

    public static string GetDefaultRoot()
    {
        var configured = Environment.GetEnvironmentVariable(HomeEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, "SpyBrowser");
        }

        var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgData))
        {
            return Path.Combine(Path.GetFullPath(xdgData), "spybrowser");
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, ".local", "share", "spybrowser");
    }
}
