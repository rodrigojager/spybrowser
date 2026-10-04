# Humanization behavior

`HumanInteractionOptions.CompatibilityMode` is opt-in. Its default remains `Legacy`, preserving the existing humanizer dispatch. Mouse trajectories remain Bézier by default; `MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory` explicitly selects the experimental native adapter.

## PlaywrightCompatible

In this mode, parameterless locator/page/frame Fill and Clear, locator Press, InsertText, mouse Wheel, and other calls not explicitly supported are delegated to the original Playwright implementation. Calls supplying options are deliberately raw as well; no options object is inspected or changed. This retains Playwright's actionability, strictness, errors, event semantics, and native support for input types/contenteditable.

Parameterless locator Click, DblClick and Hover do a Playwright trial first, optionally prepare pointer movement toward the current target center, then invoke the native locator action exactly once. Preparatory movement is additional pointer activity, not a promise that every event matches an unwrapped call. If no usable bounding box is available, the native action remains the final operation. The final Playwright operation re-resolves and validates the target, so movement does not authorize clicking a stale rectangle. Click options (Force, Trial, Position, modifiers, button, timeout, etc.) remain raw.

Explicit Type/PressSequentially and keyboard Type remain paced operations. Text is enumerated as Unicode scalar values (not grapheme clusters); the SDK does not claim universal IME/composition support. A total `TypingDeadlineMilliseconds` budget (default 30 seconds) limits pauses and further characters; cancellation and page closure are checked between characters. A command already in-flight in Playwright cannot be forcibly cancelled by this SDK. Use raw Playwright (or `Humanize=false`) when exact input events or IME behavior matter. Fill and InsertText are not converted to paced typing. Press chords stay native.

## Trajectory ownership

`HumanActions` owns random state and per-page cursor knowledge through weak page tables; trajectory data is shared by the pure Cursory library, while each generation uses isolated random state. The internal Bézier seam remains unchanged and selected by default. Cursory is an experimental opt-in adapter over `SpyBrowser.Cursory`, not a copied algorithm or plugin API.

Cursory sampling frequency and directness are passed to the pure generator. The adapter only scales its returned timestamps to `MouseMinimumDurationMilliseconds`/`MouseMaximumDurationMilliseconds`; it does not regenerate the selected recording. The scheduler uses monotonic absolute times, awaits each native call serially, skips overdue intermediate samples, and observes a bounded `CursoryMovementDeadlineMilliseconds`. It may not meet planned timing under transport or browser load, and deadline/page-close/fault can stop before the endpoint.

Pointer position is page-specific and is recorded only after a confirmed send. The adapter does not quantize generated CSS coordinates, but the local Chromium/Playwright browser test currently observes integer `MouseEvent.clientX/clientY` even for fractional inputs and DPR 1, 1.25, 1.5, and 2; this is a browser/driver observation, not evidence that all browsers round identically. Since Playwright cannot query the physical cursor position, the first Cursory move sends just the requested endpoint as a documented bootstrap anchor; subsequent moves can generate a path. Wrapped raw `Mouse.MoveAsync` (including calls with options) updates that observation. Wrapped `DownAsync`/`UpAsync` tracks observed button state; while a button is down, selected Cursory movement delegates a single endpoint move rather than preparing a path. Calls through `PlaywrightHumanizer.Unwrap` cannot be observed: after external raw mouse movement, call `humanizer.InvalidateMousePosition(page)` before the next humanized move. This invalidation intentionally clears position knowledge, not button ownership.

The Cursory native library is a preview whose upstream parity and dataset redistribution review remain separate gates. Selecting it explicitly should be limited to local evaluation until those gates pass. No additional trajectory-generation process is launched; Playwright's normal driver remains part of the ordinary Playwright runtime.

## Verification

Run `dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj`. Browser motion tests use `BrowserFact`/`BrowserTheory` and are skipped unless `SPYBROWSER_RUN_BROWSER_TESTS=1`; provision the repo's Playwright Chromium if enabled. Report skips separately: unit tests alone do not establish browser coverage. A reproducible motion benchmark is provided at `tools/native-motion-benchmark`; record its machine/runtime/browser output rather than treating timings as a CI performance guarantee.
