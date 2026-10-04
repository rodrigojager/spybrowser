using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

const int seed = 21021;
const int plannedMilliseconds = 300;
var variant = args[0]; // baseline-bezier, candidate-bezier, candidate-cursory, candidate-off
var output = args[1];
var startedUtc = DateTimeOffset.UtcNow;
var sourceSha = Environment.GetEnvironmentVariable("MOTION_SOURCE_SHA") ?? throw new InvalidOperationException("MOTION_SOURCE_SHA is required");
var feedManifestSha = Environment.GetEnvironmentVariable("MOTION_FEED_MANIFEST_SHA256") ?? throw new InvalidOperationException("MOTION_FEED_MANIFEST_SHA256 is required");
var feedManifest = Environment.GetEnvironmentVariable("MOTION_FEED_MANIFEST") ?? throw new InvalidOperationException("MOTION_FEED_MANIFEST is required");
var sourceArchiveSha = Environment.GetEnvironmentVariable("MOTION_SOURCE_ARCHIVE_SHA256") ?? throw new InvalidOperationException("MOTION_SOURCE_ARCHIVE_SHA256 is required");
var sourceTreeSha = Environment.GetEnvironmentVariable("MOTION_SOURCE_TREE_SHA256") ?? throw new InvalidOperationException("MOTION_SOURCE_TREE_SHA256 is required");
var sourceVersion = Environment.GetEnvironmentVariable("MOTION_PACKAGE_VERSION") ?? throw new InvalidOperationException("MOTION_PACKAGE_VERSION is required");
var isOff = variant == "candidate-off";
var mode = variant.EndsWith("cursory", StringComparison.Ordinal) ? "Cursory" : "Bezier";

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var process = Process.GetCurrentProcess();
var workingSetBefore = process.WorkingSet64;
var allocatedBefore = GC.GetTotalAllocatedBytes(true);
var cases = new[]
{
    new Case("short-subpixel", 100.25, 100.5, 101.0, 100.75, false),
    new Case("long", 35, 40, 1_020, 650, false),
    new Case("stable-hover-click", 80, 70, 180, 125, true),
    new Case("slow-transport-12ms", 35, 40, 600, 350, false),
    new Case("independent-pages", 35, 40, 600, 350, false)
};
var runs = new List<object>();
foreach (var item in cases)
{
    var count = item.Name == "independent-pages" ? 3 : 1;
    var contexts = new List<IBrowserContext>();
    try
    {
        var tasks = new List<Task<object>>();
        for (var pageIndex = 0; pageIndex < count; pageIndex++)
        {
            var context = await browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new() { Width = 1_200, Height = 800 } });
            contexts.Add(context);
            var page = await context.NewPageAsync();
            var endX = item.EndX + pageIndex * 35;
            var endY = item.EndY + pageIndex * 25;
            await page.SetContentAsync(item.Click
                ? $"<button id='target' style='box-sizing:border-box;position:absolute;left:{endX - 45}px;top:{endY - 20}px;width:90px;height:40px'>activate</button>"
                : "<div id='target' style='position:absolute;left:0;top:0;width:1200px;height:800px'></div>");
            await page.EvaluateAsync("window.samples=[];window.hovers=0;window.clicks=0;window.activationOrder=[];addEventListener('mousemove',e=>samples.push([performance.now(),e.clientX,e.clientY]));document.querySelector('#target').addEventListener('mouseenter',()=>{window.hovers++;window.activationOrder.push('hover')});document.querySelector('#target').addEventListener('click',()=>{window.clicks++;window.activationOrder.push('click')})");
            var pageHeapBefore = await page.EvaluateAsync<double?>("performance.memory?.usedJSHeapSize ?? null");
            IPage actionPage = item.Name == "slow-transport-12ms" ? SlowPageProxy.Create(page, 12) : page;
            if (!isOff)
            {
                var options = new HumanInteractionOptions { MouseMinimumDurationMilliseconds = plannedMilliseconds, MouseMaximumDurationMilliseconds = plannedMilliseconds };
                SetProperty(options, "RandomSeed", seed);
                var algorithmProperty = typeof(HumanInteractionOptions).GetProperty("MouseAlgorithm");
                if (algorithmProperty is not null)
                    algorithmProperty.SetValue(options, Enum.Parse(algorithmProperty.PropertyType, mode));
                var compatibilityProperty = typeof(HumanInteractionOptions).GetProperty("CompatibilityMode");
                if (compatibilityProperty is not null)
                    compatibilityProperty.SetValue(options, Enum.Parse(compatibilityProperty.PropertyType, "PlaywrightCompatible"));
                actionPage = new PlaywrightHumanizer(options).Wrap(actionPage);
            }
            // Baseline has no seed API; report this fact rather than implying paired random trajectories.
            tasks.Add(MeasureAsync(page, actionPage, item, endX, endY, pageIndex, plannedMilliseconds, mode, isOff, variant, pageHeapBefore));
        }
        runs.Add(new { caseName = item.Name, pages = await Task.WhenAll(tasks) });
    }
    finally { foreach (var context in contexts) await context.CloseAsync(); }
}

