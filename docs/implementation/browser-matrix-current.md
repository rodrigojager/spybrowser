# Current browser matrix integration evidence

Runtime product source is `1e9c3e9`; the following working-tree test mechanics changes are separately identified and must be committed before the final source/package audit. This report does not declare final release acceptance.

## Actual executions

| Scope | Environment | Result | Retained TRX directory |
|---|---|---|---|
| Required 1.61, complete suites, foreground focus enabled | Windows, .NET SDK 8.0.319 | 15 Cursory + 130 Playwright passed, 0 skipped | `artifacts/goal/required-headed-current-results/` |
| Required 1.61, complete suites, foreground focus enabled | WSL Ubuntu 20.04, .NET SDK 8.0.319, Xvfb | 15 + 130 passed, 0 skipped | `artifacts/goal/linux-required161-clean-results/` |
| Reviewed 1.63, complete suites, foreground focus enabled | Windows, .NET SDK 8.0.319 | 15 + 130 passed, 0 skipped | `artifacts/goal/latest-current-windows-results/` |
| Reviewed 1.63, complete suites, foreground focus enabled | WSL Ubuntu 24.04, Microsoft .NET SDK 8.0.319, Xvfb, non-root `spyreview` | 15 + 130 passed, 0 skipped | `artifacts/goal/ubuntu24-clock-fixed-latest-results/` |
| Required 1.61, selected native movement/input/diagnostic/integration contracts, headed stable Edge | Windows, Edge 154.0.4258.53 | 16 passed | `artifacts/goal/channel-msedge-controlled-results/` |
| Same selected scope, headed stable Chrome | Windows, Chrome 154.0.8037.93 | 16 passed | `artifacts/goal/channel-chrome-controlled-results/` |

The channel rows are selected contracts, not a claim that every test honors the channel setting. These are current-source executions, not final installed-package executions.

## Failures retained and concrete corrections

1. The optional foreground-focus test previously returned early while xUnit counted it passed. It now has an explicit `HeadedProbeFact` skip when not enabled. Earlier no-skip totals do **not** prove that headed test executed. The runs above enabled its flag; standalone actual Windows and Xvfb runs also passed in `artifacts/goal/headed-focus-current-{windows,linux}/`.
2. Page independence compared two `performance.now()` values despite different page creation/time origins. On Ubuntu 24 the difference was 1684.8 ms, with the page created earlier already having completed a typing phase. Event timestamps now use `performance.timeOrigin + performance.now()`. The existing 1500 ms assertion was retained, not widened. Original failure: `artifacts/goal/ubuntu24-complete-latest-results/`.
3. Headed Chrome/Edge introduced OS-DPI rounding even for **raw Playwright with no SpyBrowser references**: configured 1 yielded 1.0000000149011612; configured 1.5 yielded 1.5000000596046448; configured 2 yielded 2.0000000298023224. A raw control demonstrated exact 1/1.25/1.5/2 with `--force-device-scale-factor=1` normalizing the host window base DPI while each context still used its configured emulation scale. Only the geometry test fixture opts into that host normalization; its exact equality, endpoint assertions, and all four scales remain. Original failures and the sixteen raw observations: `artifacts/goal/channel-{msedge,chrome}-current-results/`, `artifacts/goal/dpr-channel-raw-control.log`. Production DPR comparison remains 0.01 and still warns for configured 1.25 versus observed 1.5.
4. Reviewed Playwright 1.63 does not support Ubuntu 20.04 downloads. No host-platform override was used to pretend support. A separate Ubuntu 24.04 distro was imported from the official Canonical WSL rootfs, SHA-256 `8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea` verified against HTTPS `SHA256SUMS`. Stored under `D:/Temp/spybrowser-ubuntu24-provision/`, distro `SpyBrowser-Ubuntu24`, leaving the original/default distro unchanged. The native latest driver/browser and stable Chrome were provisioned there. Initial missing headless shell/Chrome and unsupported-OS failures remain in the corresponding logs/TRX.
5. Switching Playwright versions in a reused Linux output directory left newer driver files beside the older .NET assembly because copy-preserve-newest did not overwrite them. Required 1.61 was rerun with a fresh, explicit `--artifacts-path`, not accepted from that mixed output. The wrong-driver failure is retained in `artifacts/goal/linux-required-clock-fixed-results/`. Version lanes must use distinct output directories.

Fresh-process default/off/Cursory controls also passed the tightened 1,000,000-byte constructor allocation ceiling on Windows and Linux, with isolated positive Cursory loading. Evidence: `artifacts/goal/default-resource-1e9c3e9-{windows,linux}/`. Their explicit dirty/source metadata remains preliminary.

Distribution rights remain unverified, external publication remains prohibited, and criterion acceptance/final-feed consumer evidence remains pending. No default promotion or publication occurred.
