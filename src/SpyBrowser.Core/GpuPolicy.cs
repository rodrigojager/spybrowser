namespace SpyBrowser.Core;

/// <summary>
/// Controls how SpyBrowser treats the rendering environment. The default never
/// invents a hardware GPU and therefore avoids contradictory fingerprints.
/// </summary>
public enum GpuPolicy
{
    Auto,
    RequireHardware,
    AllowSoftware,
    ForceSoftware,
    ExperimentalMask
}
