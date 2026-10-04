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
    private double? _cursorX;
    private double? _cursorY;
    private readonly IMouseTrajectoryStrategy _trajectory = new BezierTrajectoryStrategy();

    internal HumanizationCompatibilityMode CompatibilityMode => _options.CompatibilityMode;

    public HumanActions(HumanInteractionOptions? options = null)
    {
        _options = options ?? new HumanInteractionOptions();
        _options.Validate();
        _random = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
    }

    public async Task ClickAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var (page, _, _) = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
    }

    public async Task DoubleClickAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        var (page, _, _) = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
        await DelayAsync(
            _options.DoubleClickIntervalMinimumMilliseconds,
            _options.DoubleClickIntervalMaximumMilliseconds,
            cancellationToken).ConfigureAwait(false);
        await ClickCurrentPositionAsync(page, cancellationToken).ConfigureAwait(false);
    }

    public async Task HoverAsync(ILocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        _ = await MoveToLocatorAsync(locator, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(
        IPage page,
        double targetX,
        double targetY,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        var viewport = page.ViewportSize;
        var startX = _cursorX ?? (viewport?.Width ?? 1280) / 2d;
        var startY = _cursorY ?? (viewport?.Height ?? 720) / 2d;
        await MoveMouseAsync(page, startX, startY, targetX, targetY, cancellationToken).ConfigureAwait(false);
        _cursorX = targetX;
        _cursorY = targetY;
    }

    public async Task ClickAsync(
        IPage page,
        double targetX,
        double targetY,
        bool doubleClick = false,
        CancellationToken cancellationToken = default)
    {
        await MoveAsync(page, targetX, targetY, cancellationToken).ConfigureAwait(false);
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

    public async Task PressAsync(
        ILocator locator,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        await HoverAsync(locator, cancellationToken).ConfigureAwait(false);
        await locator.FocusAsync().ConfigureAwait(false);
        await PressFocusedAsync(locator.Page, key, cancellationToken).ConfigureAwait(false);
    }

    public async Task TypeAsync(
        ILocator locator,
        string text,
        bool replaceExisting = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(text);
        await ClickAsync(locator, cancellationToken).ConfigureAwait(false);
        if (replaceExisting)
        {
            await locator.Page.Keyboard.PressAsync("Control+A").ConfigureAwait(false);
            await DelayAsync(30, 90, cancellationToken).ConfigureAwait(false);
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

            await locator.Page.Keyboard.TypeAsync(rune.ToString()).ConfigureAwait(false);
            await DelayAsync(
                _options.KeyMinimumDelayMilliseconds,
                _options.KeyMaximumDelayMilliseconds,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ScrollAsync(
        IPage page,
        double deltaY,
        CancellationToken cancellationToken = default)
        => await ScrollAsync(page, 0, deltaY, cancellationToken).ConfigureAwait(false);

    public async Task ScrollAsync(
        IPage page,
        double deltaX,
        double deltaY,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
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

    public async Task TypeFocusedAsync(
        IPage page,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(text);
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

            await page.Keyboard.TypeAsync(rune.ToString()).ConfigureAwait(false);
            await DelayAsync(
                _options.KeyMinimumDelayMilliseconds,
                _options.KeyMaximumDelayMilliseconds,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task PressFocusedAsync(
        IPage page,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await page.Keyboard.DownAsync(key).ConfigureAwait(false);
        await DelayAsync(35, 115, cancellationToken).ConfigureAwait(false);
        await page.Keyboard.UpAsync(key).ConfigureAwait(false);
    }

    internal async Task CompatibleLocatorActionAsync(ILocator locator, string action, CancellationToken cancellationToken = default)
    {
        var page = locator.Page;
        if (action != "click" && action != "dblclick" && action != "hover")
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        // Trial verifies Playwright actionability before any preparatory pointer side effects.
        if (action == "click")
        {
            await locator.ClickAsync(new LocatorClickOptions { Trial = true }).ConfigureAwait(false);
        }
        else if (action == "dblclick")
        {
            await locator.DblClickAsync(new LocatorDblClickOptions { Trial = true }).ConfigureAwait(false);
        }
        else
        {
            await locator.HoverAsync(new LocatorHoverOptions { Trial = true }).ConfigureAwait(false);
        }

        try
        {
            await locator.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            var box = await locator.BoundingBoxAsync().ConfigureAwait(false);
            if (box is not null && box.Width > 0 && box.Height > 0)
            {
                var x = box.X + box.Width / 2d;
                var y = box.Y + box.Height / 2d;
                await MoveAsync(page, x, y, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (PlaywrightException) when (!cancellationToken.IsCancellationRequested)
        {
            // Preparation is best effort; the one native action below owns final actionability/errors.
        }

        // Always leave the semantic action (including dblclick/detail and revalidation) to Playwright.
        switch (action)
        {
            case "click": await locator.ClickAsync().ConfigureAwait(false); break;
            case "dblclick": await locator.DblClickAsync().ConfigureAwait(false); break;
            case "hover": await locator.HoverAsync().ConfigureAwait(false); break;
        }
    }

    internal async Task CompatibleTypeAsync(ILocator locator, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        using var deadline = new InteractionDeadline(TimeSpan.FromMilliseconds(_options.TypingDeadlineMilliseconds), cancellationToken);
        await locator.FocusAsync().ConfigureAwait(false);
        var runes = text.EnumerateRunes().ToArray();
        for (var index = 0; index < runes.Length; index++)
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (locator.Page.IsClosed)
            {
                throw new InvalidOperationException("The page closed during human-paced typing.");
            }

            await locator.Page.Keyboard.TypeAsync(runes[index].ToString()).ConfigureAwait(false);
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
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible })
            .ConfigureAwait(false);
        await locator.ClickAsync(new LocatorClickOptions { Trial = true }).ConfigureAwait(false);
        await locator.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
        var box = await locator.BoundingBoxAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The locator has no visible bounding box.");
        var targetX = box.X + box.Width * NextDouble(0.25, 0.75);
        var targetY = box.Y + box.Height * NextDouble(0.25, 0.75);
        var page = locator.Page;
        await MoveAsync(page, targetX, targetY, cancellationToken).ConfigureAwait(false);
        return (page, targetX, targetY);
    }

    private async Task ClickCurrentPositionAsync(IPage page, CancellationToken cancellationToken)
    {
        await page.Mouse.DownAsync().ConfigureAwait(false);
        await DelayAsync(
            _options.ClickHoldMinimumMilliseconds,
            _options.ClickHoldMaximumMilliseconds,
            cancellationToken).ConfigureAwait(false);
        await page.Mouse.UpAsync().ConfigureAwait(false);
    }

    private async Task MoveMouseAsync(
        IPage page,
        double startX,
        double startY,
        double targetX,
        double targetY,
        CancellationToken cancellationToken)
    {
        foreach (var point in _trajectory.Create(startX, startY, targetX, targetY, _random, _options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.Mouse.MoveAsync((float)point.X, (float)point.Y).ConfigureAwait(false);
            await Task.Delay(point.DelayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    private double NextDouble(double minimum, double maximum) =>
        minimum + _random.NextDouble() * (maximum - minimum);

    private Task DelayAsync(int minimum, int maximum, CancellationToken cancellationToken) =>
        Task.Delay(_random.Next(minimum, maximum + 1), cancellationToken);
}
