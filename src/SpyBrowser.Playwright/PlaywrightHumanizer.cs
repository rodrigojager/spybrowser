using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Playwright;

/// <summary>
/// Transparently decorates the official Playwright interfaces. Every member
/// remains available; supported interaction calls are human-paced and all
/// other calls delegate verbatim to the original object.
/// </summary>
public sealed class PlaywrightHumanizer
{
    private readonly HumanizationScope _scope;

    public PlaywrightHumanizer(HumanInteractionOptions? options = null)
    {
        _scope = new HumanizationScope(options ?? new HumanInteractionOptions());
    }

    internal HumanizationScope Scope => _scope;

    public T Wrap<T>(T original)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(original);
        return (T)_scope.WrapValue(original, typeof(T), ResolvePage(original))!;
    }

    public static T Unwrap<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return value is IHumanizedPlaywrightObject wrapped ? (T)wrapped.Original : value;
    }

    /// <summary>Invalidates the tracked pointer position after movement through an unwrapped/raw API.</summary>
    public void InvalidateMousePosition(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        _scope.InvalidateMousePosition(Unwrap(page));
    }

    private static IPage? ResolvePage(object value) => value switch
    {
        IPage page => page,
        ILocator locator => locator.Page,
        IFrame frame => frame.Page,
        _ => null
    };
}

internal interface IHumanizedPlaywrightObject
{
    object Original { get; }
}

internal sealed class HumanizationScope
{
    private static readonly MethodInfo WrapTaskMethod = typeof(HumanizationScope)
        .GetMethod(nameof(WrapTaskAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo WrapGatedInputMethod = typeof(HumanizationScope)
        .GetMethod(nameof(WrapGatedInputAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly HumanInteractionOptions _options;
    private readonly ConditionalWeakTable<object, object> _proxies = new();
    private readonly ConditionalWeakTable<IPage, HumanActions> _actions = new();

    internal void InvalidateMousePosition(IPage page) =>
        _actions.GetValue(page, _ => new HumanActions(_options)).InvalidateMousePosition(page);

    internal async Task ObserveRawMouseCallAsync(IPage page, string method, object?[] arguments, Task operation)
    {
        await operation.ConfigureAwait(false);
        var actions = _actions.GetValue(page, _ => new HumanActions(_options));
        if (method == nameof(IMouse.MoveAsync) && arguments.ElementAtOrDefault(0) is float x && arguments.ElementAtOrDefault(1) is float y)
        {
            actions.ObserveRawMouseMove(page, x, y);
        }
        else if (method is nameof(IMouse.DownAsync) or nameof(IMouse.UpAsync))
        {
            var option = arguments.ElementAtOrDefault(0);
            var button = option?.GetType().GetProperty("Button")?.GetValue(option)?.ToString() ?? "left";
            actions.ObserveRawMouseButton(page, button, method == nameof(IMouse.DownAsync));
        }
    }

    public HumanizationScope(HumanInteractionOptions options)
    {
        options.Validate();
        _options = options;
    }

    internal void RegisterAlias(object rawObject, object publicObject)
    {
        _proxies.Remove(rawObject);
        _proxies.Add(rawObject, publicObject);
    }

    public object? WrapValue(object? value, Type declaredType, IPage? pageHint)
    {
        if (value is null)
        {
            return null;
        }

        if (value is IHumanizedPlaywrightObject)
        {
            return value;
        }

        if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = declaredType.GetGenericArguments()[0];
            return WrapTaskMethod.MakeGenericMethod(resultType).Invoke(this, [value, pageHint]);
        }

        if (value is IReadOnlyList<IBrowserContext> contexts)
        {
            return contexts.Select(context => (IBrowserContext)WrapValue(context, typeof(IBrowserContext), pageHint)!).ToArray();
        }

        if (value is IReadOnlyList<IPage> pages)
        {
            return pages.Where(page => !ProbePageRegistry.For(page.Context).IsProbe(page))
                .Select(page => (IPage)WrapValue(page, typeof(IPage), page)!).ToArray();
        }

        if (value is IReadOnlyList<ILocator> locators)
        {
            return locators.Select(locator => (ILocator)WrapValue(locator, typeof(ILocator), pageHint)!).ToArray();
        }

        if (value is IReadOnlyList<IFrame> frames)
        {
            return frames.Select(frame => (IFrame)WrapValue(frame, typeof(IFrame), frame.Page)!).ToArray();
        }

        if (value is IReadOnlyList<IElementHandle> handles)
        {
            return handles.Select(handle => (IElementHandle)WrapValue(handle, typeof(IElementHandle), pageHint)!).ToArray();
        }

        return value switch
        {
            IBrowser browser => GetOrCreate(browser, pageHint),
            IBrowserContext context => GetOrCreate(context, pageHint),
            IPage page => GetOrCreate(page, page),
            IFrame frame => GetOrCreate(frame, frame.Page),
            ILocator locator => GetOrCreate(locator, locator.Page),
            IFrameLocator frameLocator => GetOrCreate(frameLocator, pageHint),
            IElementHandle handle => GetOrCreate(handle, pageHint),
            IMouse mouse when pageHint is not null => GetOrCreate(mouse, pageHint),
            IKeyboard keyboard when pageHint is not null => GetOrCreate(keyboard, pageHint),
            _ => value
        };
    }

    internal object? WrapEventValue(object? value) => value is null
        ? null
        : WrapValue(value, value.GetType(), ResolvePage(value, null));

    public async Task RunInputAsync(IPage page, MethodInfo method, Func<Task> invoke)
    {
        var typing = method.Name.Contains("Fill", StringComparison.Ordinal) ||
                     method.Name.Contains("Type", StringComparison.Ordinal) ||
                     method.Name.Contains("InsertText", StringComparison.Ordinal);
        var budget = typing ? _options.TypingDeadlineMilliseconds : _options.ActionDeadlineMilliseconds;
        await PageInputState.For(page).RunAsync(invoke, budget).ConfigureAwait(false);
    }

    internal object WrapGatedInput(Task gate, Func<Task?> operation, Type returnType, IPage pageHint)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>)) return gate;
        return WrapGatedInputMethod.MakeGenericMethod(returnType.GetGenericArguments()[0])
            .Invoke(this, [gate, operation, pageHint])!;
    }

