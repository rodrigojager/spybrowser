# Independent review: proposed acceptance promotions

**Disposition: APPROVE all 57 proposed `pending → verified` promotions, at criterion scope only.** This is a read-only review of the change list, proposed ledger, canonical evidence ledger, exact original issue bullets, direct source/assertion evidence and retained run artifacts. No test was run here. The review neither changes nor downgrades any pre-existing `verified` criterion.

The historical/browser comparison evidence is tied to production source `5fa99de`; required/latest runs at that source are supplied as passed artifacts, with the test-only `d574f6b` edge run also available. Parent independently matched all 113 input-fragment test references to passed records in the required/latest/d574 TRXs. These results support only their mapped literal assertions, not untested behavior. The changed status is evidence-backed and in-scope; it is not blanket suite, release, or distribution approval.

## Decisions

`APPROVE` is stated per proposed criterion below. IDs are `ticket.criterion`; cited audit entries map the original issue literal to the direct assertion/artifact. For input tickets, the evidence source is `recovery-audit-input.json` (`entries`); native tickets use `recovery-audit-native.json` (`criteria`); diagnostics/distribution uses `recovery-audit-distribution.json` (`audit.entries`); the five recovery corrections use `recovery-audit-closure.json` (`evidence`). These mappings cite test-specific assertions/source, not mere path existence. Current successful run artifacts cited by the audits are `artifacts/goal/5fa99de-required-results/required-final-cut.trx`, `artifacts/goal/5fa99de-latest-results/latest-final-cut.trx`, and, for test-only edge additions, `artifacts/goal/d574f6b-edge-results/edge-controlled-boundaries.trx`; Cursory assertions are in `artifacts/goal/5fa99de-cursory-results/cursory-final-cut.trx`.

