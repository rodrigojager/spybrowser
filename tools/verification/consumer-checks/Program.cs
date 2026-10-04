using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using RpaFlow.Playwright;
using RpaFlow.Runtime;
using SpyBrowser.Playwright;

var output = ArgumentValue(args, "--output") ?? Environment.CurrentDirectory;
Directory.CreateDirectory(output);
var checks = new List<object>();
var listener = new HttpListener();
var port = FreePort();
var origin = $"http://127.0.0.1:{port}/";
listener.Prefixes.Add(origin);
listener.Start();
var server = ServeAsync(listener);
try
{
    var options = new PlaywrightRuntimeOptions(true, "spybrowser", 10, 10,
        output, output, Locale: "pt-BR", ViewportWidth: 1173, ViewportHeight: 777,
        SpyBrowserHumanize: true);
    await using var session = await BrowserLauncher.LaunchAsync(options);
    IBrowserContext? observed = null;
    session.Browser.Context += (_, context) => observed = context;
    var context = await session.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new() { Width = 1173, Height = 777 },
        Locale = "pt-BR",
        TimezoneId = BrowserTimeZone()
    });
    var wrappedPage = await context.NewPageAsync();
    var page = PlaywrightHumanizer.Unwrap(wrappedPage);
    Require(ReferenceEquals(observed, context), "Context event reference equals returned context");
    Require(session.Browser.Contexts.Contains(context), "Browser.Contexts includes created context");
    var wrapper = !ReferenceEquals(context, PlaywrightHumanizer.Unwrap(context));
    Require(wrapper, "RpaBlockly adapter exposes wrapped context with Humanize enabled");
    var environment = await page.EvaluateAsync<string[]>("[Intl.DateTimeFormat().resolvedOptions().timeZone, navigator.language, String(innerWidth), String(innerHeight)]");
    Require(environment[0] == BrowserTimeZone(), $"Timezone local/UTC observed as {environment[0]}");
    Require(environment[1].StartsWith("pt-BR", StringComparison.OrdinalIgnoreCase), $"Locale preserved as {environment[1]}");
    Require(environment[2] == "1173" && environment[3] == "777", "Explicit viewport preserved");
    checks.Add(Pass("humanize-on, Chromium launch via pinned RpaBlockly BrowserLauncher, in-memory rpablockly identity, locale, timezone, viewport and context event/collection"));

    var statePath = Path.Combine(output, "storage-state.json");
    await page.GotoAsync(origin);
    var stateContext = context;
    await stateContext.AddCookiesAsync([new() { Name = "contract", Value = "loaded", Url = origin }]);
    await stateContext.StorageStateAsync(new() { Path = statePath });
    Require(File.Exists(statePath), "StorageStatePath output created");
    checks.Add(Pass("StorageStatePath write on local loopback origin"));

    var shot = Path.Combine(output, "consumer.png");
    await page.ScreenshotAsync(new() { Path = shot });
    Require(File.Exists(shot) && new FileInfo(shot).Length > 0, "Screenshot artifact created");
    await page.GetByText("Navigate", new() { Exact = true }).ClickAsync();
    await page.WaitForURLAsync("**/next");
    Require(page.Url.EndsWith("/next", StringComparison.Ordinal), "Navigation completed");
    checks.Add(Pass("Screenshot, navigation and local loopback served page"));

    await page.GotoAsync(origin);
    var popupTask = page.WaitForPopupAsync();
    await page.GetByText("Popup", new() { Exact = true }).ClickAsync();
    var popup = await popupTask;
    await popup.WaitForLoadStateAsync();
    Require(await popup.TitleAsync() == "popup", "Popup title available");
    var frame = page.FrameLocator("#nested").FrameLocator("#inner");
    Require(await frame.Locator("#frame-value").InnerTextAsync() == "frame-ok", "Nested frame content reachable");
    checks.Add(Pass("Popup and nested frame workflow"));

    var downloadTask = page.WaitForDownloadAsync();
    await page.GetByText("Download", new() { Exact = true }).ClickAsync();
    var download = await downloadTask;
    var downloaded = Path.Combine(output, "download.txt");
    await download.SaveAsAsync(downloaded);
    Require(await File.ReadAllTextAsync(downloaded) == "download-ok", "Loopback download body matches");
    checks.Add(Pass("Loopback download and saved artifact"));

    var runnerOptions = options with { StorageStatePath = statePath };
    var restoredCookie = string.Empty;
    var restoreRunner = new RpaRunner([new ConsumerStep("restore cookie", async (rpa, token) =>
    {
        await rpa.Page.GotoAsync(origin);
        restoredCookie = await rpa.Page.EvaluateAsync<string>("document.cookie");
    })], runnerOptions);
    await restoreRunner.RunAsync(Request("runner-storage-restore"), [], CancellationToken.None);
    Require(restoredCookie.Contains("contract=loaded", StringComparison.Ordinal), "RpaRunner restored cookie using StorageStatePath");
    checks.Add(Pass("Actual RpaRunner local flow restores StorageStatePath cookies"));

    var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    IBrowserContext? cancelledContext = null;
    Task? outstandingFill = null;
    var cancellingRunner = new RpaRunner([new ConsumerStep("cancel pending fill", async (rpa, token) =>
    {
        await rpa.Page.GotoAsync(origin);
        cancelledContext = rpa.Page.Context;
        outstandingFill = rpa.Page.Locator("#fill-never-present").FillAsync("cancel-me");
        cancellationStarted.TrySetResult();
        await outstandingFill.WaitAsync(token);
    })], options);
    using (var fillCancellation = new CancellationTokenSource())
    {
        var runTask = cancellingRunner.RunAsync(Request("cancel-fill"), [], fillCancellation.Token);
        await cancellationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        fillCancellation.Cancel();
        try { await runTask; throw new InvalidOperationException("Expected actual RpaRunner cancellation"); }
        catch (OperationCanceledException) { }
    }
    if (outstandingFill is not null)
    {
        try { await outstandingFill.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception) { }
    }
    Require(cancelledContext is { IsClosed: true } && outstandingFill is { IsCompleted: true },
        $"RpaRunner cancellation closes actual context and settles pending Fill; closed={cancelledContext?.IsClosed}, fill completed={outstandingFill?.IsCompleted}, faulted={outstandingFill?.IsFaulted}, canceled={outstandingFill?.IsCanceled}");
    checks.Add(Pass("Fill cancellation closes actual RpaRunner context and observes underlying Fill task"));

    var concurrentInputs = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
    async Task RunMemoryJob(string id, string marker)
    {
        var runner = new RpaRunner([new ConsumerStep("isolated memory input", async (rpa, token) =>
        {
            await rpa.Page.GotoAsync(origin);
            var received = rpa.ExecutionRequest.Input["marker"]?.GetValue<string>() ?? "";
            concurrentInputs[id] = received;
            await rpa.Page.EvaluateAsync("value => document.body.dataset.marker = value", received);
        })], options);
        await runner.RunAsync(Request(id, marker), [], CancellationToken.None);
    }
    await Task.WhenAll(RunMemoryJob("memory-a", "input-a"), RunMemoryJob("memory-b", "input-b"));
    Require(concurrentInputs.TryGetValue("memory-a", out var markerA) && markerA == "input-a" &&
        concurrentInputs.TryGetValue("memory-b", out var markerB) && markerB == "input-b",
        "Concurrent RpaRunner jobs preserve separate in-memory inputs with shared identity configuration");
    checks.Add(Pass("Two concurrent actual RpaRunner jobs retain independent inputs without profile leases"));

    var lateSession = await BrowserLauncher.LaunchAsync(options);
    var deliveredSession = new TaskCompletionSource<BrowserSession>(TaskCreationOptions.RunContinuationsAsynchronously);
    var cleanupHelper = typeof(BrowserLauncher).GetMethod("AwaitCancellableResourceAsync", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("RpaBlockly cancellable-resource helper was not found");
    var launchCancellation = new CancellationTokenSource();
    var cleanupTask = (Task<BrowserSession>)cleanupHelper.MakeGenericMethod(typeof(BrowserSession)).Invoke(null,
        [deliveredSession.Task, new Func<BrowserSession, Task>(s => s.DisposeAsync().AsTask()), launchCancellation.Token])!;
    launchCancellation.Cancel();
    try { await cleanupTask; throw new InvalidOperationException("Expected cancellation before late resource delivery"); }
    catch (OperationCanceledException) { }
    deliveredSession.SetResult(lateSession);
    var cleanupDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
    while (lateSession.Browser.IsConnected && DateTime.UtcNow < cleanupDeadline) await Task.Delay(50);
    Require(!lateSession.Browser.IsConnected, "RpaBlockly's delayed-result cleanup disconnected the actual native browser");
    checks.Add(Pass("Actual RpaBlockly cancellation helper cleans a controlled late BrowserSession (native IsConnected=false)"));

    await context.CloseAsync();
    Require(!session.Browser.Contexts.Contains(context), "Closed context removed from Browser.Contexts");
    checks.Add(Pass("Context close removes collection entry"));

    await using var rawSession = await BrowserLauncher.LaunchAsync(options with { SpyBrowserHumanize = false });
    var raw = await rawSession.NewContextAsync(new BrowserNewContextOptions());
    Require(ReferenceEquals(raw, PlaywrightHumanizer.Unwrap(raw)), "Humanize=false context remains raw");
    await raw.CloseAsync();
    checks.Add(Pass("Humanize off remains raw"));
}
catch (Exception ex)
{
    checks.Add(new { name = "local consumer subset", status = "failed", error = ex.ToString() });
    WriteEvidence(output, checks);
    throw;
}
finally
{
    listener.Stop();
    try { await server; } catch (HttpListenerException) { }
}

