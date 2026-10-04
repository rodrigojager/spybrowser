using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Playwright;
using SpyBrowser.Cursory;
using SpyBrowser.Playwright;

const int warmRuns = 100;
var start = new TrajectoryPoint(20.25, 30.5);
var end = new TrajectoryPoint(500.75, 280.125);
var options = new TrajectoryOptions();
var coldWatch = Stopwatch.StartNew();
var cold = CursoryTrajectoryGenerator.Generate(start, end, options);
coldWatch.Stop();

for (var i = 0; i < 10; i++) _ = CursoryTrajectoryGenerator.Generate(start, end, options);
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
var warmWatch = Stopwatch.StartNew();
for (var i = 0; i < warmRuns; i++) _ = CursoryTrajectoryGenerator.Generate(start, end, options);
warmWatch.Stop();
var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

Console.WriteLine($"Machine={Environment.MachineName}; OS={RuntimeInformation.OSDescription}; ProcessArch={RuntimeInformation.ProcessArchitecture}; Framework={RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Cursory assembly={typeof(CursoryTrajectoryGenerator).Assembly.GetName().Version}; Playwright={typeof(IPage).Assembly.GetName().Version}; coldMs={coldWatch.Elapsed.TotalMilliseconds:F3}; warmRuns={warmRuns}; warmMeanMs={warmWatch.Elapsed.TotalMilliseconds / warmRuns:F3}; allocatedBytesPerTrajectory={allocated / warmRuns}; coldPointCount={cold.Points.Count}");
Console.WriteLine("Timing values are observations from this run, not performance or real-time guarantees.");

using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync();
await page.SetContentAsync("<script>window.received=[];addEventListener('mousemove',e=>received.push(performance.now()));</script>");
var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
{
    MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
    MouseMinimumDurationMilliseconds = 100,
    MouseMaximumDurationMilliseconds = 1_500
});
var wrapped = humanizer.Wrap(page);
await wrapped.Mouse.MoveAsync(20.25f, 30.5f); // documented unknown-position anchor
var domWatch = Stopwatch.StartNew();
await wrapped.Mouse.MoveAsync((float)end.X, (float)end.Y);
domWatch.Stop();
var domTimes = await page.EvaluateAsync<double[]>("received");
Console.WriteLine($"Browser={browser.Version}; generationAndDispatchWallMs={domWatch.Elapsed.TotalMilliseconds:F3}; domMousemoveCount={domTimes.Length}; domFirstMs={(domTimes.Length > 1 ? domTimes[1] - domTimes[0] : 0):F3}; domLastMs={(domTimes.Length > 1 ? domTimes[^1] - domTimes[^2] : 0):F3}");
