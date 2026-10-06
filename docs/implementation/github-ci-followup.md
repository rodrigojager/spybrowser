# GitHub CI follow-up

Status: implemented locally; no commit, push, tag, or publication was performed.

## Source boundary

- `global.json`: SDK 8.0.319 now uses `rollForward: disable`.
- `.github/workflows/ci.yml`: both CI SDK setup steps use `global-json-file: global.json`. Playwright versions, filters, SDK recording, and browser opt-in settings remain unchanged.
- `tests/SpyBrowser.Tests/BrowserIntegrationTests.cs`: retain exact requested timezone assertions; compare browser-resolved timezone against independently queried native `Intl.DateTimeFormat` canonicalization. Add UTC and Etc/UTC browser theory cases.
- No production C#, datasets, release workflow, feeds, or package binaries were changed.

## Local validation

Ran with SDK `8.0.319` and `SPYBROWSER_RUN_BROWSER_TESTS=1`:

```text
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --filter 'FullyQualifiedName~Browser_timezone_matches_native_intl_canonicalization|FullyQualifiedName~Effective_callbacks_are_reported_for_persistent_context_and_browser_page_modes' --logger 'trx;LogFileName=timezone-focused.trx' --results-directory artifacts/goal/github-ci-followup/test-results -p:MicrosoftPlaywrightVersion=1.61.0
```

Exit code 0; 3 passed, 0 failed, 0 skipped. TRX: `artifacts/goal/github-ci-followup/test-results/timezone-focused.trx`; bounded command log: `artifacts/goal/github-ci-followup/timezone-focused.log`.

Passed test cases:

- `SpyBrowser.Tests.BrowserIntegrationTests.Browser_timezone_matches_native_intl_canonicalization(requestedTimezone: "UTC")` (test ID `ab06633c-af62-4923-8d3d-fbe90dfe4d24`)
- `SpyBrowser.Tests.BrowserIntegrationTests.Browser_timezone_matches_native_intl_canonicalization(requestedTimezone: "Etc/UTC")` (test ID `0199721b-af46-1d22-1b10-b48af0d4245f`)
- `SpyBrowser.Tests.BrowserIntegrationTests.Effective_callbacks_are_reported_for_persistent_context_and_browser_page_modes` (test ID `481d768d-4b7d-96c1-13a4-f227312ce6bd`)

## External blocker to report

GitHub Actions artifact uploads failed due to account storage quota. Exact logged error:

```text
Failed to CreateArtifact: Artifact storage quota has been hit. Unable to upload any new artifacts. Usage is recalculated every 6-12 hours.
More info on storage limits: https://docs.github.com/en/billing/managing-billing-for-github-actions/about-billing-for-github-actions#calculating-minute-and-storage-spending
```

This is an external administrative issue. No artifact upload behavior or evidence collection was weakened, and the failed release run published no artifacts. Parent/user action is required to resolve quota; preserve the original CI logs.
