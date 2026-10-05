# Linux final-source zero-skip proof — `4d8a18c`

## Result

Three full-solution Linux lanes were built and run from fresh extractions of the frozen Git archive at `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`. Each lane executed all 177 product and 37 Cursory tests: **531/531 product and 111/111 Cursory passed; zero skips, failures, errors, timeouts, aborted, inconclusive, not-runnable, or not-executed tests**. The TRX `total` equals `executed` and `passed` in all six result files.

| Lane | Playwright | Browser configuration | Product | Cursory |
|---|---|---|---:|---:|
| Required | 1.61.0 | bundled/default | 177 passed, 0 skipped | 37 passed, 0 skipped |
| Reviewed | 1.63.0 | bundled/default; source archive's `tools/verification/playwright-api-baseline-1.63.0.json` | 177 passed, 0 skipped | 37 passed, 0 skipped |
| Chrome | 1.61.0 | `SPYBROWSER_BROWSER_CHANNEL=chrome`, Google Chrome 154.0.8037.97 | 177 passed, 0 skipped | 37 passed, 0 skipped |

Each suite ran headed configuration under `xvfb-run -a` with `SPYBROWSER_RUN_BROWSER_TESTS=1`, `SPYBROWSER_RUN_HEADED_PROBE_TESTS=1`, and `SPYBROWSER_HEADED=1`. The channel/headed settings configure the intended lane but do not force every existing fixture to honor them; this is not a claim that every fixture ran in real Chrome or headed mode.

## Frozen source and environment

- Archive: `D:/Temp/spybrowser-linux-final-4d8a18c.zip`, generated with `git archive --format=zip` from the specified commit; SHA-256: `2a162b4210353a44bfbb57eaf26a56197602900824f629f1483c19ec86c91f26`.
- Separate clean source extractions: `/home/spyreview/linux-final-4d8a18c-source` and `/home/spyreview/linux-final-4d8a18c-source-163`.
- Distro/OS/architecture: `SpyBrowser-Ubuntu24`, Ubuntu 24.04 x86_64; account `spyreview`, uid 1001 (non-root).
- SDK: 8.0.319 at `/home/spyreview/dotnet-sdk-8.0.319`; archived `global.json` selects 8.0.319 (`latestFeature`).
- Browser cache: `/home/spyreview/.cache/ms-playwright`; installed browsers include Chromium 1228/1243. Existing NuGet package cache was reused without clearing it. No global service/restart was performed.
- Each run restored and built its own source tree, then tested it with `--no-build --no-restore`. All builds succeeded with zero warnings and errors.

## Commands and evidence

Per lane, executed in the lane source directory (with `DOTNET_ROOT=/home/spyreview/dotnet-sdk-8.0.319`, SDK added to `PATH`, `PLAYWRIGHT_BROWSERS_PATH=/home/spyreview/.cache/ms-playwright`, and the lane variables above):

```text
dotnet restore SpyBrowser.sln -p:MicrosoftPlaywrightVersion=<1.61.0|1.63.0> --nologo
dotnet build SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=<1.61.0|1.63.0> --no-restore --nologo
xvfb-run -a dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=<1.61.0|1.63.0> --no-build --no-restore --logger trx --results-directory /mnt/d/SpyBrowser/artifacts/goal/linux-final-4d8a18c/<lane>/results --nologo
```

The Windows Python launcher bounded its own WSL client to 1800 seconds and would kill only that client PID tree on timeout; none timed out. Detailed exact commands/environment/counts are in `artifacts/goal/linux-final-4d8a18c/run-status.json`; each lane's `test.log` contains restore/build/test output and its `results/` folder retains both full-solution TRX files.

The first setup invocation stopped before tests because `unzip` was unavailable. It was diagnosed and extraction switched to Python's standard-library `zipfile` before running any test lane. There were no test failures to retry. No repository tests, `src`, feed or ledger/docs outside the assigned proof paths were modified; no retries, fixture skips, or test tolerance changes were applied.