    private async Task<TResult> WrapGatedInputAsync<TResult>(Task gate, Func<Task?> operation, IPage pageHint)
    {
        await gate.ConfigureAwait(false);
        // The operation has completed under the input lease. Await its typed result without
        // invoking it again or using a blocking Result getter, then apply normal return wrapping.
        var result = await ((Task<TResult>)operation()!).ConfigureAwait(false);
        return (TResult)WrapValue(result, typeof(TResult), pageHint)!;
    }

    public void TrackDefaultTimeout(IPage page, int milliseconds) =>
        PageInputState.For(page).SetDefaultTimeout(milliseconds);

    public bool TryHumanize(
        object target,
        IPage? pageHint,
        MethodInfo method,
        object?[] arguments,
        out object? result)
    {
        var page = ResolvePage(target, pageHint);
        if (page is null)
        {
            result = null;
            return false;
        }

        var actions = _actions.GetValue(page, _ => new HumanActions(_options));
        if (target is ILocator locator && TryLocator(actions, locator, method, arguments, out result))
        {
            return true;
        }

        if (target is IPage targetPage &&
            TrySelectorOwner(actions, selector => targetPage.Locator(selector), method, arguments, out result))
        {
            return true;
        }

        if (target is IFrame frame &&
            TrySelectorOwner(actions, selector => frame.Locator(selector), method, arguments, out result))
        {
            return true;
        }

        if (target is IMouse && TryMouse(actions, page, method, arguments, out result))
        {
            return true;
        }

        if (target is IKeyboard && TryKeyboard(actions, page, method, arguments, out result))
        {
            return true;
        }

        result = null;
        return false;
    }

    private object GetOrCreate<T>(T target, IPage? pageHint)
        where T : class
    {
        return _proxies.GetValue(target, _ => HumanizingDispatchProxy<T>.Create(target, this, pageHint));
    }

