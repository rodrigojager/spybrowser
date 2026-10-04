using System.Linq.Expressions;
using System.Reflection;

namespace SpyBrowser.Playwright;

internal static class PlaywrightEventBridge
{
    private static readonly MethodInfo ForwardMethod = typeof(PlaywrightEventBridge)
        .GetMethod(nameof(Forward), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static Delegate Adapt(Delegate handler, HumanizationScope scope)
    {
        var invoke = handler.GetType().GetMethod("Invoke")!;
        var parameters = invoke.GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        var args = Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))));
        var call = Expression.Call(ForwardMethod, Expression.Constant(handler), Expression.Constant(scope), args);
        return Expression.Lambda(handler.GetType(), call, parameters).Compile();
    }

    private static void Forward(Delegate handler, HumanizationScope scope, object?[] args)
    {
        for (var i = 0; i < args.Length; i++) args[i] = scope.WrapEventValue(args[i]);
        try { handler.DynamicInvoke(args); }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }
}
