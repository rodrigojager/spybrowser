using System.Reflection;
using System.Runtime.Loader;
using SpyBrowser.Cursory;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

public sealed class NativeFaultContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_or_corrupt_embedded_dataset_fails_before_generator_returns(bool removeResourceName)
    {
        Assembly source = typeof(CursoryTrajectoryGenerator).Assembly;
        string resourceName = source.GetManifestResourceNames().Single(name => name.EndsWith("trajectories.json.gz", StringComparison.Ordinal));
        byte[] original = File.ReadAllBytes(source.Location);
        byte[] damaged;
        if (removeResourceName)
        {
            damaged = (byte[])original.Clone();
            byte[] name = System.Text.Encoding.UTF8.GetBytes(resourceName);
            int offset = IndexOf(damaged, name);
            Assert.True(offset >= 0, "Expected embedded resource name in PE metadata.");
            damaged[offset] = (byte)'X';
        }
        else
        {
            using var stream = source.GetManifestResourceStream(resourceName)!;
            byte[] gzip = new byte[stream.Length];
            stream.ReadExactly(gzip);
            damaged = (byte[])original.Clone();
            int offset = IndexOf(damaged, gzip);
            Assert.True(offset >= 0, "Expected embedded gzip payload in PE image.");
            damaged[offset + gzip.Length / 2] ^= 0x01;
        }

        var loadContext = new AssemblyLoadContext($"faulted-cursory-{Guid.NewGuid():N}", isCollectible: true);
        try
        {
            using var image = new MemoryStream(damaged, writable: false);
            Assembly isolated = loadContext.LoadFromStream(image);
            Type pointType = isolated.GetType("SpyBrowser.Cursory.TrajectoryPoint", throwOnError: true)!;
            Type optionsType = isolated.GetType("SpyBrowser.Cursory.TrajectoryOptions", throwOnError: true)!;
            object start = Activator.CreateInstance(pointType, 10d, 20d)!;
            object end = Activator.CreateInstance(pointType, 700d, 450d)!;
            object options = Activator.CreateInstance(optionsType)!;
            optionsType.GetProperty("Seed")!.SetValue(options, (UInt128)21021);
            MethodInfo generate = isolated.GetType("SpyBrowser.Cursory.CursoryTrajectoryGenerator", throwOnError: true)!
                .GetMethod("Generate", BindingFlags.Public | BindingFlags.Static)!;

            var failure = Assert.Throws<TargetInvocationException>(() => generate.Invoke(null, [start, end, options]));
            Assert.IsType<InvalidDataException>(failure.InnerException);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public async Task Repeated_transport_pauses_coalesce_each_overdue_run_and_preserve_final_endpoint()
    {
        var clock = new PausingClock();
        var scheduler = new MonotonicMovementScheduler(clock);
        var sent = new List<(double X, long At)>();
        TimedMousePoint[] path = [new(1, 1, 0), new(2, 2, 10), new(3, 3, 20), new(4, 4, 30), new(5, 5, 40), new(9.5, 8.25, 50)];

        await scheduler.ExecuteAsync(() => false, (x, _) =>
        {
            sent.Add((x, clock.GetTimestamp()));
            clock.AdvanceTransportPause();
            return Task.CompletedTask;
        }, path, TimeSpan.FromMilliseconds(200), _ => { }, CancellationToken.None);

        Assert.Equal(new[] { 1d, 3d, 5d, 9.5d }, sent.Select(item => item.X));
        Assert.Equal(new long[] { 0, 20, 40, 60 }, sent.Select(item => item.At));
    }

    [Fact]
    public async Task Transport_send_failure_is_preserved_and_stops_endpoint_and_future_sends()
    {
        var clock = new FaultClock();
        var scheduler = new MonotonicMovementScheduler(clock);
        var sent = new List<double>();
        var confirmed = new List<double>();
        var expected = new IOException("transport failed on second dispatch");

        IOException actual = await Assert.ThrowsAsync<IOException>(() => scheduler.ExecuteAsync(
            () => false,
            (x, _) =>
            {
                sent.Add(x);
                if (sent.Count == 2) throw expected;
                return Task.CompletedTask;
            },
            [new(1, 1, 0), new(2, 2, 10), new(3, 3, 20), new(9, 9, 30)],
            TimeSpan.FromMilliseconds(100),
            point => confirmed.Add(point.X),
            CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(new[] { 1d, 2d }, sent);
        Assert.Equal(new[] { 1d }, confirmed);
        Assert.DoesNotContain(9d, sent);
    }

    private static int IndexOf(byte[] source, byte[] value)
    {
        for (int i = 0; i <= source.Length - value.Length; i++)
        {
            if (source.AsSpan(i, value.Length).SequenceEqual(value)) return i;
        }
        return -1;
    }

    private sealed class PausingClock : IMovementClock
    {
        public long Now { get; private set; }
        public long Frequency => 1_000;
        private int _sendCount;
        public long GetTimestamp() => Now;
        public Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Now = Math.Max(Now, timestamp);
            return Task.CompletedTask;
        }
        public void AdvanceTransportPause()
        {
            _sendCount++;
            if (_sendCount is 1 or 2 or 3) Now += 20;
        }
    }

    private sealed class FaultClock : IMovementClock
    {
        public long Now { get; private set; }
        public long Frequency => 1_000;
        public long GetTimestamp() => Now;
        public Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Now = Math.Max(Now, timestamp);
            return Task.CompletedTask;
        }
    }
}
