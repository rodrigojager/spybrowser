namespace SpyBrowser.Cursory.Internal;

/// <summary>Resamples recorded intervals and preserves upstream rounding/duplicate-time behavior.</summary>
internal static class TrajectoryTiming
{
    private const double Epsilon = 2.220446049250313e-16;
    private const int MaximumSampleCount = 100_000;

    internal static double[] Sample(double[] times, double frequency, double randomizer, RandomSampler random)
    {
        double totalTime = Math.Max(1, RoundToEven(times[^1] - times[0]));
        double requestedCount = RoundToEven(totalTime * frequency / 1000);
        if (!double.IsFinite(requestedCount) || requestedCount > MaximumSampleCount)
            throw new ArgumentOutOfRangeException(nameof(frequency), "Requested sample count exceeds 100000.");
        int intervalCount = Math.Max(1, (int)requestedCount);
        double[] recordedIntervals = PositiveIntervals(times);
        int offset = random.Integer(recordedIntervals.Length);
        var intervals = new double[intervalCount];
        for (int i = 0; i < intervalCount; i++)
        {
            int sourceIndex = (int)Math.Floor((i + 0.5) * recordedIntervals.Length / intervalCount);
            intervals[i] = recordedIntervals[(sourceIndex + offset) % recordedIntervals.Length];
        }
        Rescale(intervals, totalTime);

        if (randomizer != 0)
        {
            for (int i = 0; i < intervalCount; i++)
                intervals[i] = Math.Max(intervals[i] + random.Uniform(-randomizer, randomizer), Epsilon);
            Rescale(intervals, totalTime);
        }

        var result = new double[intervalCount + 1];
        double elapsed = 0;
        for (int i = 0; i < intervalCount; i++)
        {
            elapsed += intervals[i];
            result[i + 1] = RoundToEven(elapsed);
        }
        result[^1] = totalTime;
        return result;
    }

    private static double[] PositiveIntervals(double[] times)
    {
        var intervals = new List<double>(times.Length - 1);
        for (int i = 1; i < times.Length; i++)
        {
            double interval = times[i] - times[i - 1];
            if (interval > 0) intervals.Add(interval);
        }
        return intervals.Count == 0 ? [1] : intervals.ToArray();
    }

    private static void Rescale(double[] intervals, double totalTime)
    {
        double sum = NumericCompat.PairwiseSum(intervals);
        double scale = totalTime / sum;
        for (int i = 0; i < intervals.Length; i++) intervals[i] *= scale;
    }

    private static double RoundToEven(double value) => Math.Round(value, MidpointRounding.ToEven);
}
