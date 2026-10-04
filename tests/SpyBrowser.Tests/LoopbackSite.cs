using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SpyBrowser.Tests;

internal sealed class LoopbackSite : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private Task? _serveTask;

    public string Url { get; }
    public string? PeerUrl { get; set; }

    public LoopbackSite()
    {
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        Url = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(Url);
    }

    public Task StartAsync()
    {
        _listener.Start();
        _serveTask = ServeAsync();
        return Task.CompletedTask;
    }

    private async Task ServeAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext request;
            try
            {
                request = await _listener.GetContextAsync().WaitAsync(_stop.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) when (_stop.IsCancellationRequested) { break; }

            var path = request.Request.Url?.AbsolutePath;
            var rootFrame = path == "/root"
                ? WebUtility.HtmlEncode(PeerUrl ?? throw new InvalidOperationException("The cross-origin peer URL was not configured."))
                : string.Empty;
            var html = path switch
            {
                "/root" => $"<iframe id='outer' src='{rootFrame}frame'></iframe>",
                "/frame" => $"<iframe id='inner' src='{Url}nested'></iframe>",
                "/nested" => "<button id='go' class='item' onclick=\"this.dataset.clicked='yes'\">go</button><button class='item'>second</button>",
                "/opener" => "<button id='open-one' onclick=\"window.open('/popup?one')\">one</button><button id='open-two' onclick=\"window.open('/popup?two')\">two</button>",
                _ => "<button>popup</button>"
            };
            var bytes = Encoding.UTF8.GetBytes($"<!doctype html><html><body>{html}</body></html>");
            request.Response.ContentType = "text/html; charset=utf-8";
            request.Response.ContentLength64 = bytes.Length;
            await request.Response.OutputStream.WriteAsync(bytes);
            request.Response.Close();
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Close();
        try { _serveTask?.GetAwaiter().GetResult(); } catch (HttpListenerException) { }
        _stop.Dispose();
    }
}
