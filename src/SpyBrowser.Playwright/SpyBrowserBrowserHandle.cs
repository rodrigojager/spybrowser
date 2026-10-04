using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>The effective configuration and consistency findings for a context created by this browser handle.</summary>
public sealed record ContextConsistencyDiagnostics(
    ConsistencyExpectations EffectiveExpectations,
    ConsistencyReport Consistency);

/// <summary>
/// Owns a launched browser and creates identity-configured contexts and pages.
/// </summary>
public sealed class SpyBrowserBrowserHandle : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly ConditionalWeakTable<IBrowserContext, ContextConsistencyDiagnostics> _contextDiagnostics = new();
    private int _disposed;

    internal SpyBrowserBrowserHandle(
        IPlaywright playwright,
        IBrowser rawBrowser,
        BrowserIdentity identity,
        PlaywrightHumanizer? humanizer,
        BrowserRuntimeProvenance runtimeProvenance,
        Func<BrowserNewContextOptions?, Task<(IBrowserContext Context, ConsistencyExpectations Expectations, ConsistencyReport Report)>> contextFactory,
        Func<BrowserNewPageOptions?, Task<(IPage Page, ConsistencyExpectations Expectations, ConsistencyReport Report)>> pageFactory)
    {
        _playwright = playwright;
        RawBrowser = rawBrowser;
        Identity = identity;
        RuntimeProvenance = runtimeProvenance;
        async Task<IBrowserContext> CreateContextAsync(BrowserNewContextOptions? options)
        {
            var created = await contextFactory(options).ConfigureAwait(false);
            var rawContext = (IBrowserContext)PlaywrightHumanizer.Unwrap(created.Context);
            _contextDiagnostics.Add(rawContext, new ContextConsistencyDiagnostics(created.Expectations, created.Report));
            return created.Context;
        }

        async Task<IPage> CreatePageAsync(BrowserNewPageOptions? options)
        {
            var created = await pageFactory(options).ConfigureAwait(false);
            var rawContext = (IBrowserContext)PlaywrightHumanizer.Unwrap(created.Page.Context);
            _contextDiagnostics.Add(rawContext, new ContextConsistencyDiagnostics(created.Expectations, created.Report));
            return created.Page;
        }

        var configured = ConfiguredBrowserProxy.Create(rawBrowser, CreateContextAsync, CreatePageAsync, humanizer);
        Browser = configured;
    }

    public BrowserIdentity Identity { get; }

    /// <summary>The owned official Playwright instance.</summary>
    public IPlaywright PlaywrightInstance => _playwright;

    /// <summary>
    /// Configured browser adapter. Context events report contexts created by its configured
    /// factories only; contexts created through <see cref="RawBrowser"/> bypass configuration
    /// and are intentionally not announced retroactively. Context event handlers receive this
    /// configured adapter as sender and the configured context as event arguments.
    /// </summary>
    public IBrowser Browser { get; }

    public IBrowser RawBrowser { get; }

    /// <summary>Immutable launch facts sourced from the selected configuration and running browser process, not User-Agent.</summary>
    public BrowserRuntimeProvenance RuntimeProvenance { get; }

    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null) =>
        Browser.NewContextAsync(options);

    public Task<IPage> NewPageAsync(BrowserNewPageOptions? options = null) =>
        Browser.NewPageAsync(options);

    /// <summary>
    /// Returns the effective post-callback expectations and findings for a context created by this handle.
    /// Returns null for raw-browser contexts or contexts not created by this handle.
    /// </summary>
    public ContextConsistencyDiagnostics? GetConsistencyDiagnostics(IBrowserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rawContext = (IBrowserContext)PlaywrightHumanizer.Unwrap(context);
        return _contextDiagnostics.TryGetValue(rawContext, out var diagnostics) ? diagnostics : null;
    }

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
