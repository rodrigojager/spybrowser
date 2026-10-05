# Current-source distribution/platform gate closure

**Disposition: criterion-level closure with release gates still open.** This document records the recovered Linux proof and maps only 19.05, 19.11, and every literal criterion in tickets 22–24. Product evidence is bound to source `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a` (archive SHA-256 `85fcc57493d1b490ad5ce2bda137971a6a12daf276d9a29aee4c8e9da6e0d242`). HEAD `35b6656` is production-identical; later changes are harness/test-only. Machine-readable per-criterion assertions and artifact paths are in [recovery-final-gates.json](recovery-final-gates.json).

No runtime, tests, feed, or ledger were changed. No new tests were run for this closure. No source finalization, feed parity, publication, rights clearance, or default promotion is claimed.

## Evidence provenance and counters

- Linux source: exact `5fa99de` archive at `artifacts/goal/linux-5fa99de-required161/source-5fa99de.tar`; SDK 8.0.319 at `/home/spyreview/dotnet-sdk-8.0.319`; distro `SpyBrowser-Ubuntu24`.
- Required 1.61.0 runs used non-root `spyreview uid=1001`; environment `DOTNET_ROOT=/home/spyreview/dotnet-sdk-8.0.319`, `PATH=/home/spyreview/dotnet-sdk-8.0.319:/usr/bin:/bin`, `HOME=/home/spyreview`, `NUGET_PACKAGES=/home/spyreview/.nuget/packages`.
- Every Linux full 177-product run remains reported as **total 177, executed/passed 176, failed/error/timeout/aborted 0, one explicitly NotExecuted headed focus probe**. Cursory is **37/37 passed**. Do not call this 177 passed or zero skips. The exact headed probe has its own Xvfb TRX: **1/1 passed, 0 skipped**. Reviewed 1.63 full run was uid 0, not non-root; its separate headed probe also passed under Xvfb. Chrome full run at 1.61 likewise retains its one original probe skip and a separate headed 1/1 pass.
- `artifacts/goal/linux-5fa99de-{required161,reviewed163,chrome}/run-status.json` contains exact TRX names, counters, non-passed test names, source/archive hashes and identity/environment. See `recovered-linux-proof.md` for literal commands/log paths.
- Parent Windows matrix: required 1.61 177 tests, latest 1.63 177, Cursory 37; Chrome 177 and Edge 177 (Edge run uses test-only HEAD `d574f6b`; production source matches 5fa99de).

### Headed-lane interpretation (22.03/22.04)

The literal 22.04 wording requires regular headless coverage and additional headed/Xvfb, Chrome and Windows Edge lanes; it does **not** say the headless full suite must itself have zero skips. The separately executed, named headed probe passes 1/1 under Xvfb and therefore supplies the explicit headed probe lane. The original full-suite NotExecuted entry remains in the evidence and is not replaced or subtracted. This meets the literal lane requirement while preserving the skip, not a claim of a 177/177 green full run. Criterion 22.03 also requires unexecuted work to be made explicit; the per-run JSON/TRX does that.

## 19 — Cross-platform snapshot criteria

| ID | Status | Exact assertion / scope |
|---|---|---|
| 19.05 | **verified** | Exact-source Windows required TRX covers atomic/interruption/retention; installed Windows package proof exercises real NTFS ACL denial and preserves baseline. Recovered Linux required run is non-root uid 1001. Linux installed-distribution evidence records real filesystem permission denial and baseline preservation, not a root-only chmod result. Source SHA/archive SHA as above. Linux installed feed is preliminary, not release evidence. |
| 19.11 | **verified** | Source-bound required TRX passes `Saves_unique_private_snapshots_without_secret_sentinels_and_compares_fields`, `Every_serialized_string_field_is_projected_to_bounded_safe_values`, `Interrupted_midstream_and_cancelled_serialization_remove_partial_temp_and_preserve_baseline`, `Concurrent_saves_use_distinct_paths_and_retention_is_bounded`, and `Unix_directory_permission_denial_does_not_publish_snapshot`. Linux non-root installed run also passes real permission denial, OS-terminated `SaveAsync` during SDK temp write, baseline retention and concurrent snapshot checks; Windows installed evidence additionally records NTFS ACL and two-process concurrency. This verifies the enumerated test conditions, not a claim that a preliminary feed is final. |

