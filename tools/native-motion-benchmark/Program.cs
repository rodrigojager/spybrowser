using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Cursory;
using SpyBrowser.Playwright;

const int seed = 21021;
const int warmRuns = 40;
const int plannedMilliseconds = 300;
var runStartedUtc = DateTimeOffset.UtcNow;
var sourceRevision = await GitAsync("rev-parse", "HEAD");
var sourceDirtyState = await GitAsync("status", "--porcelain=v1", "--untracked-files=all");
var outputDirectory = args.FirstOrDefault() ?? Path.Combine("artifacts", "motion-quality");
Directory.CreateDirectory(outputDirectory);
var datasetPath = FindDatasetPath();
var datasetHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(datasetPath))).ToLowerInvariant();

// Snapshot around the first invocation. These diagnostics are outside browser dispatch timing.
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
var coldManagedHeapBefore = GC.GetTotalMemory(forceFullCollection: true);
using var coldProcess = Process.GetCurrentProcess();
coldProcess.Refresh();
var coldWorkingSetBefore = coldProcess.WorkingSet64;
var coldAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
var coldWatch = Stopwatch.StartNew();
var coldPath = CursoryTrajectoryGenerator.Generate(new(20, 30), new(21, 31), new TrajectoryOptions { Seed = seed });
coldWatch.Stop();
var coldAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - coldAllocatedBefore;
var coldManagedHeapAfter = GC.GetTotalMemory(forceFullCollection: true);
coldProcess.Refresh();
var coldWorkingSetAfter = coldProcess.WorkingSet64;
var coldGeneration = new
{
    elapsedMs = coldWatch.Elapsed.TotalMilliseconds,
    pointCount = coldPath.Points.Count,
    currentThreadManagedAllocatedBytes = coldAllocatedBytes,
    managedHeapBeforeBytes = coldManagedHeapBefore,
    managedHeapAfterBytes = coldManagedHeapAfter,
    managedHeapDeltaBytes = coldManagedHeapAfter - coldManagedHeapBefore,
    processWorkingSetBeforeBytes = coldWorkingSetBefore,
    processWorkingSetAfterBytes = coldWorkingSetAfter,
    processWorkingSetDeltaBytes = coldWorkingSetAfter - coldWorkingSetBefore,
    measurementScope = "fresh-process first Cursory generator call; managed heap and process working-set snapshots are process-wide diagnostics, not exclusive generator ownership or peak memory"
};
var repeatPath = CursoryTrajectoryGenerator.Generate(new(20, 30), new(21, 31), new TrajectoryOptions { Seed = seed });
var deterministicGeneration = coldPath.Points.Count == repeatPath.Points.Count &&
    coldPath.Points.Zip(repeatPath.Points).All(pair => Math.Abs(pair.First.X - pair.Second.X) <= 1e-9 && Math.Abs(pair.First.Y - pair.Second.Y) <= 1e-9) &&
    coldPath.Timings.Zip(repeatPath.Timings).All(pair => Math.Abs(pair.First - pair.Second) <= 1e-9);
