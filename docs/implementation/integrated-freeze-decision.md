# Integrated packaging source: parent freeze decision

The parent accepts the integrated source for **a new local final-package verification cycle**, using `integrated-packaging-source-review.json` (`ACCEPT_SOURCE_FOR_NEW_LOCAL_FINAL_VERIFICATION`) and the directly rerun 12 packaging tests. The next freeze commit, not the previous 4d8a18c feed, is the source identity for that build.

The C# production files and numerical fixtures are unchanged from the accepted implementation. The Cursory project now provides complete standalone corresponding sources, legal/build files, an exact SDK 8.0.319 pin, C# 12 and reference framework 8.0.22, plus conditional private SourceLink build dependencies. The CI duplicate-reference failure was diagnosed and corrected, and a real subsequent CI restore passed.

The producer now emits a real raw-content SourceLink mapping with the full frozen SHA, normalized compiler paths, actual PDB/document checksum checks and nuspec/package identity validation. The repaired producer was exercised on a separate explicitly preliminary candidate; this is not current integrated-SHA final-package proof. All old failures, reports and feeds remain retained.

Source–program correspondence was established for the previous package with the correct SDK across 432 metadata/method-body/exception/resource entries; standalone SDK 10 selected a different toolchain and caused the earlier IL discrepancy. Whole-PE byte identity is not claimed. The next package must independently repeat this correspondence check and must contain its SDK pin.

Required next steps are a new immutable source-bound local feed and direct package/symbol/SourceLink inspection, isolated installed Windows/non-root Linux checks, three real RpaBlockly configurations, pure-package parity/benchmark and final criterion-by-criterion association. No Cursory/Legacy/Bézier default is promoted. The canonical ledger remains 239/260; the remaining 21 release criteria are not accepted by this source decision.

External publication is still prohibited. Dataset redistribution rights remain unverified; local technical finality is neither rights clearance nor operational publication approval. Do not relabel/repack the preserved 4d8a18c feed as the corrected final candidate.
