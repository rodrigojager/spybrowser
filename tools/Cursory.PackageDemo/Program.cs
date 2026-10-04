using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SpyBrowser.Cursory;

const string datasetSha256 = "1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203";
var assembly = typeof(CursoryTrajectoryGenerator).Assembly;
var resourceName = "SpyBrowser.Cursory.trajectories.json.gz";
string HashResource()
{
    using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException($"Missing package resource {resourceName}");
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
var before = HashResource();
if (before != datasetSha256) throw new InvalidDataException($"Unexpected resource SHA-256 {before}");
var start = new TrajectoryPoint(-12.5, 25.25);
var end = new TrajectoryPoint(500, 280.75);
Trajectory Generate() => CursoryTrajectoryGenerator.Generate(start, end,
    new TrajectoryOptions { Seed = 42, Frequency = 60 });
var first = Generate();
var repeat = Generate();
if (!first.Points.SequenceEqual(repeat.Points) || !first.Timings.SequenceEqual(repeat.Timings))
    throw new InvalidOperationException("Seeded generation was not repeatable.");
if (first.Points[0] != start || first.Points[^1] != end)
    throw new InvalidOperationException("Trajectory endpoints differ from the request.");
if (first.Points.Count != first.Timings.Count || first.Points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)) ||
    first.Timings.Any(t => !double.IsFinite(t)) || !first.Timings.Zip(first.Timings.Skip(1), (a, b) => b >= a).All(ok => ok))
    throw new InvalidOperationException("Trajectory invariants failed.");
var concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
{
    await Task.Yield();
    return CursoryTrajectoryGenerator.Generate(start, end, new TrajectoryOptions { Seed = (UInt128)(42 + i) });
}));
if (concurrent.Length != 8 || concurrent.Any(t => t.Points[0] != start || t.Points[^1] != end))
    throw new InvalidOperationException("Concurrent package generation failed.");
try
{
    CursoryTrajectoryGenerator.Generate(new(double.NaN, 0), end);
    throw new InvalidOperationException("Invalid input was accepted.");
}
catch (ArgumentOutOfRangeException) { }
var after = HashResource();
if (before != after) throw new InvalidOperationException("Embedded dataset changed during generation.");
var consumerAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Consumer entry assembly unavailable.");
var dependenciesPath = Path.ChangeExtension(consumerAssembly.Location, ".deps.json");
Console.WriteLine($"Assembly={assembly.Location}");
Console.WriteLine($"TargetFramework={RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Trajectory points={first.Points.Count}; start={first.Points[0]}; end={first.Points[^1]}");
Console.WriteLine($"Repeatable=true; concurrentGenerations={concurrent.Length}; resourceSHA256={after}");
Console.WriteLine($"Dataset provenance=JWriter20/cursory-js@16fff97fab05bb6b0c6753b2dc136a7692634cec; records=2356; Python records=2357; removed id=2281");
Console.WriteLine($"Runtime dependencies:");
using (var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(dependenciesPath)))
{
    foreach (var library in json.RootElement.GetProperty("libraries").EnumerateObject())
    {
        Console.WriteLine($"  {library.Name}");
        if (!library.Name.StartsWith("Cursory.PackageDemo/", StringComparison.Ordinal) &&
            !library.Name.StartsWith("SpyBrowser.Cursory/", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected non-BCL runtime dependency: {library.Name}");
    }
}
Console.WriteLine("PASS: package-only consumer completed with BCL dependencies only.");
