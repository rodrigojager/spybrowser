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

## Independently executed results

The parent ran fresh Windows and WSL processes from clean commit `179a69a936059a767e188a1476528cda2f890f7e`. Linux used a Git bundle/clone of that exact commit, not a Windows worktree pointer or an overridden revision. Both reports record `sourceDirty:false`, dataset SHA-256 `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203`, .NET SDK 8.0.319/runtime 8.0.22, Playwright 1.61 and Chromium 149.0.7827.55. Each has **14/14 completed real DOM runs**, all fourteen with before/after CDP target memory measurements.

- Windows: cold first call 376.16 ms, 36,805,656 allocated bytes, managed-heap snapshot delta 18,820,968 bytes; warm p50/p95 12.01/34.24 ms, 2,943,844 bytes/trajectory.
- Ubuntu 20.04: cold 250.00 ms, 36,795,376 allocated bytes, heap delta 18,829,984 bytes; warm p50/p95 9.80/45.77 ms, 2,943,844 bytes/trajectory.

The 10 ms initial p95 investigation target was **not met** on this shared host. No hard timing gate or humanness conclusion is inferred. Reports: `artifacts/goal/benchmark-memory-{windows,linux}-current/motion-quality.{json,md}` in the parent repository. Full commands/output are retained alongside them. These are local observations, not historical installed-package comparison, a cross-OS timing-equivalence claim, exclusive per-page RSS, or CAPTCHA/detection outcomes.
