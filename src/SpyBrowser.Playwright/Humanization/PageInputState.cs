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
    private int _defaultTimeout = 30_000;
    private int _disposed;

    private PageInputState(IPage page)
    {
        _page = page;
        page.Close += (_, _) => Dispose();
        page.Context.Close += (_, _) => Dispose();
    }

    public static PageInputState For(IPage page) => States.GetValue(page, static value => new PageInputState(value));

    public void SetDefaultTimeout(int milliseconds) => Volatile.Write(ref _defaultTimeout, milliseconds);

    public async Task RunAsync(Func<Task> operation, int configuredBudgetMilliseconds, CancellationToken cancellationToken = default)
    {
        var observedDefault = Volatile.Read(ref _defaultTimeout);
        var budget = Math.Min(configuredBudgetMilliseconds, observedDefault);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        deadline.CancelAfter(budget);
        await _gate.WaitAsync(deadline.Token).ConfigureAwait(false);
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            var task = operation();
            try
            {
                await task.WaitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            {
                // Playwright calls do not accept CancellationToken. Close the owned page to stop
                // the driver operation, then observe its actual task before releasing the gate.
                try { await _page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).ConfigureAwait(false); }
                catch { /* Preserve timeout, cancellation, or the original operation failure. */ }
                try { await task.ConfigureAwait(false); }
                catch { /* The underlying task is observed; never replace the initiating cause. */ }
                deadline.Token.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
