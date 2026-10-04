namespace SpyBrowser.Playwright.Humanization;

/// <summary>Per-page mouse knowledge; only confirmed wrapped sends establish position.</summary>
internal sealed class PageMouseState
{
    private readonly HashSet<string> _buttonsDown = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _buttonsUncertain = new(StringComparer.OrdinalIgnoreCase);

    internal double? X { get; private set; }
    internal double? Y { get; private set; }
    internal bool HasKnownPosition => X.HasValue && Y.HasValue;
    internal bool HasButtonDown => _buttonsDown.Count != 0 || _buttonsUncertain.Count != 0;

    internal void ConfirmPosition(double x, double y)
    {
        X = x;
        Y = y;
    }

    internal void InvalidatePosition()
    {
        X = null;
        Y = null;
    }

    internal void ObserveButton(string button, bool isDown)
    {
        if (isDown)
        {
            _buttonsDown.Add(button);
            _buttonsUncertain.Remove(button);
        }
        else
        {
            _buttonsDown.Remove(button);
            _buttonsUncertain.Remove(button);
        }
    }

    internal void ObserveButtonUncertain(string button)
    {
        _buttonsDown.Remove(button);
        _buttonsUncertain.Add(button);
    }
}
