# Fresh-process default-resource checks

`newtools/default-resource-checks` is a BCL + official Microsoft.Playwright console probe for the Bezier/Legacy default and disabled-humanization resource/allocation contract. It does not edit or instrument product code. The observer resolves `SpyBrowser.Cursory.Internal.Dataset.Shared` with reflection and reads only `Lazy<Recording[]>.IsValueCreated`; looking up the field/type initializes the lazy wrapper, not `Lazy.Value`. A missing type, field, wrong generic type, or property fails the case (never skips it).

## What is checked

The parent starts a new OS process for each independent `default`, `off`, and `positive-control` case. Each child:

- asserts `HumanInteractionOptions` defaults to Bezier + Legacy and browser-launch `Humanize` defaults false;
- resolves `Dataset.Shared`, proves it starts not-created, and warms the observer before measuring;
- measures **only synchronous constructor-time managed allocations on the current thread** with `GC.GetAllocatedBytesForCurrentThread`: 100 `HumanActions` plus 100 `PlaywrightHumanizer` constructions (JIT/one warm construction happens before the sample). The 3 MiB aggregate ceiling is intentionally above the embedded 2,023,688-byte expanded data by 48%, but below that payload plus ordinary construction overhead; it would catch materializing the complete dataset in the measured batch. Compressed and expanded sizes and the 2,356-record count are recorded. This is not an allocation measurement of the complete application/browser and is not a latency/CPU threshold.
- launches the official Playwright Chromium driver and a real browser. Default wraps and routes mouse move, locator click, and sequential typing through the default humanizer. Off performs those same local-DOM operations without constructing a `PlaywrightHumanizer` or wrapping the raw page. Both require `Shared.IsValueCreated == false` before construction, after construction, and after browser input actions. The browser, driver, page, and their allocations are outside the constructor measurement.
- runs positive-control in its own process with Cursory selected. It anchors an initially unknown pointer, then performs a second real mouse move to create a generated trajectory, and requires `Shared.IsValueCreated == true`. This demonstrates that the negative observer detects a genuine generation-triggered load.

JSON output per child records OS/runtime, package/assembly version, source revision and preliminary dirty state, selected algorithm/compatibility mode, humanization route, before/after lazy state, measured bytes and budget, and compressed/expanded resource size. Output is evidence for the exact checked source only; the captured pre-commit dirty flag is preliminary and must not be represented as the final committed-tree state.

## Run from the repository root

Windows (Release build; separate processes and results):

```powershell
$env:DEFAULT_RESOURCE_SOURCE_REVISION = (git rev-parse HEAD)
$env:DEFAULT_RESOURCE_SOURCE_DIRTY = if (git status --porcelain) { 'dirty' } else { 'clean' }
dotnet run --project newtools/default-resource-checks/DefaultResourceChecks.csproj -c Release -- run newtools/default-resource-checks/results/windows
```

WSL/Linux uses a separate artifact root so it cannot race or reuse Windows build outputs. Example when the Linux .NET 8 SDK is installed at `/home/rodrigo/.dotnet-spybrowser`:

```bash
export PATH="/home/rodrigo/.dotnet-spybrowser:/usr/bin:/bin"
export DEFAULT_RESOURCE_SOURCE_REVISION="$(git rev-parse HEAD)"
export DEFAULT_RESOURCE_SOURCE_DIRTY="$(if test -z "$(git status --porcelain)"; then echo clean; else echo dirty; fi)"
dotnet build newtools/default-resource-checks/DefaultResourceChecks.csproj -c Release --artifacts-path newtools/default-resource-checks/.artifacts-linux
dotnet newtools/default-resource-checks/.artifacts-linux/bin/DefaultResourceChecks/release/DefaultResourceChecks.dll run newtools/default-resource-checks/results/wsl
```

Each process has a 150-second bound; timeout kills its child process tree and fails. Do not run many browser suites concurrently on a shared host. These are real browser checks; no test is skipped when Chromium/driver setup fails.

For an exact published/feed package instead of project references, set `SpyBrowserPackageVersion` to the exact version and configure the final package source in NuGet configuration, e.g.:

```bash
dotnet run --project newtools/default-resource-checks/DefaultResourceChecks.csproj -c Release -p:SpyBrowserPackageVersion=0.2.0-beta.1 -- run results/feed-candidate
```

That conditional package reference uses the normal NuGet restore source and the same probe code; the default development configuration uses project references. Preserve each package-feed run's JSON separately and record the feed/package identity. This ticket does not publish or push packages.

## Validation record

At implementation revision `06aedb1a364a9d972a846d3ecf733cba9300e0fe`, with the implementation worktree dirty (new probe/docs and generated output not yet committed), Windows and WSL each passed all three fresh-process modes using actual local Chromium. Results are retained in `newtools/default-resource-checks/results/{windows,wsl}/{default,off,positive-control}.json`. The constructor measurement was 185,600 bytes per 100 constructions in all modes. Default and off both reported the dataset uncreated before and after real browser interactions; the isolated positive control reported false before and true after generated Cursory movement. The full source dirty flag in WSL evidence is supplied from the Windows worktree metadata because this checkout's `.git` worktree pointer contains a Windows absolute path that WSL Git cannot resolve. These are preliminary source/dirty metadata, not final committed evidence.
