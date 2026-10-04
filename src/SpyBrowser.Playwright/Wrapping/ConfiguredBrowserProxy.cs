using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Playwright;

namespace SpyBrowser.Playwright;

/// <summary>Routes public browser factories through the launch identity configuration.</summary>
internal class ConfiguredBrowserProxy : DispatchProxy, IHumanizedPlaywrightObject
{
    private IBrowser _browser = null!;
    private Func<BrowserNewContextOptions?, Task<IBrowserContext>> _contexts = null!;
    private Func<BrowserNewPageOptions?, Task<IPage>> _pages = null!;
    private PlaywrightHumanizer? _humanizer;
    private readonly object _eventLock = new();
    private readonly List<(string Name, Delegate Handler, Delegate Bridge)> _eventHandlers = [];
    private readonly List<Delegate> _contextHandlers = [];
    private readonly HashSet<IBrowserContext> _announcedContexts = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IBrowserContext, EventHandler<IBrowserContext>> _contextCloseHandlers = new(ReferenceEqualityComparer.Instance);
    private bool _closed;

    object IHumanizedPlaywrightObject.Original => _browser;

    internal static void Detach(IBrowser browser) => ((ConfiguredBrowserProxy)(object)browser).DetachEvents();

    public static IBrowser Create(
        IBrowser browser,
        Func<BrowserNewContextOptions?, Task<IBrowserContext>> contexts,
        Func<BrowserNewPageOptions?, Task<IPage>> pages,
        PlaywrightHumanizer? humanizer)
    {
        var proxy = Create<IBrowser, ConfiguredBrowserProxy>();
        var implementation = (ConfiguredBrowserProxy)(object)proxy;
        implementation._browser = browser;
        implementation._contexts = contexts;
        implementation._pages = pages;
        implementation._humanizer = humanizer;
        humanizer?.Scope.RegisterAlias(browser, proxy);
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method is null) throw new InvalidOperationException("Missing browser method.");
        var arguments = args ?? [];
        var eventName = method.Name.StartsWith("add_", StringComparison.Ordinal) ? method.Name[4..] :
            method.Name.StartsWith("remove_", StringComparison.Ordinal) ? method.Name[7..] : null;
        if (eventName == nameof(IBrowser.Context) && arguments.ElementAtOrDefault(0) is Delegate contextHandler)
        {
            if (method.Name.StartsWith("add_", StringComparison.Ordinal))
            {
                lock (_eventLock) if (!_closed) _contextHandlers.Add(contextHandler);
                return null;
            }
            lock (_eventLock)
            {
                var index = _contextHandlers.FindLastIndex(candidate => candidate.Equals(contextHandler));
                if (index >= 0) _contextHandlers.RemoveAt(index);
            }
            return null;
        }

        if (eventName is not null && arguments.ElementAtOrDefault(0) is Delegate handler)
        {
            if (method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                PlaywrightEventBridge.IsSupported(_browser.GetType(), eventName))
            {
                Delegate bridge;
                lock (_eventLock)
                {
                    if (_closed) return null;
                    bridge = PlaywrightEventBridge.Adapt(
                        handler,
                        _humanizer?.Scope,
                        eventName == "Close" ? DetachEvents : null);
                    _eventHandlers.Add((eventName, handler, bridge));
                }
                arguments[0] = bridge;
            }
            else if (method.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                lock (_eventLock)
                {
                    var index = _eventHandlers.FindLastIndex(e => e.Name == eventName && e.Handler.Equals(handler));
                    if (index >= 0) { arguments[0] = _eventHandlers[index].Bridge; _eventHandlers.RemoveAt(index); }
                }
            }
        }
        if (method.Name == nameof(IBrowser.NewContextAsync))
            return CreateAndAnnounceContextAsync((BrowserNewContextOptions?)arguments.ElementAtOrDefault(0));
        if (method.Name == nameof(IBrowser.NewPageAsync))
            return CreateAndAnnouncePageAsync((BrowserNewPageOptions?)arguments.ElementAtOrDefault(0));
        try
        {
            var result = method.Invoke(_browser, arguments);
            if (_humanizer is null) return result;
            if (method.Name == "get_Contexts" && result is IReadOnlyList<IBrowserContext> contexts)
                return contexts.Select(_humanizer.Wrap).ToArray();
            return result;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private async Task<IBrowserContext> CreateAndAnnounceContextAsync(BrowserNewContextOptions? options)
    {
        var context = await _contexts(options).ConfigureAwait(false);
        AnnounceConfiguredContext(context);
        return context;
    }

    private async Task<IPage> CreateAndAnnouncePageAsync(BrowserNewPageOptions? options)
    {
        var page = await _pages(options).ConfigureAwait(false);
        AnnounceConfiguredContext(page.Context);
        return page;
    }

    private void AnnounceConfiguredContext(IBrowserContext context)
    {
        var raw = PlaywrightHumanizer.Unwrap(context);
        Delegate[] handlers;
        lock (_eventLock)
        {
            if (_closed || !_announcedContexts.Add(raw)) return;
            // A context closed during asynchronous preparation is not published.
            if (!_browser.Contexts.Any(candidate => ReferenceEquals(candidate, raw)))
            {
                _announcedContexts.Remove(raw);
                return;
            }
            handlers = _contextHandlers.ToArray();
        }
        EventHandler<IBrowserContext> closed = (_, _) => ForgetContext(raw);
        raw.Close += closed;
        // Recheck after attaching: close could race between the membership check and subscription.
        if (!_browser.Contexts.Any(candidate => ReferenceEquals(candidate, raw)))
        {
            raw.Close -= closed;
            ForgetContext(raw);
            return;
        }
        lock (_eventLock)
        {
            if (_closed || !_announcedContexts.Contains(raw))
            {
                raw.Close -= closed;
                return;
            }
            _contextCloseHandlers[raw] = closed;
        }
        foreach (var handler in handlers)
        {
            try { handler.DynamicInvoke(this, context); }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }
    }

    private void ForgetContext(IBrowserContext context)
    {
        EventHandler<IBrowserContext>? closeHandler;
        lock (_eventLock)
        {
            _announcedContexts.Remove(context);
            _contextCloseHandlers.Remove(context, out closeHandler);
        }
        if (closeHandler is not null) context.Close -= closeHandler;
    }

    internal void DetachEvents()
    {
        (string Name, Delegate Bridge)[] bridges;
        (IBrowserContext Context, EventHandler<IBrowserContext> Handler)[] contextCloseHandlers;
        lock (_eventLock)
        {
            if (_closed) return;
            _closed = true;
            _contextHandlers.Clear();
            bridges = _eventHandlers.Select(entry => (entry.Name, entry.Bridge)).ToArray();
            _eventHandlers.Clear();
            _announcedContexts.Clear();
            contextCloseHandlers = _contextCloseHandlers.Select(pair => (pair.Key, pair.Value)).ToArray();
            _contextCloseHandlers.Clear();
        }
        foreach (var (context, handler) in contextCloseHandlers)
        {
            try { context.Close -= handler; }
            catch { /* Browser teardown owns native event cleanup. */ }
        }
        foreach (var (name, bridge) in bridges)
        {
            var eventInfo = _browser.GetType().GetEvent(name);
            try { eventInfo?.RemoveEventHandler(_browser, bridge); }
            catch { /* Browser teardown owns native event cleanup. */ }
        }
    }
}