var genSamples = new double[warmRuns];
var allocBefore = GC.GetAllocatedBytesForCurrentThread();
for (var i = 0; i < warmRuns; i++)
{
    var started = Stopwatch.GetTimestamp();
    _ = CursoryTrajectoryGenerator.Generate(new(20, 30), new(500, 280), new TrajectoryOptions { Seed = (UInt128)(uint)(seed + i) });
    genSamples[i] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
var generationAllocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;
var processBeforeBrowser = Process.GetCurrentProcess().WorkingSet64;
var totalManagedAllocatedBeforeBrowser = GC.GetTotalAllocatedBytes(true);

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var cases = new[]
{
    new MotionCase("short-subpixel", 100.25, 100.5, 101.0, 100.75, "movement"),
    new MotionCase("long", 35, 40, 1_020, 650, "movement"),
    new MotionCase("stable-button-activation", 80, 70, 180, 125, "button"),
    new MotionCase("slow-transport-acknowledged-12ms-before-move", 35, 40, 600, 350, "slow"),
    new MotionCase("concurrent-independent-pages", 35, 40, 600, 350, "concurrent")
};
var results = new List<object>();
var realDomRunCount = 0;
foreach (var algorithm in Enum.GetValues<MouseTrajectoryAlgorithm>())
foreach (var motionCase in cases)
{
    var pageCount = motionCase.Kind == "concurrent" ? 3 : 1;
    using var caseProcess = Process.GetCurrentProcess();
    caseProcess.Refresh();
    var caseWorkingSetBefore = caseProcess.WorkingSet64;
    var caseManagedHeapBefore = GC.GetTotalMemory(forceFullCollection: false);
    var contexts = new List<IBrowserContext>();
    var tasks = new List<Task<object>>();
    for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new() { Width = 1_200, Height = 800 } });
        contexts.Add(context);
        var page = await context.NewPageAsync();
        var endX = motionCase.EndX + (pageIndex * 35);
        var endY = motionCase.EndY + (pageIndex * 25);
        await page.SetContentAsync(motionCase.Kind == "button"
            ? $"<button id='target' style='position:absolute;left:{endX - 45}px;top:{endY - 20}px;width:90px;height:40px'>activate</button><script>window.samples=[];window.hovers=0;window.clicks=0;addEventListener('mousemove',e=>samples.push([performance.now(),e.clientX,e.clientY]));document.querySelector('#target').addEventListener('mouseenter',()=>window.hovers++);document.querySelector('#target').addEventListener('click',()=>window.clicks++);</script>"
            : "<script>window.samples=[];addEventListener('mousemove',e=>samples.push([performance.now(),e.clientX,e.clientY]));</script>");
        var pageHeapBefore = await ReadPageMemoryAsync(context, page);
        IPage actionPage = motionCase.Kind == "slow" ? SlowMousePageProxy.Create(page, 12) : page;
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            MouseAlgorithm = algorithm,
            RandomSeed = seed,
            MouseMinimumDurationMilliseconds = plannedMilliseconds,
            MouseMaximumDurationMilliseconds = plannedMilliseconds,
            CursoryMovementDeadlineMilliseconds = 10_000
        });
        var wrapped = humanizer.Wrap(actionPage);
        await wrapped.Mouse.MoveAsync((float)motionCase.StartX, (float)motionCase.StartY);
        await page.EvaluateAsync("samples.length=0");
        tasks.Add(ObserveMoveAsync(context, page, wrapped, algorithm, motionCase, endX, endY, pageIndex, plannedMilliseconds, pageHeapBefore));
    }
    var pageResults = await Task.WhenAll(tasks);
    caseProcess.Refresh();
    var caseWorkingSetAfter = caseProcess.WorkingSet64;
    var caseManagedHeapAfter = GC.GetTotalMemory(forceFullCollection: false);
    results.Add(new
    {
        algorithm = algorithm.ToString(),
        caseName = motionCase.Name,
        processMemoryDiagnostic = new
        {
            workingSetBeforeBytes = caseWorkingSetBefore,
            workingSetAfterBytes = caseWorkingSetAfter,
            workingSetDeltaBytes = caseWorkingSetAfter - caseWorkingSetBefore,
            managedHeapBeforeBytes = caseManagedHeapBefore,
            managedHeapAfterBytes = caseManagedHeapAfter,
            managedHeapDeltaBytes = caseManagedHeapAfter - caseManagedHeapBefore,
            scope = "benchmark process-wide snapshots around this case (includes Playwright/driver and all concurrent pages); not attributable to a page/target or SDK-only state"
        },
        pages = pageResults
    });
    realDomRunCount += tasks.Count;
    foreach (var context in contexts) await context.CloseAsync();
}
var processAfterBrowser = Process.GetCurrentProcess().WorkingSet64;
var totalManagedAllocatedAfterBrowser = GC.GetTotalAllocatedBytes(true);

