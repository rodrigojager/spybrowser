using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Resolves a browser executable without coupling SpyBrowser to a downloader,
/// licensing system, or a particular Chromium distribution.
/// </summary>
public interface IBrowserExecutableProvider
{
    ValueTask<string?> ResolveExecutableAsync(
        BrowserIdentity identity,
        CancellationToken cancellationToken = default);
}
