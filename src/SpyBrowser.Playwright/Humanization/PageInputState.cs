using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace SpyBrowser.Playwright.Humanization;

/// <summary>Serializes SDK-visible input per page and owns interruption of its in-flight call.</summary>
internal sealed class PageInputState
{
    private static readonly ConditionalWeakTable<IPage, PageInputState> States = new();
    private readonly IPage _page;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _lifetimeToken;
    private int _defaultTimeout = Timeout.Infinite;
    private int _disposed;

    private PageInputState(IPage page)
    {
        _page = page;
        _lifetimeToken = _lifetime.Token;
        page.Close += OnPageClose;
        page.Context.Close += OnContextClose;
    }

    public static PageInputState For(IPage page) => States.GetValue(page, static value => new PageInputState(value));

    public void SetDefaultTimeout(int milliseconds) => Volatile.Write(ref _defaultTimeout, Math.Max(0, milliseconds));

    public async Task RunAsync(Func<CancellationToken, Task> operation, int configuredBudgetMilliseconds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var observedDefault = Volatile.Read(ref _defaultTimeout);
        var budget = observedDefault == 0 || observedDefault == Timeout.Infinite
            ? configuredBudgetMilliseconds
            : Math.Min(configuredBudgetMilliseconds, observedDefault);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken, cancellationToken);
        if (budget > 0) deadline.CancelAfter(budget);
        await _gate.WaitAsync(deadline.Token).ConfigureAwait(false);
        Task? operationTask = null;
        var gateTransferred = false;
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            operationTask = operation(deadline.Token);
            await operationTask.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            try
            {
                await _page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false })
                    .WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            }
            catch { /* Preserve the initiating cancellation/timeout. */ }
            if (operationTask is not null)
            {
                try
                {
                    await operationTask.WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    gateTransferred = true;
                    _ = operationTask.ContinueWith(
                        completed =>
                        {
                            _ = completed.Exception;
                            _gate.Release();
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch { /* The underlying operation is observed; preserve cancellation. */ }
            }
            throw;
        }
        finally
        {
            if (!gateTransferred) _gate.Release();
        }
    }

    private void OnPageClose(object? sender, IPage page) => Dispose();
    private void OnContextClose(object? sender, IBrowserContext context) => Dispose();

    private void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _page.Close -= OnPageClose;
        _page.Context.Close -= OnContextClose;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
