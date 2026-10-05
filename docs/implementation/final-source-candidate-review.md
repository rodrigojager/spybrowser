# Independent source-candidate readiness review

**Disposition: `ACCEPT_SOURCE_FOR_LOCAL_FINAL_VERIFICATION`.** This accepts the frozen technical source candidate for the next source-bound **local** `--final` feed and verification cycle. It does not approve a release, external distribution, default promotion, or the plan's final acceptance. No product/runtime changes are indicated by this review.

## Review basis and decision boundary

- Workspace HEAD: `35b6656caa1b272c074f32b16da67387cb1374df`.
- Production source/package evidence is bound to `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`; the 5fa source archive SHA-256 is `85fcc57493d1b490ad5ce2bda137971a6a12daf276d9a29aee4c8e9da6e0d242`. Diff review confirms product source is unchanged from 5fa; subsequent changes are tests/harness. The actual RpaBlockly event check is included in the 14-check installed-feed evidence, not inferred from a test declaration.
- Reviewed the complete plan `docs/plans/qualidade-humanizacao-cursory-nativo.md` (all sections through 17), the literal 24 numbered issues under `.scratch/spybrowser-cursory-nativo/issues/`, current `docs/implementation/ticket-evidence.json`, scoped acceptance/recovery audits, and cited source-bound artifacts. This is a read-only evidence review; no tests or builds were run here.
- This decision answers source-runtime readiness, not whether a current final package exists. Fresh installed RpaBlockly, matrix, package parity/content, and pure-package benchmark evidence must be produced against the new final feed before package/release acceptance.

## Technical findings

The reviewed source implements the planned complete technical slice without an identified missing runtime deliverable: supported Playwright actions preserve their native semantics or conservatively route raw; Cursory is a separate .NET/BCL library with pinned data, RNG/fixtures and validation; scheduler/deadline and page lifetime work are covered; wrapper factories, reference identity, frame/collection/event propagation have direct assertions; diagnostics/probes/snapshots and privacy boundaries are present. Public APIs/defaults retain the historical compatibility baseline and Cursory remains opt-in. The source-bound Windows/Linux test artifacts, current exact-source Linux zero-skip matrix and fresh installed consumer evidence materially support this determination; their exact scopes and limits remain below.

The LGPL/data-rights uncertainty is **not** technical evidence of defective runtime source and does **not** bar a private/local technical verification feed. It continues to block external publication. This review makes no legal-clearance claim. Ticket 23.05 is correctly a verified *explicit blocker/checklist* criterion, not clearance; ticket 24.10 is an authorization boundary and does not imply permission to publish.

## All 24 plan issues reviewed

Every ticket was reviewed against its literal issue text and mapped evidence; no issue was excluded or scope-reduced. The statuses below are what the current canonical `ticket-evidence.json` records, not an assertion that all plan/final-release gates are closed. `V` = verified in that ledger; `P` = pending. Pending package evidence does not by itself imply a source-code defect.

