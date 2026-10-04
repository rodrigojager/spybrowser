using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace SpyBrowser.Playwright.Humanization;

/// <summary>Serializes SDK-visible input per page and owns interruption of its in-flight call.</summary>
internal sealed class PageInputState
{
    private static readonly ConditionalWeakTable<IPage, PageInputState> States = new();
    private static readonly AsyncLocal<InputLease?> AmbientLease = new();
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

    public async Task RunAsync(
        Func<CancellationToken, Task> operation,
        int configuredBudgetMilliseconds,
        CancellationToken cancellationToken = default,
        int? explicitTimeoutMilliseconds = null,
        bool nativeDefaultTimeoutApplies = false,
        bool closePageOnBudgetCancellation = false)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var observedDefault = Volatile.Read(ref _defaultTimeout);
        // Playwright's explicit operation timeout overrides page/context defaults; zero is unlimited.
        var selectedTimeout = explicitTimeoutMilliseconds ?? observedDefault;
        // Explicit Playwright options and a tracked Playwright default (including zero) belong
        // to the native operation. Duplicating either timeout here can win the race, turn a native
        // TimeoutException into cancellation, and close the page. In particular, Timeout=0 is
        // native-unlimited, not SDK-defaulted.
        var nativeOwnsTimeout = explicitTimeoutMilliseconds.HasValue || (nativeDefaultTimeoutApplies && observedDefault <= 0);
        var budget = explicitTimeoutMilliseconds.HasValue ? Timeout.Infinite :
            nativeDefaultTimeoutApplies ? (observedDefault > 0 ? observedDefault : Timeout.Infinite) :
            selectedTimeout > 0 ? selectedTimeout : configuredBudgetMilliseconds;
        var operationStarted = Stopwatch.GetTimestamp();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken, cancellationToken);
        if (!nativeOwnsTimeout && budget > 0) deadline.CancelAfter(budget);
        await _gate.WaitAsync(deadline.Token).ConfigureAwait(false);
        Task? operationTask = null;
        var gateTransferred = false;
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            var previousLease = AmbientLease.Value;
            // Native Playwright defaults own the action timeout after admission. Keep the
            // monotonic lease for derived SDK stages, but avoid racing its native TimeoutException.
            if (nativeDefaultTimeoutApplies && budget > 0) deadline.CancelAfter(Timeout.Infinite);
            AmbientLease.Value = budget > 0 ? new InputLease(this, deadline.Token, budget, operationStarted) : null;
            try
            {
                operationTask = operation(deadline.Token);
                await operationTask.WaitAsync(deadline.Token).ConfigureAwait(false);
            }
            finally
            {
                AmbientLease.Value = previousLease;
            }
        }
        catch (OperationCanceledException)
        {
            // WaitAsync only cancels our wait, not a Playwright call already sent to its driver.
            // If the operation has actually stopped (or never started), preserve the page and gate.
            var operationStopped = operationTask is null ||
                await ObserveBoundedAsync(operationTask, TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            // An owned core deadline can win before this linked timer's callback is
            // scheduled. Preserve legacy closure for that SDK cancellation too;
            // explicit native timeouts and caller/lifetime cancellation are distinct.
            var budgetExpired = !nativeOwnsTimeout && !nativeDefaultTimeoutApplies &&
                !cancellationToken.IsCancellationRequested && !_lifetimeToken.IsCancellationRequested;
            if (!operationStopped || (closePageOnBudgetCancellation && budgetExpired))
            {
                // Stop admitting work before close is attempted: CloseAsync may itself fail or stall.
                CancelLifetime();
                var closeTask = ClosePageAsync();
                await ObserveBoundedAsync(closeTask, TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                if (!operationStopped && operationTask is not null)
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
            }
            throw;
        }
        finally
        {
            if (!gateTransferred) _gate.Release();
        }
    }

    public static int? GetRemainingBudgetMilliseconds(IPage page, CancellationToken token)
    {
        var lease = AmbientLease.Value;
        if (lease is null || !ReferenceEquals(lease.State._page, page) || lease.Token != token) return null;
        var remaining = lease.BudgetMilliseconds - Stopwatch.GetElapsedTime(lease.Started).TotalMilliseconds;
        if (remaining <= 0)
        {
            token.ThrowIfCancellationRequested();
            throw new TimeoutException("The shared input-operation budget expired.");
        }

        return Math.Max(1, (int)Math.Ceiling(remaining));
    }

    private sealed record InputLease(PageInputState State, CancellationToken Token, int BudgetMilliseconds, long Started);

    private async Task ClosePageAsync()
    {
        try
        {
            if (!_page.IsClosed)
            {
                await _page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).ConfigureAwait(false);
            }
        }
        catch { /* The original timeout/cancellation is the primary failure. */ }
    }

    private static async Task<bool> ObserveBoundedAsync(Task task, TimeSpan timeout)
    {
        try
        {
            await task.WaitAsync(timeout).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            _ = task.ContinueWith(
                completed => _ = completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return false;
        }
        catch
        {
            // Awaiting observes faults and cancellations. Cleanup must not replace the initiating error.
            return true;
        }
    }

    private void OnPageClose(object? sender, IPage page) => Dispose();
    private void OnContextClose(object? sender, IBrowserContext context) => Dispose();

    private void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _page.Close -= OnPageClose;
        _page.Context.Close -= OnContextClose;
        CancelLifetime();
    }

    private void CancelLifetime()
    {
        try { _lifetime.Cancel(); }
        catch (ObjectDisposedException) { }
    }
}
