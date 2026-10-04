# Factory and popup stress acceptance proof

Scope: the remaining factory-profile-lease criterion in issue 11 and multi-popup / immediate-close identity and lifetime criterion in issue 14. This adds browser acceptance coverage only; it does not change the Playwright-facing product API or runtime.

## Executable evidence

`tests/SpyBrowser.Tests/FactoryStressAcceptanceTests.cs` contains two bounded `BrowserFact` tests. They use a unique `TemporaryDirectory` (under the OS temporary directory) per test and an in-memory identity; the persistent-profile test's known layout is resolved through `IdentityStore.GetProfileDirectory` / `GetLeasePath`. No test profile or browser data is written outside that owned temporary root.

- **Disposable context / lease proof:** launch two simultaneously-live `LaunchContextAsync` browser contexts for the same identity; check the identity profile directory and lease file are absent. While both pages remain usable, launch `LaunchPersistentContextAsync` for the same identity and its canonical profile. Assert that this real launch creates the profile and lease file and that the two disposable pages remain open and usable. This is an execution proof, not an assertion based only on source inspection or a missing file.
- **Popup stress:** launch a real browser with humanization enabled, `MouseAlgorithm=Cursory`, `PlaywrightCompatible`, and a fixed seed. The seed controls trajectory-generation repeatability; browser scheduling remains real. Disable Chromium popup blocking explicitly, then use one Playwright click on a local parent page to create three children synchronously from the user action. `IBrowserContext.Page` and parent `IPage.Popup` callbacks each collect all three pages behind task-completion barriers (no sleep/poll timing assumptions). Check raw identity, same cached wrapper instance through both events and `context.Pages`, and correct owning context.
- Install input, mouse, and click handlers through each child's **unwrapped raw** page. Concurrently use the wrapped popup pages for sequential typing and click actions. Check each child's exact independent DOM value and action result, observed mouse activity, and unchanged parent input; closing one child twice must remove only that child, and the other two remain usable. Each browser operation has a 60-second ceiling; the test runner uses a 90-second hang dump.

The callback/collection and DOM assertions detect wrong page hints/dispatch, wrapper duplication, and cross-page interaction state that a single-popup check cannot establish. The native SDK remains the source of native channel errors; this test does not translate or suppress them. On each surviving child, the first wrapped mouse move must finish at the requested point with no generated intermediate path (at most two DOM `mousemove` observations); the next move must show generated movement. A Linux run observed the opener's physical pointer position followed by the child target on its first move, while Windows surfaced only the target. The bounded assertion distinguishes that active-tab handoff from an inherited generated trajectory without claiming DOM-event count equals protocol calls or asserting OS cursor timing.

## Running

Run from the assigned checkout and preserve unique output directories, especially when repeating an OS lane:

```powershell
$env:SPYBROWSER_RUN_BROWSER_TESTS = '1'
$env:SPYBROWSER_HEADED = '1'
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release `
  --blame-hang-timeout 90s `
  --artifacts-path D:/SpyBrowser-work/popup-factory-stress-proof/artifacts/factory-stress-win-<run-id> `
  --logger 'trx;LogFileName=factory-stress-win-<run-id>.trx' `
  --results-directory D:/SpyBrowser-work/popup-factory-stress-proof/artifacts/factory-stress-win-<run-id>/results
```

The test project resolves the repository's pinned Microsoft.Playwright 1.61.0 by default. Do not point this run at a newer Playwright driver copied over the 1.61.0 driver or reuse prior output files: compiled wrappers, managed Playwright assemblies and driver must match. Do not rely on old MSBuild output to restore an older driver over a newer one.

For the corresponding WSL lane, use a clean checkout/source and unique Linux artifacts/results directories. This lane uses .NET SDK 8.0.319 side-by-side in `/home/rodrigo/.dotnet`, and sets `DOTNET_ROOT`/`PATH` explicitly. Restore/build the pinned Microsoft.Playwright 1.61.0 test sources first, then archive the resulting `.playwright` driver tree into `/home/rodrigo/.dotnet-spybrowser` (do not repurpose a 1.63.0 driver). Set `PLAYWRIGHT_DRIVER_SEARCH_PATH=/home/rodrigo/.dotnet-spybrowser` and prepend `/home/rodrigo/.dotnet-spybrowser/.playwright/node/linux-x64` to `PATH`. Install the Chromium revision required by that driver and its Linux dependencies; run headed with Xvfb. Set the same browser-test/headed flags and hang timeout. Capture the command, SDK, driver, artifact path, and passed/failed/skipped counts in the run report; never count the normal disabled-lane skip as browser evidence. No retry that broadens timeouts or changes assertions should be presented as a passing original run.

## Validation record

Final headed Windows run: .NET SDK 8.0.319, Microsoft.Playwright 1.61.0, 138 executed = 137 passed, 0 failed, 1 skipped. Both new tests passed. Artifacts/results: `artifacts/factory-stress-win-full-verified-20261020/`.

Final headed WSL Ubuntu 24.04/Xvfb run: .NET SDK 8.0.319, Microsoft.Playwright 1.61.0 managed package plus the archived bundled driver (`.playwright/package/package.json` reports `1.61.1-beta-1782139630000`), Chromium revision 1228, 138 executed = 137 passed, 0 failed, 1 skipped. Both new tests passed. Artifacts/results: `artifacts/factory-stress-linux-full-verified-20261020/`. The one skip in each lane is the existing explicitly gated `Headed_probe_does_not_change_the_existing_pages_focus_or_active_element`; browser tests were enabled and run.

The updated acceptance tests were also run focused on both OS lanes after the final cursor/close assertions: Windows 2/2 (no skips) in `artifacts/factory-stress-win-cursor-acceptance-20261020/`; WSL 2/2 (no skips) in `artifacts/factory-stress-linux-cursor-acceptance-20261020/`. The final complete suites above include this exact source. During earlier setup, a full Windows run with the initial 20-second cap timed out launching the first disposable browser, and an existing concurrency timing assertion also exceeded its pre-existing 3500ms upper bound (4496ms); the disposable-operation cap was raised to 60 seconds without changing tolerances or assertions. Subsequent complete Windows run passed all non-skipped tests. The first WSL full-suite attempt lacked the pinned Chromium install and failed 40 existing browser cases at launch; after installing driver-matched Chromium/dependencies, the final complete WSL run passed. A strict one-DOM-event anchor assertion also exposed the opener pointer-position handoff on Linux; the final test asserts the exact requested endpoint and absence of a generated path while allowing that observed host handoff, and its focused plus complete cross-OS runs passed. One intermediate Windows full run had an unrelated existing 10-second direct-helper timeout under contention (136 passed, 1 failed, 1 skipped); the repeated complete run passed all non-skipped tests without changing that threshold. These setup/initial attempts are not counted as passing evidence.