var processWorkingSetDelta = Process.GetCurrentProcess().WorkingSet64 - workingSetBefore;
var browserPhaseManagedAllocatedBytes = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
var report = new
{
    schemaVersion = 1,
    run = new { startedUtc, seedRequested = seed, baselineRngSeedable = false, sourceSha, sourceTreeSha256 = sourceTreeSha, sourceArchiveSha256 = sourceArchiveSha, sourceVersion, feedManifest, feedManifestSha256 = feedManifestSha, variant, datasetSha256 = ReadDatasetSha256() },
    environment = new { machine = Environment.MachineName, cpu = GetCpuName(), logicalProcessors = Environment.ProcessorCount, os = RuntimeInformation.OSDescription, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), framework = RuntimeInformation.FrameworkDescription, sdk = GetDotnetSdk(), playwright = typeof(IPage).Assembly.GetName().Version?.ToString(), browser = browser.Version, browserMode = "Playwright Chromium headless; browser executable tracked by Playwright", processId = Environment.ProcessId },
    processMemory = new { workingSetDeltaBytes = processWorkingSetDelta, managedAllocatedBytesAggregate = browserPhaseManagedAllocatedBytes, method = "process-level snapshots across all cases, including Playwright/browser overhead; no per-page process attribution" },
    conditions = new { configuration = "headless Chromium, 1200x800 CSS viewport, isolated fresh context per task/page; identical fixture, seed requested, endpoints and duration options", movementAlgorithms = new[] { "historical Bezier", "candidate Bezier", "candidate Cursory", "candidate off (native Playwright)" }, sameSeedMeansSameTrajectory = false, limitation = "The historical implementation's RNG is not exposed/seedable; requested seed applies only where the package exposes RandomSeed, and timestamps are measured, not deterministic." },
    runs
};
Directory.CreateDirectory(output);
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
using (var parsed = JsonDocument.Parse(json))
{
    foreach (var page in parsed.RootElement.GetProperty("runs").EnumerateArray().SelectMany(x => x.GetProperty("pages").EnumerateArray()))
        if (!page.GetProperty("taskCompleted").GetBoolean() || !page.GetProperty("chronological").GetBoolean())
            throw new InvalidOperationException($"Functional task failure: case={page.GetProperty("caseName").GetString()} page={page.GetProperty("pageIndex").GetInt32()} endpointError={page.GetProperty("endpointErrorPixels").GetDouble()} order={page.GetProperty("activationOrder")}");
}
await File.WriteAllTextAsync(Path.Combine(output, "result.json"), json);
await File.WriteAllTextAsync(Path.Combine(output, "result.md"), ToMarkdown(report));
Console.WriteLine($"Wrote {Path.GetFullPath(Path.Combine(output, "result.json"))}");

