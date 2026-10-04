# Click/Fill acceptance proof (tickets 06 and 08)

Scope: tests/evidence only. Runtime files under `src/` were inspected but not changed (read-only ownership boundary).

## Evidence inventory

| Contract | Existing direct local-browser evidence | Assessment |
|---|---|---|
| 06: native Fill/Clear behavior for text, password, number/date, textarea, contenteditable, JS-controlled input; no synthesized key events | `ActionContractMatrixTests.Fill_and_clear_match_raw_for_native_controls_and_javascript_controlled_input` | Already covered; do not duplicate. |
| 06: `Timeout=0`, empty/explicit options, no options mutation, page/frame routes, hidden/disabled/multiple-match actionability and errors | `ActionContractMatrixTests.Fill_routes_options_actionability_and_frame_routes_match_native_contract` | Already covered. |
| 06: closing context while native Fill waits for a missing target | `ActionContractMatrixTests.Closing_context_while_fill_waits_observes_the_original_task` | Direct Fill-specific test already exists; do not infer this from a typing cancellation test or duplicate it. It starts the Fill task, closes its context and asserts failure/non-success. |
| 08: native dblclick event and `click.detail` sequence | `RemainingInputContractTests.Compatible_double_click_preserves_native_detail_and_dblclick_event` | Already covered with `click(1), click(2), dblclick(2)`. |
| 08: Trial has no extra pointer preparation; default click occurs once; option-bearing click keeps Force/Position/Modifiers/Button and options objects unmodified | `ActionContractMatrixTests.Trial_click_options_do_not_prepare_pointer_and_default_click_runs_once`; `Click_options_and_raw_routes_preserve_effects_without_mutating_options` | Existing direct browser coverage. |
| 08: overlay/detach, native link navigation, hover, and one successful click effect | `ActionContractMatrixTests.Locator_actions_keep_one_effect_through_detach_overlay_and_link_navigation`; `Trial_click_options_do_not_prepare_pointer_and_default_click_runs_once` | Existing coverage. Detachment is pre-action; it is not evidence of post-effect failure behavior. |
| 08: preparation and final action with a target that moves during pointer preparation | `ClickFillAcceptanceProofTests.Compatible_click_re_resolves_a_target_that_moves_during_pointer_preparation` | Added: the first mousemove relocates the target; the final native locator click must resolve the relocated target and produce exactly one click inside its new bounds. |

`acceptance06.09` mapping is not treated as evidence. The ticket's requirement is specifically cancellation of a Fill awaiting an unavailable field while its context closes; the existing test above is the direct proof. Typing cancellation tests do not satisfy it.

## Remaining proof gaps / status

This evidence suite does **not** establish all ticket 08 criteria. In particular, it does not yet force and count a native click/double-click failure *after* a partial DOM effect, nor demonstrate a deterministic unsafe-preparation failure that falls back to the native action before effects. The existing detached-target case fails before the action, so it cannot close those gaps. Do not describe either ticket as complete on this evidence alone. No unsupported route was added and no assertion was weakened.

## Validation

On the assigned worktree, with Chromium browser tests enabled:

```text
SPYBROWSER_RUN_BROWSER_TESTS=1 dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ClickFillAcceptanceProofTests|FullyQualifiedName~ActionContractMatrixTests|FullyQualifiedName~RemainingInputContractTests'
```

Observed: build succeeded; 18 passed, 0 failed, 0 skipped. Initial `--no-restore` attempt was blocked because `project.assets.json` was absent; `dotnet restore tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj` completed, then the focused Release browser run passed.
