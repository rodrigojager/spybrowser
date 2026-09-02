using System.Runtime.InteropServices;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

public sealed record EnvironmentDoctorReport(
    string OperatingSystem,
    string Architecture,
    string RootDirectory,
    int IdentityCount,
    IReadOnlyList<string> BrowserCandidates,
    bool? LinuxDirectRenderingDeviceAvailable,
    IReadOnlyList<string> Warnings);

public static class EnvironmentDoctor
{
    public static async Task<EnvironmentDoctorReport> InspectAsync(
        string? identitiesRoot = null,
        CancellationToken cancellationToken = default)
    {
        var store = new IdentityStore(identitiesRoot);
        var identities = await store.ListAsync(cancellationToken).ConfigureAwait(false);
        var candidates = FindBrowserCandidates();
        var warnings = new List<string>();
        if (candidates.Count == 0)
        {
            warnings.Add("No system Chrome, Chromium, or Edge executable was found in common locations.");
        }

        bool? linuxDirectRenderingDeviceAvailable = OperatingSystem.IsLinux()
            ? Directory.Exists("/dev/dri") && Directory.EnumerateFileSystemEntries("/dev/dri").Any()
            : null;
        if (linuxDirectRenderingDeviceAvailable == false)
        {
            warnings.Add("Linux has no visible /dev/dri device; hardware WebGL may be unavailable.");
        }

        return new EnvironmentDoctorReport(
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            store.RootDirectory,
            identities.Count,
            candidates,
            linuxDirectRenderingDeviceAvailable,
            warnings);
    }

    private static IReadOnlyList<string> FindBrowserCandidates()
    {
        var paths = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            AddWindowsCandidate(paths, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
            AddWindowsCandidate(paths, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
            AddWindowsCandidate(paths, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe");
            AddWindowsCandidate(paths, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe");
        }
        else
        {
            foreach (var candidate in new[]
                     {
                         "/usr/bin/google-chrome",
                         "/usr/bin/google-chrome-stable",
                         "/usr/bin/chromium",
                         "/usr/bin/chromium-browser",
                         "/usr/bin/microsoft-edge"
                     })
            {
                if (File.Exists(candidate))
                {
                    paths.Add(candidate);
                }
            }
        }

        return paths.Distinct(PathComparer()).ToArray();
    }

    private static void AddWindowsCandidate(ICollection<string> candidates, string root, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var candidate = segments.Aggregate(root, Path.Combine);
        if (File.Exists(candidate))
        {
            candidates.Add(candidate);
        }
    }

    private static StringComparer PathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
