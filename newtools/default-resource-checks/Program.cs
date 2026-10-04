using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Cursory;
using SpyBrowser.Playwright;

const string DatasetTypeName = "SpyBrowser.Cursory.Internal.Dataset";
const long ConstructionAllocationLimit = 3L * 1024 * 1024; // 100 constructions; exceeds the full expanded resource by 48%.

if (args.Length == 1 && args[0] == "--child")
{
    Console.Error.WriteLine("A child case name is required.");
    return 2;
}

if (args.Length == 2 && args[0] == "--child")
{
    return await RunCaseAsync(args[1]);
}

if (args.Length is < 1 or > 2 || args[0] != "run")
{
    Console.Error.WriteLine("Usage: DefaultResourceChecks run [output-directory]");
    return 2;
}

var outputDirectory = Path.GetFullPath(args.Length == 2 ? args[1] : "default-resource-results");
Directory.CreateDirectory(outputDirectory);
var cases = new[] { "default", "off", "positive-control" };
foreach (var name in cases)
{
    var start = new ProcessStartInfo(Environment.ProcessPath!)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Environment.CurrentDirectory
    };
    if (Environment.ProcessPath is { } processPath && Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    start.ArgumentList.Add("--child");
    start.ArgumentList.Add(name);
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start fresh child process.");
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
    try { await process.WaitForExitAsync(timeout.Token); }
    catch (OperationCanceledException)
    {
        process.Kill(entireProcessTree: true);
        throw new TimeoutException($"Fresh-process case '{name}' exceeded 150 seconds.");
    }
    var output = await stdout;
    var error = await stderr;
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"Case '{name}' exited {process.ExitCode}. stdout: {output}\nstderr: {error}");
    var resultPath = Path.Combine(outputDirectory, name + ".json");
    await File.WriteAllTextAsync(resultPath, output.Trim() + Environment.NewLine);
    Console.WriteLine($"{name}: PASS ({resultPath})");
}
return 0;

