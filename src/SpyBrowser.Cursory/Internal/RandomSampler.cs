namespace SpyBrowser.Cursory.Internal;

/// <summary>NumPy-compatible distributions backed by one call-local PCG64 instance.</summary>
internal sealed class RandomSampler(Pcg64 bitGenerator)
{
    internal double Uniform(double low, double high) => low + (high - low) * bitGenerator.NextDouble();

    internal double Normal(double scale) => scale * StandardNormal();

    internal int Integer(int exclusiveUpperBound) => bitGenerator.Integers(exclusiveUpperBound);

    internal int Choice(double[] weights)
    {
        var cumulative = new double[weights.Length];
        double total = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            total += weights[i];
            cumulative[i] = total;
        }
        for (int i = 0; i < cumulative.Length; i++) cumulative[i] /= total;

        double draw = bitGenerator.NextDouble();
        int lower = 0, upper = cumulative.Length;
        while (lower < upper)
        {
            int middle = (lower + upper) / 2;
            if (draw < cumulative[middle]) upper = middle;
            else lower = middle + 1;
        }
        return lower;
    }

    internal double StandardNormal()
    {
        while (true)
        {
            ulong draw = bitGenerator.NextUInt64();
            int index = (int)(draw & 255);
            draw >>= 8;
            bool negative = (draw & 1) != 0;
            ulong magnitude = (draw >> 1) & 0x000fffffffffffffUL;
            double value = magnitude * ZigguratTables.WI_DOUBLE[index];
            if (negative) value = -value;
            if (magnitude < ZigguratTables.KI_DOUBLE[index]) return value;
            if (index == 0) return SampleTail(magnitude);
            if (AcceptWedge(index, value)) return value;
        }
    }

    private double SampleTail(ulong magnitude)
    {
        while (true)
        {
            double x = -ZigguratTables.ZIGGURAT_NOR_INV_R * NumericCompat.Log1P(-bitGenerator.NextDouble());
            double y = -NumericCompat.Log1P(-bitGenerator.NextDouble());
            if (y + y > x * x)
            {
                bool negative = ((magnitude >> 8) & 1) != 0;
                double tail = ZigguratTables.ZIGGURAT_NOR_R + x;
                return negative ? -tail : tail;
            }
        }
    }

    private bool AcceptWedge(int index, double value)
    {
        double wedge = (ZigguratTables.FI_DOUBLE[index - 1] - ZigguratTables.FI_DOUBLE[index]) * bitGenerator.NextDouble()
                     + ZigguratTables.FI_DOUBLE[index];
        return wedge < Math.Exp(-0.5 * value * value);
    }
}
