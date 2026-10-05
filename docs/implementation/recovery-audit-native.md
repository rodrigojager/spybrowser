# Recovery audit: native Cursory tickets 01–05

**Disposition: partial evidence audit; no production/test changes.** Scope is the literal acceptance criteria in `.scratch/spybrowser-cursory-nativo/issues/01-...` through `05-...`, current source/tests and the canonical ledger. The detailed per-criterion records are in [`recovery-audit-native.json`](recovery-audit-native.json). Only the new JSON and this Markdown are audit outputs; the canonical `ticket-evidence.json` was not changed.

- Workspace `D:/SpyBrowser`; HEAD `d574f6b6eedc026ebaaa7ed0528d0c20cc988dea`.
- Source/package evidence is bound to `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`; the supplied scope says subsequent changes are test fixture boundary fixes only. No inference that those tests alter the package was made.
- Parent-supplied TRX artifacts, not rerun here: `artifacts/goal/5fa99de-required-results/required-final-cut.trx` 177/177 passed; `artifacts/goal/5fa99de-latest-results/latest-final-cut.trx` 177/177 passed; `artifacts/goal/5fa99de-cursory-results/cursory-final-cut.trx` 37/37 passed. Checked the TRX counters and matching test result names; tests were executed, not skipped.
- **52 verified, 3 pending** across 55 criteria. A pending label means required evidence is not established, not that implementation is known broken.

## Status by ticket

| Ticket | Verified criteria | Pending criteria |
|---|---|---|
| 01 | 01.02, .03, .05–.10 | **01.01**, **01.04** |
| 02 | 02.01–.11 | — |
| 03 | 03.01–.12 | — |
| 04 | 04.01–.11 | — |
| 05 | 05.01–.09, .11 | **05.10** |

## Material gaps and interpretation

- **01.01:** No matched before/after source-cut browser observation covers movement, hover and click. Aggregate passing runs do not provide that comparison.
- **01.04:** Existing tests prove selected default/raw/opt-in behavior in their scope. No source-cut-bound public API signature/options compatibility reflection or equivalent complete assertion is supplied. This is a proof gap, not an identified compatibility failure.
- **05.10:** The source-bound package benchmark evidence records cold/warm generation and allocations. A current-source matched browser DOM cadence/measurement report with machine and limitations is not supplied; older DOM reports are not attributed to 5fa99de. This is a bounded evidence gap, not a demonstrated performance regression.
- **02.08 versus 02.09:** 02.08 is verified as package attribution/metadata: LGPL-3.0-or-later and upstream authorship/notices are truthful. It is **not** rights clearance. 02.09 is verified at its stated scope because unresolved provenance/redistribution conditions are investigated, recorded in the manifest/checklist, and explicitly block external publication. Independent dataset rights remain **UNVERIFIED**; publication stays blocked.
- The exact local package proof is `artifacts/goal/native-package-demo-5fa99de/evidence.json`, package SHA-256 `44555bb6c5517f45bb8285875d1a50fbf9df7b214e75b21d7bce62d87acb20ac`, with source commit 5fa99de; inspection and attribution are `artifacts/goal/package-inspection-5fa99de.log` and `artifacts/goal/package-attribution-5fa99de.json`. It is review-only, not a publishable/final feed.
- Test-specific mappings, including executed browser cases, pinned fixture/parity assertions, fake-clock scheduler cases and consumer evidence, appear per criterion in the JSON. Historical evidence is not credited where the current source-cut artifact does not support it.

## Audit actions

Read the five tickets, canonical ledger, mapped source/test files, package manifests/evidence, documentation and supplied TRX artifacts. Verified TRX counters: required 177/177, latest 177/177, Cursory 37/37; required/latest include Passed local-browser DPR 1/1.25/1.5/2 cases and fake-clock scheduler cases. **No tests were run by this audit.** No WSL, services or process modifications. No ledger, runtime, tests, settings or other reports were edited.