## 22 — Playwright upgrade matrix

All eleven criteria are verified at the stated evidence scope. The Linux full-suite result keeps the one skip explicit; the separate headed probe supplies the headed lane. The Linux `run-status.json` files record exact per-run test counters and TRX paths; build and full source-bound command details are in `recovered-linux-proof.md`.

| ID | Status | Criterion-specific evidence/assertion |
|---|---|---|
| 22.01 | **verified** | CI uses required Playwright 1.61.0 on Windows/Linux and explicitly installs Chromium (Linux with OS dependencies). Exact-source Linux 1.61 build succeeds; product 176/177 with one named NotExecuted headed probe; Cursory 37/37. |
| 22.02 | **verified** | `resolve-latest` resolves stable latest once, emits/reports the version and passes it to related lanes. Current reviewed latest is 1.63.0; exact-source build/full run retained. Corrected browser-enabled isolated API audit is 2/2; an earlier invocation lacking the browser enable flag is excluded. |
| 22.03 | **verified** | CI builds, runs browser-enabled tests and packs Core/Playwright/Cursory/Compatibility.CloakBrowser/Cli; `if: always()` retains result artifacts. Original NotExecuted probe is explicit in run-status/TRX and its separate execution is recorded; no silent-pass or zero-skip claim. |
| 22.04 | **verified** | Linux required/reviewed full runs plus separate Xvfb headed probe; Chrome full run plus separate Xvfb probe; parent Windows Chrome and Edge each 177, Edge source product-identical. Scheduled/manual CI has Ubuntu Chrome/Xvfb and Windows Edge lanes. One full-run skip stays counted. |
| 22.05 | **verified** | Run status and proof record exact source/archive, SDK 8.0.319, Playwright lane 1.61/1.63, Linux distro/user; Chrome lane identifies installed `/usr/bin/google-chrome-stable`. No lane is attributed to an uninstalled version. |
| 22.06 | **verified** | Reviewed 1.63 API audit tests pass 2/2 with zero skipped after browser enablement; this is the finite reviewed API surface, not an exhaustive future API-combination audit. |
| 22.07 | **verified** | Full source-bound suite passes 176 product cases and Cursory 37 (one explicitly NotExecuted); retained tests cover implemented browser contracts. Separate actual RpaBlockly proof asserts exact-once Context/Page event callbacks and reference identity; its 17 operations are individually mapped, not inferred from aggregate count. |
| 22.08 | **verified** | Exercised CI/browser and RpaBlockly routes use local browser/loopback tests. External mixed CAPTCHA/provider suite is explicitly not run and is not counted. |
| 22.09 | **verified** | CI has no test retry policy, propagates failures, and uploads artifacts with `if: always()`. Linux run status preserves the actual skip and logs/TRX. This establishes transparent reporting, not absence of all possible flakiness. |
| 22.10 | **verified** | Required minimum stays 1.61.0; latest stable is an additive scheduled/manual lane, not an automatic minimum upgrade. |
| 22.11 | **verified** | `recovered-linux-proof.md` records reproducible exact commands/environment; CI contains install/test commands. Xvfb is treated as a virtual display, not GPU hardware evidence. |

## 23 — Installed prerelease and rollback

Only **23.05** is verified, and only because the literal criterion accepts an explicit distribution block instead of clearance. All other statuses preserve preliminary-only package scope. Linux installed evidence reports **13 technical checks PASS**, `technicalAllPassed=true`, but `allPassed=false`; the rights check is `BLOCKED`, `externalPublicationAllowed=false`. Feed version is `0.2.0-beta.2.review.5fa99de`, `isFinal=false`, `declaredFinalCommit=null`.

