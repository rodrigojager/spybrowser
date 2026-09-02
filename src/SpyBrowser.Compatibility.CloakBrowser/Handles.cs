using CloakBrowser.Human;
using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace CloakBrowser;

public sealed class CloakBrowserHandle : IAsyncDisposable
{
    private readonly SpyBrowserBrowserHandle _handle;

    private readonly bool _humanizeEnabled;

    internal CloakBrowserHandle(SpyBrowserBrowserHandle handle, bool humanizeEnabled)
    {
        _handle = handle;
        _humanizeEnabled = humanizeEnabled;
    }

    public BrowserIdentity Identity => _handle.Identity;

    public IBrowser Browser => _handle.Browser;

    public IBrowser RawBrowser => _handle.RawBrowser;

    public IPlaywright PlaywrightInstance => _handle.PlaywrightInstance;

    public bool HumanizeEnabled => _humanizeEnabled;

    public Task<IPage> NewPageAsync(BrowserNewPageOptions? options = null) =>
        _handle.NewPageAsync(options);

    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = null) =>
        _handle.NewContextAsync(options);

    public async Task<HumanPage> NewHumanPageAsync(BrowserNewPageOptions? options = null) =>
        new(await _handle.NewPageAsync(options).ConfigureAwait(false));

    public async Task CloseAsync() => await DisposeAsync().ConfigureAwait(false);

    public ValueTask DisposeAsync() => _handle.DisposeAsync();
}

public sealed class CloakContextHandle : IAsyncDisposable
{
    private readonly SpyBrowserContextHandle _handle;

    private readonly bool _humanizeEnabled;

    internal CloakContextHandle(SpyBrowserContextHandle handle, bool humanizeEnabled)
    {
        _handle = handle;
        _humanizeEnabled = humanizeEnabled;
    }

    public BrowserIdentity Identity => _handle.Identity;

    public IBrowserContext Context => _handle.Context;

    public IBrowserContext RawContext => _handle.RawContext;

    public IBrowser? Browser => _handle.Browser;

    public IBrowser? RawBrowser => _handle.RawBrowser;

    public IReadOnlyList<IPage> Pages => _handle.Pages;

    public bool HumanizeEnabled => _humanizeEnabled;

    public Task<IPage> NewPageAsync() => _handle.NewPageAsync();

    public async Task<HumanPage> NewHumanPageAsync() =>
        new(await _handle.NewPageAsync().ConfigureAwait(false));

    public async Task CloseAsync() => await DisposeAsync().ConfigureAwait(false);

    public ValueTask DisposeAsync() => _handle.DisposeAsync();
}
