# Native mouse movement benchmark (local observation)

Run with:

```sh
dotnet run --project tools/native-motion-benchmark/NativeMotion.Benchmark.csproj -c Release
```

This is a reproducible local diagnostic, not a CI threshold. It separates the first generation call from warmed generation, measures current-thread managed allocations across 100 trajectories, then observes DOM `mousemove` timestamps during a wrapped Cursory move. The DOM sample includes the documented one-point unknown-position anchor. It reports wall-clock time and browser-delivered events, not just the planned trajectory timestamps.

Observed during implementation in this worktree:

```text
Machine=DESKTOP-KLQV3JP; OS=Microsoft Windows 10.0.19045; ProcessArch=X64; Framework=.NET 8.0.22
Cursory assembly=0.2.0.0; Playwright=1.61.0.0; coldMs=290.506; warmRuns=100; warmMeanMs=5.385; allocatedBytesPerTrajectory=868135; coldPointCount=48
Browser=149.0.7827.55; generationAndDispatchWallMs=898.587; domMousemoveCount=40; domFirstMs=16.700; domLastMs=16.500
```

Values vary with machine load, OS scheduling, browser installation, and process state. The allocation count is a managed current-thread delta, not total process allocation. No performance target or real-time guarantee is inferred. This baseline is intended to feed the later benchmark/quality ticket; repeat on a documented reference machine before comparing revisions.
