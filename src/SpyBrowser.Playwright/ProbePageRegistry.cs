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
    private readonly HashSet<IPage> _pendingPages = new(ReferenceEqualityComparer.Instance);
    private readonly List<(IPage[] Pages, Action Publish)> _pendingEvents = [];
    private int _activeClassifications;
    internal Func<Task<IPage>>? ProbePageFactory { get; set; }

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

    internal IReadOnlyList<IPage> Filter(IReadOnlyList<IPage> pages)
    {
        lock (_pendingEvents)
            return pages.Where(page => !IsProbe(page) && !_pendingPages.Contains(PlaywrightHumanizer.Unwrap(page))).ToArray();
    }

    // Defer only page events whose identity is ambiguous while NewPageAsync is in flight.
    internal bool DeferPageEvent(IPage[] pages, Action publish)
    {
        lock (_pendingEvents)
        {
            if (_activeClassifications == 0) return false;
            foreach (var page in pages) _pendingPages.Add(PlaywrightHumanizer.Unwrap(page));
            _pendingEvents.Add((pages, publish));
            return true;
        }
    }

    private void BeginClassification()
    {
        lock (_pendingEvents) _activeClassifications++;
    }

    private void CompleteClassification(IPage? probe)
    {
        List<(IPage[] Pages, Action Publish)>? ready = null;
        lock (_pendingEvents)
        {
            if (probe is not null) ProbeMarkers.GetValue(PlaywrightHumanizer.Unwrap(probe), _ => new object());
            if (--_activeClassifications == 0)
            {
                ready = _pendingEvents.ToList();
                _pendingEvents.Clear();
                _pendingPages.Clear();
            }
        }
        if (ready is null) return;
        foreach (var item in ready)
            if (!item.Pages.Any(IsProbe)) item.Publish();
    }

    internal async Task<IPage> CreateUserPageAsync(Func<Task<IPage>> create)
    {
        await _creation.WaitAsync().ConfigureAwait(false);
        try { return await create().ConfigureAwait(false); }
        finally { _creation.Release(); }
    }

    internal async Task<IPage> CreateProbePageAsync(CancellationToken cancellationToken)
    {
        await _creation.WaitAsync(cancellationToken).ConfigureAwait(false);
        BeginClassification();
        Task<IPage>? creation = null;
        IPage? createdPage = null;
        var classificationResolved = false;
        try
        {
            creation = (ProbePageFactory ?? _context.NewPageAsync)();
            createdPage = await creation.WaitAsync(cancellationToken).ConfigureAwait(false);
            classificationResolved = true;
            CompleteClassification(createdPage);
            return createdPage;
        }
        catch (OperationCanceledException) when (!classificationResolved && creation is { IsCompleted: false })
        {
            // Keep the identity fence active until the uncancellable native operation
            // settles. Observe faults and close only the page it created.
            _ = ObserveLateCreationAsync(creation);
            throw;
        }
        catch
        {
            if (!classificationResolved) CompleteClassification(null);
            if (classificationResolved && createdPage is not null)
                await CloseProbePageAsync(createdPage).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _creation.Release();
        }
    }

    private async Task ObserveLateCreationAsync(Task<IPage> creation)
    {
        var classificationResolved = false;
        IPage? page = null;
        try
        {
            page = await creation.ConfigureAwait(false);
            classificationResolved = true;
            CompleteClassification(page);
        }
        catch
        {
            if (!classificationResolved) CompleteClassification(null);
        }
        finally
        {
            if (page is not null)
            {
                try { await CloseProbePageAsync(page).ConfigureAwait(false); }
                catch { /* The operation already timed out; never leak its late page. */ }
            }
        }
    }

    internal async Task CloseProbePageAsync(IPage page)
    {
        var raw = PlaywrightHumanizer.Unwrap(page);
        ProbeMarkers.GetValue(raw, _ => new object());
        if (!raw.IsClosed) await raw.CloseAsync().ConfigureAwait(false);
    }
}
