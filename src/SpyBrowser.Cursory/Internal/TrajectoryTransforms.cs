namespace SpyBrowser.Cursory.Internal;

/// <summary>Geometry transforms used before and after temporal resampling.</summary>
internal static class TrajectoryTransforms
{
    private const double Epsilon = 2.220446049250313e-16;
    private const int KnotCount = 5;
    private const double KnotStrength = 0.15;

    internal static TrajectoryPoint[] Morph(
        TrajectoryPoint[] points, TrajectoryPoint start, TrajectoryPoint end,
        double dx, double dy, double distance)
    {
        TrajectoryPoint originalStart = points[0];
        TrajectoryPoint originalEnd = points[^1];
        double originalDx = originalEnd.X - originalStart.X;
        double originalDy = originalEnd.Y - originalStart.Y;
        double originalDistance = Math.Sqrt(originalDx * originalDx + originalDy * originalDy);
        double scale = originalDistance != 0 ? distance / originalDistance : 1;
        double rotation = Math.Atan2(dy, dx) - Math.Atan2(originalDy, originalDx);
        double cosine = Math.Cos(rotation);
        double sine = Math.Sin(rotation);

        var result = new TrajectoryPoint[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            double x = (points[i].X - originalStart.X) * scale;
            double y = (points[i].Y - originalStart.Y) * scale;
            result[i] = new(x * cosine - y * sine + start.X, x * sine + y * cosine + start.Y);
        }
        result[0] = start;
        result[^1] = end;
        return result;
    }

    internal static TrajectoryPoint[] Jitter(TrajectoryPoint[] points, double distance, RandomSampler random)
    {
        int count = points.Length;
        var gaps = new double[count];
        var normalX = new double[count];
        var normalY = new double[count];
        var scales = new double[count];
        var noise = new double[count];
        bool wholePixelPath = points.All(point =>
            point.X == Math.Truncate(point.X) && point.Y == Math.Truncate(point.Y));

        for (int i = 0; i < count - 1; i++)
        {
            double dx = points[i + 1].X - points[i].X;
            double dy = points[i + 1].Y - points[i].Y;
            gaps[i] = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }
        for (int i = 0; i < count; i++)
        {
            double averageGap = ((i > 0 ? gaps[i - 1] : 0) + (i < count - 1 ? gaps[i] : 0)) / 2;
            scales[i] = 0.01 * (averageGap / Math.Max(averageGap, 1)) * Math.Min(1, distance / 400);
            TrajectoryPoint ahead = points[i == count - 1 ? count - 1 : i + 1];
            TrajectoryPoint behind = points[i == 0 ? 0 : i - 1];
            double tangentX = ahead.X - behind.X;
            double tangentY = ahead.Y - behind.Y;
            double tangentLength = Math.Sqrt(tangentX * tangentX + tangentY * tangentY);
            if (tangentLength > Epsilon)
            {
                normalX[i] = -tangentY / tangentLength;
                normalY[i] = tangentX / tangentLength;
                if (wholePixelPath)
                {
                    normalX[i] = Math.Truncate(normalX[i]);
                    normalY[i] = Math.Truncate(normalY[i]);
                }
            }
            noise[i] = random.StandardNormal();
        }

        var result = new TrajectoryPoint[count];
        const double correlation = 0.75;
        double innovationScale = Math.Sqrt(1 - correlation * correlation);
        double offset = 0;
        for (int i = 0; i < count; i++)
        {
            offset = i == 0 ? noise[i] : correlation * offset + innovationScale * noise[i];
            result[i] = new(
                points[i].X + normalX[i] * offset * scales[i],
                points[i].Y + normalY[i] * offset * scales[i]);
        }
        return result;
    }

    internal static TrajectoryPoint[] Knot(
        TrajectoryPoint[] points, TrajectoryPoint start, TrajectoryPoint end, RandomSampler random)
    {
        int count = points.Length;
        var offsetX = new double[count];
        var offsetY = new double[count];
        for (int knot = 0; knot < KnotCount; knot++)
        {
            double x = random.Normal((Math.Max(start.X, end.X) - Math.Min(start.X, end.X)) / 4)
                + (start.X + end.X) / 2;
            double y = random.Normal((Math.Max(start.Y, end.Y) - Math.Min(start.Y, end.Y)) / 4)
                + (start.Y + end.Y) / 2;
            x = Math.Clamp(x, Math.Min(start.X, end.X), Math.Max(start.X, end.X));
            y = Math.Clamp(y, Math.Min(start.Y, end.Y), Math.Max(start.Y, end.Y));

            var toKnotX = new double[count];
            var toKnotY = new double[count];
            var distances = new double[count];
            double maximumDistance = 0;
            for (int i = 0; i < count; i++)
            {
                toKnotX[i] = x - points[i].X;
                toKnotY[i] = y - points[i].Y;
                distances[i] = Math.Sqrt(toKnotX[i] * toKnotX[i] + toKnotY[i] * toKnotY[i]);
                maximumDistance = Math.Max(maximumDistance, distances[i]);
            }
            if (maximumDistance < 1e-6) continue;
            for (int i = 0; i < count; i++)
            {
                double strength = (1 - distances[i] / maximumDistance) * KnotStrength;
                offsetX[i] += toKnotX[i] * strength;
                offsetY[i] += toKnotY[i] * strength;
            }
        }

        var result = new TrajectoryPoint[count];
        for (int i = 0; i < count; i++)
            result[i] = new(points[i].X + SignedSqrt(offsetX[i]), points[i].Y + SignedSqrt(offsetY[i]));
        return result;
    }

    private static double SignedSqrt(double value) => Math.CopySign(Math.Sqrt(Math.Abs(value)), value);
}
