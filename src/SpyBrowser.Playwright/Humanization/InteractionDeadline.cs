using System.Diagnostics;

namespace SpyBrowser.Playwright.Humanization;

/// <summary>A monotonic total budget for one interaction and its SDK-owned pauses.</summary>
internal sealed class InteractionDeadline : IDisposable
{
    private readonly CancellationTokenSource _source;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly TimeSpan _budget;

    public InteractionDeadline(TimeSpan budget, CancellationToken cancellationToken)
    {
        if (budget <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(budget));
        _budget = budget;
        _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _source.CancelAfter(budget);
    }

    public CancellationToken Token => _source.Token;

    public int RemainingMilliseconds
    {
        get
        {
            Token.ThrowIfCancellationRequested();
            var remaining = _budget - Stopwatch.GetElapsedTime(_started);
            if (remaining <= TimeSpan.Zero)
            {
                _source.Cancel();
                Token.ThrowIfCancellationRequested();
            }

            return Math.Max(1, (int)Math.Ceiling(remaining.TotalMilliseconds));
        }
    }

    public void ThrowIfExpired() => _ = RemainingMilliseconds;

    public void Dispose() => _source.Dispose();
}
