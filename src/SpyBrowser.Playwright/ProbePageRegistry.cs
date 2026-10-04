using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace SpyBrowser.Playwright;

/// <summary>Tracks diagnostic pages separately from pages exposed to automation.</summary>
internal sealed class ProbePageRegistry
{
    private static readonly ConditionalWeakTable<IBrowserContext, ProbePageRegistry> Registries = new();
    private static readonly ConditionalWeakTable<IPage, object> ProbeMarkers = new();
    private readonly IBrowserContext _context;
    private readonly SemaphoreSlim _creation = new(1, 1);
    private readonly HashSet<IPage> _pages = new(ReferenceEqualityComparer.Instance);
    private int _probeCreation;

    private ProbePageRegistry(IBrowserContext context) => _context = context;

    internal static ProbePageRegistry For(IBrowserContext context)
    {
        var raw = PlaywrightHumanizer.Unwrap(context);
        return Registries.GetValue(raw, c => new(c));
    }

    internal bool IsProbe(IPage page)
    {
        var raw = PlaywrightHumanizer.Unwrap(page);
        return ProbeMarkers.TryGetValue(raw, out _);
    }

    internal IReadOnlyList<IPage> Filter(IReadOnlyList<IPage> pages) => pages.Where(page => !IsProbe(page)).ToArray();

    internal async Task<IPage> CreateUserPageAsync(Func<Task<IPage>> create)
    {
        await _creation.WaitAsync().ConfigureAwait(false);
        try { return await create().ConfigureAwait(false); }
        finally { _creation.Release(); }
    }

    internal async Task<IPage> CreateProbePageAsync(CancellationToken cancellationToken)
    {
        await _creation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Interlocked.Exchange(ref _probeCreation, 1);
            var page = await _context.NewPageAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_pages) _pages.Add(page);
            ProbeMarkers.GetValue(page, _ => new object());
            return page;
        }
        finally
        {
            Interlocked.Exchange(ref _probeCreation, 0);
            _creation.Release();
        }
    }

    // Page events are raised while NewPageAsync is in flight. Event filtering
    // uses the serialized creation operation, never a global event suppression flag.
    internal bool IsCreatingProbe => Volatile.Read(ref _probeCreation) != 0;

    internal async Task CloseProbePageAsync(IPage page)
    {
        var raw = PlaywrightHumanizer.Unwrap(page);
        lock (_pages) _pages.Add(raw);
        ProbeMarkers.GetValue(raw, _ => new object());
        if (!raw.IsClosed) await raw.CloseAsync().ConfigureAwait(false);
    }
}
