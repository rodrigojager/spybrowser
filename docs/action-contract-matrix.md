# Action-contract matrix evidence (tickets 06–09)

This evidence slice maps `tests/SpyBrowser.Tests/ActionContractMatrixTests.cs` to the acceptance audit at `artifacts/goal/audit-contract-slices.md` and its JSON companion. Tests are owned `BrowserFact`s and use local browser pages only; no CAPTCHA or third-party URL is used. Raw Playwright runs are comparison baselines. Error checks use exception categories/strictness rather than platform-specific message equality.

Run the focused lane (the explicit flag is required):

```sh
SPYBROWSER_RUN_BROWSER_TESTS=1 dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~ActionContractMatrixTests --logger 'console;verbosity=minimal'
```

## Evidence

| Slice | Test evidence | What is demonstrated |
|---|---|---|
| 06.2–06.5 | `Fill_and_clear_match_raw_for_native_controls_and_javascript_controlled_input` | Raw/wrapped text, password sentinel, number, date, textarea, contenteditable and JS-controlled fields; values/input and absence of synthetic keyboard events for Fill. Clear keyboard-event count is compared against native behavior. |
| 06.4, 06.6–06.7 | `Fill_routes_options_actionability_and_frame_routes_match_native_contract` | Hidden/disabled/strict error categories, timeout zero, explicit and empty options, Page and FrameLocator routes. This test retains a strict page-alive assertion and currently fails on the known native-options timeout/page-lifecycle regression. |
| 06.9 | `Closing_context_while_fill_waits_observes_the_original_task` | Closes context during bounded unavailable-locator Fill, awaits the original task and checks it did not succeed. |
| 07.1–07.4, 07.6 | `Insert_press_and_type_keep_distinct_event_traces_and_unicode_order` | InsertText input-only event, Press key/chord trace (Control+A, Backspace, Shift, ArrowLeft, Enter), sequential text with accent, emoji/surrogate pair, combining mark, Cyrillic, Arabic and Hebrew. |
| 07.5 | `Click_options_and_raw_routes_preserve_effects_without_mutating_options` | Explicit sequential Delay/Timeout options remain unchanged; option-bearing native action values are snapshotted. |
| 08.2–08.5, 08.7 | `Trial_click_options_do_not_prepare_pointer_and_default_click_runs_once`; `Locator_actions_keep_one_effect_through_detach_overlay_and_link_navigation`; `Click_options_and_raw_routes_preserve_effects_without_mutating_options` | Trial vs raw movement/click baseline, one successful effect, hover, overlay, detach, link navigation, and Position/Force/Modifiers/Button option preservation. These are partly affected by the currently observed Playwright options/page lifecycle defect. |
| 09.2–09.3, 09.6–09.7 | `Typing_deadline_is_bounded_and_closing_page_stops_later_characters` | Over-budget long input asserts failure and bounded prefix/timing; closes the page mid-stream and observes the actual operation. Current SDK lifecycle failure can close the page before the prefix assertion. |
| 07.9, 09.10 | `Diagnostics_never_include_typed_secret_sentinel_and_unsupported_options_are_raw` | Captured enabled diagnostics are serialized and checked not to contain a unique sentinel; explicit Delay keeps raw append behavior. |
| timeout regression | `Wrapped_timeout_failure_keeps_the_page_alive_and_raw_errors_are_category_compared` | Compares raw/wrapped timeout categories using separate pages and explicitly asserts the wrapped page remains alive. It currently fails: wrapper options timeout closes its page. Do not weaken this assertion. |

## Known producer regression / current validation

The focused run compiled and executed 10 tests: **6 passed, 4 failed**. Failures observed:

- `Wrapped_timeout_failure_keeps_the_page_alive_and_raw_errors_are_category_compared`: wrapped timeout closes its page, violating the retained page-alive contract.
- `Fill_routes_options_actionability_and_frame_routes_match_native_contract`: native hidden-field timeout closes the page before completing the matrix.
- `Typing_deadline_is_bounded_and_closing_page_stops_later_characters`: page closes before the test can read the retained typing prefix.
- `Locator_actions_keep_one_effect_through_detach_overlay_and_link_navigation`: explicit-options timeout closes the page during detach probing.

This is the reported duplicate `PageInputState`/native-options timeout lifecycle bug; the parent lifecycle-router work owns the product fix. The tests and page-alive assertion remain in place as regression evidence. The matrix must be rerun after that fix is merged; these results are not acceptance of criteria 06–09.

## Still not directly proved / follow-up after lifecycle fix

- 07.7/07.8: raw-vs-wrapped Press/InsertText failure stack/category and SDK key cleanup on injected mid-chord error.
- 08.1/08.4/08.6/08.8/08.9: final raw invocation count across success/failure/unsafe-prep fallback, final coordinate coherence, deterministic phase-by-phase deadline exhaustion and an induced partial-effect failure without replay. Current source call structure is not browser proof.
- 09.1 public `HumanActions.TypeAsync(replaceExisting:true)` is a Fill route, distinct from wrapper paced Type; this task makes no product change. 09.4 unsupported-composition fallback before first character, 09.5 complete raw Delay equivalence, and 09.8 mid-sequence focus/detach no-replay still need direct assertions.
- The options click assertions currently record option snapshots/effects but do not exhaust every independent Force/Position/Modifiers/Button permutation. No assertion here implies arbitrary methods are humanized.

No product source was edited. Continue the remaining matrix work after parent merges its lifecycle fix, without deleting the failing assertions to achieve a green lane.
