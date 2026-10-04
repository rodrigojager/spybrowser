# Diagnostics gap proof: resource contract and missing WebGL

This change adds evidence for two audited gaps without changing runtime behavior, existing tests, or the renderer privacy projection.

## Disabled storage and bounded hot-path allocations

`DiagnosticsResourceContractTests.Disabled_recorder_has_no_storage_and_allocation_comparison_is_observable` compares the same warmed `IPage.TitleAsync` proxy-call workload with diagnostics disabled and enabled. It uses a completed fake `Task<string>` and calls `GetAwaiter().GetResult()` synchronously from the measurement thread, so `GC.GetAllocatedBytesForCurrentThread` observes the complete wrapper/recorder path on one thread. Each mode receives 512 warm-up calls and 8,192 measured calls. It prints total and per-call allocated bytes; enabled mode uses recorder capacity 32 and verifies that exactly 32 records are retained and the remainder counted as dropped. It also verifies disabled mode has neither snapshot nor recorder.

These values measure incremental cost against this fake-page/wrapper baseline only. The test imposes no arbitrary byte ceiling and does not claim a universal browser, protocol, or total humanized-action cost. The recorder's bounded storage claim is separately asserted by exact retained/dropped counts.

## Real browser without WebGL

`MissingWebGLBrowserTests.Disabled_WebGL_is_live_unverified_warning_not_hardware_failure_and_leaves_user_pages_untouched` is an opt-in browser integration test, not a fabricated diagnostics object. It launches the actual configured Chromium with `--disable-webgl` and `--disable-webgl2`, requests the normal isolated GPU probe, and selects `GpuPolicy.RequireHardware`. It requires both observed WebGL contexts to be unavailable and `gpu.webgl-unavailable` to be a Warning. It rejects an inferred `gpu.hardware-required` error, confirms `FailOnConsistencyErrors`' default remains non-blocking for this warning, and checks the probe left no user or context pages behind. The test does not establish physical GPU availability or invent hardware policy from missing observations.

## Validation record

Source base: `1e9c3e9f0e2ec8c24960fee47b355b2a05d6fd56`. Both lanes built the added tests and ran the real missing-WebGL browser test with `SPYBROWSER_RUN_BROWSER_TESTS=1`.

- Windows 10, .NET SDK 8.0.319, artifacts in `D:/SpyBrowser-work/diagnostic-gap-proof/artifacts/diagnostic-gap-proof/windows`:
  `SPYBROWSER_RUN_BROWSER_TESTS=1 dotnet test D:/SpyBrowser-work/diagnostic-gap-proof/tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --artifacts-path D:/SpyBrowser-work/diagnostic-gap-proof/artifacts/diagnostic-gap-proof/windows -p:RestoreIgnoreFailedSources=true --filter 'FullyQualifiedName~DiagnosticsResourceContractTests|FullyQualifiedName~MissingWebGLBrowserTests' --logger 'console;verbosity=normal' --blame-hang-timeout 90s` — **2 passed, 0 failed, 0 skipped**. A separate detailed-logger allocation rerun passed (**1 passed**) and reported **6,160,544 B total / 752.02 B per call disabled; 13,238,592 B / 1,616.04 B per call enabled**; capacity 32, retained 32, dropped 8,672.
- Ubuntu 20.04 WSL, .NET SDK 8.0.319 from `/home/rodrigo/.dotnet-spybrowser`; source copy `/home/rodrigo/diagnostic-gap-proof-1e9c3e9`; explicitly set `HOME=/home/rodrigo`, `DOTNET_ROOT=/home/rodrigo/.dotnet-spybrowser`, and `PATH=/home/rodrigo/.dotnet-spybrowser:/usr/local/bin:/usr/bin:/bin`; artifacts in the Linux copy's `artifacts/linux`:
  `SPYBROWSER_RUN_BROWSER_TESTS=1 dotnet test /home/rodrigo/diagnostic-gap-proof-1e9c3e9/tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --artifacts-path /home/rodrigo/diagnostic-gap-proof-1e9c3e9/artifacts/linux --filter 'FullyQualifiedName~DiagnosticsResourceContractTests|FullyQualifiedName~MissingWebGLBrowserTests' --logger 'console;verbosity=normal' --blame-hang-timeout 90s` — **2 passed, 0 failed, 0 skipped**. A separate detailed-logger allocation rerun passed (**1 passed**) and reported **6,160,544 B total / 752.02 B per call disabled; 13,238,592 B / 1,616.04 B per call enabled**; capacity 32, retained 32, dropped 8,672.

The allocation figures are from the fake-page same-thread wrapper workload only; they are not a universal browser cost or performance ceiling. The live WebGL-off test succeeded in both Windows and Linux browser lanes. No full-suite, headed-focus, physical-GPU, publication, or distribution result is claimed. The Linux validation used an owned copy of the exact test sources at the stated source base; the final validation summary was added to this document afterward. Product code and existing tests were not changed.
