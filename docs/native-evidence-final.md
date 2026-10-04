# Native evidence closure: tickets 01/03/04/05/21

Scope is limited to native motion benchmark tooling, tests, and documentation. `HumanActions`, `PlaywrightHumanizer`, and `PageInputState` were not changed. This is an evidence closeout, not a legal clearance or a claim of a fully releasable package.

## Evidence and criterion mapping

- **01.1 / 21.1, .2, .5, .8:** the benchmark runs the same seeded local tasks on Bezier and Cursory: real DOM movement, stable-button hover and click, short/subpixel, long, acknowledged 12 ms pre-dispatch delay, and three independent concurrent pages. It asserts 14 completed chronological DOM runs. It does not yet provide the requested before/after historical movement+hover+click artifact against `e217359` using an isolated package consumer. The current in-process Bezier/Cursory comparison is not a substitute for that historical comparison; no defaults were switched and no historic performance values were fabricated.
- **03.10/.11:** algorithm fixtures and Windows/Linux differential behavior remain separate from this benchmark; warm p95, allocations, and dispatch are measured independently. Classification and source/dirty provenance are part of schema v2. A 10 ms p95 remains an investigation target, never a shared-runner hard gate.
- **04.2 / 05.5:** controlled Windows Chromium DPR 1, 1.25, 1.5, and 2 tests pass sequentially (7 native movement tests total, including real page close and held-button behavior). The retained prior failure remains in `artifacts/goal/wave4-fixed/...01_44_31[1].trx` and is not overwritten. The controlled run passed under low concurrency; this does not disprove that the earlier shared-load timeout occurred.
- **04.9:** an actual browser page close during an in-flight Cursory movement interrupts the pending action; this test complements, not replaces, the existing fake-clock close/cancel/deadline tests.
- **05.3:** fixed-seed strategy test verifies timestamp resizing preserves the selected point sequence/order and sample count (frequency), while resizing the terminal duration to the configured 1000 ms.
- **05.8:** wrapper-observed button down is preserved through movement; the browser test inspects DOM `MouseEvent.buttons`. An unwrapped external down/up cannot be observed or queried, so documentation recommends raw routing while another caller may own buttons, forbids blanket release, and does not pretend invalidation learns button state.
- **21.3/.4/.6/.7:** benchmark v2 includes exact source revision, dirty status, seed, dataset hash, environment, cold/warm generation and current-thread allocation. Browser-phase allocations and working set remain aggregate process snapshots, not exclusive per-page or peak attribution. Classifications explicitly separate functional correctness, descriptive performance, contextual evidence, and what qualifies as a regression. No regression verdict is possible without a same-context paired historical baseline.

## Current measured run

`artifacts/native-evidence/current-run/motion-quality.json` is a real Windows 10 / .NET 8.0.22 / Playwright 1.61 / Chromium 149 run with seed 21021 and all 14 real DOM cases. It captured source `5890749dc7fbdac40d0876e8f8dace07b8d36735` and listed the worktree dirty state at measurement time (benchmark/test/docs edits). Cursory cold load+generation measured 249.81 ms; warm p50/p95 was 9.14/24.16 ms across 40 trajectories, with 2,943,918 managed bytes allocated per trajectory. The p95 and allocation warrant optimization/investigation work; they are not a test failure threshold. Browser performance is context-specific and no 10 ms hard gate is applied.

The all-DPR command ran without a competing full suite, max test host count 1:

```powershell
$env:SPYBROWSER_RUN_BROWSER_TESTS = '1'
dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~NativeCursoryMovementTests' --logger 'console;verbosity=minimal' -- RunConfiguration.MaxCpuCount=1
```

Observed result: **7 passed, 0 failed, 0 skipped**, 10 s. The matrix exercises all four DPR values, sample-resize invariant, close-mid-movement, and observed held-button preservation. Scheduler fake-clock coverage remains in `MonotonicMovementSchedulerTests`; no scheduler/product change was made. The old DPR2 timeout artifact is retained as a failure. The controlled run cannot attribute that earlier timeout conclusively beyond its full-suite/concurrent-browser context; it does show the case passes in this isolated low-concurrency run. Do not raise the production deadline based on either run.

## Remaining blockers / limits

1. No Linux SDK is installed in the available WSL Ubuntu environment (`dotnet: command not found`), so this task could not execute Linux tests/benchmark or provide the requested Windows/Linux stage differential report. Do not infer Linux success from historical logs. Run on an equipped Linux lane.
2. The historical `e217359` baseline versus candidate movement+hover+click comparison through an isolated package consumer was not executed or fabricated. Existing package evidence is historical/stale relative to current source. Exact source/package binding and testability must precede claiming parity.
3. Dataset chain-of-title and redistribution rights are still unverified. **External distribution remains BLOCKED**, as ticket 02 accepts; no legal clearance is claimed.
4. Current benchmark measures benchmark-process aggregate allocation/working set and per-page JS heap snapshots only. It cannot attribute browser-process peak memory per page.

## Validation performed

- `dotnet restore tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj` — succeeded for the assigned worktree.
- `dotnet test ... --filter 'FullyQualifiedName~NativeCursoryMovementTests'` without browser opt-in — 1 unit pass, 3 browser tests skipped; skips are reported rather than treated as browser evidence.
- Browser-enabled native suite command above — 7 pass, including full controlled DPR matrix.
- `dotnet run --project tools/native-motion-benchmark/NativeMotion.Benchmark.csproj -c Release --no-restore -- artifacts/native-evidence/current-run` — wrote JSON/Markdown; all 14 cases completed and validated, including DOM hover and click.
- `wsl.exe bash -lc 'cd /mnt/d/SpyBrowser-work/native-evidence-final && dotnet --version'` — blocked: no Linux `dotnet` executable in the available WSL image.
