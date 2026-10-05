# Linux zero-skip full-solution proof — source 5fa99de

## Outcome

Three complete Linux solution runs passed under recovered `SpyBrowser-Ubuntu24` WSL, all as non-root `spyreview` (uid 1001), using exact source archive commit `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`, SDK 8.0.319, and `--no-build --no-restore`. The exact archive remains unchanged (SHA-256 `85fcc57493d1b490ad5ce2bda137971a6a12daf276d9a29aee4c8e9da6e0d242`). All runs used `xvfb-run -a` with `SPYBROWSER_RUN_BROWSER_TESTS=1`, `SPYBROWSER_RUN_HEADED_PROBE_TESTS=1`, and `SPYBROWSER_HEADED=1`.

| Lane | Playwright / browser | Product tests | Cursory tests |
|---|---|---:|---:|
| Required | 1.61.0 | 177 passed, 0 skipped, 0 failed | 37 passed, 0 skipped, 0 failed |
| Reviewed | 1.63.0 | 177 passed, 0 skipped, 0 failed | 37 passed, 0 skipped, 0 failed |
| Chrome | 1.61.0, `chrome` channel; Google Chrome 154.0.8037.97 | 177 passed, 0 skipped, 0 failed | 37 passed, 0 skipped, 0 failed |

TRX verification: all 6 result files show total=executed=passed and zero failed, errors, timeouts, aborted, inconclusive, not-runnable, or not-executed tests. Combined: 531/531 product and 111/111 Cursory passed; zero skips. Full commands, environment, counts and per-run logs/TRX are retained in `artifacts/goal/linux-5fa99de-zero-skip/run-status.json` and its sibling lane folders (`required161`, `reviewed163`, `chrome161`).

## Prior skip and correction

The earlier full suites reported 176/177 because `SPYBROWSER_RUN_HEADED_PROBE_TESTS=1` was omitted. Their TRX identifies the sole skip as “Headed focus lane not enabled (set SPYBROWSER_RUN_HEADED_PROBE_TESTS=1).” This rerun explicitly enables that flag (alongside browser-test and headed flags); the full-solution product totals are now 177 passed, zero skipped. Historical result files were not erased or relabeled.

## Execution and scope

The Windows client invoked WSL through Python `subprocess` with a 300-second timeout and `taskkill /T /F` limited to its own WSL client process tree on timeout. No run timed out. Playwright cache was `/home/spyreview/.cache/ms-playwright`; the SDK was `/home/spyreview/dotnet-sdk-8.0.319`. Chrome lane used `SPYBROWSER_BROWSER_CHANNEL=chrome`.

The browser-channel/headed settings do not force every existing fixture to honor `HEAD` or the channel. This is a zero-skip full-solution matrix configured for those modes, not a claim that every fixture used real Chrome or ran headed. No tests/assertions, runtime, fixture defaults, retry/deadline settings, fixture RNG, or DPR were changed. Source archive hash was rechecked. Publication, preliminary-feed, and rights statuses remain unchanged.
