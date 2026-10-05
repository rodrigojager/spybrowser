# Recovered Linux homologation — 5fa99de

## Result

Homologation resumed in the recovered `SpyBrowser-Ubuntu24` WSL distro as non-root `spyreview` (uid 1001); the reviewed Playwright 1.63 root runs used uid 0 in the same distro. The recovery probe in `artifacts/goal/executor-recovery/post-restart-probes.json` records both `wsl --list --verbose` and the distro probe as exit 0 (7.31 s for `echo/id/dotnet/df/date`). No further WSL service restart was made, and Ubuntu/Docker were not started.

All source-linked verification used the Git archive of product/package source `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a` (full SHA), not the later fixture-only HEAD. Archive SHA-256: `85fcc57493d1b490ad5ce2bda137971a6a12daf276d9a29aee4c8e9da6e0d242`; it is retained at `artifacts/goal/linux-5fa99de-required161/source-5fa99de.tar`. The preliminary package feed identifies that exact commit and archive hash; package version is `0.2.0-beta.2.review.5fa99de`, Playwright 1.61.0, `isFinal=false`, `externalPublicationAllowed=false`.

At first, plain `dotnet --version` from the checkout reported the SDK resolver mismatch: global.json requests 8.0.319 while only 8.0.131 was installed. No SDK version was silently substituted. The exact 8.0.319 SDK archive was fetched from `https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.319/dotnet-sdk-8.0.319-linux-x64.tar.gz` (SHA-256 `7896db0c14acd75618da090e5a69b8564405bfc0fac4e2d21d10cbeda14ed209`), retained at `artifacts/goal/linux-5fa99de-required161/sdk-8.0.319.tar.gz`, and extracted to `/home/spyreview/dotnet-sdk-8.0.319`. `dotnet --version` then returned exactly `8.0.319`. Builds and tests used this SDK and the exact 5fa99de archive source. Logs and TRX files are retained alongside each matrix result.

## Commands and observed test outcomes

All commands below ran in `SpyBrowser-Ubuntu24`; the 1.61.0 runs used non-root `spyreview`. Common environment: `DOTNET_ROOT=/home/spyreview/dotnet-sdk-8.0.319`, `PATH=/home/spyreview/dotnet-sdk-8.0.319:/usr/bin:/bin`, `HOME=/home/spyreview`, `NUGET_PACKAGES=/home/spyreview/.nuget/packages`.

### Required Playwright 1.61.0

From `/home/spyreview/linux-5fa99de-source` (the unpacked exact source archive):

```sh
dotnet build SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0 --nologo
SPYBROWSER_RUN_BROWSER_TESTS=1 SPYBROWSER_DOTNET_SDK_VERSION=8.0.319 \
  dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0 \
  --no-build --logger trx --results-directory /home/spyreview/linux-5fa99de-required161/results \
  --blame-hang-timeout 90s --nologo
```

Build: succeeded, 0 warnings/errors. Full solution: `SpyBrowser.Tests` 176 passed, 0 failed, 1 skipped (177 total); `SpyBrowser.Cursory.Tests` 37 passed, 0 failed (37 total). The sole skip was the explicitly headed focus probe, not a test failure or masked browser-test skip; that exact probe passed in the Xvfb run below. TRX files: `artifacts/goal/linux-5fa99de-required161/results/`; full log: `test.log`.

### Reviewed Playwright 1.63.0 isolated and root evidence

Built the same source archive with `-p:MicrosoftPlaywrightVersion=1.63.0` (0 warnings/errors; `artifacts/goal/linux-5fa99de-reviewed163/build.log`). The isolated API audit command was run with `SPYBROWSER_RUN_BROWSER_TESTS=1`:

```sh
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release \
  -p:MicrosoftPlaywrightVersion=1.63.0 --no-build \
  --filter FullyQualifiedName~PlaywrightApiAuditTests --logger trx \
  --results-directory /home/spyreview/linux-5fa99de-source-163/isolated-results --nologo
```

Observed: 2 passed, 0 failed, 0 skipped. (An earlier diagnostic invocation without the browser-test enable flag skipped its browser audit; it was not counted as evidence and was rerun correctly.)

The full 1.63.0 suite was run as root with browser tests enabled: 176/177 product tests passed, 0 failed, one headed-probe skip; Cursory 37/37 passed, 0 failed/skips. The exact full-suite command (with root environment set as described) was:

```sh
dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.63.0 \
  --no-build --logger trx --results-directory /home/spyreview/linux-5fa99de-source-163/root-results \
  --blame-hang-timeout 90s --nologo
```

