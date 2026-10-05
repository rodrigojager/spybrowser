# Bounded acceptance mapping — tickets 15–24

**Disposition: PARTIAL.** Exact issue criteria and canonical ledger IDs are mapped individually in the companion JSON. Workspace HEAD: `d574f6b6eedc026ebaaa7ed0528d0c20cc988dea`; production/package source: `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`. Later corrections are test-only.

No ledger, product, tests or other documents were edited.

| Ticket | Verified | Partial | Pending | Blocked |
|---:|---:|---:|---:|---:|
| 15 | 11 | 0 | 0 | 0 |
| 16 | 11 | 0 | 0 | 0 |
| 17 | 11 | 0 | 0 | 0 |
| 18 | 11 | 0 | 0 | 0 |
| 19 | 8 | 0 | 3 | 0 |
| 20 | 11 | 1 | 0 | 0 |
| 21 | 10 | 1 | 0 | 0 |
| 22 | 0 | 11 | 0 | 0 |
| 23 | 1 | 10 | 1 | 0 |
| 24 | 0 | 10 | 0 | 0 |

## Findings and limits

- Diagnostics 15–19 use criterion-specific source/assertion mappings from `acceptance-native-diagnostics-current.json`; pending means a specific proof gap, not an asserted product defect.
- Ticket 20: three source-bound 5fa99de consumer configurations report 13/13 checks each. The separate 14-item semantic audit was not performed.
- Ticket 21: BCL-only package consumer; three benchmark runs p50 5.825 ms / p95 11.160 ms. A 10 ms target is an investigation threshold, not a universal guarantee. Historical package comparison records 28/28 actual DOM runs, 37 old API types unchanged, and 13 additive types / 74 members.
- Ticket 22: Windows required/latest 177, Cursory 37, Chrome 177, current Edge 177 are mapped. Current Linux proof is absent: SpyBrowser-Ubuntu24 executor killed after 15 h; CLI list+echo timed out. Older 776 Linux is not substituted. This is an environment evidence gap.
- Tickets 23–24: installed Windows proof is source/package-bound to 5fa99de and `technicalAllPassed=true`; it is preliminary and not a final-feed gate. Byte-exact packages and full source provenance are mapped.
- Rights clearance is absent. The explicit local-only block is truthful. No publish, repack, finalization or default promotion.

## Validation

- Read original issue criteria 15–24, canonical `ticket-evidence.json`, and cited retained proof reports.
- Confirmed workspace HEAD and preserved pre-existing changes.
- No tests run; this task was criterion evidence mapping. Full assertion/artifact paths are in `recovery-audit-distribution.json`.
