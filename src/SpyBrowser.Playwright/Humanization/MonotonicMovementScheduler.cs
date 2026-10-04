using System.Diagnostics;
namespace SpyBrowser.Playwright.Humanization;

internal interface IMovementClock
{
    long GetTimestamp();
    long Frequency { get; }
    Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken);
}

internal sealed class StopwatchMovementClock : IMovementClock
{
    public long GetTimestamp() => Stopwatch.GetTimestamp();
    public long Frequency => Stopwatch.Frequency;

    public Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken)
    {
        var remaining = timestamp - GetTimestamp();
        if (remaining <= 0) return Task.CompletedTask;
        var delay = TimeSpan.FromSeconds((double)remaining / Frequency);
        return Task.Delay(delay, cancellationToken);
    }
}

internal readonly record struct TimedMousePoint(double X, double Y, double OffsetMilliseconds);

/// <summary>Serially dispatches a trajectory against absolute monotonic deadlines.</summary>
internal sealed class MonotonicMovementScheduler
{
    private readonly IMovementClock _clock;

    internal MonotonicMovementScheduler(IMovementClock? clock = null) => _clock = clock ?? new StopwatchMovementClock();

    internal async Task ExecuteAsync(
        Func<bool> pageIsClosed,
        Func<double, double, Task> send,
        IReadOnlyList<TimedMousePoint> points,
        TimeSpan deadline,
        Action<TimedMousePoint> onConfirmed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pageIsClosed);
        ArgumentNullException.ThrowIfNull(send);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(onConfirmed);
        if (points.Count == 0) throw new ArgumentException("A trajectory must contain at least one point.", nameof(points));
        if (deadline <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(deadline));

        var started = _clock.GetTimestamp();
        var end = started + ToTicks(deadline);
        var index = 0;
        while (index < points.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageIsClosed()) throw new InvalidOperationException("The page closed during Cursory mouse movement.");
            if (_clock.GetTimestamp() >= end) throw new TimeoutException("The Cursory mouse movement deadline expired.");

            var due = started + ToTicks(TimeSpan.FromMilliseconds(points[index].OffsetMilliseconds));
            if (due > _clock.GetTimestamp())
            {
                await _clock.DelayUntilAsync(Math.Min(due, end), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (pageIsClosed()) throw new InvalidOperationException("The page closed during Cursory mouse movement.");
                if (_clock.GetTimestamp() >= end) throw new TimeoutException("The Cursory mouse movement deadline expired.");
            }

            // Skip overdue intermediate samples and send only the newest due sample.
            var sendIndex = index;
            var now = _clock.GetTimestamp();
            while (sendIndex + 1 < points.Count &&
                   started + ToTicks(TimeSpan.FromMilliseconds(points[sendIndex + 1].OffsetMilliseconds)) <= now)
            {
                sendIndex++;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var point = points[sendIndex];
            await send(point.X, point.Y).ConfigureAwait(false);
            onConfirmed(point);
            index = sendIndex + 1;
        }
    }

    private long ToTicks(TimeSpan duration) => checked((long)(duration.TotalSeconds * _clock.Frequency));
}
