namespace SpyBrowser.Cursory;

/// <summary>One cursor coordinate in CSS-pixel units.</summary>
public readonly record struct TrajectoryPoint(double X, double Y);

/// <summary>A sampled cursor path. Points and millisecond offsets have equal lengths.</summary>
public sealed class Trajectory
{
    public IReadOnlyList<TrajectoryPoint> Points { get; }
    public IReadOnlyList<double> Timings { get; }

    internal Trajectory(TrajectoryPoint[] points, double[] timings)
    {
        Points = Array.AsReadOnly(points);
        Timings = Array.AsReadOnly(timings);
    }
}

/// <summary>Options for the experimental standalone recorded-trajectory generator.</summary>
public sealed record TrajectoryOptions
{
    public double Frequency { get; init; } = 60;
    public double FrequencyRandomizer { get; init; } = 1;
    public double Directness { get; init; } = 0.65;
    /// <summary>Non-negative NumPy-compatible seed; null obtains operating-system entropy.</summary>
    public UInt128? Seed { get; init; }
}
