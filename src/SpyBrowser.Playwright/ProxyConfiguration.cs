using Microsoft.Playwright;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

internal static class ProxyConfiguration
{
    public static Proxy? Resolve(NetworkIdentitySettings network)
    {
        if (string.IsNullOrWhiteSpace(network.ProxyServer))
        {
            return null;
        }

        return new Proxy
        {
            Server = network.ProxyServer,
            Bypass = network.ProxyBypass,
            Username = ResolveRequiredEnvironment(network.ProxyUsernameEnvironment),
            Password = ResolveRequiredEnvironment(network.ProxyPasswordEnvironment)
        };
    }

    private static string? ResolveRequiredEnvironment(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return null;
        }

        var value = Environment.GetEnvironmentVariable(environmentName);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"Proxy credential environment variable '{environmentName}' is not set.");
        }

        return value;
    }
}
