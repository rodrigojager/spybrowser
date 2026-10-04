namespace SpyBrowser.Playwright;

public sealed record BrowserSurfaceDiagnostics
{
    public WebGlSurfaceDiagnostics WebGl1 { get; init; } = new();

    public WebGlSurfaceDiagnostics WebGl2 { get; init; } = new();

    public WebGpuSurfaceDiagnostics WebGpu { get; init; } = new();

    public string UserAgent { get; init; } = string.Empty;

    public string Platform { get; init; } = string.Empty;

    /// <summary>Optional client-hint values. Empty/unavailable values are not treated as conflicts.</summary>
    public bool ClientHintsAvailable { get; init; }

    public string? ClientHintPlatform { get; init; }

    public string? ClientHintArchitecture { get; init; }

    public string? ClientHintModel { get; init; }

    public string[] ClientHintBrands { get; init; } = Array.Empty<string>();

    public int HardwareConcurrency { get; init; }

    public double? DeviceMemoryGb { get; init; }

    public string[] Languages { get; init; } = Array.Empty<string>();

    public string TimezoneId { get; init; } = string.Empty;

    public ScreenSurfaceDiagnostics Screen { get; init; } = new();

    public bool UsesSoftwareRendering =>
        WebGl1.IsSoftwareRenderer || WebGl2.IsSoftwareRenderer || WebGpu.IsSoftwareAdapter;
}

public sealed record WebGlSurfaceDiagnostics
{
    private static readonly string[] SoftwareMarkers =
    [
        "swiftshader",
        "llvmpipe",
        "software rasterizer",
        "softpipe"
    ];

    public bool Available { get; init; }

    public string? Vendor { get; init; }

    public string? Renderer { get; init; }

    public string? Version { get; init; }

    public string? ShadingLanguageVersion { get; init; }

    public string? CanvasSampleHash { get; init; }

    public double RenderDurationMilliseconds { get; init; }

    public bool IsSoftwareRenderer =>
        SoftwareMarkers.Any(marker =>
            (Renderer ?? string.Empty).Contains(marker, StringComparison.OrdinalIgnoreCase));
}

public sealed record WebGpuSurfaceDiagnostics
{
    public bool Available { get; init; }

    public string? Vendor { get; init; }

    public string? Architecture { get; init; }

    public string? Device { get; init; }

    public string? Description { get; init; }

    public bool IsFallbackAdapter { get; init; }

    public string? Error { get; init; }

    public bool IsSoftwareAdapter => IsFallbackAdapter ||
        new[] { Vendor, Architecture, Device, Description }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Any(value => value!.Contains("swiftshader", StringComparison.OrdinalIgnoreCase) ||
                          value.Contains("software", StringComparison.OrdinalIgnoreCase) ||
                          value.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase));
}

public sealed record ScreenSurfaceDiagnostics
{
    public int Width { get; init; }

    public int Height { get; init; }

    public int AvailableWidth { get; init; }

    public int AvailableHeight { get; init; }

    public int ViewportWidth { get; init; }

    public int ViewportHeight { get; init; }

    public double DevicePixelRatio { get; init; }
}
