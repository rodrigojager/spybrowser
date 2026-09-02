using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace CloakBrowser.Human;

public enum HumanPreset
{
    Default,
    Careful
}

/// <summary>
/// Compatibility view of the most commonly configured Cloak human timings.
/// SpyBrowser's transparent Playwright decorator remains the execution engine.
/// </summary>
public sealed class HumanConfig
{
    public double TypingDelay { get; init; } = 70;

    public double TypingDelaySpread { get; init; } = 40;

    public double TypingPauseChance { get; init; } = 0.1;

    public Range TypingPauseRange { get; init; } = new(400, 1000);

    public Range KeyHold { get; init; } = new(15, 35);

    public int MouseMinSteps { get; init; } = 25;

    public int MouseMaxSteps { get; init; } = 80;

    public Range ClickHoldButton { get; init; } = new(60, 150);

    public Range ScrollDeltaBase { get; init; } = new(80, 130);

    public bool IdleBetweenActions { get; init; }

    public HumanConfig Clone() => new()
    {
        TypingDelay = TypingDelay,
        TypingDelaySpread = TypingDelaySpread,
        TypingPauseChance = TypingPauseChance,
        TypingPauseRange = TypingPauseRange,
        KeyHold = KeyHold,
        MouseMinSteps = MouseMinSteps,
        MouseMaxSteps = MouseMaxSteps,
        ClickHoldButton = ClickHoldButton,
        ScrollDeltaBase = ScrollDeltaBase,
        IdleBetweenActions = IdleBetweenActions
    };
}

public readonly record struct Range(double Min, double Max);

public sealed record HumanActionOptions
{
    public float? Timeout { get; init; }

    public bool? Force { get; init; }

    public HumanConfig? HumanConfig { get; init; }
}

public sealed class HumanPage
{
    public HumanPage(IPage page, HumanConfig? config = null)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Config = config ?? new HumanConfig();
    }

    public IPage Page { get; }

    public HumanConfig Config { get; }

    public CursorState Cursor { get; } = new();

    public static Task<HumanPage> CreateAsync(IPage page, HumanConfig? config = null) =>
        Task.FromResult(new HumanPage(page, config));

    public Task<IResponse?> GotoAsync(string url, PageGotoOptions? options = null) =>
        Page.GotoAsync(url, options);

    public Task ClickAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).ClickAsync(ClickOptions(options));

    public Task DblClickAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).DblClickAsync(DblClickOptions(options));

    public Task HoverAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).HoverAsync(HoverOptions(options));

    public Task TypeAsync(string selector, string text, HumanActionOptions? options = null) =>
        Page.Locator(selector).PressSequentiallyAsync(text, PressSequentiallyOptions(options));

    public Task FillAsync(string selector, string value, HumanActionOptions? options = null) =>
        Page.Locator(selector).FillAsync(value, FillOptions(options));

    public Task CheckAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).CheckAsync(CheckOptions(options));

    public Task UncheckAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).UncheckAsync(UncheckOptions(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(
        string selector,
        string[] values,
        HumanActionOptions? options = null) =>
        Page.Locator(selector).SelectOptionAsync(values, SelectOptions(options));

    public Task PressAsync(
        string selector,
        string key,
        HumanActionOptions? options = null) =>
        Page.Locator(selector).PressAsync(key, PressOptions(options));

    public Task SetCheckedAsync(
        string selector,
        bool value,
        HumanActionOptions? options = null) =>
        Page.Locator(selector).SetCheckedAsync(value, SetCheckedOptions(options));

    public Task TapAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).TapAsync(TapOptions(options));

    public Task PressSequentiallyAsync(
        string selector,
        string text,
        HumanActionOptions? options = null) =>
        Page.Locator(selector).PressSequentiallyAsync(text, PressSequentiallyOptions(options));

    public Task ClearAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).ClearAsync(ClearOptions(options));

    public Task FocusAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).FocusAsync(new LocatorFocusOptions { Timeout = options?.Timeout });

    public Task ScrollIntoViewIfNeededAsync(string selector, HumanActionOptions? options = null) =>
        Page.Locator(selector).ScrollIntoViewIfNeededAsync(
            new LocatorScrollIntoViewIfNeededOptions { Timeout = options?.Timeout });

    public Task DragAndDropAsync(
        string source,
        string target,
        HumanActionOptions? options = null) =>
        Page.DragAndDropAsync(source, target, new PageDragAndDropOptions
        {
            Timeout = options?.Timeout,
            Force = options?.Force
        });

    public Task MouseMoveAsync(double x, double y) => Page.Mouse.MoveAsync((float)x, (float)y);

    public Task MouseClickAsync(double x, double y) => Page.Mouse.ClickAsync((float)x, (float)y);

    public Task KeyboardTypeAsync(string text) => Page.Keyboard.TypeAsync(text);

    private static LocatorClickOptions ClickOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorDblClickOptions DblClickOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorHoverOptions HoverOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorFillOptions FillOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorCheckOptions CheckOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorUncheckOptions UncheckOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorSelectOptionOptions SelectOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorPressOptions PressOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout
    };

    private static LocatorSetCheckedOptions SetCheckedOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorTapOptions TapOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };

    private static LocatorPressSequentiallyOptions PressSequentiallyOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout
    };

    private static LocatorClearOptions ClearOptions(HumanActionOptions? options) => new()
    {
        Timeout = options?.Timeout,
        Force = options?.Force
    };
}

