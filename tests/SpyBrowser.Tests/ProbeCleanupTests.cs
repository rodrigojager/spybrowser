using System.Reflection;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class ProbeCleanupTests
{
    [Fact]
    public async Task Evaluation_cancellation_closes_probe_and_observes_late_protocol_fault_without_more_sends()
    {
        var contextState = new FakeContextState();
        var context = CreateProxy<IBrowserContext, FakeContext>(contextState);
        var evaluation = new TaskCompletionSource<BrowserSurfaceDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pageState = new FakePageState(context, evaluation.Task);
        var page = CreateProxy<IPage, FakePage>(pageState);
        var registry = ProbePageRegistry.For(context);
        registry.ProbePageFactory = () => Task.FromResult(page);
        try
        {
            using var cancellation = new CancellationTokenSource();
            // Cancel at the actual protocol boundary, not by racing two thread-pool timers.
            pageState.OnEvaluation = cancellation.Cancel;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GpuProbe.RunAsync(context, TimeSpan.FromSeconds(3), cancellation.Token));
            Assert.Equal(1, pageState.EvaluationCalls);
            Assert.Equal(1, pageState.CloseCalls);
            Assert.DoesNotContain(page, registry.Filter([page]));

            evaluation.SetException(new InvalidOperationException("private protocol payload"));
            await Task.Delay(50);
            Assert.True(evaluation.Task.IsFaulted);
            Assert.Equal(1, pageState.EvaluationCalls);
            Assert.Equal(1, pageState.CloseCalls);
        }
        finally
        {
            registry.ProbePageFactory = null;
        }
    }

    [Fact]
    public async Task Close_fault_is_observed_and_reports_stable_secret_free_cleanup_outcome_without_pending_probe()
    {
        const string secret = "secret-browser-endpoint-token";
        var contextState = new FakeContextState();
        var context = CreateProxy<IBrowserContext, FakeContext>(contextState);
        var pageState = new FakePageState(context, Task.FromResult(new BrowserSurfaceDiagnostics()))
        {
            CloseError = new InvalidOperationException(secret)
        };
        var page = CreateProxy<IPage, FakePage>(pageState);
        var registry = ProbePageRegistry.For(context);
        registry.ProbePageFactory = () => Task.FromResult(page);
        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => GpuProbe.RunAsync(context, TimeSpan.FromSeconds(2)));
            Assert.Equal("GPU probe page cleanup failed.", error.Message);
            Assert.DoesNotContain(secret, error.ToString(), StringComparison.Ordinal);
            Assert.Equal(1, pageState.CloseCalls);
            Assert.DoesNotContain(page, registry.Filter([page]));
            Assert.True(pageState.CloseFaultObserved.Task.IsCompleted);
        }
        finally
        {
            registry.ProbePageFactory = null;
        }
    }

    private static T CreateProxy<T, TProxy>(object state) where T : class where TProxy : StateProxy
    {
        var proxy = DispatchProxy.Create<T, TProxy>();
        ((TProxy)(object)proxy).State = state;
        return proxy;
    }

    public abstract class StateProxy : DispatchProxy
    {
        internal object State { get; set; } = null!;
    }

    public class FakeContext : StateProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Pages") return ((FakeContextState)State).Pages;
            if (method?.ReturnType == typeof(bool)) return false;
            return Default(method?.ReturnType);
        }
    }

    public class FakePage : StateProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var state = (FakePageState)State;
            if (method?.Name == "get_Context") return state.Context;
            if (method?.Name == "get_IsClosed") return state.Closed;
            if (method?.Name == "CloseAsync")
            {
                state.CloseCalls++;
                if (state.CloseError is not null)
                {
                    state.CloseFaultObserved.TrySetResult();
                    return Task.FromException(state.CloseError);
                }
                state.Closed = true;
                return Task.CompletedTask;
            }
            if (method?.Name == "EvaluateAsync")
            {
                state.EvaluationCalls++;
                state.OnEvaluation?.Invoke();
                return state.Evaluation;
            }
            return Default(method?.ReturnType);
        }
    }

    private static object? Default(Type? type)
    {
        if (type == typeof(Task)) return Task.CompletedTask;
        if (type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(Task<>))
            return typeof(ProbeCleanupTests).GetMethod(nameof(Completed), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, null);
        return type is null || type == typeof(void) ? null : type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static Task<T> Completed<T>() => Task.FromResult(default(T)!);

    public sealed class FakeContextState
    {
        public IReadOnlyList<IPage> Pages => [];
    }

    public sealed class FakePageState(IBrowserContext context, Task<BrowserSurfaceDiagnostics> evaluation)
    {
        public IBrowserContext Context { get; } = context;
        public Task<BrowserSurfaceDiagnostics> Evaluation { get; } = evaluation;
        public TaskCompletionSource CloseFaultObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception? CloseError { get; init; }
        public Action? OnEvaluation { get; set; }
        public int EvaluationCalls;
        public int CloseCalls;
        public bool Closed;
    }
}
