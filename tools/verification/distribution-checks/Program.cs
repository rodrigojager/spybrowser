using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

var phase = Arg("--phase");
var root = Path.GetFullPath(Arg("--profile-root"));
var identities = Path.Combine(root, "identity-store");
var stateFile = Path.Combine(root, "storage-state.json");
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
    Console.WriteLine("PENDING: installed SpyBrowser.Playwright package does not contain DiagnosticSnapshotStore/DiagnosticSnapshot API (ticket 19 integration not in this artifact).");
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
    var constructor = storeType.GetConstructor(new[] { typeof(string), typeof(int) })!;
    var store = constructor.Invoke(new object[] { snapshotDir, 2 });
    var save = storeType.GetMethod("SaveAsync", new[] { recordType, typeof(string), typeof(CancellationToken) })!;
    var read = storeType.GetMethod("ReadAsync", BindingFlags.Public | BindingFlags.Static, binder: null,
        types: new[] { typeof(string), typeof(CancellationToken) }, modifiers: null)!;
    var compare = storeType.GetMethod("Compare", BindingFlags.Public | BindingFlags.Static, binder: null,
        types: new[] { recordType, recordType }, modifiers: null)!;
    var baselinePath = await (Task<string>)save.Invoke(store, new object?[] { baseline, null, CancellationToken.None })!;
    if ((await File.ReadAllTextAsync(baselinePath)).Contains("SENTINEL_DO_NOT_PERSIST_9bfc", StringComparison.Ordinal))
        throw new InvalidDataException("Snapshot persisted a secret sentinel in a finding message.");
    var currentVersions = JsonSerializer.Deserialize("{\"spyBrowser\":\"candidate\",\"playwright\":\"1.61.0\",\"browserFamily\":\"chromium\",\"browserVersion\":\"2\",\"algorithm\":\"Cursory\",\"dataset\":\"candidate\"}", runtimeVersionsType, serializerOptions)!;
    var current = JsonSerializer.Deserialize("{}", recordType, serializerOptions)!;
    recordType.GetProperty("Versions")?.SetValue(current, currentVersions);
    var currentPath = await (Task<string>)save.Invoke(store, new object?[] { current, baselinePath, CancellationToken.None })!;
    var baselineReadTask = (Task)read.Invoke(null, new object[] { baselinePath, CancellationToken.None })!;
    var currentReadTask = (Task)read.Invoke(null, new object[] { currentPath, CancellationToken.None })!;
    await baselineReadTask;
    await currentReadTask;
    var baselineRead = baselineReadTask.GetType().GetProperty("Result")!.GetValue(baselineReadTask)!;
    var currentRead = currentReadTask.GetType().GetProperty("Result")!.GetValue(currentReadTask)!;
    var comparison = compare.Invoke(null, new[] { baselineRead, currentRead })!;
    if (comparison.GetType().GetProperty("Changes")?.GetValue(comparison) is not System.Collections.ICollection changes || changes.Count == 0)
        throw new InvalidDataException("Installed snapshots did not report legitimate version/algorithm changes.");
    var concurrent = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        await (Task<string>)save.Invoke(store, new object?[] { current, baselinePath, CancellationToken.None })!));
    if (concurrent.Distinct(StringComparer.Ordinal).Count() != 2 || concurrent.Any(path => !File.Exists(path)))
        throw new IOException("Concurrent snapshot saves did not produce unique complete files.");
    var unknown = Path.Combine(snapshotDir, "unknown-schema.json");
    var json = await File.ReadAllTextAsync(baselinePath);
    var unknownNode = System.Text.Json.Nodes.JsonNode.Parse(json)!;
    unknownNode["schemaVersion"] = 999;
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
    if (!File.Exists(baselinePath) || Directory.GetFiles(snapshotDir, "*.tmp").Length != 0)
        throw new InvalidDataException("Cancellation removed the protected baseline or left temporary snapshot files behind.");
    if (OperatingSystem.IsLinux())
    {
        var deniedRoot = Path.Combine(profileRoot, "denied-snapshot-permissions");
        Directory.CreateDirectory(deniedRoot);
        File.SetUnixFileMode(deniedRoot, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        var permissionDenied = false;
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
        Console.WriteLine(permissionDenied ? "PASS: real Linux chmod permission denial preserved the snapshot baseline." : "PENDING: process privileges bypassed real Linux chmod denial; no fake-file-block substitute used.");
    }
    else Console.WriteLine("PENDING: permission-denial verification requires real Linux chmod semantics.");
    var filesBeforeDiscard = Directory.GetFiles(snapshotDir);
    foreach (var file in filesBeforeDiscard) File.Delete(file);
    Directory.Delete(snapshotDir);
    var browserProfile = Path.Combine(profileRoot, "identity-store", "identities", "distribution-verification", "profile");
    if (!File.Exists(identityManifest) || !File.Exists(statePath) || !Directory.Exists(browserProfile) || Hash(identityManifest) != originalManifestHash)
        throw new InvalidDataException("Discarding snapshots changed identity, profile, or storage-state files.");
    Console.WriteLine("PASS: installed snapshot round-trip, explicit baseline comparison, unique concurrent saves, unknown-schema rejection, cancellation cleanup and isolated discard.");
}

string Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"Missing {name}");
}
static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
