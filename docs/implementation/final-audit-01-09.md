# Final criterion audit — tickets 01–09

**Disposition: PARTIAL — 94/95 criteria verified at stated scope; 06.09 remains a gap.** This is a criterion review, not a release/promotion verdict or a blanket “green because tests passed” declaration. Dataset redistribution rights remain **UNVERIFIED**, so external publication remains blocked.

## Scope and method

- Workspace/source revision: `D:/SpyBrowser`, HEAD `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`; product C# source baseline `5fa99decd83295a96b7f2441f1fd3a0f1fca0c3a`.
- Scope is only canonical tickets 01–09 from `docs/implementation/ticket-evidence.json` and `.scratch/spybrowser-cursory-nativo/issues/01..09`; exact criterion wording/order is retained in the paired JSON. Dependency IDs are those in canonical records: 03 depends on 02; 04 on 01 and 03; 05 on 04; 07 on 06; 08 on 01 and 06; 09 on 07.
- Checked current source/package correspondence and prior criterion-specific assertion/test/TRX references. Prior pass labels were not accepted alone; direct evidence and its stated scope are the basis. No suites/packages/browser matrix were rerun; no new TRX IDs are claimed.
- Only these two audit files are owned outputs. No product code, test, package/feed, issue, or shared ledger changed.

## Current final package/source boundary

- `docs/implementation/ef2d0ea-package-proof.json` identifies final source commit and the review feed. The source archive is `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`; actual four package and symbol hashes are recorded there. The Cursory package has no runtime dependencies and includes source/notices/licenses; all 11 packaged C# source files match frozen source after newline normalization.
- The previous SDK10-vs-SDK8 IL discrepancy is resolved by rebuilding actual packaged standalone source with its SDK 8.0.319 pin: checker reports **432/432 entries, 23 types** equal, including methods/IL, exception regions and embedded resources. This is semantic/compiler correspondence, not whole-PE byte identity. See `artifacts/goal/final-compiler-correspondence/ef2d0ea/correspondence.json`.
- Actual SourceLink CDI map is `/_/*`. A producer display-field typo is not evidence that actual CDI mapping is absent; proof is based on actual CDI. Remote source retrieval was not tested.
- **Legal/distribution gap:** dataset rights remain `UNVERIFIED`; producer says `externalPublicationAllowed:false`. LGPL/copyleft source attribution and inclusion of notices are not legal clearance of dataset rights.

## Per-ticket tally

| Ticket | Title | Verified | Gap | Dependencies |
|---:|---|---:|---:|---|
| 01 | Tornar o movimento substituível sem mudar o comportamento atual | 10 | 0 | — |
| 02 | Gerar trajetórias gravadas em uma biblioteca .NET autocontida | 11 | 0 | — |
| 03 | Completar o algoritmo Cursory com paridade upstream | 12 | 0 | 2 |
| 04 | Executar Cursory por Mouse.MoveAsync com ativação explícita | 11 | 0 | 1, 3 |
| 05 | Manter o movimento correto sob atraso, escala e estado desconhecido | 11 | 0 | 4 |
| 06 | Preencher e limpar campos no modo compatível | 9 | 1 | — |
| 07 | Inserir texto e executar atalhos sem alterar seus eventos | 10 | 0 | 6 |
| 08 | Clicar, dar clique duplo e passar o mouse com ação final nativa | 10 | 0 | 1, 6 |
| 09 | Digitar com ritmo variável sem corromper conteúdo | 10 | 0 | 7 |
| **Total** | **95 criteria** | **94** | **1** | |

### Findings requiring explicit boundaries

- **02.09** is verified only for recorded provenance/conditions, an explicit unresolved-rights checklist, and fail-closed publication. Redistribution permission itself is **not verified**; do not present this as a legal pass.
- **06.09 — GAP.** Exact current `RemainingInputContractTests.Native_default_timeout_on_compatible_fill_preserves_playwright_exception_category` tests native timeout exception category for a hidden Fill target; it does not close the context while Fill is awaiting the unavailable field. `InputAcceptanceClosureTests.Repeated_wrapped_input_and_helper_lifecycles_close_idempotently_and_release_handles` closes only after successful typing/click. Close by adding/running a browser case that starts compatible Fill against an unavailable field, closes context while the native wait is pending, awaits the Fill task itself, and asserts prompt failure/no later effect/no orphan. Capture the exact passing TRX case.
- **06.10 — scoped verified.** Installed consumer evidence supports unchanged consumer source use/API shape; Windows installed proof reports candidate Cursory, Bézier and off paths plus rollback. It is not an exhaustive test of future method/options combinations.
- Input-contract review distinguishes source-level forwarding from behavior: SDK-owned methods use their own `Cleanup` and do not relax deadlines; direct caller options/raw Playwright route must remain forwarded. Existing option/interface checks are listed criterion-by-criterion in JSON; do not treat a public API reflection comparison alone as proof of runtime semantics.
- Numerical fixture tolerances remain upstream-approved; DPR remains 0.01; no broadened tolerance or timing budget is claimed. 10ms p95 is an investigation budget, not a CI acceptance threshold.
- No paired-seed before/after claim for historical unseeded Bézier RNG. Historical behavior has its original DOM task/baseline comparison; seeded exact vectors apply to Cursory fixtures, not legacy trajectories.

## Final run evidence and limits

- `docs/implementation/ef2d0ea-installed-windows-proof.json` and `...linux-proof.json` report 13/13 technical checks passed each; both retain the expected rights-only exit code 2. Linux consumer used Chrome for Testing 153; the proofs cover installed-package scenarios, not all ticket criteria or every platform/browser combination.
- Parent-supplied source-recovery execution evidence (native 37; closure 41; input 42) and browser matrix are historical, not rerun here. Reported Windows required/reviewed runs preserve the late-page 176+1 split; reported Chrome/Edge and Linux runs have separate TRX records. Those outcomes are not converted into an aggregate blanket pass; each JSON criterion row points to its direct assertion/evidence scope.
- Current acceptance is about finite requirements and equivalent prior source. No “all 260” statement is made. The canonical ledger remains untouched.

## Owned files and validation

- `docs/implementation/final-audit-01-09.md` — this human review.
- `docs/implementation/final-audit-01-09.json` — 95 criterion records: exact requirement, verified/gap, evidence paths/details, limitations, and exact closure cases for the gap.
- Validation was read-only review only; no tests rerun. `zg` was attempted but timed out; native file/test inspection was used afterward.
