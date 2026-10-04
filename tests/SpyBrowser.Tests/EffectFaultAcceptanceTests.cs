using System.Reflection;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class EffectFaultAcceptanceTests
{
    [Theory]
    [InlineData("click", false, false)]
    [InlineData("click", true, false)]
    [InlineData("click", false, true)]
    [InlineData("click", true, true)]
    [InlineData("dblclick", false, false)]
    [InlineData("dblclick", true, false)]
    [InlineData("dblclick", false, true)]
    [InlineData("dblclick", true, true)]
    public async Task Compatible_final_native_action_is_not_repeated_when_it_faults_after_effect(
        string action, bool unsafeBox, bool faultAfterEffect)
    {
        var page = DispatchProxy.Create<IPage, RawPage>();
        var locator = DispatchProxy.Create<ILocator, RawLocator>();
        var raw = RawLocator.Get(locator);
        raw.PageValue = page;
        raw.BoxIsUnsafe = unsafeBox;
        raw.FaultAfterEffect = faultAfterEffect;
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            ActionDeadlineMilliseconds = 10_000
        }).Wrap(locator);

        Exception? actual = null;
        if (action == "click") actual = await Record.ExceptionAsync(() => wrapped.ClickAsync());
        else actual = await Record.ExceptionAsync(() => wrapped.DblClickAsync());

        Assert.Equal(1, raw.TrialCalls);
        Assert.Equal(1, raw.ScrollCalls);
        Assert.True(actual is null || faultAfterEffect && ReferenceEquals(actual, raw.Marker), $"Unexpected wrapped exception: {actual}");
        Assert.Equal(1, raw.FinalCalls);
        Assert.Equal(0, RawPage.Get(page).MouseMoves);
        Assert.Equal(faultAfterEffect ? 1 : 0, raw.Effects);
        Assert.Equal(faultAfterEffect, actual is not null);
        if (faultAfterEffect)
        {
            Assert.Same(raw.Marker, actual);
            Assert.NotNull(actual!.StackTrace);
            Assert.Contains(nameof(RawLocator.MakeMarker), actual.StackTrace);
        }
        Assert.NotNull(raw.FinalOptions);
        Assert.False((bool)raw.FinalOptions!.GetType().GetProperty("Trial")!.GetValue(raw.FinalOptions)!);
        Assert.True(Convert.ToDouble(raw.FinalOptions.GetType().GetProperty("Timeout")!.GetValue(raw.FinalOptions)) > 0);
    }

    [Fact]
    public async Task Explicit_native_options_are_forwarded_by_identity_without_mutation()
    {
        var page = DispatchProxy.Create<IPage, RawPage>();
        var locator = DispatchProxy.Create<ILocator, RawLocator>();
        var raw = RawLocator.Get(locator);
        raw.PageValue = page;
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
        }).Wrap(locator);
        var options = new LocatorClickOptions { Force = true, Timeout = 4321 };

        await wrapped.ClickAsync(options);

        Assert.Same(options, raw.ExplicitOptions);
        Assert.True(options.Force);
        Assert.Equal(4321, options.Timeout);
        Assert.Equal(1, raw.FinalCalls);
        Assert.Equal(0, raw.TrialCalls);
    }

    public class RawLocator : DispatchProxy
    {
        internal IPage? PageValue;
        internal bool BoxIsUnsafe;
        internal bool FaultAfterEffect;
        internal int TrialCalls;
        internal int ScrollCalls;
        internal int FinalCalls;
        internal int Effects;
        internal object? FinalOptions;
        internal object? ExplicitOptions;
        internal readonly Exception Marker = MakeMarker();
        internal static RawLocator Get(ILocator locator) => (RawLocator)(object)locator;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method?.Name)
            {
                case "get_Page": return PageValue;
                case nameof(ILocator.ScrollIntoViewIfNeededAsync):
                    ScrollCalls++;
                    return Task.CompletedTask;
                case nameof(ILocator.BoundingBoxAsync):
                    var resultType = method.ReturnType.GetGenericArguments()[0];
                    object? box = null;
                    if (BoxIsUnsafe)
                    {
                        box = Activator.CreateInstance(resultType)!;
                        SetCoordinate(resultType, box, "X", 2);
                        SetCoordinate(resultType, box, "Y", 3);
                        SetCoordinate(resultType, box, "Width", 0);
                        SetCoordinate(resultType, box, "Height", 20);
                    }
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [box]);
                case nameof(ILocator.ClickAsync):
                case nameof(ILocator.DblClickAsync):
                    var options = args?.FirstOrDefault();
                    if (options?.GetType().GetProperty("Trial")?.GetValue(options) is true)
                    {
                        TrialCalls++;
                        return Task.CompletedTask;
                    }
                    if (options?.GetType().GetProperty("Force")?.GetValue(options) is true)
                    {
                        ExplicitOptions = options;
                        FinalCalls++;
                        return Task.CompletedTask;
                    }
                    FinalCalls++;
                    FinalOptions = options;
                    Effects++;
                    return FaultAfterEffect ? FaultAsync() : Task.CompletedTask;
                default:
                    return RawPage.Default(method?.ReturnType);
            }
        }

        private static void SetCoordinate(Type resultType, object box, string property, double value)
        {
            var info = resultType.GetProperty(property)!;
            info.SetValue(box, Convert.ChangeType(value, info.PropertyType));
        }

        private async Task FaultAsync()
        {
            await Task.Yield();
            ThrowMarker();
        }

        internal void ThrowMarker() => throw Marker;

        internal static Exception MakeMarker()
        {
            try { ThrowAtEffectOrigin(); }
            catch (Exception exception) { return exception; }
            throw new InvalidOperationException("Unreachable");
        }

        internal static void ThrowAtEffectOrigin() => throw new FinalActionMarkerException();

        private sealed class FinalActionMarkerException : Exception { }
    }

    public class RawPage : DispatchProxy
    {
        private readonly IMouse _mouse = DispatchProxy.Create<IMouse, RawMouse>();
        private readonly IBrowserContext _context = DispatchProxy.Create<IBrowserContext, RawContext>();
        internal int MouseMoves;
        internal static RawPage Get(IPage page) => (RawPage)(object)page;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Mouse")
            {
                ((RawMouse)(object)_mouse).Owner = this;
                return _mouse;
            }
            return method?.Name switch
            {
            "get_Context" => _context,
            "get_IsClosed" => false,
            _ => Default(method?.ReturnType)
            };
        }

        public class RawMouse : DispatchProxy
        {
            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                if (method?.Name == nameof(IMouse.MoveAsync))
                {
                    Interlocked.Increment(ref ((RawPage)(object)Owner!).MouseMoves);
                    return Task.CompletedTask;
                }
                return Default(method?.ReturnType);
            }
            internal object? Owner;
        }

        public class RawContext : DispatchProxy
        {
            protected override object? Invoke(MethodInfo? method, object?[]? args) => Default(method?.ReturnType);
        }

        internal static object? Default(Type? type) => type == typeof(void) || type is null ? null :
            type == typeof(Task) ? Task.CompletedTask :
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
                ? typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0])
                    .Invoke(null, [type.GetGenericArguments()[0].IsValueType ? Activator.CreateInstance(type.GetGenericArguments()[0]) : null])
                : type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
