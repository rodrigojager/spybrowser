# SpyBrowser 0.2.0-beta.2 — local source-candidate notes

> **Status:** operational draft for a local source candidate only. This is not a publication announcement, final-feed declaration, or legal clearance. The currently available `0.2.0-beta.2.review.5fa99de` feed is preliminary and is not parity evidence for the current source candidate. Build a new source-bound feed and rerun the installed/consumer gates before calling these notes package-verified.

## Humanization is opt-in; compatibility defaults are preserved

- Cursory trajectory generation is available as an explicit opt-in choice. The existing Bézier / Legacy behavior remains the default; `Humanize=false` remains an unhumanized path. No global default is silently changed.
- In PlaywrightCompatible mode, native Playwright semantics are preserved where specified: `Fill` remains native fill; `InsertText` is not converted to key-by-key typing; shortcuts, double-click and final click/hover actions remain native operations. Unsupported or explicit options may use the documented raw path. There is no silent retry/fallback after an action may already have taken effect.
- Explicit SDK action deadlines/options are forwarded under the documented rules. Existing consumer calls that provide options such as `Timeout` may consequently take a conservative raw route; this is not described as humanized coverage.
- Rollback choices are Bézier/Legacy, `Humanize=false`, or pinning the previous package. Do not switch algorithms silently in the middle of an action.

## Diagnostics and snapshots

Diagnostics are opt-in and designed to avoid secrets: do not emit typed text, passwords, cookies, tokens, storage state, full URLs, or personally revealing selectors by default. Optional diagnostic snapshots use their own schema, independent of identity manifests and Cursory dataset versions. Persistence is optional; writes are atomic, retention is bounded/configurable, and snapshots can be disabled/discarded. On rollback, a newer/unknown snapshot is ignored or rejected according to its contract; it must not rewrite identity, profile, cookies, or storage state. These behaviors still require rerun against the new installed candidate before being claimed as final-package acceptance.

## Limits and evidence boundaries

- Advanced raw operations, IME/composition and some Unicode grapheme cases, CDP and other explicitly unsupported routes are not claimed as humanized. Use native/raw behavior where required; no text is silently truncated or semantically rewritten.
- Playwright/browser matrix and RpaBlockly checks are documented in [the current matrix](implementation/browser-matrix-current.md). They are bound to source 5fa99de or the noted source-compatible fixture cut, not to a new final feed.
- The local benchmark is an observation, not a timing guarantee; the 10 ms p95 investigation target is not a CI threshold. No performance, stealth, CAPTCHA, or detection-reduction guarantee is made.
- External dataset redistribution rights remain blocked. A separate Cursory DLL, source rebuild, or package inspection does not constitute legal clearance. External publication is not authorized.