    private async Task<T> WrapTaskAsync<T>(Task<T> task, IPage? pageHint)
    {
        var value = await task.ConfigureAwait(false);
        return (T)WrapValue(value, typeof(T), pageHint)!;
    }

    private static IPage? ResolvePage(object target, IPage? pageHint) => target switch
    {
        IPage page => page,
        ILocator locator => locator.Page,
        IFrame frame => frame.Page,
        _ => pageHint
    };

    private static bool TryLocator(
        HumanActions actions,
        ILocator locator,
        MethodInfo method,
        object?[] arguments,
        out object? result)
    {
        var compatible = HumanizationPolicy.IsPlaywrightCompatible(actions.CompatibilityMode);
        if (method.Name == nameof(ILocator.ClickAsync) && HasOnlyDefaultOptions(arguments, 0))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "click") : actions.ClickAsync(locator);
            return true;
        }
        if (method.Name == nameof(ILocator.DblClickAsync) && HasOnlyDefaultOptions(arguments, 0))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "dblclick") : actions.DoubleClickAsync(locator);
            return true;
        }
        if (method.Name == nameof(ILocator.HoverAsync) && HasOnlyDefaultOptions(arguments, 0))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "hover") : actions.HoverAsync(locator);
            return true;
        }
        if (!compatible && method.Name == nameof(ILocator.FillAsync) &&
            arguments.ElementAtOrDefault(0) is string fill && HasOnlyDefaultOptions(arguments, 1))
        {
            result = actions.TypeAsync(locator, fill, replaceExisting: true);
            return true;
        }
        if ((method.Name == nameof(ILocator.TypeAsync) || method.Name == nameof(ILocator.PressSequentiallyAsync)) &&
            arguments.ElementAtOrDefault(0) is string text && HasOnlyDefaultOptions(arguments, 1))
        {
            result = compatible ? actions.CompatibleTypeAsync(locator, text) : actions.TypeAsync(locator, text, replaceExisting: false);
            return true;
        }
        if (!compatible && method.Name == nameof(ILocator.PressAsync) &&
            arguments.ElementAtOrDefault(0) is string key && HasOnlyDefaultOptions(arguments, 1))
        {
            result = actions.PressAsync(locator, key);
            return true;
        }
        if (!compatible && method.Name == nameof(ILocator.ClearAsync) && HasOnlyDefaultOptions(arguments, 0))
        {
            result = actions.TypeAsync(locator, string.Empty, replaceExisting: true);
            return true;
        }
        result = null;
        return false;
    }

    private static bool TrySelectorOwner(
        HumanActions actions,
        Func<string, ILocator> locatorFactory,
        MethodInfo method,
        object?[] arguments,
        out object? result)
    {
        if (arguments.ElementAtOrDefault(0) is not string selector)
        {
            result = null;
            return false;
        }

        var locator = locatorFactory(selector);
        var compatible = HumanizationPolicy.IsPlaywrightCompatible(actions.CompatibilityMode);
        if (method.Name is nameof(IPage.ClickAsync) && HasOnlyDefaultOptions(arguments, 1))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "click") : actions.ClickAsync(locator);
            return true;
        }

        if (method.Name is nameof(IPage.DblClickAsync) && HasOnlyDefaultOptions(arguments, 1))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "dblclick") : actions.DoubleClickAsync(locator);
            return true;
        }

        if (method.Name is nameof(IPage.HoverAsync) && HasOnlyDefaultOptions(arguments, 1))
        {
            result = compatible ? actions.CompatibleLocatorActionAsync(locator, "hover") : actions.HoverAsync(locator);
            return true;
        }

        if (!compatible && method.Name is nameof(IPage.FillAsync) &&
            arguments.ElementAtOrDefault(1) is string fill &&
            HasOnlyDefaultOptions(arguments, 2))
        {
            result = actions.TypeAsync(locator, fill, replaceExisting: true);
            return true;
        }

        if (method.Name is nameof(IPage.TypeAsync) &&
            arguments.ElementAtOrDefault(1) is string text &&
            HasOnlyDefaultOptions(arguments, 2))
        {
            result = compatible ? actions.CompatibleTypeAsync(locator, text) : actions.TypeAsync(locator, text, replaceExisting: false);
            return true;
        }

        if (!compatible && method.Name is nameof(IPage.PressAsync) &&
            arguments.ElementAtOrDefault(1) is string key &&
            HasOnlyDefaultOptions(arguments, 2))
        {
            result = actions.PressAsync(locator, key);
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryMouse(
        HumanActions actions,
        IPage page,
        MethodInfo method,
        object?[] arguments,
        out object? result)
    {
        if (method.Name == nameof(IMouse.MoveAsync) &&
            arguments.ElementAtOrDefault(0) is float x &&
            arguments.ElementAtOrDefault(1) is float y &&
            HasOnlyDefaultOptions(arguments, 2))
        {
            result = actions.MoveAsync(page, x, y);
            return true;
        }

        if (actions.CompatibilityMode != HumanizationCompatibilityMode.PlaywrightCompatible &&
            (method.Name == nameof(IMouse.ClickAsync) || method.Name == nameof(IMouse.DblClickAsync)) &&
            arguments.ElementAtOrDefault(0) is float clickX &&
            arguments.ElementAtOrDefault(1) is float clickY &&
            HasOnlyDefaultOptions(arguments, 2))
        {
            result = actions.ClickAsync(page, clickX, clickY, method.Name == nameof(IMouse.DblClickAsync));
            return true;
        }

        if (actions.CompatibilityMode != HumanizationCompatibilityMode.PlaywrightCompatible &&
            method.Name == nameof(IMouse.WheelAsync) &&
            arguments.ElementAtOrDefault(0) is float deltaX &&
            arguments.ElementAtOrDefault(1) is float deltaY)
        {
            result = actions.ScrollAsync(page, deltaX, deltaY);
            return true;
        }

        result = null;
        return false;
    }

    private static bool TryKeyboard(
        HumanActions actions,
        IPage page,
        MethodInfo method,
        object?[] arguments,
        out object? result)
    {
        if ((method.Name == nameof(IKeyboard.TypeAsync) ||
             (actions.CompatibilityMode != HumanizationCompatibilityMode.PlaywrightCompatible && method.Name == nameof(IKeyboard.InsertTextAsync))) &&
            arguments.ElementAtOrDefault(0) is string text &&
            HasOnlyDefaultOptions(arguments, 1))
        {
            result = actions.TypeFocusedAsync(page, text);
            return true;
        }

        if (actions.CompatibilityMode != HumanizationCompatibilityMode.PlaywrightCompatible && method.Name == nameof(IKeyboard.PressAsync) &&
            arguments.ElementAtOrDefault(0) is string key &&
            HasOnlyDefaultOptions(arguments, 1))
        {
            result = actions.PressFocusedAsync(page, key);
            return true;
        }

        result = null;
        return false;
    }

    private static bool HasOnlyDefaultOptions(object?[] arguments, int requiredCount) =>
        arguments.Skip(requiredCount).All(argument => argument is null);
}

