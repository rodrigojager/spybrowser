# Final local delivery checklist — ef2d0ea

**Local implementation accepted: all 24 tickets and 260 literal criteria verified. External publication and default promotion remain prohibited.** See the [independent closure decision](final-acceptance-closure.md); the earlier gap findings remain preserved in their audit reports.

## Frozen candidate

- [x] Independently accepted source: `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`; see [integrated freeze decision](integrated-freeze-decision.md). Later evidence/reporting and test-only changes are not relabeled as package source.
- [x] New immutable producer feed: `D:/Temp/spybrowser-feed-final-ef2d0ea`, version `0.2.0-beta.2.final.ef2d0ea`. Older preliminary and 4d feeds remain preserved, not repacked.
- [x] Source archive SHA-256: `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`; producer manifest SHA-256: `689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`.
- [x] Four actual nupkg/symbol pairs, full-SHA nuspec identity, license/notices and complete corresponding-source build/repack payload inspected: [package proof](ef2d0ea-package-proof.md).
- [x] Actual raw-URL SourceLink CDI in four PDBs; 58 tracked source checksums verified, 13 generated documents excluded. The retained manifest's display-field typo is separately explained in [source-map report note](ef2d0ea-source-map-report-note.md); actual CDI mapping is `/_/*`. Remote availability of the unpushed SHA is not claimed.
- [x] SDK 8.0.319 standalone source rebuild corresponds across 432 entries / 23 types, with no compared method/metadata/local/exception/resource differences. Whole-PE byte identity is **not** claimed.

## Installed and consumer gates

- [x] New package-only Windows consumer: 13 technical passes, no failed/pending technical checks; [Windows proof](ef2d0ea-installed-windows-proof.md).
- [x] New package-only Linux consumer, non-root UID 1001: 13 technical passes, no failed/pending technical checks; [Linux proof](ef2d0ea-installed-linux-proof.md).
- [x] Cursory opt-in, Bézier/Legacy and Humanize off use the same commands. Separate BCL Cursory DLL/data verified; normal Playwright driver is allowed, not confused with a generation sidecar.
- [x] Installed snapshots: save/select baseline/compare/discard, independent schema, unknown-schema rejection, private permissions, real permission denial, crash during write, concurrency, baseline preservation, safe disable/discard and pinned prior-package rollback without profile/identity/storage-state damage. Named checks and their assertions are in the installed proofs, not inferred from aggregate counts.
- [x] Actual RpaBlockly final-feed runs: Cursory/compatible/on, Bézier/Legacy/on, Cursory/compatible/off each 14/14, `candidateFinalParity=true`. The separate map has 17 semantic assertion groups; neither number denotes exhaustive API coverage. SDK 10.0.401 was resolved in each actual isolated consumer CWD. [Consumer proof](ef2d0ea-consumer-proof.md).
- [x] Source browser-contract matrix: Windows and Linux required/reviewed lanes 177 product + 37 Cursory each; Chrome/Edge scope and original reviewed Windows failure retained. These are frozen 4d source runs, with unchanged production C# at ef2d, **not** a claim of full-suite ef2d reruns: [matrix](browser-matrix-current.md).
- [x] Reviewed [final-package native/benchmark/historical proof](ef2d0ea-native-proof.md): BCL-only consumer assertions, 37/37 native tests, 28 completed historical page observations, old37 API types with no removals/default changes and74 additive members. Three fresh processes each1cold/20warmups/500samples: median cold401.380ms, warm p507.563ms, p9511.049ms, allocation2,160,426bytes/generation. These measured ef2d observations exceed the10ms investigation target; no universal timing/CI guarantee or paired historical RNG claim.
- [x] Closed the identified06.09 evidence finding with a direct pending compatible Fill/context-close test awaiting the original task: [proof](final-fill-context-close-proof.md), 1/1 focused pass. Initial mistaken exception-type assertion and failed TRX remain preserved; no production change or repack.
- [x] Independent audits reconciled all 260 literal IDs, texts and dependencies. Both final gaps closed; [closure decision](final-acceptance-closure.md) accepts local implementation. Canonical ledger verifies 260/260; its original 239 verified statuses and immutable contract were preserved. Bookkeeping is not a substitute for the cited direct assertions.

## Operator documentation and publication-only holds

- [x] [Release notes](../release-notes-0.2.0-beta.2.md) describe compatible/Legacy semantics, unchanged defaults, raw limitations, options/deadlines, rollback, matrix scope and diagnostics privacy.
- [x] [Snapshot operations](../snapshot-operations.md) documents optional activation, comparison, baseline/retention, private access, crash temporary-file limitations and safe discard; no dedicated export API or telemetry service is claimed. Installed technical snapshot checks passed on both platforms; all ten ticket-24 criteria are accepted in the independent closure decision.
- [x] Dataset redistribution rights remain **UNVERIFIED**, an explicit distribution block permitted by criterion 23.05, not clearance. Both installed runners have `allPassed=false` and exit 2 solely for that block.
- [x] Cursory remains opt-in; global Bézier/Legacy defaults and raw `Humanize=false` remain unchanged.
- [x] External NuGet/GitHub publication and global-default promotion require separate specific operational approval, in addition to rights clearance. Neither action has been performed or authorized by this implementation task.

**Decision:** local implementation delivered and independently accepted against all 260 criteria, with immutable ef2d package provenance and installed technical gates passed. External release remains blocked independently; this checklist is not legal or publication approval.
