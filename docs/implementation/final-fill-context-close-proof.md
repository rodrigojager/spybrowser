# Compatible Fill context-close proof (06.09)

**Disposition:** Direct test proof added and passed; this is a tests-only delta, not a production or package change. It does not clear the independent `datasetRights=UNVERIFIED` publication block and does not promote the audit or ledger.

## Proof

`SpyBrowser.Tests.FillContextCloseAcceptanceTests.Compatible_fill_waiting_on_disabled_field_fails_when_context_closes_and_settles_original_task` starts the wrapped Playwright-compatible `ILocator.FillAsync` against a disabled input using explicit `Timeout = 0`. A start signal and `IsCompleted` checks establish that the operation remains pending. Before closing, the test checks the original value and zero input events. It closes the owning browser context, awaits the returned Fill task with a separate 10-second safety bound, requires the observed `TaskCanceledException` (Playwright's pending native locator operation is canceled by context closure, not an action timeout), verifies the error is not a timeout, confirms the Fill task is complete, and confirms the context is closed. The final assertions establish settlement/no orphan task; the value/event observations are made while the page is still accessible, before closure.

The actual `tools/verification/consumer-checks/Program.cs:158-171` check is a different, narrower cancellation route: it runs `RpaRunner.RunAsync`, cancels its token, then awaits the runner and checks context closure. It does **not** await the original Fill task directly (and its stated lanes passing 14/14 do not fill that direct-proof gap). The new test directly awaits the task returned by wrapped Fill; its safety bound is only a test guard.

## Source/package correspondence

The test passes an explicit `LocatorFillOptions`, and current `PlaywrightHumanizer` compatible mode does not substitute a helper Fill; the proxy's native path reaches `targetMethod.Invoke(_target, nativeArguments)` (`src/SpyBrowser.Playwright/PlaywrightHumanizer.cs`, `HumanizingDispatchProxy<T>.InvokeCore`). Thus it exercises wrapped API dispatch to the underlying Playwright locator's Fill, not `RpaRunner`'s cancellation wrapper. No production source was changed for this task. Frozen production identity remains `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`; test-only addition requires no package rebuild/repack.

## Execution

- SDK: 8.0.319; browser opt-in `SPYBROWSER_RUN_BROWSER_TESTS=1`; temporary/cache home directed to `D:/Temp`.
- Focused command: `dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj --no-restore --filter FullyQualifiedName~FillContextCloseAcceptanceTests.Compatible_fill_waiting_on_disabled_field_fails_when_context_closes_and_settles_original_task --artifacts-path artifacts/goal/final-fill-context-close/build --results-directory artifacts/goal/final-fill-context-close/results --logger 'trx;LogFileName=fill-context-close.trx' --verbosity minimal`
- Final TRX: `artifacts/goal/final-fill-context-close/results/fill-context-close.trx`, run `f6c4c1de-b379-4b7e-90bf-b3ab6b91eca5`; 1 total, 1 executed, 1 passed, 0 failed, 0 skipped. TestCase: `SpyBrowser.Tests.FillContextCloseAcceptanceTests.Compatible_fill_waiting_on_disabled_field_fails_when_context_closes_and_settles_original_task`.
- An initial run failed only because the test expected `PlaywrightException`; observed context-close result was `TaskCanceledException`. That first TRX is preserved at `artifacts/goal/final-fill-context-close/results/fill-context-close-first-failure.trx`. The assertion was corrected to the observed Playwright pending-command cancellation and the focused rerun passed. The action timeout is explicitly disabled, and closure is completed before awaiting the task.

**Residual scope:** This proves the literal direct wrapped-Fill/context-close case with a disabled input and Chromium. It does not claim broader browser-engine coverage or independently rerun the three consumer lanes. No default promotion, publication, ledger update, or audit relabeling was performed.