static string GetCpuName()
{
    var windows = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    if (!string.IsNullOrWhiteSpace(windows)) return windows;
    try { return File.ReadLines("/proc/cpuinfo").FirstOrDefault(line => line.StartsWith("model name", StringComparison.Ordinal))?.Split(':', 2).Last().Trim() ?? "not reported"; }
    catch { return "not reported"; }
}
static string GetDotnetSdk()
{
    try { using var p = Process.Start(new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true, UseShellExecute = false }); if (p is null) return "not reported"; var text = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit(); return text; }
    catch { return "not reported"; }
}
static string? ReadDatasetSha256()
{
    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "SpyBrowser.Cursory");
    var resource = assembly?.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("trajectories.json.gz", StringComparison.Ordinal));
    if (assembly is null || resource is null) return null;
    using var stream = assembly.GetManifestResourceStream(resource)!;
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
static void SetProperty(object target, string name, object value) => typeof(HumanInteractionOptions).GetProperty(name)?.SetValue(target, value);
static async Task<object> MeasureAsync(IPage raw, IPage action, Case c, double x, double y, int page, int planned, string algorithm, bool off, string runVariant, double? pageHeapBefore)
{
    await action.Mouse.MoveAsync((float)c.StartX, (float)c.StartY);
    await raw.EvaluateAsync("samples.length=0;window.hovers=0;window.clicks=0;window.activationOrder=[]");
    var watch = Stopwatch.StartNew();
    await action.Mouse.MoveAsync((float)x, (float)y);
    var movementSampleCount = await raw.EvaluateAsync<int>("samples.length");
    if (c.Click)
    {
        await action.Locator("#target").HoverAsync();
        await action.Locator("#target").ClickAsync();
    }
    watch.Stop();
    var rawJson = await raw.EvaluateAsync<string>("JSON.stringify({samples,hovers,clicks,activationOrder})");
    using var doc = JsonDocument.Parse(rawJson);
    var allSamples = doc.RootElement.GetProperty("samples").EnumerateArray().Select(r => r.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();
    var samples = allSamples.Take(movementSampleCount).ToArray();
    var intervals = new List<double>(); var velocities = new List<double>(); var length = 0d; var chronological = true;
    for (var i = 1; i < samples.Length; i++)
    {
        var dt = samples[i][0] - samples[i - 1][0]; if (!double.IsFinite(dt) || dt < 0) chronological = false; intervals.Add(dt);
        var dx = samples[i][1] - samples[i - 1][1]; var dy = samples[i][2] - samples[i - 1][2]; var distance = Math.Sqrt(dx * dx + dy * dy); length += distance; velocities.Add(dt > 0 ? distance * 1000 / dt : 0);
    }
    var accels = new List<double>();
    for (var i = 1; i < velocities.Count; i++) { var dt = ((intervals[i] + intervals[i - 1]) / 2000); accels.Add(dt > 0 ? Math.Abs(velocities[i] - velocities[i - 1]) / dt : 0); }
    double Quantile(List<double> values, double q) => values.Count == 0 ? 0 : values.Order().ElementAt(Math.Clamp((int)Math.Ceiling(values.Count * q) - 1, 0, values.Count - 1));
    var dom = samples.Length;
    var taskDomEventCount = allSamples.Length;
    var hovered = doc.RootElement.GetProperty("hovers").GetInt32() > 0; var clicked = !c.Click || doc.RootElement.GetProperty("clicks").GetInt32() == 1;
    var finalError = samples.Length == 0 ? double.PositiveInfinity : Math.Sqrt(Math.Pow(samples[^1][1] - x, 2) + Math.Pow(samples[^1][2] - y, 2));
    var activationOrder = doc.RootElement.GetProperty("activationOrder").EnumerateArray().Select(v => v.GetString()).ToArray();
    var hoverOrder = Array.IndexOf(activationOrder, "hover"); var clickOrder = Array.IndexOf(activationOrder, "click");
    var activationOrderValid = !c.Click || hoverOrder >= 0 && clickOrder > hoverOrder && activationOrder.Count(v => v == "click") == 1;
    var complete = dom > 0 && finalError <= 1 && (!c.Click || hovered && clicked && activationOrderValid);
    return new { caseName = c.Name, pageIndex = page, algorithm = off ? "Off" : algorithm, seedUsed = off || runVariant == "baseline-bezier" ? (int?)null : seed, requestedSeed = 21021, plannedDurationMilliseconds = planned, observedDispatchAndActionWallMilliseconds = watch.Elapsed.TotalMilliseconds, observedDomDurationMilliseconds = samples.Length < 2 ? 0 : samples[^1][0] - samples[0][0], domMoveCount = dom, totalTaskDomEventCount = taskDomEventCount, observedLengthPixels = length, observedDisplacementPixels = samples.Length == 0 ? 0 : Math.Sqrt(Math.Pow(samples[^1][1] - samples[0][1], 2) + Math.Pow(samples[^1][2] - samples[0][2], 2)), observedEndpoint = samples.Length == 0 ? null : new { x = samples[^1][1], y = samples[^1][2] }, requestedEndpoint = new { x, y }, endpointErrorPixels = double.IsFinite(finalError) ? finalError : 0, intervalMs = new { p50 = Quantile(intervals, .5), p95 = Quantile(intervals, .95), max = intervals.DefaultIfEmpty().Max(), unit = "ms" }, velocityPxPerSecond = new { p50 = Quantile(velocities, .5), p95 = Quantile(velocities, .95), unit = "px/s" }, accelerationPxPerSecondSquaredP95 = Quantile(accels, .95), accelerationMethod = "absolute adjacent segment-velocity delta divided by mean adjacent event interval; px/s^2", pausesAtLeast50ms = intervals.Count(v => v >= 50), hoverCompleted = !c.Click || hovered, clickCompleted = clicked, activationOrder = activationOrder, activationOrderValid, chromiumPageJavaScriptHeapBytesBefore = pageHeapBefore, chromiumPageJavaScriptHeapBytesAfter = await raw.EvaluateAsync<double?>("performance.memory?.usedJSHeapSize ?? null"), taskCompleted = complete, chronological };
}
static string ToMarkdown(dynamic report) => "# Historical package comparison\n\n" +
    $"Variant: `{report.run.variant}`; source SHA: `{report.run.sourceSha}`; feed manifest SHA-256: `{report.run.feedManifestSha256}`; OS: `{report.environment.os}`; browser: `{report.environment.browser}`.\n\n" +
    "| Case | Page | Algorithm | Planned ms | DOM ms | Dispatch/action ms | Path px | displacement px | intervals p95 ms | velocity p95 px/s | acceleration p95 px/s² | pauses ≥50ms | hover | click | complete |\n|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|:---:|:---:|:---:|\n" +
    string.Join("\n", ((IEnumerable<dynamic>)report.runs).SelectMany(group => ((IEnumerable<dynamic>)group.pages).Select(p => $"| {p.caseName} | {p.pageIndex} | {p.algorithm} | {p.plannedDurationMilliseconds} | {p.observedDomDurationMilliseconds:F1} | {p.observedDispatchAndActionWallMilliseconds:F1} | {p.observedLengthPixels:F1} | {p.observedDisplacementPixels:F1} | {p.intervalMs.p95:F1} | {p.velocityPxPerSecond.p95:F1} | {p.accelerationPxPerSecondSquaredP95:F1} | {p.pausesAtLeast50ms} | {p.hoverCompleted} | {p.clickCompleted} | {p.taskCompleted} |"))) + "\n\nHistorical movement randomness is not seedable. Timing observations are not promised bit-for-bit reproducible. Functional completion is separate from performance differences; no regression is asserted without paired same-environment evidence.\n";

sealed record Case(string Name, double StartX, double StartY, double EndX, double EndY, bool Click);
public class SlowPageProxy : DispatchProxy
{
    private IPage target = null!; private int delay;
    public static IPage Create(IPage page, int delayMs) { var proxy = Create<IPage, SlowPageProxy>(); var self = (SlowPageProxy)(object)proxy; self.target = page; self.delay = delayMs; return proxy; }
    protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args) { if (method is null) throw new MissingMethodException(); if (method.Name == "get_Mouse") return SlowMouseProxy.Create(target.Mouse, delay); return method.Invoke(target, args); }
}
public class SlowMouseProxy : DispatchProxy
{
    private IMouse target = null!; private int delay;
    public static IMouse Create(IMouse mouse, int delayMs) { var proxy = Create<IMouse, SlowMouseProxy>(); var self = (SlowMouseProxy)(object)proxy; self.target = mouse; self.delay = delayMs; return proxy; }
    protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args) { if (method is null) throw new MissingMethodException(); if (method.Name == nameof(IMouse.MoveAsync)) return MoveAsync(args); return method.Invoke(target, args); }
    private async Task MoveAsync(object?[]? args) { await Task.Delay(delay); await target.MoveAsync((float)args![0]!, (float)args[1]!, args.ElementAtOrDefault(2) as MouseMoveOptions); }
}
