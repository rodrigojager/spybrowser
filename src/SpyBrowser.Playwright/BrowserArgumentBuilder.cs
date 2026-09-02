using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

internal static class BrowserArgumentBuilder
{
    public static IReadOnlyList<string> Build(
        BrowserIdentity identity,
        GpuPolicy gpuPolicy,
        IReadOnlyList<string> extraArguments)
    {
        var arguments = new List<string>();
        if (identity.Network.DisableNonProxiedWebRtc &&
            !string.IsNullOrWhiteSpace(identity.Network.ProxyServer))
        {
            arguments.Add("--force-webrtc-ip-handling-policy=disable_non_proxied_udp");
        }

        switch (gpuPolicy)
        {
            case GpuPolicy.RequireHardware:
                arguments.Add("--ignore-gpu-blocklist");
                arguments.Add("--enable-gpu-rasterization");
                break;
            case GpuPolicy.ForceSoftware:
                arguments.Add("--use-gl=angle");
                arguments.Add("--use-angle=swiftshader");
                arguments.Add("--enable-unsafe-swiftshader");
                break;
            case GpuPolicy.Auto:
            case GpuPolicy.AllowSoftware:
            case GpuPolicy.ExperimentalMask:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(gpuPolicy), gpuPolicy, "Unknown GPU policy.");
        }

        arguments.AddRange(extraArguments.Where(argument => !string.IsNullOrWhiteSpace(argument)));
        return arguments.Distinct(StringComparer.Ordinal).ToArray();
    }
}
