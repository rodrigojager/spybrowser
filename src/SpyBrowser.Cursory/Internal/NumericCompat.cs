namespace SpyBrowser.Cursory.Internal;

/// <summary>Small numeric compatibility helpers used by the pinned upstream algorithm.</summary>
internal static class NumericCompat
{
    private const double Epsilon = 2.220446049250313e-16;

    internal static double Log1P(double value)
    {
        if (value == -1) return double.NegativeInfinity;
        if (value < -1 || double.IsNaN(value)) return double.NaN;
        if (Math.Abs(value) > 1e-4) return Math.Log(1 + value);

        // Compensate for rounding in 1+x, then evaluate log around the exact sum.
        double sum = 1 + value;
        double correction = (value - (sum - 1)) / sum;
        return Math.Log(sum) + correction;
    }

    internal static double Hypot(double x, double y)
    {
        const double minimumNormal = 2.2250738585072014e-308;
        const double splitter = 134217729;
        x = Math.Abs(x);
        y = Math.Abs(y);
        bool hasNaN = double.IsNaN(x) || double.IsNaN(y);
        double max = 0;
        if (x > max) max = x;
        if (y > max) max = y;
        if (double.IsPositiveInfinity(max)) return double.PositiveInfinity;
        if (hasNaN) return double.NaN;
        if (max == 0) return max;

        int exponent = FrexpExponent(max);
        if (exponent < -1023) return minimumNormal * Hypot(x / minimumNormal, y / minimumNormal);
        double scale = Math.Pow(2, -exponent);
        double sum = 1, productResidue = 0, sumResidue = 0;
        foreach (double coordinate in new[] { x, y })
        {
            double scaled = coordinate * scale;
            double square = scaled * scaled;
            double squareLow = TwoProductResidue(scaled, scaled, square, splitter);
            double next = sum + square;
            sumResidue += sum - next + square;
            sum = next;
            productResidue += squareLow;
        }

        double root = Math.Sqrt(sum - 1 + (productResidue + sumResidue));
        double rootSquare = -root * root;
        double rootSquareLow = TwoProductResidue(-root, root, rootSquare, splitter);
        double correctedSum = sum + rootSquare;
        sumResidue += sum - correctedSum + rootSquare;
        sum = correctedSum;
        productResidue += rootSquareLow;
        root += (sum - 1 + (productResidue + sumResidue)) / (2 * root);
        return root / scale;
    }

    private static double TwoProductResidue(double x, double y, double product, double splitter)
    {
        double xSplit = splitter * x;
        double xHigh = xSplit - (xSplit - x);
        double xLow = x - xHigh;
        double ySplit = splitter * y;
        double yHigh = ySplit - (ySplit - y);
        double yLow = y - yHigh;
        return xHigh * yHigh - product + xHigh * yLow + xLow * yHigh + xLow * yLow;
    }

    private static int FrexpExponent(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        int biased = (int)((bits >> 52) & 0x7ff);
        if (biased == 0)
        {
            bits = BitConverter.DoubleToInt64Bits(value * Math.Pow(2, 64));
            return (int)((bits >> 52) & 0x7ff) - 1022 - 64;
        }
        return biased - 1022;
    }

    /// <summary>NumPy's eight-lane pairwise reduction for the short arrays used here.</summary>
    internal static double PairwiseSum(IReadOnlyList<double> values)
    {
        int count = values.Count;
        if (count == 0) return 0;
        if (count < 8)
        {
            double sum = -0.0;
            for (int i = 0; i < count; i++) sum += values[i];
            return sum;
        }

        if (count <= 128)
        {
            Span<double> lanes = stackalloc double[8];
            for (int lane = 0; lane < 8; lane++) lanes[lane] = values[lane];
            int i = 8;
            for (; i < count - count % 8; i += 8)
                for (int lane = 0; lane < 8; lane++) lanes[lane] += values[i + lane];
            double total = lanes[0] + lanes[1] + (lanes[2] + lanes[3]) +
                           (lanes[4] + lanes[5] + (lanes[6] + lanes[7]));
            for (; i < count; i++) total += values[i];
            return total;
        }

        int half = count / 2;
        half -= half % 8;
        var left = new double[half];
        var right = new double[count - half];
        for (int i = 0; i < half; i++) left[i] = values[i];
        for (int i = half; i < count; i++) right[i - half] = values[i];
        return PairwiseSum(left) + PairwiseSum(right);
    }
}
