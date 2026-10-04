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
validated and loaded once. Seeded exact upstream parity has **not yet been
established** (see `docs/cursory-port.md`); do not rely on this preview for
bitwise reproducibility against cursory-js until the differential tests pass.

Package license is LGPL-3.0-or-later, overriding repository-wide MIT metadata.
See `LICENSE`, `COPYING.LESSER`, `NOTICE`, and `Data/upstream-manifest.json`.
Dataset redistribution remains gated pending independent provenance/legal
review. This project is not cleared for external release.
