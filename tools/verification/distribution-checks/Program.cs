using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

if (args.Length >= 3 && args[0] is "--snapshot-process-save" or "--snapshot-interrupt-worker")
{
    var snapshotAssembly = Assembly.Load("SpyBrowser.Playwright");
    var workerStoreType = snapshotAssembly.GetType("SpyBrowser.Playwright.Diagnostics.DiagnosticSnapshotStore", throwOnError: true)!;
    var workerRecordType = snapshotAssembly.GetType("SpyBrowser.Playwright.Diagnostics.DiagnosticSnapshot", throwOnError: true)!;
    var workerStore = Activator.CreateInstance(workerStoreType, args[1], 2)!;
    var workerSnapshot = JsonSerializer.Deserialize("{}", workerRecordType, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    var workerSave = workerStoreType.GetMethod("SaveAsync", new[] { workerRecordType, typeof(string), typeof(CancellationToken) })!;
    Console.WriteLine("READY");
    Console.Out.Flush();
    do
    {
        var saved = await (Task<string>)workerSave.Invoke(workerStore, new object?[] { workerSnapshot, args[2], CancellationToken.None })!;
        Console.WriteLine($"SAVED|{saved}");
        Console.Out.Flush();
    } while (args[0] == "--snapshot-interrupt-worker");
    return;
}

var phase = Arg("--phase");
var root = Path.GetFullPath(Arg("--profile-root"));
var identities = Path.Combine(root, "identity-store");
var stateFile = Path.Combine(root, "storage-state.json");
var rollbackSnapshotFile = Path.Combine(root, "snapshot-rollback-unsupported.json");
var store = new IdentityStore(identities);
var identity = File.Exists(store.GetManifestPath("distribution-verification"))
    ? await store.GetAsync("distribution-verification")
    : await store.CreateAsync(BrowserIdentity.Create("distribution-verification", "Distribution verification") with
    {
        Browser = new BrowserIdentitySettings { Engine = BrowserEngine.Chromium, Channel = null }
    });
var manifestPath = store.GetManifestPath(identity.Id);
var manifestHashBefore = Hash(manifestPath);
var profilePath = store.GetProfileDirectory(identity.Id);
var expectedAlgorithm = phase == "candidate-cursory" ? "Cursory" : "Bezier";
var humanize = phase is "candidate-cursory" or "candidate-bezier" or "previous-bezier";
var humanOptions = new HumanInteractionOptions();
var algorithmProperty = typeof(HumanInteractionOptions).GetProperty("MouseAlgorithm");
if (algorithmProperty is not null)
    algorithmProperty.SetValue(humanOptions, Enum.Parse(algorithmProperty.PropertyType, expectedAlgorithm));
else if (phase == "candidate-cursory")
    throw new InvalidOperationException("Candidate package does not expose MouseAlgorithm=Cursory.");
var browserPath = Environment.GetEnvironmentVariable("SPYBROWSER_BROWSER_EXECUTABLE");
var session = await SpyBrowserLauncher.LaunchPersistentContextAsync(new SpyBrowserLaunchOptions
{
    IdentityId = identity.Id,
    IdentitiesRoot = identities,
    Humanize = humanize,
    HumanInteraction = humanOptions,
    RunGpuProbe = false,
    FailOnConsistencyErrors = false,
    ConfigurePersistentContext = options =>
    {
        options.Headless = true;
        if (!string.IsNullOrWhiteSpace(browserPath)) options.ExecutablePath = browserPath;
        options.Args = (options.Args ?? Array.Empty<string>()).Append("--no-sandbox").ToArray();
    }
});
try
{
    var context = session.Context;
    var page = await context.NewPageAsync();
    if (phase.StartsWith("previous-", StringComparison.Ordinal))
    {
        using var stateJson = JsonDocument.Parse(await File.ReadAllTextAsync(stateFile));
        var savedCookie = stateJson.RootElement.GetProperty("cookies").EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "distribution-check" && c.GetProperty("value").GetString() == "persisted");
        if (savedCookie.ValueKind == JsonValueKind.Undefined)
            throw new InvalidDataException("The previous package did not receive the candidate-created storage-state cookie.");
        await context.AddCookiesAsync(new[] { new Cookie { Name = "distribution-check", Value = "persisted", Url = "http://127.0.0.1" } });
        if (!(await context.CookiesAsync(new[] { "http://127.0.0.1" })).Any(c => c.Name == "distribution-check" && c.Value == "persisted"))
            throw new InvalidDataException("The rollback package failed to restore the storage-state cookie.");
    }
    await page.SetContentAsync("<button id='go'>go</button><script>window.clicks=0;document.querySelector('#go').addEventListener('click',()=>window.clicks++)</script>");
    await page.Mouse.MoveAsync(8, 8);
    await page.Locator("#go").ClickAsync();
    var clicks = await page.EvaluateAsync<int>("window.clicks");
    if (clicks != 1) throw new InvalidOperationException($"Expected exactly one native click; got {clicks}.");
    if (phase.StartsWith("candidate-", StringComparison.Ordinal))
    {
        await context.AddCookiesAsync(new[] { new Cookie { Name = "distribution-check", Value = "persisted", Url = "http://127.0.0.1" } });
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = stateFile });
    }
    var stateHash = File.Exists(stateFile) ? Hash(stateFile) : "not-created";
    var expectedStateHash = Environment.GetEnvironmentVariable("EXPECTED_STORAGE_STATE_SHA256");
    if (phase.StartsWith("previous-", StringComparison.Ordinal) && expectedStateHash is not null && stateHash != expectedStateHash)
        throw new InvalidDataException("Rollback changed the saved storage-state checksum.");
    Console.WriteLine($"PASS: installed package phase={phase}; humanize={humanize}; algorithm={expectedAlgorithm}; clicks={clicks}");
    Console.WriteLine($"Profile={profilePath}; manifestSha256={manifestHashBefore}; storageStateSha256={stateHash}");
    var playwrightVersion = typeof(Playwright).Assembly.GetName().Version?.ToString() ?? "unknown";
    Console.WriteLine($"Playwright={playwrightVersion}; driver=official Microsoft.Playwright package driver");
}
finally
{
    await session.DisposeAsync();
}
if (Hash(manifestPath) != manifestHashBefore) throw new InvalidDataException("Identity manifest changed during package switch test.");
Console.WriteLine("PASS: context disposed and identity manifest unchanged.");