internal class HumanizingDispatchProxy<T> : DispatchProxy, IHumanizedPlaywrightObject
    where T : class
{
    private T _target = null!;
    private HumanizationScope _scope = null!;
    private IPage? _pageHint;
    private readonly object _eventLock = new();
    private readonly List<(string Name, Delegate Handler, Delegate Bridge)> _eventHandlers = [];

    public object Original => _target;

    public static T Create(T target, HumanizationScope scope, IPage? pageHint)
    {
        var proxy = Create<T, HumanizingDispatchProxy<T>>();
        var implementation = (HumanizingDispatchProxy<T>)(object)proxy;
        implementation._target = target;
        implementation._scope = scope;
        implementation._pageHint = pageHint;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            throw new InvalidOperationException("A Playwright proxy invocation did not provide a target method.");
        }

        var arguments = args ?? Array.Empty<object?>();
        if (arguments.ElementAtOrDefault(0) is Delegate eventHandler)
        {
            if (targetMethod.Name.StartsWith("add_", StringComparison.Ordinal) &&
                PlaywrightEventBridge.IsSupported(_target.GetType(), targetMethod.Name[4..]))
            {
                var bridge = PlaywrightEventBridge.Adapt(eventHandler, _scope);
                lock (_eventLock) _eventHandlers.Add((targetMethod.Name[4..], eventHandler, bridge));
                arguments[0] = bridge;
            }
            else if (targetMethod.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                lock (_eventLock)
                {
                    var index = _eventHandlers.FindLastIndex(entry => entry.Name == targetMethod.Name[7..] && entry.Handler == eventHandler);
                    if (index >= 0)
                    {
                        arguments[0] = _eventHandlers[index].Bridge;
                        _eventHandlers.RemoveAt(index);
                    }
                }
            }
        }
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] is IHumanizedPlaywrightObject wrapped)
            {
                arguments[index] = wrapped.Original;
            }
        }

        if (_target is IBrowserContext browserContext && targetMethod.Name == nameof(IBrowserContext.NewPageAsync))
        {
            var creation = ProbePageRegistry.For(browserContext).CreateUserPageAsync(() =>
                (Task<IPage>)targetMethod.Invoke(_target, arguments)!);
            return _scope.WrapValue(creation, targetMethod.ReturnType, _pageHint);
        }

        var page = _target switch
        {
            IPage ownedPage => ownedPage,
            ILocator locator => locator.Page,
            IFrame frame => frame.Page,
            _ => _pageHint
        };
        if (page is not null && targetMethod.Name == "SetDefaultTimeout" && arguments.ElementAtOrDefault(0) is float timeout)
        {
            _scope.TrackDefaultTimeout(page, Math.Max(1, (int)timeout));
        }

        if (page is not null && IsInputMethod(targetMethod.Name) && typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
        {
            Task? pending = null;
            var gate = _scope.RunInputAsync(page, targetMethod, async () =>
            {
                if (_scope.TryHumanize(_target, _pageHint, targetMethod, arguments, out var action))
                {
                    pending = (Task)action!;
                    await pending.ConfigureAwait(false);
                    return;
                }

                try
                {
                    pending = (Task)targetMethod.Invoke(_target, arguments)!;
                    if (_target is IMouse && targetMethod.Name is nameof(IMouse.MoveAsync) or nameof(IMouse.DownAsync) or nameof(IMouse.UpAsync))
                    {
                        // Confirm native pointer/button state before releasing the per-page lease.
                        await _scope.ObserveRawMouseCallAsync(page, targetMethod.Name, arguments, pending).ConfigureAwait(false);
                    }
                    else
                    {
                        await pending.ConfigureAwait(false);
                    }
                }
                catch (TargetInvocationException exception) when (exception.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                }
            });
            return _scope.WrapGatedInput(gate, () => pending, targetMethod.ReturnType, page);
        }

        if (_scope.TryHumanize(_target, _pageHint, targetMethod, arguments, out var humanizedResult)) return humanizedResult;
        try
        {
            var result = targetMethod.Invoke(_target, arguments);
            if (_target is IMouse && _pageHint is not null && result is Task operation &&
                targetMethod.Name is nameof(IMouse.MoveAsync) or nameof(IMouse.DownAsync) or nameof(IMouse.UpAsync))
            {
                return _scope.ObserveRawMouseCallAsync(_pageHint, targetMethod.Name, arguments, operation);
            }

            return _scope.WrapValue(result, targetMethod.ReturnType, _pageHint);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static bool IsInputMethod(string name) => name is
        "ClickAsync" or "DblClickAsync" or "TapAsync" or "HoverAsync" or "FillAsync" or "ClearAsync" or
        "TypeAsync" or "PressAsync" or "PressSequentiallyAsync" or "InsertTextAsync" or "MoveAsync" or
        "DownAsync" or "UpAsync" or "WheelAsync" or "CheckAsync" or "UncheckAsync" or "SetCheckedAsync" or
        "SelectOptionAsync" or "SetInputFilesAsync" or "DragToAsync";
}
