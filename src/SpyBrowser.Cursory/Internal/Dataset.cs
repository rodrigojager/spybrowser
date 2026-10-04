using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace SpyBrowser.Cursory.Internal;

internal sealed record Recording(TrajectoryPoint[] Points, double[] Timing, double Length);
internal static class Dataset
{
    private const int MaxGzip = 1_000_000, MaxExpanded = 32_000_000;
    private const string ExpectedHash = "1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203";
    internal static readonly Lazy<Recording[]> Shared = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Recording[] Load()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpyBrowser.Cursory.trajectories.json.gz")
            ?? throw new InvalidDataException("Embedded Cursory dataset is missing.");
        if (resource.Length > MaxGzip) throw new InvalidDataException("Embedded Cursory gzip exceeds size limit.");
        using var compressed = new MemoryStream(); resource.CopyTo(compressed);
        if (!Convert.ToHexString(SHA256.HashData(compressed.ToArray())).Equals(ExpectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Embedded Cursory dataset SHA-256 mismatch.");
        compressed.Position = 0;
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var expanded = new MemoryStream();
        var buffer = new byte[8192]; int n;
        while ((n = gzip.Read(buffer)) != 0) { if (expanded.Length + n > MaxExpanded) throw new InvalidDataException("Cursory dataset exceeds expanded size limit."); expanded.Write(buffer, 0, n); }
        using var doc = JsonDocument.Parse(expanded.ToArray(), new JsonDocumentOptions { MaxDepth = 12 });
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() != 2356) throw new InvalidDataException("Cursory dataset recording count mismatch.");
        var result = new List<Recording>(2356); long pointCount = 0;
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var points = item.GetProperty("points").EnumerateArray().Select(p => new TrajectoryPoint(p[0].GetDouble(), p[1].GetDouble())).ToArray();
            var times = item.GetProperty("timing").EnumerateArray().Select(t => t.GetDouble()).ToArray();
            var length = item.GetProperty("length").GetDouble();
            if (points.Length < 2 || points.Length != times.Length || points.Length > 100_000 || !double.IsFinite(length) || length < 0 || points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)) || times.Any(t => !double.IsFinite(t)) || !times.Zip(times.Skip(1), (a,b) => b >= a).All(x => x))
                throw new InvalidDataException("Cursory dataset contains an invalid recording.");
            pointCount += points.Length; result.Add(new(points, times, length));
        }
        if (pointCount != 112984) throw new InvalidDataException("Cursory dataset point count mismatch.");
        return result.ToArray();
    }
}
