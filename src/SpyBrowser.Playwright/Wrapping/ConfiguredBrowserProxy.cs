using System.Reflection;
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

    object IHumanizedPlaywrightObject.Original => _browser;

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
        if (_humanizer is not null && arguments.ElementAtOrDefault(0) is Delegate handler)
        {
            if (method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                PlaywrightEventBridge.IsSupported(_browser.GetType(), method.Name[4..]))
            {
                var bridge = PlaywrightEventBridge.Adapt(handler, _humanizer.Scope);
                lock (_eventLock) _eventHandlers.Add((method.Name[4..], handler, bridge));
                arguments[0] = bridge;
            }
            else if (method.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                lock (_eventLock)
                {
                    var index = _eventHandlers.FindLastIndex(e => e.Name == method.Name[7..] && e.Handler == handler);
                    if (index >= 0) { arguments[0] = _eventHandlers[index].Bridge; _eventHandlers.RemoveAt(index); }
                }
            }
        }
        if (method.Name == nameof(IBrowser.NewContextAsync))
            return _contexts((BrowserNewContextOptions?)arguments.ElementAtOrDefault(0));
        if (method.Name == nameof(IBrowser.NewPageAsync))
            return _pages((BrowserNewPageOptions?)arguments.ElementAtOrDefault(0));
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
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }
}
