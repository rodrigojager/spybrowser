using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class GpuPolicyTests
{
    [Theory]
    [InlineData("Google SwiftShader")]
    [InlineData("llvmpipe (LLVM 18.1.0, 256 bits)")]
    [InlineData("Software Rasterizer")]
    public void Software_renderers_are_detected(string renderer)
    {
        var diagnostics = new WebGlSurfaceDiagnostics
        {
            Available = true,
            Renderer = renderer
        };

        Assert.True(diagnostics.IsSoftwareRenderer);
    }

    [Fact]
    public void Hardware_policy_rejects_swiftshader()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Gpu = new GpuIdentitySettings { Policy = GpuPolicy.RequireHardware }
        };
        var diagnostics = new BrowserSurfaceDiagnostics
        {
            TimezoneId = identity.TimezoneId,
            Screen = new ScreenSurfaceDiagnostics
            {
                Width = identity.Viewport.ScreenWidth,
                Height = identity.Viewport.ScreenHeight
            },
            WebGl1 = new WebGlSurfaceDiagnostics
            {
                Available = true,
                Renderer = "ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device))"
            }
        };

        var report = IdentityConsistencyValidator.Validate(identity, GpuPolicy.RequireHardware, diagnostics);

        Assert.True(report.HasErrors);
        Assert.Contains(report.Findings, finding => finding.Code == "gpu.hardware-required");
    }

    [Fact]
    public void Experimental_mask_is_always_reported_as_detectable()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Gpu = new GpuIdentitySettings
            {
                Policy = GpuPolicy.ExperimentalMask,
                MaskVendor = "Google Inc. (NVIDIA)",
                MaskRenderer = "ANGLE (NVIDIA GeForce RTX 3060)"
            }
        };
        var diagnostics = new BrowserSurfaceDiagnostics
        {
            TimezoneId = identity.TimezoneId,
            Screen = new ScreenSurfaceDiagnostics
            {
                Width = identity.Viewport.ScreenWidth,
                Height = identity.Viewport.ScreenHeight
            },
            WebGl1 = new WebGlSurfaceDiagnostics { Available = true, Renderer = "Google SwiftShader" }
        };

        var report = IdentityConsistencyValidator.Validate(identity, GpuPolicy.ExperimentalMask, diagnostics);

        Assert.Contains(report.Findings, finding => finding.Code == "gpu.experimental-mask");
    }

    [Fact]
    public void Force_software_adds_explicit_swiftshader_arguments()
    {
        var identity = BrowserIdentity.Create("cliente-a");

        var arguments = BrowserArgumentBuilder.Build(identity, GpuPolicy.ForceSoftware, Array.Empty<string>());

        Assert.Contains("--use-angle=swiftshader", arguments);
        Assert.Contains("--enable-unsafe-swiftshader", arguments);
    }

    [Fact]
    public void Experimental_script_patches_webgl1_and_webgl2_without_to_string_concealment()
    {
        var identity = BrowserIdentity.Create("cliente-a") with
        {
            Gpu = new GpuIdentitySettings
            {
                Policy = GpuPolicy.ExperimentalMask,
                MaskVendor = "Vendor",
                MaskRenderer = "Renderer"
            }
        };

        var script = IdentityInitScriptBuilder.Build(identity, GpuPolicy.ExperimentalMask);

        Assert.Contains("WebGLRenderingContext", script, StringComparison.Ordinal);
        Assert.Contains("WebGL2RenderingContext", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Function.prototype.toString", script, StringComparison.Ordinal);
    }
}
