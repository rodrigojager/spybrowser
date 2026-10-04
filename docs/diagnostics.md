# Diagnostics and local snapshots

SpyBrowser diagnostics are advisory unless a finding is an explicit consistency error and the caller keeps `FailOnConsistencyErrors` enabled. Missing surface data is not proof of a contradiction. No GeoIP lookup, third-party evaluation service, telemetry, or additional stealth scripts are used.

## Effective expectations and findings

The validator's original `Validate(identity, policy, diagnostics)` overload remains available and derives expected values from the identity. Launch validates against context values after the caller's final options callback (locale, timezone, viewport, screen, device scale factor and user agent), so overrides are not judged against stale manifest defaults. Context handles expose `EffectiveExpectations`, `EffectiveGpuPolicy`, and immutable `RuntimeProvenance`; reports also carry runtime provenance separately from UA claims. Browser-only handles expose the per-created-context effective expectations and report through `GetConsistencyDiagnostics(context)` (null for raw/unowned contexts), including contexts surfaced by page factories. Runtime browser family/channel is derived from the selected Playwright launch configuration and browser version from the running browser process (`playwright.browser.version`); Playwright version comes from its loaded assembly (`playwright.assembly.version`). With probes disabled, launch still performs only cheap configuration checks and reports configured UA/family/platform contradictions as warnings; those checks do not open a page. Surface probing is opt-in (`RunGpuProbe`, default `false`) and is enabled explicitly by the CLI `probe` command. Browser family is compared at family level, not by literal browser version. Client Hints are only compared when explicitly available; absent/reduced hints are indeterminate. A WebGPU adapter being unavailable is not an inconsistency.

Findings use stable codes and `Information`, `Warning` or `Error` severity. Known UTC names (`UTC`, `Etc/UTC`, `Etc/GMT`, `GMT`) are aliases. Windows/IANA timezone comparison accepts an explicit platform conversion only when the converted IANA identifier matches; different IANA region identifiers are not accepted merely because they share offsets or map to a broad Windows region. Locale comparison uses BCP-47/CultureInfo normalization and compares the primary `navigator.languages` entry; later language preferences may legitimately differ. Screen dimensions are compared in CSS screen units. Viewport checks run only when both expected and observed dimensions are available; `NoViewport`/zero or unavailable measurements remain indeterminate. DPR is warning-only with the existing absolute 0.01 tolerance for numeric reporting variation. A configured 1.25 versus observed 1.5 remains a warning; unknown zoom is not used to erase that difference. No pixel/device-pixel conversion is inferred without observed evidence.

GPU software/hardware policy remains governed by `GpuPolicy`. Missing WebGL is an unverified-surface warning even under `RequireHardware`; only an actually detected software renderer violates that policy. Unavailable WebGPU alone does not. `ExperimentalMask` reports that string masking does not cover pixels, timing, WebGPU or native introspection. A canvas sample hash is an observation only, not certification of equivalent GPU behavior. Snapshot comparison also reports informational changes to privacy-coarsened renderer categories (vendor, broad model family/generation, and graphics backend). Exact renderer/driver strings and hardware hashes are never persisted; renderer updates within the same retained coarse category are intentionally not distinguishable.

The CLI `probe` command runs the existing local Playwright probe. Snapshot persistence is disabled unless `--snapshot-dir` is provided:

```text
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --snapshot-retention 20
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --baseline ./accepted/snapshot.json
```

`--baseline` always names an explicit, read-only file; the command saves the current run as a separate unique snapshot and never promotes it to baseline. `--baseline` requires `--snapshot-dir`. Snapshots have their own schema and contain runtime versions, effective expectations, normalized browser surface fields and findings. They do not include identity IDs, profile contents, pages, cookies, storage state, typed text, proxy settings, or GPU probe error text/canvas hashes. Before serialization, arbitrary strings are projected by field: version strings must be bounded version tokens, browser/platform/algorithm values become known categories, locales/timezones are validated, GPU text is reduced to vendor/software categories, and unknown finding codes become `diagnostic.other`. Browser/driver updates are informational changes unless a current consistency rule independently reports a contradiction.

Writes use a same-directory temporary file followed by an atomic rename. On Unix the file mode is owner read/write (`0600`). On Windows, the file inherits the destination directory's ACL; place the directory in a user-private location. Retention applies only to generated `snapshot-*.json` files in the chosen directory and keeps the newest configured count (default 20). The explicitly selected `--baseline` is passed to retention and protected even when it is inside that directory. A per-directory exclusive lock coordinates publish/prune across concurrent processes; temporary names are unique. Corrupt or unsupported-schema files fail with an error and are not repaired or overwritten. Snapshots are optional local artifacts; deleting them does not affect profiles or identities. Concurrent runs receive unique filenames rather than sharing an IdentityId-based path.

