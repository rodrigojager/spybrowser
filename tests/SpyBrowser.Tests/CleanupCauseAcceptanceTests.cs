using System.Reflection;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class CleanupCauseAcceptanceTests
{
    [Fact]
    public async Task Cancellation_remains_primary_when_owned_button_cleanup_faults_and_action_is_not_repeated()
    {
        var page = CreatePage(out var mouse);
        mouse.UpFailure = new IOException("cleanup Up fault");
        using var cancellation = new CancellationTokenSource();
        var actions = CreateActions(10_000);

        Task click = InvokeClick(actions, page, cancellation.Token);
        await mouse.DownStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => click);
        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, mouse.DownCalls);
        Assert.Equal(1, mouse.UpCalls);
        Assert.Equal(0, mouse.ClickCalls);
    }

    [Fact]
    public async Task Successful_hold_does_not_hide_final_Up_failure()
    {
        var page = CreatePage(out var mouse);
        var expected = new IOException("final Up fault");
        mouse.UpFailure = expected;
        var actions = CreateActions(0);

        IOException actual = await Assert.ThrowsAsync<IOException>(() => InvokeClick(actions, page, CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, mouse.DownCalls);
        Assert.Equal(1, mouse.UpCalls);
        Assert.Equal(0, mouse.ClickCalls);
    }

    [Fact]
    public async Task Caller_owned_button_uses_confirmed_coordinate_native_action_without_sdk_down_or_up_cleanup()
    {
        var page = CreatePage(out var mouse);
        var actions = CreateActions(0);
        actions.ObserveRawMouseMove(page, 41, 23);
        actions.ObserveRawMouseButton(page, "left", down: true);

        await InvokeClick(actions, page, CancellationToken.None);

        Assert.Equal(0, mouse.DownCalls);
        Assert.Equal(0, mouse.UpCalls);
        Assert.Equal(1, mouse.ClickCalls);
        Assert.Equal((41f, 23f), mouse.ClickPosition);
    }

    [Fact]
    public async Task Cancellation_before_requested_native_action_preserves_caller_button_state()
    {
        var page = CreatePage(out var mouse);
        var actions = CreateActions(0);
        actions.ObserveRawMouseMove(page, 41, 23);
        actions.ObserveRawMouseButton(page, "left", down: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeClick(actions, page, cancellation.Token));

        Assert.Equal(0, mouse.DownCalls);
        Assert.Equal(0, mouse.UpCalls);
        Assert.Equal(0, mouse.ClickCalls);
    }

    [Fact]
    public async Task Failed_down_is_not_blindly_released()
    {
        var page = CreatePage(out var mouse);
        var expected = new IOException("Down outcome uncertain");
        mouse.DownFailure = expected;
        var actions = CreateActions(0);

        IOException actual = await Assert.ThrowsAsync<IOException>(() => InvokeClick(actions, page, CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, mouse.DownCalls);
        Assert.Equal(0, mouse.UpCalls);
        Assert.Equal(0, mouse.ClickCalls);
    }

    private static HumanActions CreateActions(int holdMilliseconds) => new(new HumanInteractionOptions
    {
        ClickHoldMinimumMilliseconds = holdMilliseconds,
        ClickHoldMaximumMilliseconds = holdMilliseconds,
        ActionDeadlineMilliseconds = 20_000
    });

    private static Task InvokeClick(HumanActions actions, IPage page, CancellationToken cancellationToken) =>
        (Task)typeof(HumanActions).GetMethod("ClickCurrentPositionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(actions, [page, cancellationToken])!;

    private static IPage CreatePage(out RecordingMouse mouse)
    {
        var page = DispatchProxy.Create<IPage, RecordingPage>();
        mouse = RecordingPage.Get(page).MouseState;
        return page;
    }

    public class RecordingPage : DispatchProxy
    {
        private readonly IMouse _mouse = DispatchProxy.Create<IMouse, RecordingMouse>();
        internal RecordingMouse MouseState => (RecordingMouse)(object)_mouse;
        internal static RecordingPage Get(IPage page) => (RecordingPage)(object)page;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_Mouse" => _mouse,
            "get_IsClosed" => false,
            "get_Context" => DispatchProxy.Create<IBrowserContext, EmptyProxy>(),
            _ => EmptyProxy.Default(method?.ReturnType)
        };
    }

    public class RecordingMouse : DispatchProxy
    {
        internal readonly TaskCompletionSource DownStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Exception? DownFailure;
        internal Exception? UpFailure;
        internal int DownCalls;
        internal int UpCalls;
        internal int ClickCalls;
        internal (float X, float Y)? ClickPosition;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method?.Name)
            {
                case nameof(IMouse.DownAsync):
                    DownCalls++;
                    DownStarted.TrySetResult();
                    if (DownFailure is not null) return Task.FromException(DownFailure);
                    return Task.CompletedTask;
                case nameof(IMouse.UpAsync):
                    UpCalls++;
                    return UpFailure is null ? Task.CompletedTask : Task.FromException(UpFailure);
                case nameof(IMouse.ClickAsync):
                    ClickCalls++;
                    if (args is { Length: >= 2 }) ClickPosition = (Convert.ToSingle(args[0]), Convert.ToSingle(args[1]));
                    return Task.CompletedTask;
                default:
                    return EmptyProxy.Default(method?.ReturnType);
            }
        }
    }

    public class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Default(method?.ReturnType);

        internal static object? Default(Type? type) => type is null || type == typeof(void) ? null :
            type == typeof(Task) ? Task.CompletedTask :
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
                ? typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0])
                    .Invoke(null, [type.GetGenericArguments()[0].IsValueType ? Activator.CreateInstance(type.GetGenericArguments()[0]) : null])
                : type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
