# Native motion benchmark memory measurements

The local benchmark separates memory observations from motion generation and from the timed Playwright dispatch/action interval.

## Measurement model

- **Cold Cursory first call:** a fresh benchmark process measures elapsed generation time, current-thread managed allocations around the first `Generate` call, and managed-heap/process-working-set snapshots before and after it. The generator runs before Playwright startup. Heap/working-set deltas remain whole-process diagnostics, not exclusive Cursory ownership or peak memory.
- **Warm generation:** the existing 40 generation-only runs retain their p50/p95 timing and current-thread allocation measurement. This does not include browser dispatch.
- **Per case:** benchmark-process managed heap and working set are sampled before/after each algorithm/case group. They include Playwright, driver/runtime state, and all concurrent pages; they cannot be apportioned to an individual page or SDK state.
- **Per page/target:** before and after the real action, the benchmark requests CDP `Runtime.getHeapUsage` and `Performance.getMetrics`. These calls are outside the action stopwatch. They describe target V8/Performance values, not page-exclusive browser RSS; Chromium may share renderer processes between targets. JSON labels `Runtime.getHeapUsage` byte fields as bytes and retains the raw Performance metric names with their CDP-defined units (for example, timestamps/durations in seconds, heap sizes in bytes, and counters as named).
- **SDK state allocation:** no per-page SDK-state allocation is claimed. The benchmark cannot isolate it honestly from Playwright dispatch/runtime allocations, and process-wide deltas are not used as a proxy.

The existing 14 real DOM observations, mouse endpoint, hover/click task-completion checks, timing metrics, and validation remain unchanged. Numeric validation rejects non-finite measured values. No peak or private-hardware measurement is inferred from snapshots.

## Running

From a clean source commit, run in a fresh process on each OS with different unique output directories. For WSL use the Linux .NET installation explicitly and do not reuse Windows `bin`, `obj`, NuGet caches, or report files. The report captures the actual Git `HEAD`, dirty state, OS/runtime/browser versions, dataset hash, and environment; do not overwrite metadata or assert a revision that Git did not report. The Windows and WSL JSON/Markdown reports belong under `artifacts/goal/benchmark-memory-proof/` and should remain uncommitted unless requested.

```powershell
dotnet run --project tools/native-motion-benchmark/NativeMotion.Benchmark.csproj -c Release -- artifacts/goal/benchmark-memory-proof/windows
```

```bash
PATH=/home/rodrigo/.dotnet-spybrowser:$PATH dotnet run --project tools/native-motion-benchmark/NativeMotion.Benchmark.csproj -c Release -- artifacts/goal/benchmark-memory-proof/wsl
```

Each successful report must record 14/14 real DOM runs and all pages completed. These are local observations, not historical package-consumer evidence, a cross-OS equivalence claim, or a promise about trajectory reproducibility or CAPTCHA/humanness outcomes.
