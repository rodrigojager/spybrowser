# Testing and Playwright upgrade verification

## Pull request baseline

The required `verify` lane runs on Windows and Linux against Microsoft.Playwright **1.61.0**. It restores and builds Release, explicitly installs the Playwright-managed Chromium (and Linux OS dependencies), runs the full test suite, and packs the production projects. Test results and packages are uploaded as workflow artifacts. Browser tests are explicitly enabled in this lane; locally, where they are not enabled, xUnit reports them as **skipped** rather than passing early from a test-body return.

## Scheduled/manual compatibility checks

The weekly schedule and `workflow_dispatch` resolve the newest stable Microsoft.Playwright NuGet version once in a dedicated job. The Windows/Linux matrix always retains **1.61.0**, including on scheduled/manual runs, and adds the resolved stable version when different. The recorded value is reused for build, browser installation, tests and package dependency metadata. These local artifact packages are upgrade evidence, not external publication or permission to raise the supported minimum automatically.

The scheduled/manual jobs also exercise headed Chrome on Linux under Xvfb and Edge on Windows. Xvfb supplies a virtual display, **not hardware GPU acceleration**. Browser version, Playwright assembly version, .NET SDK, OS, and test results are written to test logs/artifacts. Browser installation and availability failures fail the lane rather than silently falling back to a different browser.

## Run locally

```powershell
# Build and run unit/contract tests (browser tests are visibly skipped).
dotnet test SpyBrowser.sln -c Release

# Install the exact baseline browser, then run real browser tests.
dotnet build SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0
$driver = Get-ChildItem tests/SpyBrowser.Tests/bin -Filter playwright.ps1 -Recurse | Select-Object -First 1
& $driver.FullName install chromium
$env:SPYBROWSER_RUN_BROWSER_TESTS = '1'
dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0
```

On Linux install browser system dependencies with `playwright.ps1 install --with-deps chromium`; for headed tests use `xvfb-run -a` and set `SPYBROWSER_HEADED=1`. To include the actual SDK version in local browser test diagnostics, set `$env:SPYBROWSER_DOTNET_SDK_VERSION = (dotnet --version)` before running the test command. Choose an installed Playwright channel with `SPYBROWSER_BROWSER_CHANNEL=chrome` or `msedge`; install it first with `playwright.ps1 install chrome` or `playwright.ps1 install msedge`. `SPYBROWSER_HEADED` defaults to headless.

## Playwright API audit

`PlaywrightApiAuditTests` snapshots public Playwright types, interfaces, enums, members, option signatures, parameter types, and return types against `tools/verification/playwright-api-baseline.json` (approved 1.61.0) or a separately reviewed `playwright-api-baseline-<version>.json`. The 1.63.0 assessment is in `docs/implementation/playwright-api-review-1.63.0.md`; its new locator-returning property and locator function are exercised by a real-browser test. Unknown versions or changed surfaces fail rather than overwriting the minimum-version baseline. New/unknown APIs continue to be delegated by the generic wrapper; this audit is a compatibility signal, **not a claim that every Playwright API is wrapped or supported**.

Only approve a deliberate baseline update after inspecting API documentation, added options/defaults, and return-object wrapping needs:

```powershell
$env:SPYBROWSER_UPDATE_PLAYWRIGHT_API_BASELINE = '1'
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release -p:MicrosoftPlaywrightVersion=<reviewed-version> --filter FullyQualifiedName~PlaywrightApiAuditTests
```

Commit the reviewed snapshot with the corresponding compatibility assessment. Never refresh it merely to make a failed upgrade green. Browser integrations use only local test content and require no public target sites or CAPTCHA results. No retries are configured to turn browser regressions into success.
