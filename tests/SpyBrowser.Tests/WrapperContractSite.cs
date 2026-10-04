using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SpyBrowser.Tests;

/// <summary>Isolated, deterministic loopback pages for wrapper contract tests.</summary>
internal sealed class WrapperContractSite : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private Task? _server;
    public string Url { get; }
    public string? PeerUrl { get; set; }

    public WrapperContractSite()
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
        _server = ServeAsync();
        return Task.CompletedTask;
    }

    private async Task ServeAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext request;
            try { request = await _listener.GetContextAsync().WaitAsync(_stop.Token); }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) when (_stop.IsCancellationRequested) { break; }
            var html = request.Request.Url?.AbsolutePath switch
            {
                "/frames" => $"<iframe id='outer' src='{PeerUrl ?? Url}frame'></iframe>",
                "/options" => "<div id='option-root'><div id='option-child'><span id='option-leaf'>match</span></div></div>",
                "/frame" => $"<iframe id='inner' src='{Url}nested'></iframe>",
                "/nested" => "<button id='policy-action' onclick=\"this.dataset.result='done'\">frame action</button><button class='item'>a</button><button class='item'>b</button>",
                "/opener" => "<button id='open-work' onclick=\"window.open('/popup')\">open</button>",
                "/popup" => "<button id='popup-action' onclick=\"this.dataset.result='acted'\">popup action</button>",
                _ => "<p>loopback fixture</p>"
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
        try { _server?.GetAwaiter().GetResult(); } catch (HttpListenerException) { }
        _stop.Dispose();
    }
}
