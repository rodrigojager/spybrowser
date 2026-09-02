# Compatibility contract

## Playwright

SpyBrowser's primary compatibility boundary is source and type compatibility
with `Microsoft.Playwright`. Public handles expose the official interfaces and
all non-intercepted members delegate to the original object.

Transparent humanization intentionally changes the timing/dispatch of a subset
of standard interaction calls. Passing advanced non-default action options uses
the raw Playwright call. `RawBrowser`, `RawContext`, and
`PlaywrightHumanizer.Unwrap` make this distinction explicit.

Compatibility is continuously checked against Playwright 1.61.0 and the latest
stable NuGet release on Windows and Linux. That is not a guarantee that an
arbitrary future release will work before its CI lane passes.

## CloakBrowser migration package

`SpyBrowser.Compatibility.CloakBrowser` is designed for recompilation of
existing applications, especially the current RpaBlockly launch path. It uses:

- assembly name `CloakBrowser`;
- namespace `CloakBrowser`;
- `CloakLauncher` launch/context/persistent-context entry points;
- `LaunchOptions` and `LaunchContextOptions` common public fields;
- `CloakBrowserHandle`/`CloakContextHandle`, `RawBrowser`/`RawContext`, page and
  context factories, `CloseAsync`, and async disposal;
- `CloakBrowser.Human.HumanPreset`, common `HumanPage` actions, and transparent
  Playwright humanization.

The package is not drop-in binary compatible with already compiled assemblies;
rebuild the consuming application after changing its NuGet reference. It also
does not reproduce vendor download, update, license, GeoIP service, diagnostic,
or every advanced humanization helper type. Unsupported GeoIP use fails rather
than silently producing a misleading identity.

Never reference the vendor `CloakBrowser` package and the SpyBrowser migration
package together: both intentionally provide `CloakBrowser.dll` and the same
top-level namespace.

## Browser engines

API compatibility is independent from browser-engine equivalence. Installed
Chrome gives genuine Chrome networking, codecs, branding, and native hardware
output, but it does not contain a third party's source-level fingerprint
patches. A caller-supplied executable can be selected without changing
Playwright-facing code.
