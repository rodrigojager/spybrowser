using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Owns a launched browser and creates identity-configured contexts and pages.
/// </summary>
public sealed class SpyBrowserBrowserHandle : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private int _disposed;

    internal SpyBrowserBrowserHandle(
        IPlaywright playwright,
        IBrowser rawBrowser,
        BrowserIdentity identity,
        PlaywrightHumanizer? humanizer,
        Func<BrowserNewContextOptions?, Task<IBrowserContext>> contextFactory,
        Func<BrowserNewPageOptions?, Task<IPage>> pageFactory)
    {
        _playwright = playwright;
        RawBrowser = rawBrowser;
        Identity = identity;
        var configured = ConfiguredBrowserProxy.Create(rawBrowser, contextFactory, pageFactory, humanizer);
        Browser = configured;
    }

    public BrowserIdentity Identity { get; }

    /// <summary>The owned official Playwright instance.</summary>
    public IPlaywright PlaywrightInstance => _playwright;

    /// <summary>
    /// Configured browser adapter. Context events report contexts created by its configured
    /// factories only; contexts created through <see cref="RawBrowser"/> bypass configuration
    /// and are intentionally not announced retroactively.
    /// </summary>
    public IBrowser Browser { get; }

    public IBrowser RawBrowser { get; }

    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null) =>
        Browser.NewContextAsync(options);

    public Task<IPage> NewPageAsync(BrowserNewPageOptions? options = null) =>
        Browser.NewPageAsync(options);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            ConfiguredBrowserProxy.Detach(Browser);
            await RawBrowser.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            _playwright.Dispose();
        }
    }
}
