# Windows final-source matrix — frozen 4d8a18c

This is a Windows source-test report for the frozen candidate only; it is not installed-package or publication evidence.

## Source and isolation

- Full source commit: `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`.
- Source archive: `git archive --format=tar <full-commit>`, SHA-256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`.
- Extracted source root: `D:/SpyBrowser/.scratch/windows-final-4d8a18c-source/` (under the repository, not a system temp directory). API audit root discovery therefore sees this exact archived `SpyBrowser.sln` and approved baseline files.
- Baselines: the frozen source's `tools/verification/playwright-api-baseline.json` (1.61) and `tools/verification/playwright-api-baseline-1.63.0.json` (reviewed 1.63). No baseline update flag was set and no source, runtime, tests, assertions, or deadlines were changed.
- .NET SDK `8.0.319` selected by `global.json`; runtime `8.0.22`; Windows 10 x64. The installed Chrome was `154.0.8037.93`; Edge was `154.0.4258.53`.

## Commands and execution settings

All runs used the isolated archived source and unique outputs under `artifacts/goal/windows-final-4d8a18c/`. The browser lanes set `SPYBROWSER_RUN_BROWSER_TESTS=1`, `SPYBROWSER_RUN_HEADED_PROBE_TESTS=1`, `SPYBROWSER_HEADED=1`, and `SPYBROWSER_DOTNET_SDK_VERSION=8.0.319`. Each dotnet process had a 1,800-second maximum; the test runner retained its existing 90-second blame/hang setting. No retries, assertion/deadline changes, or skip relaxation were used. NuGet packages were isolated per version under the owned output tree.

Required Playwright 1.61.0 was built and tested with:

```powershell
dotnet test SpyBrowser.sln -c Release -p:MicrosoftPlaywrightVersion=1.61.0 `
  --artifacts-path D:/SpyBrowser/artifacts/goal/windows-final-4d8a18c/required161/build `
  --logger 'trx;LogFileName=required161.trx' `
  --results-directory D:/SpyBrowser/artifacts/goal/windows-final-4d8a18c/required161/results `
  --blame-hang-timeout 90s --nologo
```

The solution run reported the two test projects independently. Because they used the same TRX name, the solution runner overwrote its first (Cursory) TRX with the product TRX; the Cursory project was immediately run separately, with the same environment and already-built outputs, to retain its own TRX. Reviewed 1.63.0 used the same solution command with `1.63.0` and the `reviewed163` output root. Its product project was then run sequentially with `--no-build --no-restore`, to retain a separate product TRX and avoid simultaneous test-project execution. Cursory was also rerun separately for its own 1.63.0 TRX.

The optional installed-channel runs used the required 1.61.0 product build, `--no-build --no-restore`, and respectively set `SPYBROWSER_BROWSER_CHANNEL=chrome` and `SPYBROWSER_BROWSER_CHANNEL=msedge`; each used a distinct results directory and TRX. All 177 discovered product tests were run in each channel lane. The test count does not assert that every individual fixture launches that channel.

## Results

| Lane | Product tests | Cursory tests | TRX |
|---|---:|---:|---|
| Required Playwright 1.61.0, bundled Chromium | 177 passed; 0 failed/skipped/errors/timeouts/aborts | 37 passed; 0 failed/skipped | `artifacts/goal/windows-final-4d8a18c/required161/results/required161.trx`; `required161/cursory-results/cursory37.trx` |
| Reviewed Playwright 1.63.0, bundled Chromium | **Initial:** 176 passed, 1 failed, 0 skipped. **Sequential full rerun:** 177 passed; 0 failed/skipped/errors/timeouts/aborts | 37 passed; 0 failed/skipped | Initial failure retained at `reviewed163/results/reviewed163.trx`; passing full rerun at `reviewed163/rerun-sequential/product177-sequential.trx`; Cursory at `reviewed163/cursory-results/cursory37.trx` |
| Chrome channel, Playwright 1.61.0 | 177 passed; 0 failed/skipped/errors/timeouts/aborts | — | `chrome177/results/chrome177.trx` |
| Edge channel, Playwright 1.61.0 | 177 passed; 0 failed/skipped/errors/timeouts/aborts | — | `edge177/results/edge177.trx` |

The reviewed-lane first attempt's sole failure was `Probe_preserves_user_pages_and_concurrent_new_pages_in_all_launch_modes`: `TimeoutException` at the existing five-second wait for the late page's `NewPageAsync()` completion signal (`ProbeIsolationBrowserTests.cs:212`); the subsequent close assertion was not reached. The probe's deliberately injected actual probe deadline in this phase is 100 ms; it was not changed. The failed TRX is retained. A focused run of that same test and source passed (1/1, 9 seconds; `reviewed163/diagnostic/probe-diagnostic.trx`), followed by the sequential complete product run (177/177). Evidence localizes the initial failure to the existing late-page completion bound under the full-suite run; it does not establish a more specific OS/browser cause. No timeout was raised and no failed result was deleted or relabeled.

The headed-probe fixture was explicitly enabled. All final successful 177-test TRXs report 177 executed and zero not-executed tests; the separate 37-test Cursory TRXs report 37 executed and zero not-executed. Counters in the retained initial reviewed failure TRX are 177 executed, 176 passed, 1 failed, 0 errors, 0 timeouts, 0 aborts, 0 not-executed.

No files in product source, tests, or other proof documents were changed. Existing root-level outputs and prior proofs were not overwritten. These results cover the Windows source matrix only; they do not claim every fixture used every browser channel or complete any other platform/package gate.
