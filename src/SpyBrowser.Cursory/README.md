# SpyBrowser.Cursory (experimental preview)

Standalone .NET 8 library for generating recorded mouse trajectories. It is not
integrated into SpyBrowser.Playwright and must not be interpreted as enabling a
Cursory algorithm in SpyBrowser automation.

```csharp
using SpyBrowser.Cursory;
var trajectory = CursoryTrajectoryGenerator.Generate(
    new TrajectoryPoint(20, 30), new TrajectoryPoint(500, 280),
    new TrajectoryOptions { Seed = 42, Frequency = 60 });
for (var i = 0; i < trajectory.Points.Count; i++)
    Console.WriteLine($"{trajectory.Timings[i]} ms: {trajectory.Points[i]}");
```

The DLL has no runtime dependency other than the .NET 8 BCL and carries a
compressed recording dataset. RNG state is per generation; the dataset is
validated and loaded once. The current differential suite matches 160 pinned
cursory-js trajectory cases at 1e-9 absolute pixel tolerance, plus exact PCG64
and distribution vectors. This is fixture-scoped evidence, not a guarantee of
exhaustive parity; see `docs/cursory-port.md` for incomplete criteria and the
benchmark baseline.

Package metadata declares LGPL-3.0-or-later, overriding repository-wide MIT
metadata. Algorithm source files are included under `src/`; a corresponding
source bundle still needs the embedded gzip input and build instructions that
recreate the exact package. Required full GPL/third-party license texts also
remain absent. See `LICENSE`, `COPYING.LESSER`, `NOTICE`, and
`Data/upstream-manifest.json`. Dataset redistribution remains gated pending
independent provenance/legal review. This project is not cleared for external
release.
