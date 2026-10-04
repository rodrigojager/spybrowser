# Humanization diagnostics (experimental)

Diagnostics are opt-in and local to one `PlaywrightHumanizer`. Enable them with `HumanInteractionOptions.EnableDiagnostics`; the bounded in-memory ring defaults to 256 completed calls (configurable from 1 to 10,000). Read a defensive snapshot with `PlaywrightHumanizer.GetDiagnosticsSnapshot()`. When disabled, it returns `null` and invocation records/correlation IDs are not allocated.

```csharp
var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
{
    MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
    CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
    EnableDiagnostics = true,
    DiagnosticsCapacity = 256
});
var wrappedPage = humanizer.Wrap(page);
// ...run actions...
var report = humanizer.GetDiagnosticsSnapshot();
```

A snapshot contains schema and runtime version provenance, a generated session ID, bounded records, and the number of overwritten records. Its algorithm/dataset fields describe only the configured trajectory strategy: Bezier reports `bezier:legacy-v1` and `none`; Cursory reports its bundled dataset revision without loading extra data for diagnostics. Browser version is read from Playwright's running `Browser.Version` where available (not User-Agent); the recorder labels that source explicitly. A record contains only an allowlisted Playwright method name, stable mode/reason/outcome codes, generated page/action correlation IDs, the configured trajectory algorithm, elapsed wrapper-call time, and completion time. Elapsed time measures the wrapper invocation (including any input queue wait), **not** DOM-observed event timing or physical cursor behavior. When Cursory scheduling is directly observed, planned duration/point count, dispatched/coalesced points, and final-endpoint completion are included; these are not collected for operations whose internals do not expose those values. Unknown-pointer/active-button Cursory bootstrap moves are reported as native anchors, not generated trajectories. No activation count is inferred. Browser version is best-effort and is `unavailable` if Playwright cannot provide it. Dataset provenance identifies the bundled Cursory upstream revision; the Bezier algorithm has no dataset.

Stable fallback examples include `humanization.unsupported-options`, `humanization.raw-insert-text`, `humanization.unsupported-surface`, and `humanization.native-compatible-fill`. Native compatible Fill/Clear/Press/Wheel are expected semantic-preservation decisions, not errors. Legacy wheel smoothing is distinguishable from native compatible wheel dispatch. Results are `success`, `canceled`, `timeout`, or `error`; no exception message is retained.

The report never stores action arguments, selectors, typed text, URLs, browser storage, cookies, headers, proxy values, coordinates, seeds, or exception messages. IDs are newly generated per wrapper/session/page/action and are not identity or user identifiers. There is no external observer, telemetry backend, or persistence. Retain/export snapshots only according to your own privacy policy. Unsupported APIs and raw escape hatches remain outside action-level instrumentation; a recorded raw invocation does not claim Cursory was active. Diagnostics observers cannot affect automation because no user callback is invoked.
