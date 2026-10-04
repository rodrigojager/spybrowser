using SpyBrowser.Cursory.Internal;

namespace SpyBrowser.Cursory;

/// <summary>Standalone experimental Cursory trajectory generator. This preview does not integrate with Playwright.</summary>
public static class CursoryTrajectoryGenerator
{
    /// <summary>Generate a recorded, selected and transformed path between two finite points.</summary>
    public static Trajectory Generate(TrajectoryPoint start, TrajectoryPoint end, TrajectoryOptions? options = null) =>
        GenerateCore(start, end, options, null);

    internal static Trajectory GenerateWithTrace(
        TrajectoryPoint start, TrajectoryPoint end, TrajectoryOptions options, Action<string, object?> trace) =>
        GenerateCore(start, end, options, trace);

    private static Trajectory GenerateCore(
        TrajectoryPoint start, TrajectoryPoint end, TrajectoryOptions? options, Action<string, object?>? trace)
    {
        options ??= new TrajectoryOptions();
        ValidateInput(start, end, options);
        if (start == end) return new([start], [0]);

        var random = new RandomSampler(new Pcg64(options.Seed));
        var dataset = Dataset.Shared.Value;
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double distance = NumericCompat.Hypot(dx, dy);
        if (!double.IsFinite(dx) || !double.IsFinite(dy) || !double.IsFinite(distance))
            throw new ArgumentOutOfRangeException(nameof(end), "Coordinate displacement exceeds the supported finite range.");

        Recording recording = TrajectorySelection.Select(dataset, start, end, distance, options.Directness, random, trace).Record;
        var recordingTimes = PrepareRecordingTimes(recording, distance, trace);
        var recordingPoints = TransformRecording(recording.Points, start, end, dx, dy, distance, random, trace);
        var sampleTimes = TrajectoryTiming.Sample(recordingTimes, options.Frequency, options.FrequencyRandomizer, random);
        trace?.Invoke("sampledTimings", sampleTimes);
        var sampledPoints = Interpolate(recordingPoints, recordingTimes, sampleTimes);
        trace?.Invoke("interpolatedPoints", PointPairs(sampledPoints));
        var finalPoints = TransformSamples(sampledPoints, start, end, dx, dy, distance, random, trace);
        ValidateGeneratedPoints(finalPoints);
        return new(finalPoints, sampleTimes);
    }

    private static void ValidateInput(TrajectoryPoint start, TrajectoryPoint end, TrajectoryOptions options)
    {
        if (!double.IsFinite(start.X) || !double.IsFinite(start.Y) || !double.IsFinite(end.X) || !double.IsFinite(end.Y))
            throw new ArgumentOutOfRangeException(nameof(start), "Coordinates must be finite.");
        if (!double.IsFinite(options.Frequency) || options.Frequency <= 0 || options.Frequency > 10_000)
            throw new ArgumentOutOfRangeException(nameof(options.Frequency));
        if (!double.IsFinite(options.FrequencyRandomizer) || options.FrequencyRandomizer < 0 || options.FrequencyRandomizer > 10_000)
            throw new ArgumentOutOfRangeException(nameof(options.FrequencyRandomizer));
        if (!double.IsFinite(options.Directness) || options.Directness < 0 || options.Directness > 1)
            throw new ArgumentOutOfRangeException(nameof(options.Directness));
    }

    private static double[] PrepareRecordingTimes(Recording recording, double distance, Action<string, object?>? trace)
    {
        var times = (double[])recording.Timing.Clone();
        if (distance > 0 && distance < recording.Length)
        {
            double scale = Math.Sqrt(distance / recording.Length);
            double initialTime = times[0];
            for (int i = 0; i < times.Length; i++) times[i] = initialTime + (times[i] - initialTime) * scale;
        }

        trace?.Invoke("recording", new { points = PointPairs(recording.Points), timings = (double[])times.Clone() });
        double timeOrigin = times[0];
        for (int i = 0; i < times.Length; i++) times[i] -= timeOrigin;
        trace?.Invoke("recordingTimesNormalized", times);
        return times;
    }

    private static TrajectoryPoint[] TransformRecording(
        TrajectoryPoint[] points, TrajectoryPoint start, TrajectoryPoint end,
        double dx, double dy, double distance, RandomSampler random, Action<string, object?>? trace)
    {
        var jittered = TrajectoryTransforms.Jitter(points, distance, random);
        trace?.Invoke("recordingJitter", PointPairs(jittered));
        var knotted = TrajectoryTransforms.Knot(jittered, start, end, random);
        trace?.Invoke("recordingKnots", PointPairs(knotted));
        var morphed = TrajectoryTransforms.Morph(knotted, start, end, dx, dy, distance);
        trace?.Invoke("recordingMorph", PointPairs(morphed));
        return morphed;
    }

    private static TrajectoryPoint[] Interpolate(TrajectoryPoint[] points, double[] times, double[] sampleTimes)
    {
        var sampled = new TrajectoryPoint[sampleTimes.Length];
        int previousIndex = 0;
        for (int i = 0; i < sampled.Length; i++)
        {
            double sampleTime = sampleTimes[i];
            while (previousIndex + 1 < times.Length && times[previousIndex + 1] <= sampleTime) previousIndex++;
            int nextIndex = Math.Min(previousIndex + 1, times.Length - 1);
            double alpha = times[nextIndex] != times[previousIndex]
                ? (sampleTime - times[previousIndex]) / (times[nextIndex] - times[previousIndex])
                : 0;
            sampled[i] = new(
                points[previousIndex].X + alpha * (points[nextIndex].X - points[previousIndex].X),
                points[previousIndex].Y + alpha * (points[nextIndex].Y - points[previousIndex].Y));
        }
        return sampled;
    }

    private static TrajectoryPoint[] TransformSamples(
        TrajectoryPoint[] points, TrajectoryPoint start, TrajectoryPoint end,
        double dx, double dy, double distance, RandomSampler random, Action<string, object?>? trace)
    {
        var knotted = TrajectoryTransforms.Knot(points, start, end, random);
        trace?.Invoke("sampledKnots", PointPairs(knotted));
        var jittered = TrajectoryTransforms.Jitter(knotted, distance, random);
        trace?.Invoke("sampledJitter", PointPairs(jittered));
        var morphed = TrajectoryTransforms.Morph(jittered, start, end, dx, dy, distance);
        trace?.Invoke("sampledMorph", PointPairs(morphed));
        return morphed;
    }

    private static double[][] PointPairs(TrajectoryPoint[] points) =>
        points.Select(point => new[] { point.X, point.Y }).ToArray();

    private static void ValidateGeneratedPoints(TrajectoryPoint[] points)
    {
        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
            throw new InvalidDataException("Cursory produced a non-finite coordinate.");
    }
}
