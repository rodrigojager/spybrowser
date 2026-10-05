# Final release checklist — local source candidate

**State: good to cut a local final-source-candidate feed after this documentation cut is frozen. Not a final release or publication approval.** Current source HEAD is `35b6656caa1b272c074f32b16da67387cb1374df`; production `src/` is unchanged from `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`. The available `0.2.0-beta.2.review.5fa99de` feed is preliminary and must not be relabeled final.

## Source and build — next required gate

- [ ] Freeze the complete source candidate and documentation; record full Git SHA and clean/dirty state.
- [ ] Build a **new** local feed from that exact source candidate. Do not reuse/repackage `feed-review-5fa99de` as final.
- [ ] Bind manifest, package hashes, source archive/hash, nuspec/source/PDB/license content and rebuild inspection to the same full source SHA. Verify all actual `.nupkg` contents.
- [ ] Keep a separate local feed/version, with no external publish/push and no declaration that package parity exists until checks pass.

## Existing evidence (use with its scope limits)

- Linux exact-source matrix: required 1.61, reviewed 1.63 and Chrome 1.61 each 177/177 Playwright plus 37/37 Cursory with zero skips: [`linux-zero-skip-proof.md`](linux-zero-skip-proof.md). Windows required/latest 177, Cursory 37, Chrome 177; Edge 177 is source-compatible `d574f6b`, not current-HEAD test execution. Full boundaries are in the [browser matrix](browser-matrix-current.md).
- Actual RpaBlockly adapter/event harness on preliminary feed: 14/14 checks in Cursory/PlaywrightCompatible/on mode; separate 17-operation semantic assertion map. Older Legacy/on and Humanize/off runs have 13 checks each. Re-run the consumer on the new feed: [`recovery-consumer-event-proof.md`](recovery-consumer-event-proof.md).
- Installed preliminary Linux proof: 13 technical checks passed, but `allPassed=false`, rights `BLOCKED`, `externalPublicationAllowed=false`; Windows artifacts at `artifacts/goal/installed-5fa99de-windows-proof/`. These are not final-feed installed checks.
- Recorded benchmark: three processes / 500 samples; median and p50 5.825 ms, p95 11.160 ms, 2,160,439 allocated bytes/generation. Local observation only; do not impose the 10 ms investigation target as a CI threshold or claim universal performance.
- Status ledger currently reports 239/260 after platform evidence reconciliation. The remaining 21 criteria require the new source-bound installed candidate and final documentation/evidence association. Do not claim 260/260.

## New-feed installed and consumer verification — required after build

- [ ] Run installed package/driver/mode checks on Windows and Linux against exact new-feed hashes; check Cursory opt-in, Bézier/Legacy, and Humanize off.
- [ ] Verify package graph, separate Cursory DLL and embedded data, real nupkg/nuspec/license/source/PDB contents and source rebuild. DLL separation is not a legal opinion.
- [ ] Verify rollback to the pinned prior package without identity/profile/storage-state damage; exercise snapshot save/select/compare/discard, schema separation, unknown-schema handling, permission denial, interrupted writes, concurrency and retention against the installed candidate.
- [ ] Re-run actual RpaBlockly consumer harness using the exact local feed; preserve exact source/package/SDK/browser provenance, all named checks and excluded tests. Do not infer semantic coverage from aggregate counts.
- [ ] Reconcile each remaining ticket criterion against concrete artifacts. Do not treat prior aggregate audits or this checklist as the 260-criterion signoff.

## Release and authorization gates

- [ ] Publish release notes describing opt-in Cursory, compatible-vs-Legacy semantics, unchanged defaults, option/deadline behavior, rollback, diagnostics/privacy, snapshot lifecycle, IME/advanced-raw/CDP limitations and verified matrix scope.
- [ ] Obtain independent dataset provenance/redistribution-rights clearance. Current status is blocked; ticket 23.05 permits explicitly recording a distribution block only. It is not legal clearance or permission to publish.
- [ ] Obtain separate affirmative operational approval for any external publication or global-default change (ticket 24.10). Neither is authorized by this local task.
- [ ] Keep Cursory opt-in and Legacy/Bézier defaults unchanged unless a separately approved release explicitly changes them.

## Decision

**Good to cut:** a new, local, source-bound final-candidate feed after freezing this documentation/source candidate. **Not good to publish, promote, or claim final acceptance:** installed/consumer proofs must be rerun against that new feed; remaining criteria, rights review and separate operational authorization remain open. See [goal progress](goal-progress.md) and [local source-candidate release notes](../release-notes-0.2.0-beta.2.md).
