# Humanization behavior

`HumanInteractionOptions.CompatibilityMode` is opt-in. Its default remains `Legacy`, preserving the existing humanizer dispatch and Bézier movement behavior.

## PlaywrightCompatible

In this mode, parameterless locator/page/frame Fill and Clear, locator Press, InsertText, mouse Wheel, and other calls not explicitly supported are delegated to the original Playwright implementation. Calls supplying options are deliberately raw as well; no options object is inspected or changed. This retains Playwright's actionability, strictness, errors, event semantics, and native support for input types/contenteditable.

Parameterless locator Click, DblClick and Hover do a Playwright trial first, optionally prepare pointer movement toward the current target center, then invoke the native locator action exactly once. Preparatory movement is additional pointer activity, not a promise that every event matches an unwrapped call. If no usable bounding box is available, the native action remains the final operation. The final Playwright operation re-resolves and validates the target, so movement does not authorize clicking a stale rectangle. Click options (Force, Trial, Position, modifiers, button, timeout, etc.) remain raw.

Explicit Type/PressSequentially and keyboard Type remain paced operations. Text is enumerated as Unicode scalar values (not grapheme clusters); the SDK does not claim universal IME/composition support. A total `TypingDeadlineMilliseconds` budget (default 30 seconds) limits pauses and further characters; cancellation and page closure are checked between characters. A command already in-flight in Playwright cannot be forcibly cancelled by this SDK. Use raw Playwright (or `Humanize=false`) when exact input events or IME behavior matter. Fill and InsertText are not converted to paced typing. Press chords stay native.

## Trajectory ownership

`HumanActions` owns its random generator, cursor estimate, and trajectory strategy per page through the existing weak page table; nothing mutable is shared globally. The internal `IMouseTrajectoryStrategy` returns framework-independent coordinates and delays, separating generation from Playwright dispatch. `BezierTrajectoryStrategy` extracts the existing legacy generator and remains the selected/default strategy. This seam is internal only; it is not a plugin API or the Cursory integration.

## Verification

Run `dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj`. `ActionsCompatibilityTests` launches local headless Chromium, so provision the browser with the repo's Playwright browser setup if absent. Existing browser integration tests gated by `SPYBROWSER_RUN_BROWSER_TESTS=1` must be reported as skipped/not run when that flag is unset; a passing unit-test run alone does not establish browser coverage.
