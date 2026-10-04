# Direct-helper lifecycle integration review

The public `HumanActions` signatures are unchanged. They enter the same per-raw-page `PageInputState` as wrapped routing; routing invokes internal cores while already leased. Public arguments are unwrapped before admission and dispatch, preventing nested proxy reentry. Confirmed cursor/button knowledge is weakly shared per raw page across direct helpers and humanizer instances. Explicit invalidation affects all those SDK-visible entry points; unobserved raw input still requires caller invalidation and does not justify releasing unknown buttons.

`DirectWrappedHelpersTests` runs every public input helper with already-wrapped arguments in both Legacy and PlaywrightCompatible modes. It also verifies a direct Cursory move establishes knowledge for another scope and invalidation restores the single native anchor. Existing direct-versus-wrapped serialization, separate-page parallelism and action-contract assertions remain.

## Preserved failures and test mechanics

- The stochastic thinking default can consume a 180 ms typing-test budget after one key. The fixed-cadence deadline test now sets thinking probability to zero, retaining its original 2–12 prefix and 100–1500 ms assertions. The independent lifecycle test still requires cancellation under 500 ms and legacy page closure. No production timeout or test tolerance was increased.
- An internal owned deadline can fire before its outer linked timer callback. Legacy closure now distinguishes SDK-owned cancellation from caller/lifetime cancellation and explicit native timeout ownership. The unchanged legacy closure assertion caught this race in the first integrated run.
- Immediate closure from a raw native context-created handler can remove a Playwright context channel before `Browser.NewContextAsync` initializes HAR options. Pinned 1.61 source dereferences the returned channel at that point. The recorded NRE at that exact native frame is not translated or swallowed by product code. The race test accepts it only with a captured closed/removed context, an existing close task and that native stack frame; it still requires completed closure and zero closed-context announcements. Unrelated/connected-context NREs propagate. Source fetched from `https://raw.githubusercontent.com/microsoft/playwright-dotnet/v1.61.0/src/Playwright/Core/Browser.cs` is retained at `artifacts/goal/playwright-1.61-browser-source.cs`.

## Observed integration validation

Windows and WSL required Playwright 1.61 each executed **15 Cursory + 130 Playwright tests**, all passed, no skips. Evidence: `artifacts/goal/direct-merge-full-fixed-results/`, `artifacts/goal/linux-direct-helper-review/`. These were working-tree validation runs, not a declaration of final package/source acceptance. The earlier failing runs remain in `artifacts/goal/direct-merge-full-results/` and `artifacts/goal/lifecycle-merge-required-results/`.

Generated `bin`/`obj` outputs in completed lanes were inventoried and removed after available disk fell below 1 GiB. Logs/TRX/dumps and package feeds were preserved; any such evidence inside an output directory was copied to `artifacts/goal/generated-output-evidence-backup/`. Inventory/action records are retained. No product source or user profile was removed.