var report = new
{
    schemaVersion = 2,
    run = new { startedUtc = runStartedUtc, seed, sourceRevision, sourceDirty = sourceDirtyState.Length != 0, sourceDirtyState, datasetSha256 = datasetHash, datasetFile = "src/SpyBrowser.Cursory/Data/trajectories.json.gz", datasetProvenance = "The repository's embedded cursory-js dataset; gzip bytes hashed.", realDomRunCount },
    classifications = new
    {
        functional = "PASS only when each measured DOM task completed, chronological is true, and all serialized metrics are finite; this is functional correctness evidence, not equivalence of paths.",
        performance = "Descriptive measurements (cold/warm generation, allocations, browser dispatch, DOM rhythm); 10 ms p95 is an investigation target, never a shared-runner hard gate.",
        regression = "No regression verdict without a paired report from the same machine/browser/settings and an explicit baseline revision; differences otherwise remain contextual observations.",
        context = "Compare source revision and dirty state, OS/runtime/browser/dataset/seed, concurrency, and case names before interpreting timing deltas."
    },
    environment = new
    {
        machine = Environment.MachineName,
        cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "not reported",
        logicalProcessors = Environment.ProcessorCount,
        os = RuntimeInformation.OSDescription,
        processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        framework = RuntimeInformation.FrameworkDescription,
        sdk = await GetDotnetSdkAsync(),
        playwright = typeof(IPage).Assembly.GetName().Version?.ToString(),
        browser = browser.Version,
        browserExecutable = "Playwright Chromium (headless)",
        benchmarkProcessId = Environment.ProcessId
    },
    generation = new
    {
        coldDatasetAndGeneration = coldGeneration,
        warmRuns,
        warmP50Ms = Percentile(genSamples, .50),
        warmP95Ms = Percentile(genSamples, .95),
        deterministicSameSeedWithin1eMinus9 = deterministicGeneration,
        deterministicComparisonMethod = "two generated Cursory paths with identical seed compared point coordinates and timings with absolute tolerance 1e-9",
        managedAllocatedBytesPerTrajectory = generationAllocated / warmRuns,
        investigationTargetP95Ms = 10,
        investigationTargetMetOnThisRun = Percentile(genSamples, .95) < 10,
        measurement = "Cold first-call elapsed time and current-thread managed allocations plus managed-heap/process-working-set snapshots are captured in a fresh process. Warm 40-run timings and allocations are separately measured before Playwright/browser startup; snapshots are diagnostic, not peak/exclusive ownership.",
        benchmarkProcessWorkingSetDeltaBytes = processAfterBrowser - processBeforeBrowser,
        browserPhaseManagedAllocatedBytesAggregate = totalManagedAllocatedAfterBrowser - totalManagedAllocatedBeforeBrowser,
        browserPhaseAllocationMethod = "GC.GetTotalAllocatedBytes delta for the benchmark process, including asynchronous/Playwright overhead; not attributed per page or algorithm"
    },
    cases = results,
    interpretation = new[]
    {
        "Bezier is the existing baseline; Cursory is the native recorded-trajectory implementation. Differences describe implementation cost and observed rhythm, not human-likeness or detection outcomes.",
        "Planned duration is the configured 300 ms path budget (Bezier resolves to this clamp; Cursory is scaled to this limit). Actual DOM timing is browser-observed and intentionally not bit-reproducible.",
        "The slow transport case adds an explicit artificial 12 ms delay immediately before each IMouse.MoveAsync delegate call. This is not network/CDP latency.",
        "Cold generation memory snapshots are process-wide and include managed runtime state; per-case snapshots include Playwright/driver and concurrent pages. Neither is exclusive generator/page ownership. Per-target CDP reports target V8 heap and supported Performance metrics only; Chromium may share a renderer across targets, so these are not exclusive per-page browser RSS. SDK-state allocations per page are not isolated by this harness."
    }
};
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var jsonPath = Path.Combine(outputDirectory, "motion-quality.json");
var reportJson = JsonSerializer.Serialize(report, jsonOptions);
using (var validation = JsonDocument.Parse(reportJson))
{
    if (realDomRunCount != 14) throw new InvalidOperationException($"Expected 14 real DOM measurements; got {realDomRunCount}.");
    foreach (var page in validation.RootElement.GetProperty("cases").EnumerateArray().SelectMany(group => group.GetProperty("pages").EnumerateArray()))
    {
        if (!page.GetProperty("chronological").GetBoolean() || !page.GetProperty("taskCompleted").GetBoolean())
            throw new InvalidOperationException($"Benchmark validation failed: algorithm={page.GetProperty("algorithm").GetString()}, case={page.GetProperty("taskKind").GetString()}, hover={page.GetProperty("hoverCompleted").GetBoolean()}, complete={page.GetProperty("taskCompleted").GetBoolean()}, chronological={page.GetProperty("chronological").GetBoolean()}.");
        ValidateFiniteNumbers(page);
    }
}
await File.WriteAllTextAsync(jsonPath, reportJson);
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "motion-quality.md"), ToMarkdown(jsonPath, reportJson));
Console.WriteLine($"Wrote {Path.GetFullPath(jsonPath)} and {Path.GetFullPath(Path.Combine(outputDirectory, "motion-quality.md"))}");

