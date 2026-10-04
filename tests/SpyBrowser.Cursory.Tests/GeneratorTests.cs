using System.Text.Json;
using Xunit;

namespace SpyBrowser.Cursory.Tests;

public sealed class GeneratorTests
{
    private static JsonDocument ReadFixtures() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "parity.json")));

    [Fact]
    public void Pcg64RawDrawsMatchPinnedNumpyVector()
    {
        var rng = new Internal.RandomSampler(new Internal.Pcg64((UInt128)1));
        double[] expected = [0.5118216247002567, 0.9504636963259353, 0.14415961271963373, 0.9486494471372439, 0.31183145201048545];
        foreach (double value in expected) Assert.Equal(value, rng.Uniform(0, 1));
    }

    [Fact]
    public void EndpointsAndTimingsAreValid()
    {
        var trajectory = CursoryTrajectoryGenerator.Generate(new(4, 8), new(220, 135), new() { Seed = 42 });
        Assert.Equal(new(4, 8), trajectory.Points[0]);
        Assert.Equal(new(220, 135), trajectory.Points[^1]);
        Assert.Equal(0, trajectory.Timings[0]);
        Assert.Equal(trajectory.Points.Count, trajectory.Timings.Count);
        Assert.All(trajectory.Points, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y)));
        Assert.All(trajectory.Timings.Zip(trajectory.Timings.Skip(1)), pair => Assert.True(pair.Second >= pair.First));
    }

    [Fact]
    public void RepeatedSeedIsDeterministic()
    {
        var options = new TrajectoryOptions { Seed = UInt128.MaxValue };
        var first = CursoryTrajectoryGenerator.Generate(new(-3.25, 12), new(8.5, -90), options);
        var second = CursoryTrajectoryGenerator.Generate(new(-3.25, 12), new(8.5, -90), options);
        Assert.Equal(first.Points, second.Points);
        Assert.Equal(first.Timings, second.Timings);
    }

    [Fact]
    public void DegenerateMotionIsSinglePoint()
    {
        var point = new TrajectoryPoint(1.5, -2);
        var trajectory = CursoryTrajectoryGenerator.Generate(point, point);
        Assert.Equal(new[] { point }, trajectory.Points);
        Assert.Equal(new[] { 0d }, trajectory.Timings);
    }

    [Theory]
    [InlineData(double.NaN, 0, 1, 1)]
    [InlineData(double.PositiveInfinity, 0, 1, 1)]
    public void RejectsNonfiniteCoordinates(double x, double y, double endX, double endY) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CursoryTrajectoryGenerator.Generate(new(x, y), new(endX, endY)));

    [Fact]
    public void FixtureHashMatchesProvenanceManifest()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "parity.json");
        string manifestPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "upstream-manifest.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        string expected = manifest.RootElement.GetProperty("fixtures").GetProperty("sha256").GetString()!;
        string actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(fixturePath))).ToLowerInvariant();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MatchesPinnedUpstreamFixtures()
    {
        using var document = ReadFixtures();
        Assert.Equal(160, document.RootElement.GetProperty("cases").GetArrayLength());
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = testCase.GetProperty("input");
            var start = input.GetProperty("start");
            var end = input.GetProperty("end");
            var actual = CursoryTrajectoryGenerator.Generate(
                new(start[0].GetDouble(), start[1].GetDouble()), new(end[0].GetDouble(), end[1].GetDouble()),
                new TrajectoryOptions
                {
                    Frequency = input.GetProperty("frequency").GetDouble(),
                    FrequencyRandomizer = input.GetProperty("frequencyRandomizer").GetDouble(),
                    Seed = (UInt128)input.GetProperty("seed").GetUInt64(),
                    Directness = input.GetProperty("directness").GetDouble()
                });
            var points = testCase.GetProperty("points").EnumerateArray().ToArray();
            var timings = testCase.GetProperty("timings").EnumerateArray().ToArray();
            Assert.Equal(points.Length, actual.Points.Count);
            Assert.Equal(timings.Length, actual.Timings.Count);
            for (int i = 0; i < timings.Length; i++)
            {
                Assert.Equal(timings[i].GetDouble(), actual.Timings[i]);
                Assert.InRange(Math.Abs(points[i][0].GetDouble() - actual.Points[i].X), 0, 1e-9);
                Assert.InRange(Math.Abs(points[i][1].GetDouble() - actual.Points[i].Y), 0, 1e-9);
            }
        }
    }

    [Fact]
    public void MatchesPinnedRngBitstreamAndDistributionVectors()
    {
        using var document = ReadFixtures();
        foreach (var vector in document.RootElement.GetProperty("rng").EnumerateArray())
        {
            UInt128 seed = UInt128.Parse(vector.GetProperty("seed").GetString()!);
            var raw = new Internal.Pcg64(seed);
            foreach (var expected in vector.GetProperty("raw").EnumerateArray())
                Assert.Equal(ulong.Parse(expected.GetString()!), raw.NextUInt64());

            var doubles = new Internal.Pcg64(seed);
            foreach (var expected in vector.GetProperty("doubles").EnumerateArray())
                Assert.Equal(expected.GetDouble(), doubles.NextDouble());

            var normals = new Internal.RandomSampler(new Internal.Pcg64(seed));
            foreach (var expected in vector.GetProperty("normals").EnumerateArray())
                Assert.Equal(expected.GetDouble(), normals.StandardNormal());

            var integers = new Internal.RandomSampler(new Internal.Pcg64(seed));
            int[] bounds = [1, 2, 3, 5, 86, 256, 1000, 2356, 65536, 1000000];
            for (int i = 0; i < bounds.Length; i++)
                Assert.Equal(vector.GetProperty("integers")[i].GetInt32(), integers.Integer(bounds[i]));

            var rejection = new Internal.RandomSampler(new Internal.Pcg64(seed));
            foreach (var expected in vector.GetProperty("rejectingIntegers").EnumerateArray())
                Assert.Equal(expected.GetInt32(), rejection.Integer(1_000_000));
        }
    }

    [Fact]
    public void GenerationDoesNotMutateSharedDataset()
    {
        var recordings = Internal.Dataset.Shared.Value;
        double before = recordings[0].Points[0].X;
        var timing = recordings[0].Timing[0];
        for (int seed = 0; seed < 100; seed++)
            CursoryTrajectoryGenerator.Generate(new(seed, seed / 3.0), new(100 + seed, -seed), new() { Seed = (UInt128)(uint)seed });
        Assert.Equal(before, recordings[0].Points[0].X);
        Assert.Equal(timing, recordings[0].Timing[0]);
    }

    [Fact]
    public async Task SharedDatasetSupportsConcurrentCalls()
    {
        var trajectories = await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(() =>
            CursoryTrajectoryGenerator.Generate(new(index, 0), new(200, index + 1), new() { Seed = (UInt128)index }))));
        Assert.All(trajectories, trajectory => Assert.Equal(trajectory.Points.Count, trajectory.Timings.Count));
    }

    [Fact]
    public void ThousandsOfSeededCasesPreserveTrajectoryInvariants()
    {
        const int count = 2000;
        var random = new Random(0x43555253);
        for (int seed = 0; seed < count; seed++)
        {
            var start = new TrajectoryPoint(random.Next(-1000, 1001) + random.NextDouble(), random.Next(-1000, 1001) + random.NextDouble());
            var end = new TrajectoryPoint(random.Next(-1000, 1001) + random.NextDouble(), random.Next(-1000, 1001) + random.NextDouble());
            var options = new TrajectoryOptions { Seed = (UInt128)(uint)seed, Frequency = 20 + seed % 5 * 20, FrequencyRandomizer = seed % 3, Directness = (seed % 101) / 100.0 };
            var trajectory = CursoryTrajectoryGenerator.Generate(start, end, options);
            Assert.Equal(start, trajectory.Points[0]);
            Assert.Equal(end, trajectory.Points[^1]);
            Assert.Equal(trajectory.Points.Count, trajectory.Timings.Count);
            Assert.All(trajectory.Points, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y), $"case {seed}, seed {seed}"));
            for (int index = 0; index < trajectory.Timings.Count; index++)
            {
                Assert.True(double.IsFinite(trajectory.Timings[index]) && trajectory.Timings[index] >= 0, $"case {seed}, seed {seed}");
                if (index > 0) Assert.True(trajectory.Timings[index] >= trajectory.Timings[index - 1], $"case {seed}, seed {seed}");
            }
        }
    }

    [Fact]
    public void RejectsOverflowingDisplacementAndUnboundedSamplingParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CursoryTrajectoryGenerator.Generate(new(-1e308, 0), new(1e308, 0), new() { Seed = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CursoryTrajectoryGenerator.Generate(new(0, 0), new(100, 0), new() { Seed = 1, Frequency = 10001 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CursoryTrajectoryGenerator.Generate(new(0, 0), new(1, 1), new() { Seed = 1, FrequencyRandomizer = double.NaN }));
    }
}
