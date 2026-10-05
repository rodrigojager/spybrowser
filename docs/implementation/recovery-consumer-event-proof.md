# RpaBlockly consumer event proof

**Outcome:** 13.11's concrete duplicate-event gap is closed for the tested real adapter routes. The semantic API subset audit separately maps 17 named operations to assertions; aggregate check counts are not treated as semantic coverage.

## Actual adapter/event routes

Pinned, read-only RpaBlockly source at `c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca`, `src/RpaFlow.Playwright/Core/BrowserLauncher.cs`, identifies the actual adapter: `BrowserSession.Browser` is `SpyBrowserSessionBrowser`. `NewContextAsync` awaits `handle.NewContextAsync` and calls `Track`; `Track` guards a context already in the collection/closed context, saves the event handler while locked, then invokes it once. The consumer subscribes through `session.Browser.Context`—not directly to an SDK-only event—and verifies callback count and reference identity against each returned context. It removes and re-adds the same delegate between two actual context creations and asserts one increment, detecting duplicate retained subscriptions.

The same consumer subscribes to `IBrowserContext.Page` before actual `context.NewPageAsync` through the installed SpyBrowser package. It verifies one callback and that the event argument is reference-identical to both the returned wrapped page and `context.Pages.Single()`. This binds the SDK event bridge and RpaBlockly context route to a real Chromium creation sequence. It does not claim every event or every possible subscription interleaving.

The new standalone named check is `RpaBlockly Browser.Context adapter and SDK context Page events each fire once with prepared object identity across unsubscribe/resubscribe`. It is registered in `REQUIRED_CHECKS` together with the harness check, so this run honestly reports **14/14** rather than retaining the older 13-check count.

## Semantic API subset (separate audit)

See `recovery-consumer-event-proof.json` → `semanticApiSubset.items` for 17 individually named operations, exact assertion summaries, and statuses. They cover launch, context/event/page creation, page navigation/screenshot, locator click, URL/popup wait, nested frames, download, storage state, RpaRunner execution/cancellation and context closure. These rows are traced to real harness assertions/run results; the 14/14 required-check aggregate is not counted as API coverage. The mapping is a finite subset, not an exhaustive Playwright API audit.

## Reproducible evidence

From `D:/SpyBrowser-work/recovery-consumer-events`:

```text
python tools/verification/consumer-checks/run.py --feed C:/Temp/spybrowser-feed-review-5fa99de --version 0.2.0-beta.2.review.5fa99de --consumer-checkout D:/RpaBlockly --output D:/Temp/recovery-consumer-events-run2 --mouse-algorithm cursory --compatibility-mode playwright-compatible --humanize on
```

Observed: exit 0; SDK requested 10.0.302 with `latestFeature` roll-forward and resolved to 10.0.401; Chromium; `cursory` / `playwright-compatible` / Humanize on; 14 required local checks passed, no missing or unexpected check names. Pinned consumer checkout was read-only. Preliminary feed packages were consumed as-is, not repacked or published. Feed final parity is **false** (`declaredFinalCommit` is null); this is not final parity, and no Linux rights/permissions clearance is claimed. The external mixed CAPTCHA/provider suite was not run.

Retained proof outputs:

- `artifacts/goal/recovery-consumer-events/consumer-evidence.json` — package/source/SDK/run provenance, 14/14 result.
- `artifacts/goal/recovery-consumer-events/harness-evidence.json` — individual named checks and excluded-suite status.
- The corresponding SHA-256 values, package hashes, exact source boundaries, first-attempt disposition and semantic map are recorded in the JSON proof alongside this file.

The initial successful attempt accidentally invoked the main-checkout driver and produced the old 13-check result; its output remains in `D:/Temp/recovery-consumer-events-run/`. It was not used as proof. The accepted rerun invoked the driver from this new worktree and produced 14/14.
