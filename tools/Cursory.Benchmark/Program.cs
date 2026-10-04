using System.Diagnostics;
using SpyBrowser.Cursory;

const int warmup = 20;
const int samples = 500;
var options = new TrajectoryOptions { Seed = 42 };
var timer = Stopwatch.StartNew();
var first = CursoryTrajectoryGenerator.Generate(new(0, 0), new(1280.25, 720.5), options);
timer.Stop();
Console.WriteLine($"cold_first_generation_ms={timer.Elapsed.TotalMilliseconds:F3} points={first.Points.Count}");

for (int i = 0; i < warmup; i++)
    CursoryTrajectoryGenerator.Generate(new(i, 0), new(1280.25 + i, 720.5), options with { Seed = (UInt128)(uint)i });

var elapsed = new double[samples];
long allocatedBefore = GC.GetTotalAllocatedBytes(true);
for (int i = 0; i < samples; i++)
{
    timer.Restart();
    CursoryTrajectoryGenerator.Generate(new(i, 0), new(1280.25 + i, 720.5), options with { Seed = (UInt128)(uint)i });
    timer.Stop();
    elapsed[i] = timer.Elapsed.TotalMilliseconds;
}
long allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
Array.Sort(elapsed);
Console.WriteLine($"warm_samples={samples} p50_ms={Percentile(elapsed, .50):F3} p95_ms={Percentile(elapsed, .95):F3} mean_allocated_bytes={(double)allocated / samples:F0}");
Console.WriteLine($"runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} os={System.Runtime.InteropServices.RuntimeInformation.OSDescription} arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} cpu={Environment.ProcessorCount}");

static double Percentile(double[] sorted, double percentile) => sorted[(int)Math.Ceiling(percentile * sorted.Length) - 1];
