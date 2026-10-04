# RpaBlockly real-consumer package verification

`tools/verification/consumer-checks/run.py` creates a **temporary, isolated copy** of the pinned RpaBlockly checkout (`c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca`), changes only the `SpyBrowser.Playwright` package version in `src/RpaFlow.Playwright/RpaFlow.Playwright.csproj`, adds a local NuGet source, and adds `tests/ConsumerContract`. It exercises RpaBlockly's actual public `BrowserLauncher` and its `SpyBrowserSessionBrowser` adapter via the package. It does not use a SpyBrowser project reference, modify the original checkout, or include the Cloak compatibility facade.

Run from the SpyBrowser checkout (a local package feed is created under output):

```sh
python tools/verification/consumer-checks/run.py \
  --consumer-checkout C:/Users/Rodrigo/AppData/Local/Temp/spybrowser-rpablockly-review \
  --version 0.2.0-beta.1 \
  --output artifacts/consumer-checks
```

The candidate package is packed from the current SpyBrowser checkout before consumer restore. The consumer checkout contains `global.json` requesting .NET SDK `10.0.302` with `latestFeature` roll-forward; SDK 8 at SpyBrowser root is not suitable for the net9 consumer. Playwright 1.61.0 and all original provider references are retained. Chromium must be installed for Microsoft.Playwright in the isolated consumer environment. `--feed`, `--version`, `--consumer-checkout`, and `--output` allow re-running against a later exact candidate. For a preliminary package-only build, use `--package-only`; that is not consumer parity evidence.

`evidence.json` records source commit, consumer commit, package hash/version, SDK policy, browser and Playwright pin, exact invocation, exit status and artifact paths. `harness-evidence.json` separates passing checks from explicitly incomplete contract cases. Only local BrowserLauncher/adapter flows are claimed: humanize on/off, memory identity/context event and collection identity, locale/timezone/viewport, context close, screenshot, navigation, popup, nested frame, and loopback download. Fill cancellation/lifetime cleanup, delayed launch cleanup, parallel-job isolation, runner cookie restore and full V1/V2 runner outputs remain incomplete; the large existing check program mixes CAPTCHA/provider behavior and is deliberately not represented as an all-green suite. Re-run against the final integrated package and retain both evidence files with the candidate artifacts.
