using SpyBrowser.Core;

namespace SpyBrowser.Tests;

public sealed class IdentityValidatorTests
{
    [Fact]
    public void Default_identity_is_valid()
    {
        var identity = BrowserIdentity.Create("cliente-a");

        Assert.Empty(IdentityValidator.Validate(identity));
    }

    [Fact]
    public void Experimental_gpu_mask_requires_both_values()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Gpu = new GpuIdentitySettings
            {
                Policy = GpuPolicy.ExperimentalMask,
                MaskVendor = "Google Inc. (NVIDIA)"
            }
        };

        var errors = IdentityValidator.Validate(identity);

        Assert.Contains(errors, error => error.Contains("MaskVendor and MaskRenderer", StringComparison.Ordinal));
    }

    [Fact]
    public void Proxy_credentials_cannot_be_embedded_in_uri()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Network = new NetworkIdentitySettings
            {
                ProxyServer = "http://user:password@proxy.example:8080"
            }
        };

        var errors = IdentityValidator.Validate(identity);

        Assert.Contains(errors, error => error.Contains("environment references", StringComparison.Ordinal));
    }

    [Fact]
    public void Navigator_values_require_explicit_experimental_opt_in()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Navigator = new NavigatorIdentitySettings { HardwareConcurrency = 8 }
        };

        var errors = IdentityValidator.Validate(identity);

        Assert.Contains(errors, error => error.Contains("EnableExperimentalOverrides", StringComparison.Ordinal));
    }
}
