using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

public sealed class MonotonicMovementSchedulerTests
{
    [Fact]
    public async Task Coalesces_samples_overdue_after_serial_transport_latency_and_keeps_endpoint()
    {
        var clock = new FakeClock();
        var sent = new List<(double X, double Y, long At)>();
        var scheduler = new MonotonicMovementScheduler(clock);
        var points = new[]
        {
            new TimedMousePoint(1, 1, 0),
            new TimedMousePoint(2, 2, 10),
            new TimedMousePoint(3, 3, 20),
            new TimedMousePoint(4.5, 5.25, 30)
        };

        await scheduler.ExecuteAsync(
            () => false,
            (x, y) =>
            {
                sent.Add((x, y, clock.GetTimestamp()));
                clock.Advance(25);
                return Task.CompletedTask;
            },
            points,
            TimeSpan.FromMilliseconds(200),
            _ => { },
            CancellationToken.None);

        Assert.Equal(new[] { 1d, 3d, 4.5d }, sent.Select(item => item.X));
        Assert.Equal(new long[] { 0, 25, 50 }, sent.Select(item => item.At));
        Assert.Equal(5.25, sent[^1].Y);
    }

    [Fact]
    public async Task Deadline_stops_future_sends_without_claiming_an_unsent_endpoint()
    {
        var clock = new FakeClock();
        var sent = new List<double>();
        var scheduler = new MonotonicMovementScheduler(clock);
        await Assert.ThrowsAsync<TimeoutException>(() => scheduler.ExecuteAsync(
            () => false,
            (x, _) =>
            {
                sent.Add(x);
                clock.Advance(20);
                return Task.CompletedTask;
            },
            [new(1, 1, 0), new(2, 2, 30)],
            TimeSpan.FromMilliseconds(25),
            _ => { },
            CancellationToken.None));
        Assert.Equal([1d], sent);
    }

    [Fact]
    public async Task Cancellation_during_wait_prevents_a_send()
    {
        using var cancellation = new CancellationTokenSource();
        var clock = new FakeClock { CancelOnDelay = cancellation };
        var sent = 0;
        var scheduler = new MonotonicMovementScheduler(clock);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scheduler.ExecuteAsync(
            () => false,
            (_, _) => { sent++; return Task.CompletedTask; },
            [new(1, 1, 50)],
            TimeSpan.FromMilliseconds(100),
            _ => { },
            cancellation.Token));
        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task Page_close_during_wait_prevents_a_send()
    {
        var clock = new FakeClock { CloseOnDelay = true };
        var sent = 0;
        var scheduler = new MonotonicMovementScheduler(clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.ExecuteAsync(
            () => clock.Closed,
            (_, _) => { sent++; return Task.CompletedTask; },
            [new(1, 1, 50)],
            TimeSpan.FromMilliseconds(100),
            _ => { },
            CancellationToken.None));
        Assert.Equal(0, sent);
    }

    private sealed class FakeClock : IMovementClock
    {
        public long Now { get; private set; }
        public bool Closed { get; private set; }
        public bool CloseOnDelay { get; init; }
        public CancellationTokenSource? CancelOnDelay { get; init; }
        public long GetTimestamp() => Now;
        public long Frequency => 1_000;
        public void Advance(long milliseconds) => Now += milliseconds;

        public Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Now = Math.Max(Now, timestamp);
            if (CloseOnDelay) Closed = true;
            CancelOnDelay?.Cancel();
            return Task.CompletedTask;
        }
    }
}