| Ticket | Topic | Ledger V/P | Readiness interpretation |
|---:|---|---:|---|
| 01 | Replaceable motion / legacy compatibility | 10/0 | Source ready; 28 historical real-DOM observations plus API reflection are useful scoped compatibility evidence, not new-feed parity. |
| 02 | Self-contained .NET Cursory library | 11/0 | Source ready. Historical package inspection/consumer evidence is not the final-feed inspection; unresolved dataset rights remain a separate external-distribution gate. |
| 03 | Upstream parity | 12/0 | Source ready; fixtures, RNG and stage-parity evidence are bound to the pinned dataset/source. Re-run/preserve the pure-package proof against final feed. |
| 04 | Opt-in Cursory mouse path | 11/0 | Source ready; do not promote its default. |
| 05 | Scheduling, scale and pointer state | 11/0 | Source ready; benchmark is evidence, not a hard 10 ms CI threshold. |
| 06 | Native Fill/Clear semantics | 10/0 | Source ready within documented compatibility boundary. |
| 07 | InsertText and shortcuts | 10/0 | Source ready; exact event contract has direct current-source assertions. |
| 08 | Native click/double-click/hover | 10/0 | Source ready within tested/action-specific contract; no replay or blanket event-identity claim. |
| 09 | Typing and Unicode | 10/0 | Source ready within tested grapheme/IME boundary; fallback and privacy limits remain documented. |
| 10 | Input scheduling/lifecycle | 12/0 | Source ready; evidence covers actual operation observation, cleanup ownership and concurrency boundaries, not blanket Playwright token cancellation. |
| 11 | Configured factories | 10/0 | Source ready; factory parity and options ownership are tested. |
| 12 | Frames and locator collections | 11/0 | Source ready; wrappers/identity evidence is source-bound. |
| 13 | Context events and collections | 11/0 | Source ready; exact-once/reference assertions included in the fresh 14-check RpaBlockly proof. |
| 14 | Popups/new pages | 10/0 | Source ready; popup/nested-frame propagation is included in that same proof. |
| 15 | Diagnostics/privacy | 11/0 | Source ready; no secrets or user text by default. |
| 16 | Isolated probes | 11/0 | Source ready; probe isolation and GPU-off consumer intent retained. |
| 17 | Regional/geometry consistency | 11/0 | Source ready; normalizations and unavailable-vs-conflict semantics covered. |
| 18 | Browser/rendering consistency | 11/0 | Source ready within available browser capabilities; not a stealth/fingerprint-equivalence assertion. |
| 19 | Optional snapshots | 11/0 | Source ready. Current-source Linux non-root and installed-platform evidence closes prior permission/concurrency concerns; final-package linkage is still required. |
| 20 | RpaBlockly consumer | 12/0 | Source/integration technically ready: `artifacts/goal/recovery-consumer-events/consumer-evidence.json` binds source 5fa and the pinned consumer; 14/14 named local checks pass, including exact-once events/reference identity. This is explicitly preliminary (`candidateFinalParity=false`, no declared final commit), so rerun against final feed. |
| 21 | Motion quality/cost | 11/0 | Existing actual-DOM and package benchmark artifacts support the literal benchmark criteria at their stated source/scope. They are not fresh final-feed/pure-package parity proof; generate that proof after final packaging. |
| 22 | Playwright upgrade CI/matrix | 11/11 P | **Source-ready, matrix/release evidence follow-up.** Fresh exact-source Linux `run-status.json` records required 1.61, reviewed 1.63 and Chrome lanes, each 177/177 product and 37/37 Cursory, zero skips (non-root uid 1001). Windows/source-bound results are documented in `recovery-final-gates.md`. Preserve the lane/configuration/API-surface evidence and rerun against final feed; do not silently flip ledger statuses based only on this review. |
| 23 | Installed prerelease/rollback | 1/11 P | Deliberate final-feed gate. Preliminary source-bound installed evidence exists; rebuild/install from the final local feed and prove package contents, modes, pinned e217 rollback, release docs and separate authorization. 23.05 only verifies the explicit rights block. |
| 24 | Snapshot final distribution | 0/10 P | Deliberate final-feed gate. Preliminary Windows/non-root Linux technical checks are valuable but do not prove final-package parity, final manual/checklist, or external authorization. |

### Current run/artifact limits

- `artifacts/goal/linux-5fa99de-zero-skip/run-status.json`: non-root Linux source archive, 1.61/1.63/Chrome full lanes; each lane records product **177/177** and Cursory **37/37**, no skips. It does not substitute for rebuilding and installing the final package or prove every CI configuration property.
- `artifacts/goal/recovery-consumer-events/consumer-evidence.json`: actual installed local-feed RpaBlockly run, **14/14** named checks, source and package hashes/versions recorded. It explicitly says `candidateFinalParity=false`, `declaredFinalCommit=null`; it proves the source-integrated technical consumer slice, not final-feed parity or the excluded external/CAPTCHA suite.
- `artifacts/goal/recovery-final-gates.md` distinguishes preliminary installed Windows/Linux checks from a final feed. Keep the exact e217 baseline distinct; do not substitute a different candidate as rollback evidence.
- Historical Windows DOM/API evidence establishes scoped before/after behavior and no changes to the 37 compared existing public type/member surfaces/defaults. It is not a replacement for a new final-package consumer, matrix, or benchmark run.

## Ledger reconciliation finding

The supplied handoff describes **226 verified / 34 pending**. The checked-in-worktree canonical `docs/implementation/ticket-evidence.json`, parsed read-only during this review, contains 260 criteria but currently totals **227 verified / 33 pending** (tickets 01–21 all verified; 22 has 11 pending; 23 has 11 pending plus 23.05 verified; 24 has 10 pending). I found no evidence basis to silently change a ticket status in this owned review. This one-criterion count discrepancy must be reconciled by the ledger owner; it does not identify a missing runtime feature and does not change the source-candidate decision. The separate current acceptance narrative for tickets 20–24 predates the fresh consumer evidence and must not override the exact new 14/14 artifact; equally, the new artifact must not be overstated as final-feed parity.

## Decision and required next evidence

**Accept the current source for local final verification.** No concrete source-code defect or required technical source deliverable was found that should prevent freezing this candidate. Do not call the project/plan complete. After freezing source and its docs, build a fresh local `--final` feed whose manifest, declared commit, source archive and every nupkg hash agree. Then run and retain: (1) installed RpaBlockly consumer checks and event/API boundaries, (2) the complete required/latest/platform matrix with truthful skips, (3) package/nuspec/license/source/symbol/content inspection and exact e217 rollback, and (4) source/package-bound pure BCL parity/benchmark. Keep `externalPublicationAllowed=false` until the independent dataset-rights issue is resolved and separate operational publication approval exists. No external publication or default promotion is authorized by this acceptance.

**Review validation:** read-only inspection and `git diff 5fa99de -- src tests` (product source unchanged; only two test files differ). No test/build/package operation run. Owned files: this Markdown and `final-source-candidate-review.json` only.
