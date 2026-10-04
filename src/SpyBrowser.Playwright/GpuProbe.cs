using Microsoft.Playwright;

namespace SpyBrowser.Playwright;

public static class GpuProbe
{
    private const string ProbeScript = """
        async () => {
          const hash = (value) => {
            let result = 2166136261;
            for (let i = 0; i < value.length; i++) {
              result ^= value.charCodeAt(i);
              result = Math.imul(result, 16777619);
            }
            return (result >>> 0).toString(16).padStart(8, '0');
          };

          const probeWebGl = (kind) => {
            const canvas = document.createElement('canvas');
            canvas.width = 256;
            canvas.height = 128;
            const started = performance.now();
            const gl = canvas.getContext(kind, {
              antialias: true,
              depth: true,
              stencil: true,
              failIfMajorPerformanceCaveat: false
            });
            if (!gl) {
              return { Available: false, RenderDurationMilliseconds: performance.now() - started };
            }

            const debug = gl.getExtension('WEBGL_debug_renderer_info');
            const vendor = debug
              ? gl.getParameter(debug.UNMASKED_VENDOR_WEBGL)
              : gl.getParameter(gl.VENDOR);
            const renderer = debug
              ? gl.getParameter(debug.UNMASKED_RENDERER_WEBGL)
              : gl.getParameter(gl.RENDERER);

            gl.clearColor(0.13, 0.37, 0.71, 1.0);
            gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
            const pixels = new Uint8Array(16 * 16 * 4);
            gl.readPixels(0, 0, 16, 16, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
            let sample = '';
            for (const value of pixels) sample += String.fromCharCode(value);

            return {
              Available: true,
              Vendor: String(vendor ?? ''),
              Renderer: String(renderer ?? ''),
              Version: String(gl.getParameter(gl.VERSION) ?? ''),
              ShadingLanguageVersion: String(gl.getParameter(gl.SHADING_LANGUAGE_VERSION) ?? ''),
              CanvasSampleHash: hash(sample),
              RenderDurationMilliseconds: performance.now() - started
            };
          };

          const probeWebGpu = async () => {
            if (!navigator.gpu) return { Available: false };
            try {
              const adapter = await navigator.gpu.requestAdapter();
              if (!adapter) return { Available: false };
              let info = adapter.info ?? null;
              if (!info && typeof adapter.requestAdapterInfo === 'function') {
                info = await adapter.requestAdapterInfo();
              }
              return {
                Available: true,
                Vendor: info?.vendor ?? null,
                Architecture: info?.architecture ?? null,
                Device: info?.device ?? null,
                Description: info?.description ?? null,
                IsFallbackAdapter: Boolean(adapter.isFallbackAdapter)
              };
            } catch (error) {
              return { Available: false, Error: String(error) };
            }
          };

          return {
            WebGl1: probeWebGl('webgl'),
            WebGl2: probeWebGl('webgl2'),
            WebGpu: await probeWebGpu(),
            UserAgent: navigator.userAgent,
            Platform: navigator.platform,
            HardwareConcurrency: navigator.hardwareConcurrency ?? 0,
            DeviceMemoryGb: navigator.deviceMemory ?? null,
            Languages: Array.from(navigator.languages ?? []),
            TimezoneId: Intl.DateTimeFormat().resolvedOptions().timeZone ?? '',
            Screen: {
              Width: screen.width,
              Height: screen.height,
              ViewportWidth: window.innerWidth,
              ViewportHeight: window.innerHeight,
              AvailableWidth: screen.availWidth,
              AvailableHeight: screen.availHeight,
              DevicePixelRatio: devicePixelRatio
            }
          };
        }
        """;

    public static async Task<BrowserSurfaceDiagnostics> RunAsync(
        IPage page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        cancellationToken.ThrowIfCancellationRequested();
        return await page.EvaluateAsync<BrowserSurfaceDiagnostics>(ProbeScript).ConfigureAwait(false);
    }

    /// <summary>Runs the probe in a fresh, private context page and always closes that page.</summary>
    public static async Task<BrowserSurfaceDiagnostics> RunAsync(
        IBrowserContext context,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        IPage? page = null;
        try
        {
            page = await ProbePageRegistry.For(context).CreateProbePageAsync(deadline.Token).ConfigureAwait(false);
            return await RunAsync(page, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"GPU probe exceeded its {timeout} timeout.");
        }
        finally
        {
            if (page is not null)
                await ProbePageRegistry.For(context).CloseProbePageAsync(page).ConfigureAwait(false);
        }
    }
}
