# Remaining input contracts (tickets 07–10)

This implementation closes two demonstrated gaps and records the native contracts covered by focused browser tests.

## Native semantics and errors

- Compatible locator/page double-click uses Playwright's native `DblClickAsync` for trial and final action. It is not synthesized as two independent clicks; Chromium emits two `click` events with `detail` 1 and 2, then one `dblclick` with `detail` 2.
- Compatible `InsertText` and keyboard shortcuts remain native operations. The wrapper does not take ownership of shortcut modifiers or add cleanup key events around native `PressAsync`.
- A positive Playwright `SetDefaultTimeout` observed through the wrapper remains owned by the native Playwright operation. The SDK does not race it with a second cancellation timer that changes its native timeout exception into `TaskCanceledException`. Explicit per-call timeouts remain native-owned as before. A default timeout of zero remains unlimited.
- Compatible timed typing is only paced where each grapheme is a single Unicode scalar. If any text element comprises multiple scalars (including combining sequences and joined/flag emoji), the complete original Playwright Type/PressSequentially/keyboard Type call is delegated before sending any paced prefix. This boundary is conservative; it is not a claim of general IME or composition support.

## Lifecycle and ownership boundaries

The page input gate still serializes wrapper-visible input per page. A `WaitAsync` cancellation does not imply that an already-started Playwright task has stopped; that task remains observed and gate ownership is retained until it completes (or the page is closed under the existing cleanup policy). No blanket modifier/button release is performed: only state explicitly owned by SDK operations can be cleaned up. Operations that accept unsupported options and calls through raw escape hatches remain native and outside additional humanization guarantees.

## Focused evidence

`RemainingInputContractTests` exercises native default-timeout exception parity, exact native double-click event details, and whole-operation fallback for a multi-scalar grapheme. These are local Chromium browser tests and run only with `SPYBROWSER_RUN_BROWSER_TESTS=1`.