| ID | Status | Criterion-specific evidence/assertion |
|---|---|---|
| 23.01 | **partial** | Isolated non-root Linux consumer installs local feed package and runs technical checks; source is 5fa99de, but no final feed. |
| 23.02 | **partial** | Preliminary package inventory verifies separate Cursory assembly/embedded data; installed modes pass. Final package gate remains open. |
| 23.03 | **partial** | Installed launch passes through standard Microsoft.Playwright discovery/driver; final feed absent. |
| 23.04 | **partial** | Preliminary package/nuspec/source/symbol/license/hash inventory exists; no final-feed acceptance. |
| 23.05 | **verified** | Explicit dataset redistribution-rights BLOCKED, `allPassed=false`, `externalPublicationAllowed=false`; no false claim that DLL separation is legal clearance. This is a truthful blocker satisfying this checklist criterion only—not legal clearance, publication permission or release authorization. |
| 23.06 | **partial** | Installed Cursory, Bézier and Humanize-off technical checks pass; the consumer event run is 14/14 for Cursory/compatible/on. No final-feed configuration parity. |
| 23.07 | **partial** | Installed Linux rollback to pinned e217 baseline passes; schema-999 optional snapshot remains byte-exact and profile/storage baseline is preserved. Candidate package remains preliminary. |
| 23.08 | **pending** | No final-release artifact/manual tied to a final feed was produced or validated. |
| 23.09 | **partial** | Actual RpaBlockly consumer proof has 14/14 named checks and 17 individually mapped semantic operations; historical DOM/reflection and benchmark evidence is source-linked. Consumer manifest says `candidateFinalParity=false`; no final declared commit. |
| 23.10 | **partial** | No default promotion occurred; however, preliminary runs do not verify the final package/default-promotion gate. Do not mark promoted-default requirement verified. |
| 23.11 | **partial** | Installed snapshot checks pass, but ticket 24 is not closed and snapshots are not announced as final-release homologated. |
| 23.12 | **pending** | No separate affirmative operational approval artifact. Nothing was published; external distribution remains unauthorized. |

## 24 — Snapshot validation in final distribution

Linux preliminary installed proof records all 13 technical checks passed, including snapshot save/compare/discard/schema/secrets, real permission denial, OS-crash during SDK write, rollback, schema-999 preservation and GPU-context information. The proof is valuable direct technical evidence but **does not** verify final distribution: the feed is preliminary, final source declaration is null, rights remain blocked.

| ID | Status | Criterion-specific evidence/assertion |
|---|---|---|
| 24.01 | **partial** | Isolated installed consumer executes snapshot save/select/compare/discard; preliminary package only. |
| 24.02 | **partial** | Installed schema checks and separate algorithm-data inventory pass; final feed absent. |
| 24.03 | **partial** | e217 rollback preserves profile/storage and leaves schema-999 snapshot byte-exact; candidate is preliminary. |
| 24.04 | **partial** | Installed discard/disabled-persistence checks pass with baseline preservation; no final package. |
| 24.05 | **partial** | Non-root Linux installed run passes real permission denial, OS-terminated SDK write and concurrency with baseline preserved; Windows installed evidence additionally exercises NTFS and two-process concurrency. Strong technical proof, but not final-feed proof. |
| 24.06 | **partial** | Required TRX passes secret-sentinel and all-string-field projection cases; installed schema/rollback checks pass. Final package linkage remains open. |
| 24.07 | **partial** | Installed GPU-context information check passes and baseline remains intact; preliminary package. |
| 24.08 | **partial** | Operational/distribution documentation exists, but final-release manual/signoff linked to final package is not established. |
| 24.09 | **pending** | No final release checklist/feed associates snapshots with final compatibility, licensing and core performance evidence. |
| 24.10 | **pending** | No affirmative separate approval to publish or alter defaults. Negative authorization remains; no publication/default promotion occurred. |

## Remaining gates

- **Final source/feed:** preliminary feed `C:/Temp/spybrowser-feed-review-5fa99de` manifest SHA-256 `33d541e09ccab9ec2b2de5ce7870e4dff62efc4de82c0a0b57cf7e6b44967a7d`; `isFinal=false`, `declaredFinalCommit=null`. Final feed source commit, declaration and package nuspecs must bind the actual integrated final commit before claiming final parity or release acceptance.
- **Rights:** dataset redistribution clearance is unresolved. The explicit investigation/block satisfies 23.05's condition bullet, not positive clearance and not publication authorization.
- **Release closure:** 23/24 final-package assertions, release checklist/manual, and independent operational approvals remain. Do not claim final parity, publication, or global-default promotion based on these preliminary tests.

## Validation and ownership

Read-only review of the recovered proof, run-status/TRX summaries, installed Linux package evidence, current consumer-event proof, current CI workflow and canonical ticket mapping. JSON syntax/entry-count validation is reported in the task result. Only `docs/implementation/recovery-final-gates.json` and this Markdown are owned outputs.