// Snapshot API is deliberately discovered from the installed assembly. This scaffold does not copy or emulate pending SDK APIs.
var assembly = typeof(SpyBrowserLauncher).Assembly;
var snapshotType = assembly.GetType("SpyBrowser.Playwright.Diagnostics.DiagnosticSnapshotStore", throwOnError: false);
var snapshotRecord = assembly.GetType("SpyBrowser.Playwright.Diagnostics.DiagnosticSnapshot", throwOnError: false);
if (snapshotType is null || snapshotRecord is null)
{
    if (phase.StartsWith("previous-", StringComparison.Ordinal))
    {
        var expectedSnapshotHash = Environment.GetEnvironmentVariable("EXPECTED_ROLLBACK_SNAPSHOT_SHA256");
        if (string.IsNullOrEmpty(expectedSnapshotHash) || !File.Exists(rollbackSnapshotFile) ||
            Hash(rollbackSnapshotFile) != expectedSnapshotHash)
            throw new InvalidDataException("Rollback modified or removed the unsupported newer snapshot fixture.");
        Console.WriteLine("PASS: previous package without snapshot API ignores newer snapshot unchanged while opening identity/profile/storage state.");
    }
    else throw new InvalidDataException("Candidate package is missing the required snapshot API.");
}
else
{
    var ctor = snapshotType.GetConstructor(new[] { typeof(string), typeof(int) });
    var save = snapshotType.GetMethod("SaveAsync", new[] { snapshotRecord, typeof(string), typeof(CancellationToken) });
    var read = snapshotType.GetMethod("ReadAsync", new[] { typeof(string), typeof(CancellationToken) });
    var compare = snapshotType.GetMethod("Compare", new[] { snapshotRecord, snapshotRecord });
    if (ctor is null || save is null || read is null || compare is null)
        throw new InvalidDataException("Snapshot API was found but its installed signatures do not match the approved store contract.");
    await VerifySnapshotApiAsync(snapshotType, snapshotRecord, root, stateFile, manifestPath, manifestHashBefore);
}

