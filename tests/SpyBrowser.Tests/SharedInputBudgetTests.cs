using System.Diagnostics;
using System.Reflection;
using Microsoft.Playwright;
using SpyBrowser.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class SharedInputBudgetTests
{
    [Fact]
    public async Task Queued_native_default_is_reduced_in_raw_options_and_public_cancellation_prevents_operation()
    {
        var page = FakePage.Create();
        var state = PageInputState.For(page);
        state.SetDefaultTimeout(100);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = state.RunAsync(async _ =>
        {
            entered.SetResult();
            await release.Task;
        }, 500);
        await entered.Task;

        var observedTimeout = 0f;
        var second = state.RunAsync(token =>
        {
            var remaining = PageInputState.GetRemainingBudgetMilliseconds(page, token);
            Assert.NotNull(remaining);
            var method = typeof(ILocator).GetMethod(nameof(ILocator.FillAsync), [typeof(string), typeof(LocatorFillOptions)])!;
            var args = HumanizingDispatchProxy<ILocator>.ApplyRemainingNativeTimeout(method, ["x", null], page, token);
            observedTimeout = ((LocatorFillOptions)args[1]!).Timeout!.Value;
            var supplied = new LocatorFillOptions { Force = true };
            var adjusted = HumanizingDispatchProxy<ILocator>.ApplyRemainingNativeTimeout(method, ["x", supplied], page, token);
            Assert.NotSame(supplied, adjusted[1]);
            Assert.Null(supplied.Timeout);
            Assert.True(supplied.Force);
            Assert.True(((LocatorFillOptions)adjusted[1]!).Force);
            Assert.InRange(((LocatorFillOptions)adjusted[1]!).Timeout!.Value, 1, 99);
            return Task.CompletedTask;
        }, 100, nativeDefaultTimeoutApplies: true);

        // Deterministically spend part of the operation's monotonic budget while it is queued.
        var until = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 40;
        while (Stopwatch.GetTimestamp() < until) Thread.SpinWait(64);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.InRange(observedTimeout, 1, 99);

        var finalActionReached = false;
        await Assert.ThrowsAsync<TimeoutException>(() => state.RunAsync(token =>
        {
            var untilExpiry = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 8;
            while (Stopwatch.GetTimestamp() < untilExpiry) Thread.SpinWait(64);
            _ = PageInputState.GetRemainingBudgetMilliseconds(page, token);
            finalActionReached = true;
            return Task.CompletedTask;
        }, 100, nativeDefaultTimeoutApplies: true));
        Assert.False(finalActionReached);

        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocker = state.RunAsync(async _ => { held.SetResult(); await unblock.Task; }, 500);
        await held.Task;
        using var cancellation = new CancellationTokenSource();
        var invoked = false;
        var queued = state.RunAsync(_ => { invoked = true; return Task.CompletedTask; }, 500, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        unblock.SetResult();
        await blocker;
        Assert.False(invoked);
    }

    [Fact]
    public async Task Compatible_typing_focus_receives_the_shared_default_budget_not_a_fresh_typing_budget()
    {
        var page = FakePage.Create();
        var locator = DispatchProxy.Create<ILocator, FocusLocatorProxy>();
        var proxy = (FocusLocatorProxy)(object)locator;
        proxy.Page = page;
        var state = PageInputState.For(page);
        state.SetDefaultTimeout(100);
        var actions = new HumanActions(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            TypingDeadlineMilliseconds = 15_000
        });

        await state.RunAsync(token => actions.CompatibleTypeAsync(locator, "", token), 15_000);

        Assert.InRange(proxy.FocusTimeout!.Value, 1, 100);
        Assert.Equal(1, proxy.FocusCalls);
    }

    public class FocusLocatorProxy : DispatchProxy
    {
        internal IPage Page = null!;
        internal float? FocusTimeout;
        internal int FocusCalls;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Page") return Page;
            if (method?.Name == "FocusAsync")
            {
                FocusCalls++;
                FocusTimeout = ((LocatorFocusOptions)args![0]!).Timeout;
                return Task.CompletedTask;
            }
            throw new NotSupportedException(method?.Name);
        }
    }

    private static class FakePage
    {
        public static IPage Create()
        {
            StubProxy.ContextValue = DispatchProxy.Create<IBrowserContext, StubProxy>();
            return DispatchProxy.Create<IPage, StubProxy>();
        }
    }

    public class StubProxy : DispatchProxy
    {
        internal static IBrowserContext? ContextValue;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Context") return ContextValue;
            if (method?.Name == "get_IsClosed") return false;
            return method is null ? null : DefaultValue(method.ReturnType);
        }

        private static object? DefaultValue(Type type) => type == typeof(void) ? null :
            type == typeof(Task) ? Task.CompletedTask :
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) ?
                typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, [type.GetGenericArguments()[0].IsValueType ? Activator.CreateInstance(type.GetGenericArguments()[0]) : null]) :
            type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
