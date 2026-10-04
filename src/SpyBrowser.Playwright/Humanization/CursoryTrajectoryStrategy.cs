using SpyBrowser.Cursory;

namespace SpyBrowser.Playwright.Humanization;

/// <summary>Adapts the standalone Cursory generator to Playwright's mouse dispatch.</summary>
internal sealed class CursoryTrajectoryStrategy
{
    internal IReadOnlyList<TimedMousePoint> Create(
        double startX,
        double startY,
        double targetX,
        double targetY,
        HumanInteractionOptions options)
    {
        var trajectory = CursoryTrajectoryGenerator.Generate(
            new SpyBrowser.Cursory.TrajectoryPoint(startX, startY),
            new SpyBrowser.Cursory.TrajectoryPoint(targetX, targetY),
            new TrajectoryOptions
            {
                Frequency = options.CursoryFrequency,
                FrequencyRandomizer = options.CursoryFrequencyRandomizer,
                Directness = options.CursoryDirectness,
                Seed = options.RandomSeed is int seed ? (UInt128)(uint)seed : null
            });

        var sourceDuration = trajectory.Timings[^1];
        var duration = Math.Clamp(
            sourceDuration,
            options.MouseMinimumDurationMilliseconds,
            options.MouseMaximumDurationMilliseconds);
        var scale = sourceDuration > 0 ? duration / sourceDuration : 1d;
        var points = new TimedMousePoint[trajectory.Points.Count];
        for (var index = 0; index < points.Length; index++)
        {
            var point = trajectory.Points[index];
            points[index] = new TimedMousePoint(point.X, point.Y, trajectory.Timings[index] * scale);
        }

        // Keep the exact requested endpoint, even if upstream sampling has no endpoint at the final time.
        if (points.Length == 0 || points[^1].X != targetX || points[^1].Y != targetY)
        {
            Array.Resize(ref points, points.Length + 1);
            points[^1] = new TimedMousePoint(targetX, targetY, duration);
        }
        else
        {
            points[^1] = points[^1] with { X = targetX, Y = targetY, OffsetMilliseconds = duration };
        }

        return Array.AsReadOnly(points);
    }
}
