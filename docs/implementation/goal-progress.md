# Goal progress — local source-candidate documentation cut

## Current disposition

Documentation is being aligned to the current source candidate, **not** declaring the product/release complete. HEAD is `35b6656caa1b272c074f32b16da67387cb1374df`; the production `src/` diff from `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a` is empty. The existing package feed `C:/Temp/spybrowser-feed-review-5fa99de` is preliminary (`0.2.0-beta.2.review.5fa99de`), not final-parity evidence.

The Linux exact-source zero-skip proof reports required Playwright 1.61, reviewed 1.63, and Chrome 1.61 lanes, each with 177/177 Playwright and 37/37 Cursory passes; Windows evidence has required/latest 177 and Cursory 37, Chrome 177, and Edge 177 on test cut `d574f6b` (production source-compatible). The Edge result is not relabeled as a current-HEAD run. See [browser matrix](browser-matrix-current.md), [Linux zero-skip proof](linux-zero-skip-proof.md), and [recovered Linux proof](recovered-linux-proof.md).

RpaBlockly's actual installed-package adapter/event run on the preliminary 5fa99de feed passed **14/14** required checks for Cursory/PlaywrightCompatible/Humanize on. A distinct semantic subset maps **17** named operations to assertions; aggregate counts are not semantic coverage. Older Bézier/Legacy-on and Humanize-off runs passed 13 checks each on the earlier harness and are identified as historical. No claim of current final-feed consumer parity is made. Details: [consumer event proof](recovery-consumer-event-proof.md).

The 5fa99de installed Linux proof records 13 technical checks passing, but `allPassed=false`, `externalPublicationAllowed=false`, and dataset redistribution rights BLOCKED. This explicitly satisfies the narrow ticket 23.05 blocked-distribution condition only; it is not legal clearance or publication approval. Windows installed artifacts are retained under `artifacts/goal/installed-5fa99de-windows-proof/`. Package inspection/source rebuild evidence likewise concerns a preliminary package.

Current benchmark evidence is bounded: the recorded three-process/500-sample result has median 5.825 ms, p50 5.825 ms, p95 11.160 ms and 2,160,439 allocated bytes per generation. This is a local benchmark observation, not a universal performance guarantee or stealth/detection claim. The initial 10 ms p95 investigation target is not a CI threshold. The earlier DOM benchmark reports are separate measurements and are not to be conflated with this summary.

## Remaining work before installed-candidate acceptance

1. Freeze the complete source/documentation candidate, then build a **new** local feed bound to its full source SHA and package/source hashes.
2. Inspect that new feed's package contents, source/license/PDB linkage and rebuild evidence.
3. Rerun installed-package checks on Windows and Linux and rerun the actual consumer harness against those exact packages; preserve modes, rollback, snapshots and package provenance.
4. Reconcile ticket evidence criterion by criterion. Current aggregate status is **239/260** after criterion-specific platform evidence was reconciled. The remaining 21 criteria concern the final installed candidate and release/snapshot distribution evidence; this is not a final overall acceptance declaration.
5. Keep external publication blocked pending independent dataset-rights clearance and specific operational approval. Do not promote Cursory/defaults globally.

No runtime, tests, canonical ledger, feed, publishing state, or commit is changed by this documentation task. Prior milestone notes and artifacts remain historical evidence; they are not silently reinterpreted as final-feed acceptance. The local-candidate checklist is [final release checklist](final-release-checklist.md).
