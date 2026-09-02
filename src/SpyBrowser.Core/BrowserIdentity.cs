namespace SpyBrowser.Core;

/// <summary>A stable browser identity backed by one persistent profile directory.</summary>
public sealed record BrowserIdentity
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string Locale { get; init; } = "pt-BR";

    public string TimezoneId { get; init; } = "America/Sao_Paulo";

    public ViewportIdentity Viewport { get; init; } = new();

    public BrowserIdentitySettings Browser { get; init; } = new();

    public NetworkIdentitySettings Network { get; init; } = new();

    public GpuIdentitySettings Gpu { get; init; } = new();

    public NavigatorIdentitySettings Navigator { get; init; } = new();

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public static BrowserIdentity Create(string id, string? displayName = null)
    {
        var normalized = IdentityId.Normalize(id);
        return new BrowserIdentity
        {
            Id = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim()
        };
    }
}

public sealed record ViewportIdentity
{
    public int Width { get; init; } = 1440;

    public int Height { get; init; } = 1000;

    public int ScreenWidth { get; init; } = 1920;

    public int ScreenHeight { get; init; } = 1080;

    public float DeviceScaleFactor { get; init; } = 1;
}

public sealed record BrowserIdentitySettings
{
    public BrowserEngine Engine { get; init; } = BrowserEngine.Chrome;

    public string? Channel { get; init; } = "chrome";

    public string? ExecutablePath { get; init; }

    public string? UserAgent { get; init; }
}

/// <summary>
/// Network configuration. Credentials are referenced through environment
/// variable names and are never serialized into the identity manifest.
/// </summary>
public sealed record NetworkIdentitySettings
{
    public string? ProxyServer { get; init; }

    public string? ProxyBypass { get; init; }

    public string? ProxyUsernameEnvironment { get; init; }

    public string? ProxyPasswordEnvironment { get; init; }

    public bool DisableNonProxiedWebRtc { get; init; } = true;
}

public sealed record GpuIdentitySettings
{
    public GpuPolicy Policy { get; init; } = GpuPolicy.Auto;

    /// <summary>Reported vendor used only by the explicitly experimental mask.</summary>
    public string? MaskVendor { get; init; }

    /// <summary>Reported renderer used only by the explicitly experimental mask.</summary>
    public string? MaskRenderer { get; init; }
}

/// <summary>
/// Optional navigator overrides. They are disabled by default because script
/// overrides can be detected and can create inconsistent identities.
/// </summary>
public sealed record NavigatorIdentitySettings
{
    public bool EnableExperimentalOverrides { get; init; }

    public string? Platform { get; init; }

    public int? HardwareConcurrency { get; init; }

    public double? DeviceMemoryGb { get; init; }
}