| Decision | Criterion | Direct assertion/reference and review finding |
|---|---|---|
| APPROVE | 01.03 | `GeneratorTests`/`MonotonicMovementSchedulerTests` inspect trajectory timing and positions; `NativeCursoryMovementTests.Cursory_move_reaches_fractional_endpoint_in_local_dom_at_device_scale` supplies real-browser coverage; cited TRXs passed. |
| APPROVE | 01.06 | `GeneratorTests.RepeatedSeedIsDeterministic` and `ThousandsOfSeededCasesPreserveTrajectoryInvariants`, plus browser endpoint invariant; deterministic/invariant requirement met without requiring identical unseeded browser coordinates. |
| APPROVE | 01.10 | Browser-specific endpoint/close cases are identified by browser test markers and their named results are Passed in required TRX, rather than inferred from declarations. |
| APPROVE | 02.02 | `native-package-demo-5fa99de/evidence.json` and consumer stdout: fresh package-reference-only .NET consumer executes generation, without project reference, browser, generator service or first-use download; package/source hash recorded. |
| APPROVE | 02.10 | Exact package inspection plus fresh package-only consumer/resource integrity result and the 37-test Cursory TRX substantiate package contents, embedded resource integrity and isolated consumption. |
| APPROVE | 02.11 | `src/SpyBrowser.Cursory/README.md`, `HumanInteractionOptions.cs`, and required TRX: preview boundaries are explicit and Cursory remains opt-in; no incomplete algorithm is enabled by default. |
| APPROVE | 03.02 | `GeneratorTests.MatchesPinnedRngBitstreamAndDistributionVectors` and associated exact RNG/wraparound/rejection assertions passed in Cursory TRX. |
| APPROVE | 03.07 | `GeneratorEdgeContractTests` named quadrant/axis, subpixel, zero-distance, invalid input and max-seed cases plus `ThousandsOfSeededCasesPreserveTrajectoryInvariants` passed. |
| APPROVE | 03.09 | `SpyBrowser.Cursory.Tests.csproj` and `tools/cursory-reference/README.md` show ordinary suite is .NET-only; reference tooling is maintenance-only; supplied .NET Cursory run passed. |
| APPROVE | 03.10 | `reference-stage-proof/manifest.json`, Windows/Linux stage artifacts and hash-bound stage fixtures locate parity by stage on both OSes. This is retained differential evidence, not a claim of a new Linux test run. |
| APPROVE | 03.11 | `docs/cursory-port.md` refresh/benchmark procedure plus exact package benchmark stdout/evidence records cold, warm and allocations; no unsupported performance threshold inferred. |
| APPROVE | 04.02 | `NativeCursoryMovementTests.Cursory_move_reaches_fractional_endpoint_in_local_dom_at_device_scale`: selected Cursory path reaches the local DOM endpoint, with required/latest named results passed. |
| APPROVE | 04.06 | Same local-DOM endpoint/event assertion and package-reference-only in-process DLL consumer establish browser execution and no generator sidecar. |
| APPROVE | 05.04 | `MonotonicMovementSchedulerTests` explicitly exercise fake-clock serial delay/coalescing, deadline, cancellation and close-during-wait; named required results passed. |
| APPROVE | 05.05 | `NativeCursoryMovementTests.Cursory_move_reaches_fractional_endpoint_in_local_dom_at_device_scale` passes DPR 1, 1.25, 1.5 and 2 theory cases and endpoint assertions. |
| APPROVE | 06.09 | `ActionContractMatrixTests.Closing_context_while_fill_waits_observes_the_original_task` directly awaits the original Fill task after close; this is not substituted with a typing test. |
| APPROVE | 06.10 | Three pinned RpaBlockly V1/V2 consumer lanes exercise existing consumer blocks, each reports all 13 required local checks passed. The separate semantic API audit is accurately disclosed as not run and is not this criterion. |
| APPROVE | 09.02 | `RemainingInputContractTests.Compatible_typing_delegates_multirune_grapheme_to_native_before_any_paced_prefix` plus content/order assertions demonstrate intact text and safe pre-insertion fallback for covered grapheme cases; no universal IME claim. |
| APPROVE | 09.07 | `LifecycleBrowserTests.Typing_deadline_closes_the_page_instead_of_abandoning_an_input_task` awaits the actual typing task and verifies closure stops future characters. |
| APPROVE | 09.08 | `ActionContractMatrixTests.Focus_loss_during_type_does_not_replay_or_reinsert_the_prefix` asserts the prefix is not replayed/reinserted; named current result passed. |
| APPROVE | 10.01 | `ActionContractMatrixTests.Public_helpers_share_the_page_gate_with_wrapped_input_and_keep_other_pages_parallel` directly covers helper/wrapped serialization and independent-page parallelism. |
| APPROVE | 10.08 | `PointerObservationTests.Compatible_locator_hover_during_drag_uses_one_native_hover_without_releasing_owner_button` observes native Up/owner state; direct assertion supports the bounded ownership condition. |
| APPROVE | 10.09 | `CleanupCauseAcceptanceTests.Cancellation_remains_primary_when_owned_button_cleanup_faults_and_action_is_not_repeated` covers primary cause, owned cleanup and non-replay. |
| APPROVE | 10.10 | `ActionContractMatrixTests.Closing_context_while_fill_waits_observes_the_original_task` plus cited queue/readiness/movement/click/type boundary assertions cover original-task close and action boundary behavior. |
| APPROVE | 10.11 | `InputAcceptanceClosureTests.Repeated_wrapped_input_and_helper_lifecycles_close_idempotently_and_release_handles`: retained acceptance artifact records 32 awaited browser cycles, repeated close and weak-reference release checks; test-only additions are in supplied edge/current runs. |
| APPROVE | 10.12 | `FactoryStressAcceptanceTests.Concurrent_humanized_popups_keep_identity_and_other_pages_alive_when_one_closes_immediately` asserts Cursory popup movement, concurrent wrapped input and page survival. |
| APPROVE | 15.04 | `HumanizationDiagnosticsTests` and diagnostics docs/probe output associate runtime versions and selected algorithm/dataset with the report; scoped metadata requirement, not a blanket telemetry claim. |
| APPROVE | 15.06 | `HumanizationDiagnosticsTests` plus snapshot sentinel tests assert prohibited values are absent, including error paths; no retained exception text. |
| APPROVE | 15.10 | Diagnostics assertion/docs accept raw fallback for explicit unsupported options; report does not mislabel it as a required consumer correction. |
| APPROVE | 16.04 | Probe tests and diagnostic probe artifacts exercise persistent, context and browser-only modes without changing consumer flow. |
| APPROVE | 16.07 | `ProbeIsolationBrowserTests` and probe result show `RunGpuProbe=false` remains off; no click-triggered/automatic probe. |
| APPROVE | 16.08 | Local browser probe assertions compare user-tab URL/state/references before and after, rather than relying on temporary-page file existence. |
| APPROVE | 17.01 | `DiagnosticsConsistencyTests` normalize configured/observed timezone; UTC/Etc/UTC and Windows/IANA cases are explicit and passed. |
| APPROVE | 17.11 | Regional consistency assertions include the RpaBlockly-equivalent local timezone without enabling probes by default. |
| APPROVE | 18.02 | `MissingWebGLBrowserTests` checks browser family/UA/platform conflict findings through normalized rules, not literal string equality. |
| APPROVE | 18.03 | Missing/reduced Client Hints are represented as unavailable rather than a proven contradiction in the cited finding assertions. |
| APPROVE | 18.04 | Existing GPU policy and renderer-unavailable browser assertions cover software/hardware observation only within available capability. |
| APPROVE | 18.08 | API/CLI findings use stable codes/severity; local coherent, conflict and unavailable cases are asserted in the cited tests/probe. |
| APPROVE | 19.09 | `DiagnosticSnapshotStoreTests` comparison assertion classifies browser/Playwright/renderer version changes as informational while consistency rules retain actual contradictions. |
| APPROVE | 20.01 | Three consumer `evidence.json` files pin consumer commit, SDK/Playwright/package metadata and isolated checkout; candidate source hash is explicit. |
| APPROVE | 20.02 | Consumer evidence identifies local NuGet feed/package-reference harness; no external publication is involved or required. |
| APPROVE | 20.03 | Consumer evidence records `consumerTargetFramework=net9.0`, isolated package candidate and 13/13 checks; harness names actual RpaBlockly V1/V2 compiler/runner flows. SDK resolves to 10.0.401, but the project target is explicitly net9.0. |
| APPROVE | 20.04 | Required consumer check explicitly covers Humanize on/off, timezone, viewport, selected Chromium and in-memory identity; 13/13 result recorded. |
| APPROVE | 20.05 | Consumer check asserts context event/reference/collection identity and removal after close. |
| APPROVE | 20.06 | Consumer check names StorageStatePath, local download, screenshot/navigation, frame/popup; local output is retained. |
| APPROVE | 20.07 | Consumer checks exercise actual Fill cancellation/context close and late-launch browser cleanup; these are named required checks, not inferred from generic suite pass. |
| APPROVE | 20.08 | Consumer check runs two actual RpaRunner jobs concurrently and asserts independent input/no profile lease. |
| APPROVE | 20.09 | Consumer off lane directly verifies raw behavior; explicit raw options are allowed by the literal. |
| APPROVE | 20.10 | Pinned consumer harness retains existing adapter/other providers and runs isolated configuration; no conflicting facade is introduced. |
| APPROVE | 20.12 | Consumer source-isolation record and checkout show no blocks/DSL/CAPTCHA edits; excluded full CAPTCHA/provider suite is disclosed as not run. |
| APPROVE | 21.01 | `historical-5fa99de-windows` baseline/candidate package consumers run the same seven DOM tasks across four variants, with task observations and completion recorded; matched comparison is genuine. |
| APPROVE | 21.04 | Exact package benchmark evidence/stdout records machine, OS, SDK, browser/Playwright, algorithm and dataset; limitations are explicit. |
| APPROVE | 01.01 | Historical consumer `acceptance-summary.json`/variant results records 28 actual baseline/candidate DOM observations, including stable hover-click, ordering, endpoint error and completion. RNG/timing are not overclaimed. |
| APPROVE | 01.04 | Historical installed-assembly reflection compares existing public members/signatures/defaults/shapes and options; zero differences; candidate-off consumer completes 7/7 DOM tasks. |
| APPROVE | 05.10 | Combined evidence is properly scoped: historical actual-DOM result records intervals, durations, pauses, endpoints and machine/limits; exact package benchmark records cold/warm/allocation. No claim that generation benchmark measures DOM rhythm. |
| APPROVE | 20.11 | Each package-consumer summary reports exactly 13/13 required local checks and explicitly discloses excluded suite and unrun 14-item semantic API audit; truthful reporting itself meets this bullet. |
| APPROVE | 19.06 | `DiagnosticSnapshotStore.SaveAsync` creates timestamp+GUID targets independent of IdentityId; `Concurrent_saves_use_distinct_paths_and_retention_is_bounded` asserts distinct existing paths, and installed proof records two-process concurrent saves. |

## Boundaries

No proposed promotion is rejected for merely lacking a personal rerun or future exhaustive permutation. `20.11` does **not** imply the separate semantic audit ran. `19.05` and `19.11` are not proposed promotions here; Unix non-root proof remains in progress (job 40), and this review does not claim current Linux completion or infer a gate without TRX counts. Rights clearance remains unresolved and publication blocked; none of these approvals is legal clearance or release authorization.

**Validation:** read-only inspection only; testsRunByThisReview=false; no ledger, product or test files edited. Owned outputs: this Markdown and `docs/implementation/recovery-ledger-review.json`.