static async Task<object> ObserveMoveAsync(IBrowserContext context, IPage rawPage, IPage wrappedPage, MouseTrajectoryAlgorithm algorithm, MotionCase motionCase, double endX, double endY, int pageIndex, int plannedMs, object pageMemoryBefore)
{
    var start = Stopwatch.StartNew();
    await wrappedPage.Mouse.MoveAsync((float)endX, (float)endY);
    var completed = true;
    var hovered = motionCase.Kind != "button";
    var clicked = motionCase.Kind != "button";
    if (motionCase.Kind == "button")
    {
        await rawPage.Locator("#target").HoverAsync();
        hovered = await rawPage.EvaluateAsync<bool>("window.hovers > 0");
        await rawPage.Locator("#target").ClickAsync();
        clicked = await rawPage.EvaluateAsync<bool>("window.clicks === 1");
        completed = hovered && clicked;
    }
    start.Stop();
    var raw = await rawPage.EvaluateAsync<string>("JSON.stringify({samples,clicks:window.clicks||0})");
    using var document = JsonDocument.Parse(raw);
    var samples = document.RootElement.GetProperty("samples").EnumerateArray()
        .Select(row => row.EnumerateArray().Select(cell => cell.GetDouble()).ToArray()).ToArray();
    var intervals = new List<double>();
    var velocities = new List<double>();
    var pathLength = 0d;
    var chronological = true;
    for (var i = 1; i < samples.Length; i++)
    {
        var dt = samples[i][0] - samples[i - 1][0];
        if (!double.IsFinite(dt) || dt < 0) chronological = false;
        intervals.Add(dt);
        var dx = samples[i][1] - samples[i - 1][1];
        var dy = samples[i][2] - samples[i - 1][2];
        var distance = Math.Sqrt(dx * dx + dy * dy);
        pathLength += distance;
        velocities.Add(dt > 0 ? distance * 1000 / dt : 0);
    }
    var accelerations = new List<double>();
    for (var i = 1; i < velocities.Count; i++)
    {
        var averageSeconds = ((intervals[i] + intervals[i - 1]) / 2) / 1000;
        accelerations.Add(averageSeconds > 0 ? (velocities[i] - velocities[i - 1]) / averageSeconds : 0);
    }
    var displacement = samples.Length > 0 ? Math.Sqrt(Math.Pow(samples[^1][1] - samples[0][1], 2) + Math.Pow(samples[^1][2] - samples[0][2], 2)) : 0;
    return new
    {
        pageIndex,
        algorithm = algorithm.ToString(),
        plannedDurationMilliseconds = plannedMs,
        observedDispatchAndActionWallMilliseconds = start.Elapsed.TotalMilliseconds,
        domMoveCount = samples.Length,
        chronological,
        observedLengthPixels = Finite(pathLength),
        observedDisplacementPixels = Finite(displacement),
        intervalMilliseconds = Summary(intervals, "ms"),
        velocityPixelsPerSecond = Summary(velocities, "px/s"),
        accelerationPixelsPerSecondSquared = Summary(accelerations.Select(Math.Abs).ToList(), "px/s^2"),
        accelerationMethod = "absolute change in successive segment velocities divided by mean adjacent interval seconds; magnitude summary",
        pausesOver50Milliseconds = intervals.Count(value => value >= 50),
        taskCompleted = completed,
        hoverCompleted = hovered,
        clickCompleted = clicked,
        targetMemoryBefore = pageMemoryBefore,
        targetMemoryAfter = await ReadPageMemoryAsync(context, rawPage),
        taskKind = motionCase.Kind,
        measuredAlgorithm = algorithm.ToString(),
        timingSource = "DOM event performance.now()"
    };
}

