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
    public void MatchesPinnedIntermediateStageFixtures()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stage-parity.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        string manifestPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "upstream-manifest.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        string expectedHash = manifest.RootElement.GetProperty("stageFixtures").GetProperty("sha256").GetString()!;
        string actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        Assert.Equal(expectedHash, actualHash);
        Assert.Equal("cursory-js@16fff97fab05bb6b0c6753b2dc136a7692634cec", document.RootElement.GetProperty("source").GetString());
        Assert.Equal("1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203", document.RootElement.GetProperty("datasetSha256").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("cases").GetArrayLength());
        Assert.Equal(230.2000002861023, Internal.Dataset.Shared.Value[1665].Timing[0]);

        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = testCase.GetProperty("input");
            var start = input.GetProperty("start");
            var end = input.GetProperty("end");
            var actual = new Dictionary<string, object?>();
            var trajectory = CursoryTrajectoryGenerator.GenerateWithTrace(
                new(start[0].GetDouble(), start[1].GetDouble()), new(end[0].GetDouble(), end[1].GetDouble()),
                new TrajectoryOptions
                {
                    Frequency = input.GetProperty("frequency").GetDouble(),
                    FrequencyRandomizer = input.GetProperty("frequencyRandomizer").GetDouble(),
                    Seed = (UInt128)input.GetProperty("seed").GetUInt64(),
                    Directness = input.GetProperty("directness").GetDouble()
                },
                (stage, value) => actual.Add(stage, value));
            Assert.Equal(trajectory.Points.Count, trajectory.Timings.Count);

            var expected = testCase.GetProperty("stages");
            Assert.Equal(expected.EnumerateObject().Select(property => property.Name).Order(), actual.Keys.Order());
            foreach (var property in expected.EnumerateObject())
            {
                using var actualValue = JsonDocument.Parse(JsonSerializer.Serialize(actual[property.Name]));
                double tolerance = property.Name == "selection" ? 1e-12 : 1e-9;
                AssertJsonEquivalent(property.Value, actualValue.RootElement, tolerance, property.Name);
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
            var observedNormals = new List<double>();
            foreach (var expected in vector.GetProperty("normals").EnumerateArray())
            {
                double actual = normals.StandardNormal();
                Assert.Equal(expected.GetDouble(), actual);
                observedNormals.Add(actual);
            }
            if (seed == 0) Assert.Contains(observedNormals, value => value < -3.654152885361008);
            if (seed == 1) Assert.Contains(observedNormals, value => value > 3.654152885361008);

            var integers = new Internal.RandomSampler(new Internal.Pcg64(seed));
            int[] bounds = [1, 2, 3, 5, 86, 256, 1000, 2356, 65536, 1000000];
            for (int i = 0; i < bounds.Length; i++)
                Assert.Equal(vector.GetProperty("integers")[i].GetInt32(), integers.Integer(bounds[i]));

            var rejection = new Internal.RandomSampler(new Internal.Pcg64(seed));
            var rejectingValues = vector.GetProperty("rejectingIntegers").EnumerateArray().ToArray();
            foreach (var expected in rejectingValues)
                Assert.Equal(expected.GetInt32(), rejection.Integer(1_000_000));
        }

        var forcedRejectionStream = new Internal.RandomSampler(new Internal.Pcg64((UInt128)0));
        var expectedRejections = document.RootElement.GetProperty("integerRejectionProbe").EnumerateArray().ToArray();
        foreach (var expected in expectedRejections)
            Assert.Equal(expected.GetInt32(), forcedRejectionStream.Integer(2_000_000_000));
        Assert.True(CountIntegerRejections(0, 2_000_000_000, expectedRejections.Length) > 0);
    }

    [Fact]
    public async Task ThousandsOfConcurrentCallsUseSharedImmutableDataset()
    {
        var recordings = Internal.Dataset.Shared.Value;
        byte[] before = HashDataset(recordings);
        var trajectories = await Task.WhenAll(Enumerable.Range(0, 1_000).Select(index => Task.Run(() =>
            CursoryTrajectoryGenerator.Generate(
                new(index / 7.0, -index / 3.0), new(200 + index / 5.0, index / 11.0),
                new() { Seed = (UInt128)(uint)index }))));

        Assert.Equal(1_000, trajectories.Length);
        Assert.All(trajectories, trajectory => Assert.Equal(trajectory.Points.Count, trajectory.Timings.Count));
        Assert.Equal(before, HashDataset(recordings));
    }

    [Fact]
    public void IntegerPathJitterRetainsTruncationAndNegativeSamples()
    {
        var integerDiagonal = Enumerable.Range(0, 64).Select(index => new TrajectoryPoint(index, index)).ToArray();
        var truncatedNormals = Internal.TrajectoryTransforms.Jitter(
            integerDiagonal, 63, new Internal.RandomSampler(new Internal.Pcg64((UInt128)1)));
        Assert.Equal(integerDiagonal, truncatedNormals);

        var vertical = Enumerable.Range(0, 64).Select(index => new TrajectoryPoint(0, index)).ToArray();
        var jittered = Internal.TrajectoryTransforms.Jitter(
            vertical, 63, new Internal.RandomSampler(new Internal.Pcg64((UInt128)1)));
        Assert.Contains(jittered, point => point.X < 0);
        Assert.Contains(jittered, point => point.X > 0);
        Assert.Equal(vertical.Select(point => point.Y), jittered.Select(point => point.Y));
    }

    [Fact]
    public void NumericCompatibilityCoversExtremeHypotPairwiseSumAndLog1p()
    {
        Assert.True(double.IsFinite(Internal.NumericCompat.Hypot(1e308, 1e308)));
        Assert.InRange(Math.Abs(Math.Sqrt(2) - Internal.NumericCompat.Hypot(1, 1)), 0, 1e-15);
        Assert.Equal(4, Internal.NumericCompat.PairwiseSum(new[] { 1e16, 1d, -1e16, 1d, 1d, 1d, 1d, 1d }));
        Assert.InRange(Math.Abs(-1e-12 - Internal.NumericCompat.Log1P(-1e-12)), 0, 1e-24);
    }

    private static int CountIntegerRejections(UInt128 seed, int exclusiveUpperBound, int count)
    {
        var bitGenerator = new Internal.Pcg64(seed);
        uint maximum = (uint)(exclusiveUpperBound - 1);
        ulong range = (ulong)maximum + 1;
        ulong threshold = (uint.MaxValue - maximum) % range;
        int rejected = 0;
        for (int i = 0; i < count; i++)
        {
            while (true)
            {
                ulong product = (ulong)bitGenerator.NextUInt32() * range;
                if ((uint)product >= threshold) break;
                rejected++;
            }
        }
        return rejected;
    }

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, double tolerance, string path)
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToArray();
                var actualProperties = actual.EnumerateObject().ToDictionary(property => property.Name);
                Assert.Equal(expectedProperties.Length, actualProperties.Count);
                foreach (var property in expectedProperties)
                    AssertJsonEquivalent(property.Value, actualProperties[property.Name].Value, tolerance, $"{path}.{property.Name}");
                break;
            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                Assert.Equal(expectedItems.Length, actualItems.Length);
                for (int i = 0; i < expectedItems.Length; i++)
                    AssertJsonEquivalent(expectedItems[i], actualItems[i], tolerance, $"{path}[{i}]");
                break;
            case JsonValueKind.Number:
                double expectedNumber = expected.GetDouble();
                double actualNumber = actual.GetDouble();
                if (path.Contains("candidateRecordingIndices", StringComparison.Ordinal) ||
                    path.EndsWith("selectedCandidate", StringComparison.Ordinal) ||
                    path.EndsWith("selectedRecordingIndex", StringComparison.Ordinal) ||
                    path.Contains("timings", StringComparison.Ordinal) ||
                    path.Contains("sampledTimings", StringComparison.Ordinal))
                    Assert.Equal(expectedNumber, actualNumber);
                else
                    Assert.True(Math.Abs(expectedNumber - actualNumber) <= tolerance,
                        $"{path}: expected {expectedNumber:R}, actual {actualNumber:R}, tolerance {tolerance:R}.");
                break;
            default:
                Assert.Equal(expected.ToString(), actual.ToString());
                break;
        }
    }

    private static byte[] HashDataset(Internal.Recording[] recordings)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            foreach (var recording in recordings)
            {
                writer.Write(recording.Length);
                foreach (var point in recording.Points) { writer.Write(point.X); writer.Write(point.Y); }
                foreach (double timing in recording.Timing) writer.Write(timing);
            }
        }
        return System.Security.Cryptography.SHA256.HashData(stream.ToArray());
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
