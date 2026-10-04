using System.Net;
using System.Text.Json;
using Microsoft.Playwright;
using RpaFlow.Playwright;
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

// These checks need upstream implementation work or deterministic seams not exposed by this pinned consumer.
foreach (var title in new[] {
    "Fill cancellation closes actual RpaRunner context and observes underlying Fill task",
    "Late-completing browser launch is cleaned after cancellation",
    "Two concurrent memory-identity jobs do not share input/session state",
    "Input StorageStatePath cookie restore through RpaRunner",
    "V1/V2 complete runner flow output contract (selectively not running the mixed CAPTCHA suite)"
}) checks.Add(new { name = title, status = "incomplete", reason = "Not exercised by this isolated subset; pending implementation/dependencies are not inferred as passing." });
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
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void WriteEvidence(string output, List<object> checks) => File.WriteAllText(Path.Combine(output, "harness-evidence.json"), JsonSerializer.Serialize(new { checks, completedAtUtc = DateTimeOffset.UtcNow }, new JsonSerializerOptions { WriteIndented = true }));
