using System.Text.Json;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

internal static class IdentityInitScriptBuilder
{
    public static string? Build(BrowserIdentity identity, GpuPolicy effectiveGpuPolicy)
    {
        var scripts = new List<string>();
        if (effectiveGpuPolicy == GpuPolicy.ExperimentalMask)
        {
            scripts.Add(BuildWebGlMask(identity.Gpu.MaskVendor!, identity.Gpu.MaskRenderer!));
        }

        if (identity.Navigator.EnableExperimentalOverrides)
        {
            scripts.Add(BuildNavigatorOverrides(identity.Navigator));
        }

        return scripts.Count == 0 ? null : string.Join(Environment.NewLine, scripts);
    }

    private static string BuildWebGlMask(string vendor, string renderer)
    {
        var serializedVendor = JsonSerializer.Serialize(vendor);
        var serializedRenderer = JsonSerializer.Serialize(renderer);
        return $$"""
            (() => {
              const vendor = {{serializedVendor}};
              const renderer = {{serializedRenderer}};
              const patch = (constructor) => {
                if (!constructor?.prototype) return;
                const descriptor = Object.getOwnPropertyDescriptor(constructor.prototype, 'getParameter');
                const original = descriptor?.value;
                if (typeof original !== 'function' || original.__spyBrowserPatched) return;
                const replacement = function(parameter) {
                  if (parameter === 0x9245) return vendor;
                  if (parameter === 0x9246) return renderer;
                  return Reflect.apply(original, this, arguments);
                };
                Object.defineProperty(replacement, '__spyBrowserPatched', { value: true });
                Object.defineProperty(constructor.prototype, 'getParameter', {
                  ...descriptor,
                  value: replacement
                });
              };
              patch(globalThis.WebGLRenderingContext);
              patch(globalThis.WebGL2RenderingContext);
            })();
            """;
    }

    private static string BuildNavigatorOverrides(NavigatorIdentitySettings navigator)
    {
        var overrides = new Dictionary<string, object?>();
        if (navigator.Platform is not null)
        {
            overrides["platform"] = navigator.Platform;
        }

        if (navigator.HardwareConcurrency is not null)
        {
            overrides["hardwareConcurrency"] = navigator.HardwareConcurrency;
        }

        if (navigator.DeviceMemoryGb is not null)
        {
            overrides["deviceMemory"] = navigator.DeviceMemoryGb;
        }

        var serialized = JsonSerializer.Serialize(overrides);
        return $$"""
            (() => {
              const overrides = {{serialized}};
              for (const [name, value] of Object.entries(overrides)) {
                try {
                  Object.defineProperty(Navigator.prototype, name, {
                    configurable: true,
                    enumerable: true,
                    get: () => value
                  });
                } catch { }
              }
            })();
            """;
    }
}
