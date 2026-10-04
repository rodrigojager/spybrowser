using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Owns a launched browser and creates identity-configured contexts and pages.
/// </summary>
public sealed class SpyBrowserBrowserHandle : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly Func<BrowserNewContextOptions?, Task<IBrowserContext>> _contextFactory;
    private readonly Func<BrowserNewPageOptions?, Task<IPage>> _pageFactory;
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
        _contextFactory = contextFactory;
        _pageFactory = pageFactory;
        var configured = ConfiguredBrowserProxy.Create(rawBrowser, contextFactory, pageFactory, humanizer);
        Browser = configured;
    }

    public BrowserIdentity Identity { get; }

    /// <summary>The owned official Playwright instance.</summary>
    public IPlaywright PlaywrightInstance => _playwright;

    public IBrowser Browser { get; }

    public IBrowser RawBrowser { get; }

    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null) =>
        _contextFactory(options);

    public Task<IPage> NewPageAsync(BrowserNewPageOptions? options = null) =>
        _pageFactory(options);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await RawBrowser.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            _playwright.Dispose();
        }
    }
}
