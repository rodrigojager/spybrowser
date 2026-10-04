using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Owns a configured Playwright context. Context and page objects are official
/// Playwright interfaces, optionally decorated with transparent humanization.
/// </summary>
public class SpyBrowserContextHandle : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IdentityLease? _identityLease;
    private readonly bool _closeBrowser;
    private readonly PlaywrightHumanizer? _humanizer;
    private int _disposed;

    internal SpyBrowserContextHandle(
        IPlaywright playwright,
        IBrowserContext rawContext,
        IBrowser? rawBrowser,
        BrowserIdentity identity,
        IdentityLease? identityLease,
        bool closeBrowser,
        BrowserSurfaceDiagnostics? diagnostics,
        ConsistencyReport consistency,
        PlaywrightHumanizer? humanizer)
    {
        _playwright = playwright;
        _identityLease = identityLease;
        _humanizer = humanizer;
        _closeBrowser = closeBrowser;
        RawContext = rawContext;
        RawBrowser = rawBrowser;
        Identity = identity;
        Diagnostics = diagnostics;
        Consistency = consistency;
        Context = humanizer?.Wrap(rawContext) ?? rawContext;
        Browser = rawBrowser is null ? null : humanizer?.Wrap(rawBrowser) ?? rawBrowser;
    }

    public BrowserIdentity Identity { get; }

    public IBrowserContext Context { get; }

    public IBrowserContext RawContext { get; }

    public IBrowser? Browser { get; }

    public IBrowser? RawBrowser { get; }

    public BrowserSurfaceDiagnostics? Diagnostics { get; }

    public ConsistencyReport Consistency { get; }

    /// <summary>Returns bounded interaction diagnostics when humanization diagnostics were enabled; otherwise null.</summary>
    public HumanizationDiagnosticsSnapshot? GetHumanizationDiagnosticsSnapshot() => _humanizer?.GetDiagnosticsSnapshot();

    public IReadOnlyList<IPage> Pages => ProbePageRegistry.For(RawContext).Filter(Context.Pages);

    public Task<IPage> NewPageAsync() => Context.NewPageAsync();

    /// <summary>Runs an isolated diagnostic probe without navigating or replacing a user page.</summary>
    public Task<BrowserSurfaceDiagnostics> RunGpuProbeAsync(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        GpuProbe.RunAsync(RawContext, timeout ?? TimeSpan.FromSeconds(10), cancellationToken);

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await RawContext.CloseAsync().ConfigureAwait(false);
            if (_closeBrowser && RawBrowser is not null)
            {
                await RawBrowser.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                _playwright.Dispose();
            }
            finally
            {
                if (_identityLease is not null)
                {
                    await _identityLease.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
