# Compatibility contract

## Playwright

SpyBrowser's primary compatibility boundary is source and type compatibility
with `Microsoft.Playwright`. Public handles expose the official interfaces and
all non-intercepted members delegate to the original object.

Transparent humanization intentionally changes the timing/dispatch of a subset
of standard interaction calls. Passing advanced non-default action options uses
the raw Playwright call. `RawBrowser`, `RawContext`, and
`PlaywrightHumanizer.Unwrap` make this distinction explicit.

Contexts created through either `SpyBrowserBrowserHandle.NewContextAsync` or
its public `IBrowser.NewContextAsync` use the same identity-configured factory.
The browser-facing `NewPageAsync` routes through that factory as well. Defaults
are filled only where options are unset; a caller callback runs afterward and
can override them. Supplied options objects are shallow-copied before defaults
and callbacks, so caller fields are preserved and the caller's instance is not
mutated. `RawBrowser` is an intentional unconfigured bypass. `Humanize = false`
turns off decoration, not identity defaults.

When humanization is enabled, frames, frame locators, locator collections,
context/page backlinks and the supported browser/context/page events are bridged
through a per-launch weak wrapper cache. Event callbacks are synchronous, and
only Playwright interface values in their arguments are adapted. Arbitrary
Evaluate results and user payloads are not recursively rewritten. Objects
created outside SpyBrowser's factories may be wrapped when observed, but are
not retroactively identity-configured. Humanization covers only documented
interaction calls; a wrapper does not promise interception of every Playwright
method. Disposal owns and closes the same raw Playwright resources; wrappers do
not extend their lifetime intentionally.

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
