namespace SpyBrowser.Playwright.Humanization;

internal readonly record struct TrajectoryPoint(double X, double Y, int DelayMilliseconds);

/// <summary>Internal seam between trajectory generation and Playwright input dispatch.</summary>
internal interface IMouseTrajectoryStrategy
{
    IReadOnlyList<TrajectoryPoint> Create(double startX, double startY, double targetX, double targetY, Random random, HumanInteractionOptions options);
}

internal sealed class BezierTrajectoryStrategy : IMouseTrajectoryStrategy
{
    public IReadOnlyList<TrajectoryPoint> Create(double startX, double startY, double targetX, double targetY, Random random, HumanInteractionOptions options)
    {
        double Next(double min, double max) => min + random.NextDouble() * (max - min);
        var distance = Math.Sqrt(Math.Pow(targetX - startX, 2) + Math.Pow(targetY - startY, 2));
        var duration = Math.Clamp((int)(options.MouseMinimumDurationMilliseconds + distance * Next(0.35, 0.75)), options.MouseMinimumDurationMilliseconds, options.MouseMaximumDurationMilliseconds);
        var steps = Math.Clamp(duration / 12, 8, 60);
        var normalX = -(targetY - startY);
        var normalY = targetX - startX;
        var normalLength = Math.Max(1, Math.Sqrt(normalX * normalX + normalY * normalY));
        var bend = Math.Min(100, distance * Next(-0.18, 0.18));
        var c1x = startX + (targetX - startX) * 0.33 + normalX / normalLength * bend;
        var c1y = startY + (targetY - startY) * 0.33 + normalY / normalLength * bend;
        var c2x = startX + (targetX - startX) * 0.72 - normalX / normalLength * bend * 0.45;
        var c2y = startY + (targetY - startY) * 0.72 - normalY / normalLength * bend * 0.45;
        var points = new TrajectoryPoint[steps];
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            var eased = t * t * (3 - 2 * t);
            var inverse = 1 - eased;
            var x = inverse * inverse * inverse * startX + 3 * inverse * inverse * eased * c1x + 3 * inverse * eased * eased * c2x + eased * eased * eased * targetX;
            var y = inverse * inverse * inverse * startY + 3 * inverse * inverse * eased * c1y + 3 * inverse * eased * eased * c2y + eased * eased * eased * targetY;
            points[i - 1] = new TrajectoryPoint(x, y, Math.Max(1, duration / steps));
        }
        return points;
    }
}