static async Task<object> ReadPageMemoryAsync(IBrowserContext context, IPage page)
{
    // CDP diagnostics run before/after, never inside the timed mouse dispatch/action interval.
    var session = await context.NewCDPSessionAsync(page);
    try
    {
        await session.SendAsync("Performance.enable");
        var heap = await session.SendAsync("Runtime.getHeapUsage");
        var metrics = await session.SendAsync("Performance.getMetrics");
        return new
        {
            protocol = "Chrome DevTools Protocol",
            targetHeap = heap,
            targetHeapUnit = "bytes (CDP Runtime.getHeapUsage byte-valued fields)",
            performanceMetrics = metrics,
            performanceMetricUnits = "CDP-defined per metric; raw metric names retained (timestamps/durations in seconds, heap-size metrics in bytes, count metrics as named)",
            scope = "CDP target V8 heap/metrics; renderer process may be shared with other targets and these values are not exclusive per-page browser RSS"
        };
    }
    finally
    {
        await session.DetachAsync();
    }
}

static double Finite(double value) => double.IsFinite(value) ? value : 0;
static object Summary(List<double> values, string unit) => new
{
    count = values.Count,
    min = values.Count == 0 ? 0 : Finite(values.Min()),
    median = Percentile(values.ToArray(), .50),
    p95 = Percentile(values.ToArray(), .95),
    max = values.Count == 0 ? 0 : Finite(values.Max()),
    unit
};

static string FindDatasetPath()
{
    var relative = Path.Combine("src", "SpyBrowser.Cursory", "Data", "trajectories.json.gz");
    foreach (var startingDirectory in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(startingDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
    }
    throw new FileNotFoundException("Could not locate the repository Cursory dataset.", relative);
}

static void ValidateFiniteNumbers(JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Number &&
        (!element.TryGetDouble(out var value) || !double.IsFinite(value)))
        throw new InvalidOperationException("Benchmark validation failed: non-finite metric.");
    if (element.ValueKind == JsonValueKind.Object)
        foreach (var property in element.EnumerateObject()) ValidateFiniteNumbers(property.Value);
    if (element.ValueKind == JsonValueKind.Array)
        foreach (var item in element.EnumerateArray()) ValidateFiniteNumbers(item);
}

static double Percentile(double[] values, double fraction)
{
    if (values.Length == 0) return 0;
    var sorted = values.Order().ToArray();
    return sorted[Math.Clamp((int)Math.Ceiling(fraction * sorted.Length) - 1, 0, sorted.Length - 1)];
}

static async Task<string> GitAsync(params string[] arguments)
{
    try
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start);
        if (process is null) return "unavailable";
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return process.ExitCode == 0 ? output.TrimEnd() : "unavailable";
    }
    catch { return "unavailable"; }
}

static async Task<string> GetDotnetSdkAsync()
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true, UseShellExecute = false });
        if (process is null) return "not reported";
        var text = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return text.Trim();
    }
    catch { return "not reported"; }
}