The skipped headed probe was then executed as root under Xvfb and passed (1/1, 0 failed/skipped):

```sh
SPYBROWSER_RUN_BROWSER_TESTS=1 SPYBROWSER_RUN_HEADED_PROBE_TESTS=1 \
SPYBROWSER_HEADED=1 PLAYWRIGHT_BROWSERS_PATH=/home/spyreview/.cache/ms-playwright \
  xvfb-run -a dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release \
  -p:MicrosoftPlaywrightVersion=1.63.0 --no-build \
  --filter FullyQualifiedName~Headed_probe_does_not_change --logger trx --nologo
```

Root full-suite and headed-probe TRX/log evidence: `artifacts/goal/linux-5fa99de-reviewed163/root-results/`, `root-headed-results/`, `root-test.log`, `root-headed-xvfb.log`. Isolated audit evidence: `isolated-results/`, `isolated.log`.

### Chrome and headed Xvfb

With Playwright 1.61.0 and installed `/usr/bin/google-chrome-stable`, the full suite command used `SPYBROWSER_RUN_BROWSER_TESTS=1 SPYBROWSER_BROWSER_CHANNEL=chrome`: product 176 passed, 0 failed, 1 headed-only skip (177 total); Cursory 37 passed, 0 failed/skips. The exact test command was `dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0 --no-build --logger trx --results-directory /home/spyreview/linux-5fa99de-source/chrome-results --blame-hang-timeout 90s --nologo`. The separate Chrome headed probe was run under `xvfb-run -a` with `SPYBROWSER_RUN_HEADED_PROBE_TESTS=1 SPYBROWSER_HEADED=1 SPYBROWSER_BROWSER_CHANNEL=chrome`; it passed 1/1, 0 failures/skips. Evidence: `artifacts/goal/linux-5fa99de-chrome/results/`, `headed-results/`, `chrome-full.log`, `headed-xvfb.log`.

The full-suite one-skip entries above are reported explicitly; each skipped headed probe was separately executed and passed under Xvfb. TRX records show zero failed, errored, timed-out, or aborted cases; the single xUnit-skipped headed probe in each full suite is shown explicitly above and has its own passing, zero-skip Xvfb TRX.

## Installed package proof

Ran the actual `tools/verification/distribution-checks/run.py` as non-root, using the existing preliminary candidate feed, pinned baseline feed `e217359d19a29635f2b3b5ba54664d299fd16d36`, its parent manifest, and the local NuGet dependency cache. Literal invocation:

```sh
python3 tools/verification/distribution-checks/run.py \
  --feed /mnt/c/Temp/spybrowser-feed-review-5fa99de \
  --candidate-version 0.2.0-beta.2.review.5fa99de \
  --expected-source-commit 5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a \
  --feed-manifest /mnt/c/Temp/spybrowser-feed-review-5fa99de/feed-manifest.json \
  --previous-feed /mnt/d/Temp/spybrowser-baseline-feed-e217359/feed \
  --previous-feed-manifest /mnt/d/Temp/spybrowser-baseline-feed-e217359/baseline-manifest.json \
  --previous-version 0.1.0-baseline.e217359 \
  --dependency-feed /mnt/c/Users/Rodrigo/.nuget/packages \
  --repository /mnt/d/SpyBrowser --output /mnt/d/SpyBrowser/installed-5fa99de-linux-proof
```

The run independently verified source archive/package provenance and exercised installed CLI, Cursory/Bezier/offline modes, snapshot round-trip and schema checks, real permission denial, OS-terminated save during SDK write, GPU-context information, rollback, and schema-999 preservation/old-package ignore behavior. All 13 technical checks passed (`technicalAllPassed=true`); `installed-5fa99de-linux-proof/distribution-evidence.json` and `run.log` contain command-level evidence and hashes. The per-run environment, TRX counters, run names/times, and all non-passed (including explicitly skipped) results are summarized in each matrix folder's `run-status.json`.

The required external dataset redistribution-license clearance remains blocked, so the proof intentionally records `allPassed=false` and `externalPublicationAllowed=false`. This is not a technical failure and is not permission to publish, finalize, or repackage the candidate.

## Scope and status

Only the three assigned `artifacts/goal/linux-5fa99de-{required161,reviewed163,chrome}` folders, `installed-5fa99de-linux-proof/`, and this document were written in the workspace. No runtime, tests, canonical ledger, or other documents were changed. The Linux package-linked runs used exact 5fa99de; no separate d574f6 verification was needed because the 5fa99de suites had no fixture failures (the separately targeted headed probe passed).