API consumers can use `DiagnosticSnapshotStore.SaveAsync`, `ReadAsync`, and `Compare` directly. `RuntimeVersionRecord` accepts caller-supplied version strings. Launch handles expose immutable `RuntimeProvenance`: family and effective channel are taken from the selected launch configuration (with explicit provenance labels), while browser version comes from Playwright's running browser process, never the spoofable User-Agent. CLI snapshots use those same handle facts and the actual selected humanization strategy; Bézier reports dataset `none`, Cursory records its bundled dataset revision without loading trajectory data for diagnostics, and non-humanized launches report algorithm/dataset `none`. Provenance/channel values are projected through the snapshot's safe allowlist. A caller must opt into persistence by constructing the store and invoking `SaveAsync`.

## Validation evidence

On the assigned Windows worktree, with output isolated from source `bin/obj/artifacts`, the focused suite passed (16 passed, 2 browser-gated skipped):

```text
cd /d/SpyBrowser-work/monitor-evidence-final
export PATH=/home/rodrigo/.dotnet-spybrowser:$PATH
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj --artifacts-path /tmp/spybrowser-monitor-evidence-bin --filter 'FullyQualifiedName~HumanizationDiagnosticsTests|FullyQualifiedName~DiagnosticSnapshotStoreTests'
```

With `SPYBROWSER_RUN_BROWSER_TESTS=1`, the targeted runtime-provenance and humanization-recorder browser tests passed (2 passed). A real CLI two-run check created identity `diag-e2e` with `--engine chromium`, saved its first `probe` snapshot, and ran the same probe with the first file explicitly selected as `--baseline`; the second run saved a distinct snapshot and reported `comparison: { changes: [] }` for the unchanged runtime. The persisted snapshot reported family `chromium` from `playwright.launch-configuration`, browser version `149.0.7827.55` from `playwright.browser.version`, and algorithm/dataset `none`. Exact invocation sequence (the second probe was repeated after rebuilding the compare privacy fix):

```text
cd /d/SpyBrowser-work/monitor-evidence-final
export PATH=/home/rodrigo/.dotnet-spybrowser:$PATH
dotnet build src/SpyBrowser.Cli/SpyBrowser.Cli.csproj --artifacts-path /tmp/spybrowser-monitor-evidence-bin
dotnet C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-bin\bin\SpyBrowser.Cli\debug\SpyBrowser.Cli.dll identity create diag-e2e --engine chromium --root C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\identities
dotnet C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-bin\bin\SpyBrowser.Cli\debug\SpyBrowser.Cli.dll probe diag-e2e --headless --allow-inconsistent --root C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\identities --snapshot-dir C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\snapshots --snapshot-retention 10
dotnet C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-bin\bin\SpyBrowser.Cli\debug\SpyBrowser.Cli.dll probe diag-e2e --headless --allow-inconsistent --root C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\identities --snapshot-dir C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\snapshots --snapshot-retention 10 --baseline C:\Users\Rodrigo\AppData\Local\Temp\spybrowser-monitor-evidence-cli-e2e\snapshots\snapshot-20261004T064314.1201547Z-b8461e8a10f142af8d20f082aa4b07c6.json
```

The final comparison run (after rebuilding the effective-channel provenance changes) wrote `snapshot-20261004T065050.1173868Z-e9b81efa99c04c2c9fb3532df1f0544b.json`.

A whole-solution run was also attempted with isolated artifacts; the 15 Cursory tests passed, but the Playwright project had 6 failures: five `ContextEventContractsTests` were run without their required `SPYBROWSER_RUN_BROWSER_TESTS=1`, and `PlaywrightApiAuditTests` resolves the solution root from the moved test assembly and cannot find `SpyBrowser.sln`. The test project otherwise reported 68 passed and 41 gated skips. A second browser-enabled Playwright run excluding only `PlaywrightApiAuditTests` reported 112 passed and 5 browser tests failed with `TargetClosedException`/weak-reference assertions under the shared concurrent browser lane. The focused recorder/provenance browser tests pass separately. These whole-project failures remain reported; neither run is represented as a passing full suite.

## Consistency tickets 17/18: validation evidence

