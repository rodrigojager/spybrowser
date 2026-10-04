using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Playwright;

/// <summary>
/// Optional behavioral actions. Raw Playwright methods remain available and
/// unchanged; callers opt in only for interactions that should be human-paced.
/// </summary>
public sealed class HumanActions
{
    private readonly HumanInteractionOptions _options;
    private readonly Random _random;
    // All SDK entry points operate on one physical cursor per raw page, even when
    // callers mix direct helpers and separate humanizer instances.
    private static readonly ConditionalWeakTable<IPage, PageMouseState> _mouseStates = new();
    private readonly IMouseTrajectoryStrategy _trajectory = new BezierTrajectoryStrategy();
    private readonly CursoryTrajectoryStrategy _cursory = new();
    private readonly MonotonicMovementScheduler _movementScheduler = new();

    internal HumanizationCompatibilityMode CompatibilityMode => _options.CompatibilityMode;

    internal (string Mode, string Reason) DescribeMouseMove(IPage page)
    {
        if (_options.MouseAlgorithm != MouseTrajectoryAlgorithm.Cursory) return ("humanized", "humanization.preparation-or-paced-action");
        if (!_mouseStates.TryGetValue(page, out var state) || !state.HasKnownPosition) return ("native-anchor", "humanization.pointer-unknown");
        if (state.HasButtonDown) return ("native-anchor", "humanization.active-drag");
        return ("humanized", "humanization.cursory-move");
    }

    public HumanActions(HumanInteractionOptions? options = null)
    {
        _options = options ?? new HumanInteractionOptions();
        _options.Validate();
        _random = new Random(_options.RandomSeed ?? RandomNumberGenerator.GetInt32(int.MaxValue));
    }