static string ToMarkdown(string jsonPath, string json)
{
    using var document = JsonDocument.Parse(json);
    var report = document.RootElement;
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("# Motion quality benchmark result").AppendLine();
    sb.AppendLine($"JSON: `{Path.GetFileName(jsonPath)}`").AppendLine();
    sb.AppendLine($"Machine: `{report.GetProperty("environment").GetProperty("machine").GetString()}`; OS: `{report.GetProperty("environment").GetProperty("os").GetString()}`; .NET: `{report.GetProperty("environment").GetProperty("framework").GetString()}`; browser: `{report.GetProperty("environment").GetProperty("browser").GetString()}`; seed: `{report.GetProperty("run").GetProperty("seed").GetInt32()}`.").AppendLine();
    sb.AppendLine("| Algorithm | Case | DOM moves | planned ms | wall ms | path px | displacement px | interval p95 ms | velocity p95 px/s | accel p95 px/s² | pauses >=50ms | complete | chronological |").AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|:---:|:---:|");
    foreach (var group in report.GetProperty("cases").EnumerateArray())
    foreach (var page in group.GetProperty("pages").EnumerateArray())
    {
        double Number(JsonElement parent, string name) => parent.GetProperty(name).GetDouble();
        var intervals = page.GetProperty("intervalMilliseconds");
        var velocity = page.GetProperty("velocityPixelsPerSecond");
        var acceleration = page.GetProperty("accelerationPixelsPerSecondSquared");
        sb.AppendLine($"| {group.GetProperty("algorithm").GetString()} | {group.GetProperty("caseName").GetString()} p{page.GetProperty("pageIndex").GetInt32()} | {page.GetProperty("domMoveCount").GetInt32()} | {page.GetProperty("plannedDurationMilliseconds").GetInt32()} | {Number(page, "observedDispatchAndActionWallMilliseconds"):F1} | {Number(page, "observedLengthPixels"):F1} | {Number(page, "observedDisplacementPixels"):F1} | {Number(intervals, "p95"):F1} | {Number(velocity, "p95"):F1} | {Number(acceleration, "p95"):F1} | {page.GetProperty("pausesOver50Milliseconds").GetInt32()} | {page.GetProperty("taskCompleted").GetBoolean()} | {page.GetProperty("chronological").GetBoolean()} |");
    }
    var generation = report.GetProperty("generation");
    sb.AppendLine().AppendLine($"Cursory cold generation/load: {generation.GetProperty("coldDatasetAndGeneration").GetProperty("elapsedMs").GetDouble():F1} ms; warm p50/p95: {generation.GetProperty("warmP50Ms").GetDouble():F1}/{generation.GetProperty("warmP95Ms").GetDouble():F1} ms; allocated: {generation.GetProperty("managedAllocatedBytesPerTrajectory").GetInt64()} bytes/trajectory; same-seed determinism (1e-9): {generation.GetProperty("deterministicSameSeedWithin1eMinus9").GetBoolean()}; initial 10 ms p95 target met: {generation.GetProperty("investigationTargetMetOnThisRun").GetBoolean()}.").AppendLine();
    sb.AppendLine("DOM `performance.now()` timings include actual browser scheduling. The slow case includes artificial 12 ms delay before each real mouse dispatch, not network latency. Cold first-call allocated bytes/managed heap/process working set, warm generator allocation, per-case process snapshots, and per-target CDP V8/Performance values are separate diagnostics outside timed dispatch. Process values are shared process snapshots, not per-page attribution; CDP target heap is not exclusive browser RSS because renderer processes may be shared. SDK-state allocations per page are not isolated. Results describe implementation cost and observed rhythm only; they do not claim improved humanness, stealth, CAPTCHA outcomes, or detection avoidance.");
    return sb.ToString();
}

sealed record MotionCase(string Name, double StartX, double StartY, double EndX, double EndY, string Kind);

public class SlowMousePageProxy : DispatchProxy
{
    private IPage _target = null!;
    private int _delayMs;
    public static IPage Create(IPage page, int delayMs)
    {
        var proxy = Create<IPage, SlowMousePageProxy>();
        var implementation = (SlowMousePageProxy)(object)proxy;
        implementation._target = page;
        implementation._delayMs = delayMs;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method is null) throw new MissingMethodException();
        if (method.Name == "get_Mouse") return SlowMouseProxy.Create(_target.Mouse, _delayMs);
        return method.Invoke(_target, args);
    }
}

public class SlowMouseProxy : DispatchProxy
{
    private IMouse _target = null!;
    private int _delayMs;
    public static IMouse Create(IMouse mouse, int delayMs)
    {
        var proxy = Create<IMouse, SlowMouseProxy>();
        var implementation = (SlowMouseProxy)(object)proxy;
        implementation._target = mouse;
        implementation._delayMs = delayMs;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method is null) throw new MissingMethodException();
        if (method.Name == nameof(IMouse.MoveAsync)) return MoveAsync(args);
        return method.Invoke(_target, args);
    }
    private async Task MoveAsync(object?[]? args)
    {
        await Task.Delay(_delayMs);
        await _target.MoveAsync((float)args![0]!, (float)args[1]!, args.ElementAtOrDefault(2) as MouseMoveOptions).ConfigureAwait(false);
    }
}