Focused consistency/snapshot/GPU/probe-contract tests: 29 passed on Windows .NET SDK 8.0.319 and 29 passed with the Ubuntu WSL SDK at `/home/rodrigo/.dotnet-spybrowser` (Playwright 1.61). Real browser callback test `Effective_callbacks_are_reported_for_persistent_context_and_browser_page_modes` passed on Windows and WSL/Linux (1/1 each). It exercises persistent-context, disposable-context and browser-only context/page callbacks, callback-over-explicit-options precedence, effective expectation retrieval, actual local timezone resolution, browser/runtime provenance, an intentionally conflicting configured UA producing a warning, and `FailOnConsistencyErrors=true` allowing that warning. GPU probing is disabled for the three callback paths; default-disabled probe behavior is also covered by unit test.

TRX and build outputs were isolated outside the worktree:

- Windows callback TRX: `%TEMP%`-equivalent `C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-consistency-17-18/results/consistency-17-18-callbacks-windows.trx`.
- WSL callback TRX: `/tmp/spybrowser-consistency-17-18-linux-results/consistency-17-18-callbacks-linux.trx`.
- Focused Windows/WSL unit TRX: `C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-consistency-17-18/results/consistency-17-18-focused-final-windows.trx` and `/tmp/spybrowser-consistency-17-18-linux-results/consistency-17-18-focused-final-linux.trx`.

A Windows CLI real-browser `probe` also reported runtime family `chromium`, browser version `149.0.7827.55` from `playwright.browser.version`, and Playwright `1.61.0.0`; Client Hints and WebGPU were unavailable without being treated as contradictions. Headless Chromium reported a screen-size warning (`1920x1080` expectation vs `1440x1000` observation), not an error. This is observed headless/software-rendered evidence, not a physical GPU result. The output was kept at `C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-consistency-17-18/cli-probe.json`.

The unrestricted test-project command was also attempted: it had six known harness failures because browser-gated `ContextEventContractsTests` ran without `SPYBROWSER_RUN_BROWSER_TESTS=1`, while `PlaywrightApiAuditTests` could not resolve `SpyBrowser.sln` from relocated artifacts. The final ungated unit run excluding those two environment-dependent classes passed **75**, skipped **41** browser tests and failed **0**. Do not interpret those skipped cases as browser validation; the named callback browser case above was run separately on both platforms. No GPU probe was run in the RpaBlockly path and no proxy/GeoIP inference was used.

## Validation boundaries

## Probe isolation and validation

`SpyBrowserContextHandle.Pages`, humanized `Context.Pages`, and humanized `Context.Page`/`Page.Popup` events omit the internally owned diagnostic page. The wrapper's `RawContext` is an intentional Playwright escape hatch: callers enumerating `RawContext.Pages` or subscribing directly to raw events bypass that public-work-page filter and may observe the probe. The isolated probe page is created and closed by the diagnostic operation; caller-owned pages are not navigated or closed. Probe pages use a weak identity marker rather than permanent per-page event subscriptions.

A GPU probe is opt-in at launch (`RunGpuProbe`, whose launch default is disabled) and can also be requested explicitly through the handle/API or CLI. The RpaBlockly integration explicitly disables launch probes. `--snapshot-dir` opts into persistence only; it does not cause a launch probe by itself. Probe failures and timeouts are operational errors, distinct from consistency findings; unavailable WebGL/WebGPU data is reported as unavailable and does not by itself establish a fingerprint contradiction. `WebGpu.Error` gives the browser's local reason when exposed (for example, secure-context or policy restrictions); no remote fixture or service is contacted.

The probe creates a fresh page in the configured context, never borrows/navigates an existing page, and uses a bounded timeout with `finally` cleanup. Page events that arrive during ambiguous native creation are deferred by identity until the new page resolves, then user popup/page events are published while the probe page is filtered. Headed Chromium behavior is deliberately not inferred from headless tests: page focus is sampled on an actually focused work page, and no blanket `BringToFront` is issued on borrowed pages. Confirm local headed behavior with this opt-in test (run from the source worktree with its pinned .NET SDK and installed Playwright Chromium):

```text
SPYBROWSER_RUN_BROWSER_TESTS=1 SPYBROWSER_RUN_HEADED_PROBE_TESTS=1 dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj --filter FullyQualifiedName~Headed_probe_does_not_change_the_existing_pages_focus_or_active_element
```

This check asserts `document.hasFocus()` and the active element before/after a real headed probe. On the local Windows Chrome run for this change, the command above completed **1 passed, 0 failed** (2 seconds); this is a local headed result, not a headless/Xvfb equivalence claim. The browser-enabled isolation test also exercises persistent-context, context, and browser-only modes, concurrent popup publication, creation failure, cancellation/deadline during pending creation, and closure of late-created pages. Tests gated by these environment variables must only be reported as executed when run with the corresponding locally installed Playwright browser.