    public Task ClickAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        locator = PlaywrightHumanizer.Unwrap(locator);
        return RunDirectAsync(locator.Page, _options.ActionDeadlineMilliseconds, cancellationToken, token => ClickCoreAsync(locator, token));
    }

    internal async Task ClickCoreAsync(ILocator locator, CancellationToken cancellationToken)
    {
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible)
        {
            await CompatibleLocatorActionAsync(locator, "click", cancellationToken).ConfigureAwait(false);
            return;
        }
        var (page, _, _) = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
    }

    public Task DoubleClickAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        locator = PlaywrightHumanizer.Unwrap(locator);
        return RunDirectAsync(locator.Page, _options.ActionDeadlineMilliseconds, cancellationToken, token => DoubleClickCoreAsync(locator, token));
    }

    internal async Task DoubleClickCoreAsync(ILocator locator, CancellationToken cancellationToken)
    {
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible)
        {
            await CompatibleLocatorActionAsync(locator, "dblclick", cancellationToken).ConfigureAwait(false);
            return;
        }
        var (page, _, _) = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
        await DelayAsync(_options.DoubleClickIntervalMinimumMilliseconds, _options.DoubleClickIntervalMaximumMilliseconds, cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
    }

    public Task HoverAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        locator = PlaywrightHumanizer.Unwrap(locator);
        return RunDirectAsync(locator.Page, _options.ActionDeadlineMilliseconds, cancellationToken, token => HoverCoreAsync(locator, token));
    }

    internal async Task HoverCoreAsync(ILocator locator, CancellationToken cancellationToken)
    {
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible)
        {
            await CompatibleLocatorActionAsync(locator, "hover", cancellationToken).ConfigureAwait(false);
            return;
        }
        _ = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
    }

    public Task MoveAsync(
        IPage page,
        double targetX,
        double targetY,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        page = PlaywrightHumanizer.Unwrap(page);
        return RunDirectAsync(page, _options.ActionDeadlineMilliseconds, cancellationToken, token => MoveCoreAsync(page, targetX, targetY, token));
    }

    internal async Task MoveCoreAsync(IPage page, double targetX, double targetY, CancellationToken cancellationToken)
    {
        var state = _mouseStates.GetValue(page, _ => new PageMouseState());
        cancellationToken.ThrowIfCancellationRequested();
        if (page.IsClosed) throw new InvalidOperationException("The page is closed.");
        ValidateSelectedMouseAlgorithm();

        if (_options.MouseAlgorithm == MouseTrajectoryAlgorithm.Cursory)
        {
            if (!state.HasKnownPosition || state.HasButtonDown)
            {
                // The cursor's physical position is unavailable. Bootstrap with one endpoint only;
                // during an observed drag, avoid trajectory preparation and preserve button ownership.
                cancellationToken.ThrowIfCancellationRequested();
                await page.Mouse.MoveAsync((float)targetX, (float)targetY).ConfigureAwait(false);
                state.ConfirmPosition(targetX, targetY);
                return;
            }

            var path = _cursory.Create(state.X!.Value, state.Y!.Value, targetX, targetY, _options);
            HumanizationDiagnosticsRecorder.ReportPlannedMovement(path.Count, path[^1].OffsetMilliseconds);
            var dispatched = 0;
            await _movementScheduler.ExecuteAsync(
                () => page.IsClosed,
                (x, y) => page.Mouse.MoveAsync((float)x, (float)y),
                path,
                TimeSpan.FromMilliseconds(_options.CursoryMovementDeadlineMilliseconds),
                point => state.ConfirmPosition(point.X, point.Y),
                cancellationToken,
                (sent, skipped) => { dispatched = sent; HumanizationDiagnosticsRecorder.ReportMovementProgress(sent, skipped); }).ConfigureAwait(false);
            HumanizationDiagnosticsRecorder.ReportFinalEndpointReached(dispatched > 0 && path.Count > 0 &&
                state.X == path[^1].X && state.Y == path[^1].Y);
            return;
        }

        var viewport = page.ViewportSize;
        var startX = state.X ?? (viewport?.Width ?? 1280) / 2d;
        var startY = state.Y ?? (viewport?.Height ?? 720) / 2d;
        await MoveMouseAsync(
            page,
            startX,
            startY,
            targetX,
            targetY,
            point => state.ConfirmPosition(point.X, point.Y),
            cancellationToken).ConfigureAwait(false);
    }

    public Task ClickAsync(IPage page, double targetX, double targetY, bool doubleClick = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        page = PlaywrightHumanizer.Unwrap(page);
        return RunDirectAsync(page, _options.ActionDeadlineMilliseconds, cancellationToken,
            token => ClickCoreAsync(page, targetX, targetY, doubleClick, token));
    }

    internal async Task ClickCoreAsync(IPage page, double targetX, double targetY, bool doubleClick, CancellationToken cancellationToken)
    {
        await MoveCoreAsync(page, targetX, targetY, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible)
        {
            if (doubleClick) await page.Mouse.DblClickAsync((float)targetX, (float)targetY).ConfigureAwait(false);
            else await page.Mouse.ClickAsync((float)targetX, (float)targetY).ConfigureAwait(false);
            return;
        }

        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
        if (doubleClick)
        {
            await DelayAsync(
                _options.DoubleClickIntervalMinimumMilliseconds,
                _options.DoubleClickIntervalMaximumMilliseconds,
                cancellationToken).ConfigureAwait(false);
            await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task PressAsync(ILocator locator, string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        locator = PlaywrightHumanizer.Unwrap(locator);
        return RunDirectAsync(locator.Page, _options.ActionDeadlineMilliseconds, cancellationToken, token => PressCoreAsync(locator, key, token));
    }

    internal async Task PressCoreAsync(ILocator locator, string key, CancellationToken cancellationToken)
    {
        await HoverCoreAsync(locator, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await locator.FocusAsync().ConfigureAwait(false);
        await PressFocusedCoreAsync(locator.Page, key, cancellationToken).ConfigureAwait(false);
    }

    public Task TypeAsync(ILocator locator, string text, bool replaceExisting = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(text);
        locator = PlaywrightHumanizer.Unwrap(locator);
        return RunDirectAsync(locator.Page, _options.TypingDeadlineMilliseconds, cancellationToken,
            token => TypeCoreAsync(locator, text, replaceExisting, token));
    }

    internal async Task TypeCoreAsync(ILocator locator, string text, bool replaceExisting, CancellationToken cancellationToken = default)
    {
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible)
        {
            if (!replaceExisting && !CanPaceText(text))
            {
                await locator.PressSequentiallyAsync(text).ConfigureAwait(false);
                return;
            }

            using var deadline = CreateDeadline(locator.Page, _options.TypingDeadlineMilliseconds, cancellationToken);
            var typeOptions = new LocatorPressSequentiallyOptions { Timeout = deadline.RemainingMilliseconds };
            if (replaceExisting)
            {
                await locator.FillAsync(text, new LocatorFillOptions { Timeout = deadline.RemainingMilliseconds })
                    .ConfigureAwait(false);
            }
            else
            {
                await locator.PressSequentiallyAsync(text, typeOptions).ConfigureAwait(false);
            }
            return;
        }

        await ClickCoreAsync(locator, cancellationToken).ConfigureAwait(false);
        if (replaceExisting)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await locator.Page.Keyboard.PressAsync("Control+A").ConfigureAwait(false);
            await DelayAsync(30, 90, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await locator.Page.Keyboard.PressAsync("Backspace").ConfigureAwait(false);
        }

        foreach (var rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_random.NextDouble() < _options.ThinkingPauseProbability)
            {
                await DelayAsync(
                    _options.ThinkingPauseMinimumMilliseconds,
                    _options.ThinkingPauseMaximumMilliseconds,
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await locator.Page.Keyboard.TypeAsync(rune.ToString()).ConfigureAwait(false);
            await DelayAsync(
                _options.KeyMinimumDelayMilliseconds,
                _options.KeyMaximumDelayMilliseconds,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ScrollAsync(IPage page, double deltaY, CancellationToken cancellationToken = default) => ScrollAsync(page, 0, deltaY, cancellationToken);

    public Task ScrollAsync(IPage page, double deltaX, double deltaY, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        page = PlaywrightHumanizer.Unwrap(page);
        return RunDirectAsync(page, _options.ActionDeadlineMilliseconds, cancellationToken, token => ScrollCoreAsync(page, deltaX, deltaY, token));
    }

    internal async Task ScrollCoreAsync(IPage page, double deltaX, double deltaY, CancellationToken cancellationToken)
    {
        var steps = _random.Next(_options.ScrollMinimumSteps, _options.ScrollMaximumSteps + 1);
        var weights = Enumerable.Range(0, steps)
            .Select(index => Math.Sin(Math.PI * (index + 1d) / (steps + 1d)))
            .ToArray();
        var totalWeight = weights.Sum();
        foreach (var weight in weights)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.Mouse.WheelAsync(
                (float)(deltaX * weight / totalWeight),
                (float)(deltaY * weight / totalWeight)).ConfigureAwait(false);
            await DelayAsync(15, 55, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task TypeFocusedAsync(IPage page, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(text);
        page = PlaywrightHumanizer.Unwrap(page);
        return RunDirectAsync(page, _options.TypingDeadlineMilliseconds, cancellationToken, token => TypeFocusedCoreAsync(page, text, token));
    }

    internal async Task TypeFocusedCoreAsync(IPage page, string text, CancellationToken cancellationToken)
    {
        if (CompatibilityMode == HumanizationCompatibilityMode.PlaywrightCompatible && !CanPaceText(text))
        {
            await page.Keyboard.TypeAsync(text).ConfigureAwait(false);
            return;
        }

        using var deadline = CreateDeadline(page, _options.TypingDeadlineMilliseconds, cancellationToken);
        var runes = text.EnumerateRunes().ToArray();
        for (var index = 0; index < runes.Length; index++)
        {
            var rune = runes[index];
            deadline.ThrowIfExpired();
            if (page.IsClosed) throw new InvalidOperationException("The page closed during human-paced typing.");
            if (_random.NextDouble() < _options.ThinkingPauseProbability)
            {
                await DelayAsync(_options.ThinkingPauseMinimumMilliseconds, _options.ThinkingPauseMaximumMilliseconds, deadline.Token).ConfigureAwait(false);
            }

            deadline.ThrowIfExpired();
            await page.Keyboard.TypeAsync(rune.ToString()).ConfigureAwait(false);
            if (index + 1 < runes.Length)
            {
                await DelayAsync(_options.KeyMinimumDelayMilliseconds, _options.KeyMaximumDelayMilliseconds, deadline.Token).ConfigureAwait(false);
            }
        }
    }

    public Task PressFocusedAsync(IPage page, string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        page = PlaywrightHumanizer.Unwrap(page);
        return RunDirectAsync(page, _options.ActionDeadlineMilliseconds, cancellationToken, token => PressFocusedCoreAsync(page, key, token));
    }

    internal async Task PressFocusedCoreAsync(IPage page, string key, CancellationToken cancellationToken)
    {
        using var deadline = CreateDeadline(page, _options.ActionDeadlineMilliseconds, cancellationToken);
        deadline.ThrowIfExpired();
        await page.Keyboard.PressAsync(key, new KeyboardPressOptions { Delay = Math.Min(75, deadline.RemainingMilliseconds) })
            .ConfigureAwait(false);
    }

    internal async Task CompatibleLocatorActionAsync(ILocator locator, string action, CancellationToken cancellationToken = default)
    {
        var page = locator.Page;
        if (action != "click" && action != "dblclick" && action != "hover")
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        ValidateSelectedMouseAlgorithm();
        using var deadline = CreateDeadline(page, _options.ActionDeadlineMilliseconds, cancellationToken);
        // Trial verifies actionability before preparatory pointer side effects; every stage gets only
        // the remaining part of the same monotonic budget.
        if (action == "click")
        {
            await locator.ClickAsync(new LocatorClickOptions { Trial = true, Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false);
        }
        else if (action == "dblclick")
        {
            await locator.DblClickAsync(new LocatorDblClickOptions { Trial = true, Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false);
        }
        else
        {
            await locator.HoverAsync(new LocatorHoverOptions { Trial = true, Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false);
        }

        try
        {
            await locator.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false);
            var box = await locator.BoundingBoxAsync().ConfigureAwait(false);
            if (box is not null && box.Width > 0 && box.Height > 0)
            {
                await MoveCoreAsync(page, box.X + box.Width / 2d, box.Y + box.Height / 2d, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (PlaywrightException) when (!deadline.Token.IsCancellationRequested)
        {
            // Preparation is best effort only while budget remains; final actionability belongs to Playwright.
        }

        deadline.ThrowIfExpired();
        // Exactly one native semantic action owns click/dblclick effects; never retry after it starts.
        switch (action)
        {
            case "click": await locator.ClickAsync(new LocatorClickOptions { Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false); break;
            case "dblclick": await locator.DblClickAsync(new LocatorDblClickOptions { Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false); break;
            case "hover": await locator.HoverAsync(new LocatorHoverOptions { Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false); break;
        }
    }

    internal static bool CanPaceText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Rune enumeration substitutes U+FFFD for malformed UTF-16. Reject malformed input
        // rather than claiming paced/native parity for text that Playwright may reject.
        for (var offset = 0; offset < text.Length;)
        {
            var status = System.Text.Rune.DecodeFromUtf16(text.AsSpan(offset), out _, out var consumed);
            if (status != System.Buffers.OperationStatus.Done) return false;
            offset += consumed;
        }

        var elements = System.Globalization.StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            if (elements.GetTextElement().EnumerateRunes().Skip(1).Any()) return false;
        }

        return true;
    }

    internal async Task CompatibleTypeAsync(ILocator locator, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        using var deadline = CreateDeadline(locator.Page, _options.TypingDeadlineMilliseconds, cancellationToken);
        await locator.FocusAsync(new LocatorFocusOptions { Timeout = deadline.RemainingMilliseconds }).ConfigureAwait(false);
        var runes = text.EnumerateRunes().ToArray();
        for (var index = 0; index < runes.Length; index++)
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (locator.Page.IsClosed)
            {
                throw new InvalidOperationException("The page closed during human-paced typing.");
            }

            deadline.Token.ThrowIfCancellationRequested();
            await locator.Page.Keyboard.TypeAsync(runes[index].ToString(), new KeyboardTypeOptions { Delay = 0 })
                .ConfigureAwait(false);
            if (index + 1 < runes.Length)
            {
                await DelayAsync(_options.KeyMinimumDelayMilliseconds, _options.KeyMaximumDelayMilliseconds, deadline.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task<(IPage Page, double X, double Y)> MoveToLocatorAsync(
        ILocator locator,
        CancellationToken cancellationToken)
    {
        ValidateSelectedMouseAlgorithm();
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible })
            .ConfigureAwait(false);
        await locator.ClickAsync(new LocatorClickOptions { Trial = true }).ConfigureAwait(false);
        await locator.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
        var box = await locator.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The locator has no visible bounding box.");
        var targetX = box.X + box.Width * NextDouble(0.25, 0.75);
        var targetY = box.Y + box.Height * NextDouble(0.25, 0.75);
        var page = locator.Page;
        await MoveCoreAsync(page, targetX, targetY, cancellationToken).ConfigureAwait(false);
        return (page, targetX, targetY);
    }

    private void ValidateSelectedMouseAlgorithm()
    {
        if (_options.MouseAlgorithm == MouseTrajectoryAlgorithm.Cursory) _cursory.ValidateReady();
    }

    private async Task ClickCurrentPositionAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await page.Mouse.DownAsync().ConfigureAwait(false);
        try
        {
            await DelayAsync(
                _options.ClickHoldMinimumMilliseconds,
                _options.ClickHoldMaximumMilliseconds,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // This method releases only the button it pressed; it never spans public calls.
            await page.Mouse.UpAsync().ConfigureAwait(false);
        }
    }

    private async Task MoveMouseAsync(
        IPage page,
        double startX,
        double startY,
        double targetX,
        double targetY,
        Action<TrajectoryPoint> onConfirmed,
        CancellationToken cancellationToken)
    {
        foreach (var point in _trajectory.Create(startX, startY, targetX, targetY, _random, _options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.Mouse.MoveAsync((float)point.X, (float)point.Y).ConfigureAwait(false);
            onConfirmed(point);
            await Task.Delay(point.DelayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task RunDirectAsync(IPage page, int budgetMilliseconds, CancellationToken cancellationToken, Func<CancellationToken, Task> operation) =>
        PageInputState.For(page).RunAsync(operation, budgetMilliseconds, cancellationToken);

    internal void InvalidateMousePosition(IPage page) => _mouseStates.GetValue(page, _ => new PageMouseState()).InvalidatePosition();

    internal void ObserveRawMouseMove(IPage page, double x, double y) =>
        _mouseStates.GetValue(page, _ => new PageMouseState()).ConfirmPosition(x, y);

    internal void ObserveRawMouseButton(IPage page, string button, bool down) =>
        _mouseStates.GetValue(page, _ => new PageMouseState()).ObserveButton(button, down);

    private double NextDouble(double minimum, double maximum) =>
        minimum + _random.NextDouble() * (maximum - minimum);

    private static InteractionDeadline CreateDeadline(IPage page, int configuredMilliseconds, CancellationToken cancellationToken)
    {
        var remaining = PageInputState.GetRemainingBudgetMilliseconds(page, cancellationToken);
        var budget = remaining.HasValue ? Math.Min(configuredMilliseconds, remaining.Value) : configuredMilliseconds;
        return new InteractionDeadline(TimeSpan.FromMilliseconds(budget), cancellationToken);
    }

    private Task DelayAsync(int minimum, int maximum, CancellationToken cancellationToken) =>
        Task.Delay(_random.Next(minimum, maximum + 1), cancellationToken);
}