async Task VerifySnapshotApiAsync(Type storeType, Type recordType, string profileRoot, string statePath, string identityManifest, string originalManifestHash)
{
    var snapshotDir = Path.Combine(profileRoot, "optional-snapshots");
    var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var baseline = JsonSerializer.Deserialize("{}", recordType, serializerOptions)
        ?? throw new InvalidDataException("Could not create the package's default snapshot record.");
    var findingType = assembly.GetType("SpyBrowser.Playwright.Diagnostics.ConsistencyFinding")
        ?? assembly.GetType("SpyBrowser.Playwright.ConsistencyFinding")
        ?? throw new InvalidDataException("Installed snapshot API is missing its consistency finding type.");
    var findingSeverity = findingType.GetProperty("Severity")?.PropertyType
        ?? throw new InvalidDataException("Installed consistency finding type has no severity property.");
    var finding = Activator.CreateInstance(findingType,
        Enum.Parse(findingSeverity, "Information"), "distribution.sanitization-check", "SENTINEL_DO_NOT_PERSIST_9bfc")!;
    var findings = Array.CreateInstance(findingType, 1);
    findings.SetValue(finding, 0);
    (recordType.GetProperty("Findings") ?? throw new InvalidDataException("Snapshot record has no findings field."))
        .SetValue(baseline, findings);
    var runtimeVersionsType = assembly.GetType("SpyBrowser.Playwright.Diagnostics.RuntimeVersionRecord")
        ?? throw new InvalidDataException("Installed snapshot API is missing RuntimeVersionRecord.");
    var versions = JsonSerializer.Deserialize("{\"spyBrowser\":\"candidate\",\"playwright\":\"1.61.0\",\"browserFamily\":\"chromium\",\"browserVersion\":\"1\",\"algorithm\":\"Bezier\",\"dataset\":\"baseline\"}", runtimeVersionsType, serializerOptions)!;
    recordType.GetProperty("Versions")?.SetValue(baseline, versions);
    var characteristicsProperty = recordType.GetProperty("Characteristics")
        ?? throw new InvalidDataException("Snapshot record has no browser characteristics.");
    var characteristicsType = characteristicsProperty.PropertyType;
    var baselineCharacteristics = JsonSerializer.Deserialize("{\"timezoneId\":\"UTC\",\"languages\":[\"en-US\"],\"userAgent\":\"baseline-agent\",\"platform\":\"Linux\",\"webGl1\":{\"renderer\":\"Intel baseline adapter\",\"canvasSampleHash\":\"PRIVATE_CANVAS_HASH_1\"},\"webGl2\":{\"renderer\":\"Intel baseline adapter\",\"canvasSampleHash\":\"PRIVATE_CANVAS_HASH_2\"},\"webGpu\":{\"device\":\"baseline-device\",\"error\":\"PRIVATE_GPU_ERROR\"}}", characteristicsType, serializerOptions)!;
    characteristicsProperty.SetValue(baseline, baselineCharacteristics);
    var constructor = storeType.GetConstructor(new[] { typeof(string), typeof(int) })!;
    var store = constructor.Invoke(new object[] { snapshotDir, 2 });
    var save = storeType.GetMethod("SaveAsync", new[] { recordType, typeof(string), typeof(CancellationToken) })!;
    var read = storeType.GetMethod("ReadAsync", BindingFlags.Public | BindingFlags.Static, binder: null,
        types: new[] { typeof(string), typeof(CancellationToken) }, modifiers: null)!;
    var compare = storeType.GetMethod("Compare", BindingFlags.Public | BindingFlags.Static, binder: null,
        types: new[] { recordType, recordType }, modifiers: null)!;
    var baselinePath = await (Task<string>)save.Invoke(store, new object?[] { baseline, null, CancellationToken.None })!;
    var protectedBaselineHash = Hash(baselinePath);
    var baselineJson = await File.ReadAllTextAsync(baselinePath);
    foreach (var privateSentinel in new[] { "SENTINEL_DO_NOT_PERSIST_9bfc", "PRIVATE_CANVAS_HASH_1", "PRIVATE_CANVAS_HASH_2", "PRIVATE_GPU_ERROR" })
        if (baselineJson.Contains(privateSentinel, StringComparison.Ordinal))
            throw new InvalidDataException($"Snapshot persisted a privacy-sensitive string field ({privateSentinel}).");
    var currentVersions = JsonSerializer.Deserialize("{\"spyBrowser\":\"candidate\",\"playwright\":\"1.61.0\",\"browserFamily\":\"chromium\",\"browserVersion\":\"2\",\"algorithm\":\"Cursory\",\"dataset\":\"candidate\"}", runtimeVersionsType, serializerOptions)!;
    var current = JsonSerializer.Deserialize("{}", recordType, serializerOptions)!;
    recordType.GetProperty("Versions")?.SetValue(current, currentVersions);
    characteristicsProperty.SetValue(current, JsonSerializer.Deserialize("{\"timezoneId\":\"UTC\",\"languages\":[\"en-US\"],\"userAgent\":\"candidate-agent\",\"platform\":\"Linux\",\"webGl1\":{\"renderer\":\"NVIDIA candidate adapter\"},\"webGl2\":{\"renderer\":\"NVIDIA candidate adapter\"},\"webGpu\":{\"device\":\"candidate-device\"}}", characteristicsType, serializerOptions)!);
    var currentPath = await (Task<string>)save.Invoke(store, new object?[] { current, baselinePath, CancellationToken.None })!;
    var baselineReadTask = (Task)read.Invoke(null, new object[] { baselinePath, CancellationToken.None })!;
    var currentReadTask = (Task)read.Invoke(null, new object[] { currentPath, CancellationToken.None })!;
    await baselineReadTask;
    await currentReadTask;
    var baselineRead = baselineReadTask.GetType().GetProperty("Result")!.GetValue(baselineReadTask)!;
    var currentRead = currentReadTask.GetType().GetProperty("Result")!.GetValue(currentReadTask)!;
    var comparison = compare.Invoke(null, new[] { baselineRead, currentRead })!;
    if (comparison.GetType().GetProperty("Changes")?.GetValue(comparison) is not System.Collections.ICollection changes || changes.Count == 0)
        throw new InvalidDataException("Installed snapshots did not report legitimate version/algorithm/GPU/context changes.");
    var changesJson = JsonSerializer.Serialize(changes, serializerOptions);
    if (changesJson.Contains("\"severity\":\"error\"", StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("Informational version/GPU/context changes were classified as malicious/incoherent.");
    var gpuChange = changes.Cast<object>().Any(change =>
        (string?)change.GetType().GetProperty("Field")?.GetValue(change) == "webgl1.renderer-category" &&
        change.GetType().GetProperty("Severity")?.GetValue(change)?.ToString() == "Information");
    if (!gpuChange)
        throw new InvalidDataException("Installed snapshot comparison did not emit the expected informational webgl1.renderer-category field change.");
    Console.WriteLine("PASS: snapshot-gpu-context-information field=webgl1.renderer-category severity=Information");
    var concurrent = await RunConcurrentSnapshotProcesses(snapshotDir, baselinePath);
    if (concurrent.Distinct(StringComparer.Ordinal).Count() != 2 || concurrent.Any(path => !File.Exists(path)) ||
        !File.Exists(baselinePath) || Hash(baselinePath) != protectedBaselineHash)
        throw new IOException("Concurrent SDK consumer processes did not preserve the selected baseline and produce unique complete files.");
    Console.WriteLine("PASS: concurrent SDK snapshot saves used two consumer processes; baseline retained.");
    var unknown = Path.Combine(snapshotDir, "unknown-schema.json");
    var json = await File.ReadAllTextAsync(baselinePath);
    var unknownNode = System.Text.Json.Nodes.JsonNode.Parse(json)!;
    unknownNode["schemaVersion"] = 999;
    // Leave an unsupported, privacy-projected snapshot next to the identity while
    // the genuinely older package opens the same profile. It has no snapshot reader
    // and must ignore this optional file, not require a fabricated parser.
    await File.WriteAllTextAsync(rollbackSnapshotFile, unknownNode.ToJsonString());
    await File.WriteAllTextAsync(unknown, unknownNode.ToJsonString());
    var schemaRejected = false;
    try { await (Task)read.Invoke(null, new object[] { unknown, CancellationToken.None })!; }
    catch (InvalidDataException) { schemaRejected = true; }
    if (!schemaRejected) throw new InvalidDataException("Unknown snapshot schema was accepted.");
    var cancelled = new CancellationToken(canceled: true);
    var cancellationHonored = false;
    try { await (Task)save.Invoke(store, new object?[] { current, baselinePath, cancelled })!; }
    catch (OperationCanceledException) { cancellationHonored = true; }
    if (!cancellationHonored) throw new InvalidDataException("Cancelled snapshot save unexpectedly succeeded.");
    if (!File.Exists(baselinePath) || Hash(baselinePath) != protectedBaselineHash || Directory.GetFiles(snapshotDir, "*.tmp").Length != 0)
        throw new InvalidDataException("Cancellation changed/removed the protected baseline or left temporary snapshot files behind.");
    var interruptStart = new System.Diagnostics.ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        ArgumentList = { Assembly.GetExecutingAssembly().Location, "--snapshot-interrupt-worker", snapshotDir, baselinePath }
    };
    var interruptedDuringSdkWrite = false;
    using (var child = System.Diagnostics.Process.Start(interruptStart) ?? throw new InvalidOperationException("Could not start installed SDK snapshot consumer."))
    {
        var readyTask = child.StandardOutput.ReadLineAsync();
        if (await Task.WhenAny(readyTask, Task.Delay(TimeSpan.FromSeconds(10))) != readyTask || await readyTask != "READY")
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            using var startupWait = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await child.WaitForExitAsync(startupWait.Token); }
            catch (OperationCanceledException) { throw new TimeoutException("SDK snapshot child did not exit after startup termination."); }
            var startupError = await child.StandardError.ReadToEndAsync();
            throw new TimeoutException($"SDK snapshot child did not reach its bounded READY checkpoint. stderr={startupError}");
        }
        var stdoutTask = child.StandardOutput.ReadToEndAsync();
        var stderrTask = child.StandardError.ReadToEndAsync();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline && !child.HasExited)
        {
            if (Directory.GetFiles(snapshotDir, ".*.tmp").Length > 0) { interruptedDuringSdkWrite = true; break; }
            await Task.Delay(10);
        }
        if (!child.HasExited) child.Kill(entireProcessTree: true);
        using var waitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await child.WaitForExitAsync(waitTimeout.Token); }
        catch (OperationCanceledException) { throw new TimeoutException("SDK snapshot worker did not exit after termination."); }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        Console.WriteLine($"SDK interruption worker completed {stdout.Split("SAVED|", StringSplitOptions.None).Length - 1} successful SDK saves before termination. {stderr}");
    }
    if (!File.Exists(baselinePath) || Hash(baselinePath) != protectedBaselineHash)
        throw new InvalidDataException("Terminating an installed SDK SaveAsync process changed/removed the selected baseline.");
    var crashTemps = Directory.GetFiles(snapshotDir, ".*.tmp");
    Console.WriteLine(interruptedDuringSdkWrite
        ? $"PASS: OS-terminated SDK SaveAsync during actual temporary-file write; {crashTemps.Length} orphan temp file(s) remain because process death cannot run finally (startup cleanup/recovery is not claimed)."
        : "PENDING: bounded SDK SaveAsync worker did not expose an active temp-file write before termination.");
    var permissionDenied = false;
    var deniedRoot = Path.Combine(profileRoot, "denied-snapshot-permissions");
    if (OperatingSystem.IsLinux())
    {
        Directory.CreateDirectory(deniedRoot);
        File.SetUnixFileMode(deniedRoot, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var deniedStore = constructor.Invoke(new object[] { Path.Combine(deniedRoot, "snapshots"), 2 });
            await (Task<string>)save.Invoke(deniedStore, new object?[] { current, null, CancellationToken.None })!;
        }
        catch (UnauthorizedAccessException) { permissionDenied = true; }
        catch (IOException) { permissionDenied = true; }
        finally
        {
            File.SetUnixFileMode(deniedRoot, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            if (Directory.Exists(deniedRoot)) Directory.Delete(deniedRoot, recursive: true);
        }
        Console.WriteLine(permissionDenied ? "PASS: real Linux chmod permission denial rejected SDK SaveAsync." : "PENDING: process privileges bypassed real Linux chmod denial; no fake-file-block substitute used.");
    }
    else if (OperatingSystem.IsWindows())
    {
        Directory.CreateDirectory(deniedRoot);
        string? userSid = null;
        var aclRestored = false;
        try
        {
            var whoami = new System.Diagnostics.ProcessStartInfo("whoami.exe")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { "/user", "/fo", "csv", "/nh" }
            };
            using (var identityProcess = System.Diagnostics.Process.Start(whoami) ?? throw new InvalidOperationException("Could not query the current Windows user SID."))
            {
                var identityOutput = await identityProcess.StandardOutput.ReadToEndAsync();
                var identityError = await identityProcess.StandardError.ReadToEndAsync();
                await identityProcess.WaitForExitAsync();
                if (identityProcess.ExitCode != 0) throw new IOException($"whoami could not resolve the current SID: {identityError}");
                var columns = identityOutput.Trim().TrimStart('"').TrimEnd('"').Split("\",\"", StringSplitOptions.None);
                userSid = columns.LastOrDefault(value => value.StartsWith("S-1-", StringComparison.Ordinal));
                if (userSid is null) throw new InvalidDataException($"Could not parse current user SID from whoami output: {identityOutput}");
            }
            RunAcl("/deny", $"*{userSid}:(OI)(CI)(W)");
            try
            {
                var deniedStore = constructor.Invoke(new object[] { Path.Combine(deniedRoot, "snapshots"), 2 });
                await (Task<string>)save.Invoke(deniedStore, new object?[] { current, null, CancellationToken.None })!;
            }
            catch (UnauthorizedAccessException) { permissionDenied = true; }
            catch (IOException) { permissionDenied = true; }
        }
        finally
        {
            if (userSid is not null)
            {
                RunAcl("/remove:d", $"*{userSid}");
                aclRestored = true;
            }
            if (aclRestored && Directory.Exists(deniedRoot)) Directory.Delete(deniedRoot, recursive: true);
        }
        if (File.Exists(baselinePath) && Hash(baselinePath) != protectedBaselineHash)
            throw new IOException("Real Windows ACL permission-denial check changed the protected snapshot baseline.");
        Console.WriteLine(permissionDenied ? "PASS: real Windows NTFS ACL denial rejected SDK SaveAsync; ACL restored before cleanup and baseline preserved." : "PENDING: Windows ACL did not deny SDK SaveAsync.");
    }
    else Console.WriteLine("PENDING: permission-denial verification requires real Linux chmod or Windows NTFS ACL semantics.");

    void RunAcl(string operation, string rule)
    {
        var start = new System.Diagnostics.ProcessStartInfo("icacls.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { deniedRoot, operation, rule }
        };
        using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("Could not start icacls.exe for the real Windows ACL fixture.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new IOException($"icacls {operation} failed ({process.ExitCode}): {stdout} {stderr}");
    }
    var storageStateHashBeforeDiscard = Hash(statePath);
    var filesBeforeDiscard = Directory.GetFiles(snapshotDir);
    foreach (var file in filesBeforeDiscard) File.Delete(file);
    Directory.Delete(snapshotDir);
    var browserProfile = Path.Combine(profileRoot, "identity-store", "identities", "distribution-verification", "profile");
    if (!File.Exists(identityManifest) || !File.Exists(statePath) || !Directory.Exists(browserProfile) ||
        Hash(identityManifest) != originalManifestHash || Hash(statePath) != storageStateHashBeforeDiscard)
        throw new InvalidDataException("Discarding snapshots changed identity, profile, or storage-state bytes.");
    Console.WriteLine("PASS: installed snapshot round-trip, explicit baseline comparison, two-process concurrency, unknown-schema rejection, cancellation cleanup and isolated discard.");

    async Task<string[]> RunConcurrentSnapshotProcesses(string directory, string protectedPath)
    {
        async Task<string> RunWorker()
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { Assembly.GetExecutingAssembly().Location, "--snapshot-process-save", directory, protectedPath }
            };
            using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("Could not start concurrent snapshot consumer.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Concurrent SDK snapshot process exceeded 30 seconds."); }
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0) throw new IOException($"SDK consumer process failed ({process.ExitCode}): {stderr}");
            var savedLine = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).SingleOrDefault(line => line.StartsWith("SAVED|", StringComparison.Ordinal));
            return savedLine is null ? throw new IOException($"SDK consumer emitted no completed SaveAsync path: {stdout} {stderr}") : savedLine[6..].Trim();
        }
        return await Task.WhenAll(RunWorker(), RunWorker());
    }
}

string Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"Missing {name}");
}
static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
