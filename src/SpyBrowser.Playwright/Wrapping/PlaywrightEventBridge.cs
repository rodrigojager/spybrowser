using System.Linq.Expressions;
using System.Reflection;

namespace SpyBrowser.Playwright;

internal static class PlaywrightEventBridge
{
    private static readonly HashSet<string> SupportedEvents = new(StringComparer.Ordinal)
    {
        "Context", "Page", "Popup", "Close", "FrameAttached", "FrameDetached", "FrameNavigated"
    };

    public static bool IsSupported(Type targetType, string eventName) =>
        SupportedEvents.Contains(eventName) &&
        targetType.GetEvents().Any(eventInfo => eventInfo.Name == eventName);

    private static readonly MethodInfo ForwardMethod = typeof(PlaywrightEventBridge)
        .GetMethod(nameof(Forward), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static Delegate Adapt(Delegate handler, HumanizationScope? scope, Action? afterInvoke = null)
    {
        var invoke = handler.GetType().GetMethod("Invoke")!;
        var parameters = invoke.GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var args = Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))));
        var call = Expression.Call(ForwardMethod, Expression.Constant(handler), Expression.Constant(scope), Expression.Constant(afterInvoke, typeof(Action)), args);
        return Expression.Lambda(handler.GetType(), call, parameters).Compile();
    }

    private static void Forward(Delegate handler, HumanizationScope? scope, Action? afterInvoke, object?[] args)
    {
        if (args.OfType<Microsoft.Playwright.IPage>().Any(page =>
                ProbePageRegistry.For(page.Context).IsProbe(page) ||
                ProbePageRegistry.For(page.Context).IsCreatingProbe)) return;
        if (scope is not null)
            for (var i = 0; i < args.Length; i++) args[i] = scope.WrapEventValue(args[i]);
        try
        {
            try { handler.DynamicInvoke(args); }
            catch (System.Reflection.TargetInvocationException e) when (e.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }
        finally { afterInvoke?.Invoke(); }
    }
}