static async Task<int> RunCaseAsync(string name)
{
    if (name is not ("default" or "off" or "positive-control")) throw new ArgumentException($"Unknown case '{name}'.");
    var datasetIsValueCreated = ResolveDatasetLazy(); // Reflection warmed before measured construction; never evaluates Lazy.Value.
    var datasetLoadedAtStart = datasetIsValueCreated();
    Require(!datasetLoadedAtStart, "Fresh process unexpectedly started with Cursory dataset initialized.");
    var (gzipBytes, expandedBytes) = GetDatasetResourceSizes();

    var options = name switch
    {
        "default" => new HumanInteractionOptions(),
        "off" => new HumanInteractionOptions(),
        _ => new HumanInteractionOptions { MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory, CursoryMovementDeadlineMilliseconds = 10_000 }
    };
    Require(options.MouseAlgorithm == (name == "positive-control" ? MouseTrajectoryAlgorithm.Cursory : MouseTrajectoryAlgorithm.Bezier), "Unexpected default/selected trajectory algorithm.");
    Require(options.CompatibilityMode == HumanizationCompatibilityMode.Legacy, "Compatibility default is not Legacy.");
    Require(!new SpyBrowserLaunchOptions { IdentityId = "resource-check" }.Humanize, "Browser launch humanization is unexpectedly enabled by default.");

    var allocationBytes = MeasureConstructionAllocation(options);
    Require(allocationBytes <= ConstructionAllocationLimit, $"100 HumanActions + PlaywrightHumanizer constructions allocated {allocationBytes:N0} bytes, above {ConstructionAllocationLimit:N0}.");
    Require(!datasetIsValueCreated(), "Construction allocation measurement materialized the Cursory dataset.");

    var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    try
    {
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync();
        var rawPage = await context.NewPageAsync();
        await rawPage.SetContentAsync("<button id='button'>go</button><input id='entry'><script>window.clicks=0;document.querySelector('#button').addEventListener('click',()=>window.clicks++);</script>");
        var humanizeEnabled = name != "off";
        var humanizer = humanizeEnabled ? new PlaywrightHumanizer(options) : null;
        var page = humanizer?.Wrap(rawPage) ?? rawPage;
        Require(ReferenceEquals(rawPage, PlaywrightHumanizer.Unwrap(page)), "Humanizer routing did not preserve access to the official underlying page.");
        if (name == "off")
        {
            Require(humanizer is null, "Off route created a PlaywrightHumanizer.");
            Require(ReferenceEquals(rawPage, page), "Off route wrapped the raw page.");
        }
        else
        {
            Require(humanizer is not null, "Humanized route failed to create its humanizer.");
        }

        await page.Mouse.MoveAsync(35, 45); // unknown cursor is anchored once; default Bezier path follows below
        await page.Mouse.MoveAsync(115, 95);
        await page.GetByRole(AriaRole.Button, new() { Name = "go" }).ClickAsync();
        await page.Locator("#entry").PressSequentiallyAsync("resource-check");
        Require(await rawPage.EvaluateAsync<int>("window.clicks") == 1, "Real browser click did not reach the local DOM.");
        Require(await rawPage.Locator("#entry").InputValueAsync() == "resource-check", "Real browser typing/fill did not reach the local DOM.");
        if (name == "positive-control")
            Require(datasetIsValueCreated(), "Positive Cursory control failed to initialize the shared dataset after real trajectory generation.");
        else
            Require(!datasetIsValueCreated(), $"{name} case materialized the Cursory dataset during real browser interactions.");

        var record = new Result(
            "default-resource-check-v1", name, Environment.OSVersion.ToString(), Environment.Version.ToString(),
            typeof(HumanActions).Assembly.GetName().Version?.ToString() ?? "unknown", GetPackageVersion(),
            GetSourceRevision(), GetDirtyState(), options.MouseAlgorithm.ToString(), options.CompatibilityMode.ToString(),
            humanizeEnabled, humanizer is not null, datasetLoadedAtStart, datasetIsValueCreated(),
            allocationBytes, 100, ConstructionAllocationLimit, gzipBytes, expandedBytes, 32_000_000, 2_356,
            "Constructor-only managed allocation on current thread; excludes Playwright driver, browser, page, and browser I/O.");
        Console.WriteLine(JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    finally { playwright.Dispose(); }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static long MeasureConstructionAllocation(HumanInteractionOptions options)
{
    // Warm JIT and reflection before taking the synchronous per-thread measurement.
    _ = new HumanActions(options);
    _ = new PlaywrightHumanizer(options);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 100; index++)
    {
        _ = new HumanActions(options);
        _ = new PlaywrightHumanizer(options);
    }
    return GC.GetAllocatedBytesForCurrentThread() - before;
}

static Func<bool> ResolveDatasetLazy()
{
    var assembly = typeof(CursoryTrajectoryGenerator).Assembly;
    var type = assembly.GetType(DatasetTypeName, throwOnError: true)!;
    var field = type.GetField("Shared", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingFieldException(type.FullName, "Shared");
    var value = field.GetValue(null) ?? throw new InvalidOperationException("Dataset.Shared field is null.");
    var recordingType = assembly.GetType("SpyBrowser.Cursory.Internal.Recording", throwOnError: true)!;
    var lazyType = typeof(Lazy<>).MakeGenericType(recordingType.MakeArrayType());
    if (!lazyType.IsInstanceOfType(value)) throw new InvalidOperationException("Dataset.Shared is not Lazy<Recording[]>.");
    var property = value.GetType().GetProperty(nameof(Lazy<object>.IsValueCreated))
        ?? throw new MissingMemberException(value.GetType().FullName, nameof(Lazy<object>.IsValueCreated));
    return () => (bool)(property.GetValue(value) ?? throw new InvalidOperationException("Lazy.IsValueCreated returned null."));
}

static (long GzipBytes, long ExpandedBytes) GetDatasetResourceSizes()
{
    using var stream = typeof(CursoryTrajectoryGenerator).Assembly.GetManifestResourceStream("SpyBrowser.Cursory.trajectories.json.gz")
        ?? throw new InvalidDataException("Embedded Cursory dataset resource is missing.");
    var compressedBytes = stream.Length;
    using var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
    var buffer = new byte[8192];
    long expandedBytes = 0;
    int read;
    while ((read = gzip.Read(buffer)) != 0) expandedBytes += read;
    Require(expandedBytes is > 0 and <= 32_000_000, "Expanded embedded dataset size violates the resource guard.");
    return (compressedBytes, expandedBytes);
}

static string GetPackageVersion() => typeof(HumanActions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
static string GetSourceRevision() => Environment.GetEnvironmentVariable("DEFAULT_RESOURCE_SOURCE_REVISION") ?? "not-supplied";
static string GetDirtyState() => Environment.GetEnvironmentVariable("DEFAULT_RESOURCE_SOURCE_DIRTY") ?? "not-supplied";
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed record Result(
    string Schema, string Case, string Os, string Runtime, string AssemblyVersion, string PackageVersion,
    string SourceRevision, string SourceDirtyState, string MouseAlgorithm, string CompatibilityMode,
    bool HumanizeEnabled, bool HumanizerCreated, bool DatasetIsValueCreatedBefore, bool DatasetIsValueCreatedAfter,
    long ConstructorAllocatedBytes, int ConstructorsMeasured, long ConstructorAllocationBudgetBytes,
    long CompressedDatasetBytes, long ExpandedDatasetBytes, long MaximumExpandedDatasetBytes, int DatasetRecordingCount,
    string AllocationScope);
