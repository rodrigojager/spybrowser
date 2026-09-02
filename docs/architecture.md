# Architecture

## Public boundary

SpyBrowser does not reimplement Playwright. Automation code receives official
`Microsoft.Playwright` interfaces, so locators, frames, routing, tracing,
downloads, auto-wait, evaluation, and future blocks use the API developers
already know.

```text
application / RPA blocks
          |
          v
official Microsoft.Playwright interfaces
          |
          +---- optional transparent interaction decorator
          |
          v
SpyBrowser launch + identity runtime
          |
          +---- installed Chrome / Edge
          +---- Playwright Chromium
          +---- caller executable or IBrowserExecutableProvider
```

`RawBrowser`, `RawContext`, and `PlaywrightHumanizer.Unwrap(...)` are explicit
escape hatches. They are useful for a Playwright operation whose exact native
interaction behavior matters more than pacing.

## Launch and ownership

`LaunchPersistentContextAsync` owns Playwright, a persistent context, and an
exclusive profile lease. `LaunchContextAsync` owns Playwright, a browser, and a
disposable context. `LaunchBrowserAsync` owns Playwright and a browser while its
handle factories apply identity defaults to new contexts/pages.

Disposal is idempotent and closes child resources before releasing Playwright
and the profile lease.

## Identity boundary

An identity is stable rather than randomly regenerated at every launch. Its
manifest stores non-secret configuration while the adjacent Chrome profile
stores cookies, history, storage, cache, and other browser state.

Proxy credentials are indirect environment-variable references. The operating
system file handle is the real profile lock; the lock file itself remains as
diagnostic metadata. Distinct profile paths can be leased concurrently.

The lease combines an in-process atomic registry with an operating-system file
region lock for Windows/Linux cross-process coordination. Closing the lease
releases both layers; stale metadata alone never owns a profile.

`IdentityRotationPool` provides round-robin selection across stable identities.
It tries candidates in order and relies on the same atomic leases to skip busy
profiles, including profiles owned by another process. Rotation therefore
changes the selected person/profile without generating internally inconsistent
values for an existing person.

## Transparent Playwright decorator

`PlaywrightHumanizer` uses runtime interface proxies. It preserves the exact
official interface types and transitively wraps pages, contexts, frames,
locators, handles, mouse, keyboard, tasks, and common lists.

Supported standard interactions are redirected to `HumanActions`, which uses
curved mouse paths, realistic holds, per-character typing, occasional thinking
pauses, and eased scrolling. Everything else delegates to the original object.
Advanced non-default action options delegate raw to preserve Playwright's full
semantics.

The runtime proxy has reflection dispatch overhead. Browser/network latency
normally dominates it; applications with an unusual high-frequency in-process
hot path can disable `Humanize` or use raw objects.

## GPU policy

| Policy | Behavior |
|---|---|
| `Auto` | browser defaults; reports the observed renderer |
| `RequireHardware` | acceleration hints and failure on an observed software renderer |
| `AllowSoftware` | accepts hardware or software without pretending otherwise |
| `ForceSoftware` | explicitly requests ANGLE/SwiftShader |
| `ExperimentalMask` | changes WebGL vendor/renderer strings and emits a warning |

The probe records WebGL1/WebGL2 vendor, renderer, versions, a sample pixel hash,
render duration, WebGPU information when available, and navigator/screen values.
The experimental mask cannot make SwiftShader render like an NVIDIA/AMD GPU.

## Driver and browser seams

`DriverSearchPath` selects a packaged Playwright driver before
`Playwright.CreateAsync`. That selection is process-wide, so different driver
variants belong in separate worker processes.

`ExecutablePathOverride` and `IBrowserExecutableProvider` decouple the API from
browser delivery. They permit a future independently built/signed Chromium
engine without changing RPA blocks or identity manifests. The current repo does
not include such a source-patched binary or a Chromium patch queue.

## Version policy

The public package baseline is Microsoft.Playwright 1.61.0. CI has four real
compatibility lanes: Windows/Linux crossed with baseline/latest stable. A new
Playwright release can therefore be tested before raising the package baseline.
Applications that require strict reproducibility may pin the exact tested
version through normal NuGet dependency management.
