# SpyBrowser 0.2.0-beta.2 — local final-candidate notes

> **Status:** local package `0.2.0-beta.2.final.ef2d0ea`, frozen source `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`. Installed Windows/Linux each passed 13 technical checks; all three RpaBlockly lanes passed 14/14 with final parity. Independent final review accepted all 24 tickets and 260 criteria for local implementation. This is not an external publication announcement or legal clearance; dataset redistribution rights remain UNVERIFIED.

## Humanization is opt-in; compatibility defaults are preserved

- Cursory trajectory generation is available as an explicit opt-in choice. The existing Bézier / Legacy behavior remains the default; `Humanize=false` remains an unhumanized path. No global default is silently changed.
- In PlaywrightCompatible mode, native Playwright semantics are preserved where specified: `Fill` remains native fill; `InsertText` is not converted to key-by-key typing; shortcuts, double-click and final click/hover actions remain native operations. Unsupported or explicit options may use the documented raw path. There is no silent retry/fallback after an action may already have taken effect.
- Explicit SDK action deadlines/options are forwarded under the documented rules. Existing consumer calls that provide options such as `Timeout` may consequently take a conservative raw route; this is not described as humanized coverage.
- Rollback choices are Bézier/Legacy, `Humanize=false`, or pinning the previous package. Do not switch algorithms silently in the middle of an action.

## Diagnostics and snapshots

Diagnostics are opt-in and designed to avoid secrets: do not emit typed text, passwords, cookies, tokens, storage state, full URLs, or personally revealing selectors by default. Optional diagnostic snapshots use their own schema, independent of identity manifests and Cursory dataset versions. Persistence is optional; writes are atomic, retention is bounded/configurable, and snapshots can be disabled/discarded. On rollback, a newer/unknown snapshot is ignored or rejected according to its contract; it must not rewrite identity, profile, cookies, or storage state. Both final-package installed consumers exercised save/select baseline/compare/discard, permission denial, crash during write, concurrency and prior-package rollback. Those technical checks passed and all ticket-24 criteria were independently accepted for local implementation, distinct from external publication approval. See the [operator manual](snapshot-operations.md) for activation, comparison, baseline protection, retention and safe discard. There is no dedicated export API; no telemetry service is introduced.

## Limits and evidence boundaries

- Advanced raw operations, IME/composition and some Unicode grapheme cases, CDP and other explicitly unsupported routes are not claimed as humanized. Use native/raw behavior where required; no text is silently truncated or semantically rewritten.
- Playwright/browser matrix and RpaBlockly checks are documented in [the current matrix](implementation/browser-matrix-current.md). Full browser suites are bound to frozen 4d source with unchanged production C# at ef2d; actual installed and consumer runs are bound to the new ef2d package bytes. Original failures and sequential reruns are retained, not relabeled.
- The local benchmark is an observation, not a timing guarantee; the 10 ms p95 investigation target is not a CI threshold. No performance, stealth, CAPTCHA, or detection-reduction guarantee is made.
- External dataset redistribution rights remain blocked. A separate Cursory DLL, source rebuild, or package inspection does not constitute legal clearance. External publication is not authorized.