public sealed class CursorState
{
    public double X { get; internal set; }

    public double Y { get; internal set; }
}

internal static class HumanConfigMapper
{
    public static HumanInteractionOptions Apply(
        HumanInteractionOptions source,
        IReadOnlyDictionary<string, object> overrides)
    {
        var typingDelay = ReadDouble(overrides, "typing_delay", "TypingDelay");
        var typingSpread = ReadDouble(overrides, "typing_delay_spread", "TypingDelaySpread") ?? 0;
        var pauseChance = ReadDouble(overrides, "typing_pause_chance", "TypingPauseChance");
        var mouseMinimum = ReadInt(overrides, "mouse_minimum_duration_milliseconds", "MouseMinimumDurationMilliseconds");
        var mouseMaximum = ReadInt(overrides, "mouse_maximum_duration_milliseconds", "MouseMaximumDurationMilliseconds");
        var scrollMinimum = ReadInt(overrides, "scroll_minimum_steps", "ScrollMinimumSteps");
        var scrollMaximum = ReadInt(overrides, "scroll_maximum_steps", "ScrollMaximumSteps");

        return source with
        {
            KeyMinimumDelayMilliseconds = typingDelay is null
                ? source.KeyMinimumDelayMilliseconds
                : Math.Max(0, (int)Math.Round(typingDelay.Value - typingSpread)),
            KeyMaximumDelayMilliseconds = typingDelay is null
                ? source.KeyMaximumDelayMilliseconds
                : Math.Max(0, (int)Math.Round(typingDelay.Value + typingSpread)),
            ThinkingPauseProbability = pauseChance ?? source.ThinkingPauseProbability,
            MouseMinimumDurationMilliseconds = mouseMinimum ?? source.MouseMinimumDurationMilliseconds,
            MouseMaximumDurationMilliseconds = mouseMaximum ?? source.MouseMaximumDurationMilliseconds,
            ScrollMinimumSteps = scrollMinimum ?? source.ScrollMinimumSteps,
            ScrollMaximumSteps = scrollMaximum ?? source.ScrollMaximumSteps
        };
    }

    private static int? ReadInt(
        IReadOnlyDictionary<string, object> values,
        string snakeCase,
        string pascalCase) => ReadDouble(values, snakeCase, pascalCase) is { } value
            ? checked((int)Math.Round(value))
            : null;

    private static double? ReadDouble(
        IReadOnlyDictionary<string, object> values,
        string snakeCase,
        string pascalCase)
    {
        if (!values.TryGetValue(snakeCase, out var value) && !values.TryGetValue(pascalCase, out value))
        {
            return null;
        }

        return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