// The mixed CAPTCHA/provider suite is deliberately excluded, so no full V2 output contract is inferred.
checks.Add(new { name = "Full V2 DSL runner output contract (the mixed CAPTCHA/provider suite is intentionally excluded)", status = "incomplete", reason = "This local subset exercises RpaRunner but does not run the full V2 DSL/CAPTCHA/provider suite." });
WriteEvidence(output, checks);
Console.WriteLine($"Consumer subset: {checks.Count(x => ((dynamic)x).status == "passed")} passed; incomplete cases recorded in {Path.Combine(output, "harness-evidence.json")}");

static async Task ServeAsync(HttpListener listener)
{
    while (listener.IsListening)
    {
        HttpListenerContext c;
        try { c = await listener.GetContextAsync(); } catch (HttpListenerException) { break; }
        var path = c.Request.Url?.AbsolutePath ?? "/";
        if (path == "/file") { c.Response.Headers.Add("Content-Disposition", "attachment; filename=download.txt"); await Write(c, "download-ok", "text/plain"); }
        else if (path == "/next") await Write(c, "<title>next</title>navigated", "text/html");
        else if (path == "/popup") await Write(c, "<title>popup</title>popup", "text/html");
        else await Write(c, """
            <!doctype html><title>consumer</title><a href="/next">Navigate</a>
            <button onclick="window.open('/popup')">Popup</button><a href="/file">Download</a>
            <iframe id="nested" srcdoc="&lt;iframe id='inner' srcdoc='&amp;lt;span id=&amp;quot;frame-value&amp;quot;&amp;gt;frame-ok&amp;lt;/span&amp;gt;'&gt;&lt;/iframe&gt;"></iframe>
            """, "text/html");
        c.Response.Close();
    }
}
static async Task Write(HttpListenerContext c, string content, string type)
{ var bytes = System.Text.Encoding.UTF8.GetBytes(content); c.Response.ContentType = type; c.Response.ContentLength64 = bytes.Length; await c.Response.OutputStream.WriteAsync(bytes); }
static string BrowserTimeZone() { var id = TimeZoneInfo.Local.Id; if (OperatingSystem.IsWindows() && TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var iana)) id = iana; return id.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase) ? "UTC" : id; }
static int FreePort() { var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); l.Start(); var p = ((System.Net.IPEndPoint)l.LocalEndpoint).Port; l.Stop(); return p; }
static string? ArgumentValue(string[] a, string key) { var i = Array.IndexOf(a, key); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
static object Pass(string name) => new { name, status = "passed" };
static FlowExecutionRequest Request(string id, string? marker = null) => new(id,
    marker is null ? new JsonObject() : new JsonObject { ["marker"] = marker }, new JsonObject(), new JsonObject());
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void WriteEvidence(string output, List<object> checks) => File.WriteAllText(Path.Combine(output, "harness-evidence.json"), JsonSerializer.Serialize(new { checks, completedAtUtc = DateTimeOffset.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));

sealed class ConsumerStep(string name, Func<RpaContext, CancellationToken, Task> execute) : IRpaStep
{
    public string Name => name;
    public Task ExecuteAsync(RpaContext context, CancellationToken cancellationToken) => execute(context, cancellationToken);
}
