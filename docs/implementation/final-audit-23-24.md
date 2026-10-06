# Final audit: tickets 23–24

Frozen candidate `0.2.0-beta.2.final.ef2d0ea` / `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`. Feed `D:/Temp/spybrowser-feed-final-ef2d0ea`; source archive SHA-256 `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`; producer manifest SHA-256 `689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`.

## Result

All 22 literal criteria were individually audited: **21 verified, 1 gap(s), 0 unreviewed**. Local criterion-level evidence only; not an external-release approval. Canonical ledger was not edited.

### Gaps

- **24.09** — Native proof is now reviewed and binds benchmark/historical evidence to ef2d, but `docs/implementation/final-release-checklist.md` remains stale: its native-report acceptance item is unchecked and review is described as pending. Update that checklist item to cite the native proof and observations, keeping timing descriptive; do not imply a universal 10 ms guarantee.


## 23.01 — VERIFIED

**Pacotes são construídos e instalados via feed local em consumidor limpo, não apenas por project reference dentro da solution.**

Installed Windows proof JSON: actualInstalledConsumer.cwdOutsideRepository=true, projectReference=false; distribution-checks/run.py rejects ProjectReference. Linux consumer is isolated at /home/spyreview/installed-ef2d0ea-work. Exact final feed/package hashes match frozen ef2d0ea.

Evidence: `docs/implementation/ef2d0ea-installed-windows-proof.json`.

## 23.02 — VERIFIED

**O grafo contém a DLL Cursory separada, dados embarcados e nenhuma dependência adicional de servidor/processo de geração.**

Package proof: actual Cursory package has only lib/net8.0/SpyBrowser.Cursory.dll, embedded gzip data once, no runtime dependencies or Playwright; source rebuild 432 entries/23 types/zero differences. tools/package-demo/verify_package.py checks DLL list and data hash.

Evidence: `docs/implementation/ef2d0ea-package-proof.md`.

## 23.03 — VERIFIED

**O teste reconhece a instalação e o driver normal do Playwright; não exige erroneamente a ausência de todo Node no processo de automação.**

Installed Program.cs prints official Microsoft.Playwright package driver; actual named browser-executable-and-official-driver check PASS Windows and Linux. Normal browser driver is allowed; absence of all Node is not required.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 23.04 — VERIFIED

**Conteúdo real de nupkg/nuspec, fontes correspondentes, símbolos, licenças e avisos de código/dados/helpers são verificados.**

Package proof: four actual nupkg + four snupkg hashes; full commit nuspec; four actual SourceLink CDI mappings, 58 source checksums, packaged corresponding sources/licenses/notices, standalone rebuild/repack correspondence. verify_package.py PASS package/nuspec/source/data/licenses/symbols. Display typo annotation distinguishes canonical CDI /_/*; remote source access not claimed.

Evidence: `docs/implementation/ef2d0ea-package-proof.json`.

## 23.05 — VERIFIED

**Checklist LGPL/proveniência está concluído ou aponta bloqueio explícito de distribuição; separação em DLL não é apresentada como parecer jurídico suficiente.**

distribution.md and upstream-manifest record rights unresolved. Both installed proof JSONs: technicalAllPassed=true, 13 PASS/zero failures/pending; allPassed=false, exit 2 solely external-dataset-distribution-license, externalPublicationAllowed=false. This meets explicit block alternative, not legal clearance.

Evidence: `docs/implementation/ef2d0ea-installed-windows-proof.json`.

## 23.06 — VERIFIED

**Ativar Cursory, selecionar Bézier e Humanize=false funcionam pelos mesmos comandos Playwright, sem fallback silencioso durante uma ação.**

Windows/Linux named installed checks candidate-cursory, candidate-bezier, candidate-off PASS. distribution-checks/Program.cs asserts phase, algorithm, Humanize and completed click; no silent retry/fallback is claimed.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 23.07 — VERIFIED

**Pin da versão anterior é demonstrado sem migração destrutiva de identidade/perfil; recursos continuam descartados corretamente.**

Installed Windows/Linux evidence: previous-package-provenance, previous-bezier-rollback, previous-humanize-off PASS. Program.cs asserts disposed context/unchanged identity manifest; actual prior e217359 opens same profile and leaves schema-999 snapshot byte-exact.

Evidence: `docs/implementation/ef2d0ea-installed-linux-proof.json`.

## 23.08 — VERIFIED

**Releases documentam semântica do modo compatível, diferenças do legado, matriz homologada, limites do produto e rollback.**

Release notes document compatible vs Legacy semantics, raw limitations/deadlines, matrix and rollback. Current matrix binds frozen 4d browser suites and preserves Windows 176+1 late NewPage timeout plus focused and sequential full pass; no ef2d full-suite rerun claimed.

Evidence: `docs/release-notes-0.2.0-beta.2.md`.

## 23.09 — VERIFIED

**Evidências RpaBlockly, browser contracts, paridade e benchmark são associadas ao candidato instalado.**

Final Rpa package proof: three lanes 14/14, final parity true; browser matrix: source-bound 177+37 lanes, not relabeled ef2d reruns. ef2d0ea-native-proof.json/.md directly bind native package consumer, existing 37-test TRX, three fresh-process benchmark runs and historical 28 DOM observations/API diff to frozen ef2d candidate. Benchmark has 1 cold/20 warmups/500 timed calls/process; observed warm p95 11.049,13.597,10.106 ms, two exceed 10ms investigation budget. Criterion asks association, not a performance threshold; no universal CI guarantee or paired RNG claim.

Evidence: `docs/implementation/ef2d0ea-native-proof.md`.

## 23.10 — VERIFIED

**Nenhum default global é promovido silenciosamente; Cursory permanece opt-in até decisão explícita.**

Release notes state Cursory opt-in, Bézier/Legacy default and Humanize=false raw; installed candidate-cursory/bezier/off checks PASS. No global promotion or production source change.

Evidence: `docs/release-notes-0.2.0-beta.2.md`.

## 23.11 — VERIFIED

**Snapshots históricos não são dependência obrigatória desta pré-release; se já incluídos no pacote, só são anunciados como homologados após o ticket 24.**

Release notes explicitly say final-package installed consumers exercised snapshot save/select/compare/discard and criterion review remains distinct. Windows/Linux named checks PASS, so claims technical exercise after ticket-24 technical gates, not overall release acceptance.

Evidence: `docs/release-notes-0.2.0-beta.2.md`.

## 23.12 — VERIFIED

**Publicação em NuGet/GitHub externo exige aprovação operacional separada; este ticket não autoriza esse efeito por si só.**

Release notes/checklist and installed proof say publication prohibited/not authorized, externalPublicationAllowed=false; no publication occurred. Separate approval requirement remains in force; this audit grants none.

Evidence: `docs/implementation/final-release-checklist.md`.

## 24.01 — VERIFIED

**Consumidor instalado a partir de pacote executa salvar, selecionar baseline, comparar e descartar snapshots, sem acesso ao workspace de desenvolvimento.**

Actual package-only Windows/Linux snapshot-save-explicit-baseline-compare-concurrency-schema-secrets-discard checks PASS; cwd outside repo/no ProjectReference. Installed Program.cs reflects DiagnosticSnapshotStore signatures and executes SaveAsync/ReadAsync/Compare.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.02 — VERIFIED

**Schema de snapshot é independente do schema de identidade e da versão dos dados do algoritmo.**

Program.cs creates snapshot/runtime version records independently of identity manifest or dataset; unknown schema rejection assertion passes. Snapshot operations manual states snapshot, identity and dataset schemas independent.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.03 — VERIFIED

**Rollback do pacote não altera/corrompe perfil ou manifesto de identidade; snapshots de versão mais nova são ignorados ou rejeitados de forma clara conforme contrato.**

Windows/Linux previous-bezier-rollback and previous-package-ignores-newer-snapshot checks PASS. Program.cs verifies schema-999 fixture hash unchanged and old package opens same identity/profile/storage state. No old-reader compatibility is claimed.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.04 — VERIFIED

**Desligar persistência de snapshots e excluir arquivos do recurso não remove storage state, cookies, perfis ou identidades.**

Program.cs discard assertion deletes only snapshot directory and verifies identity manifest, profile, storage state unchanged; named installed discard PASS both platforms. Disable by omitting dir/store call per manual.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.05 — VERIFIED

**Falta de permissão, interrupção de escrita e sessões concorrentes são exercitadas no formato distribuído, preservando baseline anterior válida.**

Actual NTFS ACL (Windows) and nonroot Linux permission denial PASS; OS termination during SDK temp write preserves baseline; concurrent two-process saves make unique complete files and preserve baseline. Windows orphan temp caveat retained; no cleanup guarantee.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.06 — VERIFIED

**Testes confirmam ausência de secrets e isolamento dos dados do recurso, inclusive após erros e rollback.**

Program.cs injects privacy sentinels/canvas/GPU error and rejects their appearance in snapshot. Installed save/error/concurrency/crash/rollback tests pass; evidence records hashes only and no cookie values.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.07 — VERIFIED

**Mudanças legítimas de versões e GPU aparecem como informação contextual, sem alterar automaticamente a baseline nem bloquear a execução.**

Program.cs requires version/algorithm/GPU/context deltas to be informational and asserts webgl1.renderer-category severity=Information; named installed snapshot-gpu-context-information PASS both platforms. No automatic promotion/block.

Evidence: `tools/verification/distribution-checks/Program.cs`.

## 24.08 — VERIFIED

**Manual operacional cobre ativação, comparação, retenção, exportação quando disponível e descarte seguro.**

snapshot-operations.md covers activation, explicit comparison/baseline, retention, safe permissions, failure/rollback, discard; explicitly says no dedicated export API, addressing export when available.

Evidence: `docs/snapshot-operations.md`.

## 24.09 — GAP

**Release checklist da entrega completa associa snapshots às evidências de compatibilidade, licenciamento e desempenho do núcleo.**

ef2d0ea-native-proof.json/.md has now been reviewed and binds candidate-native tests, benchmark observations and historical/API results to the frozen package, so performance evidence is available. However docs/implementation/final-release-checklist.md is stale: it still leaves “Accept the recovered final-package native/benchmark/historical report” unchecked and says criterion review pending. Update that checklist entry to cite the native proof and measured results (keeping timing descriptive); only then does the release checklist fully associate snapshots with compatibility, licensing and performance evidence.

Evidence: `docs/implementation/final-release-checklist.md`.

## 24.10 — VERIFIED

**Não se publica externamente ou altera defaults globais sem aprovação específica, mesmo com todos os testes concluídos.**

Installed proofs/checklist state externalPublicationAllowed=false, no publication or promotion; release notes preserve defaults. Criterion requires separate approval, not proof of unauthorized publishing; no approval is granted here.

Evidence: `docs/implementation/final-release-checklist.md`.

## Dependencies and scope limits

Ticket 23 depends on 18, 20, 21, 22; ticket 24 on 19 and 23. This audit evaluates literal criteria and direct artifacts/assertions; it is not dependency-closure certification.

* Dataset redistribution rights remain **UNVERIFIED/BLOCKED**. Both installed runners: 13 technical passes, zero fail/pending, but `allPassed=false` / exit 2 solely for rights. This is permitted by 23.05 and is not clearance.
* Package evidence binds exact source ef2d0ea/version. Actual CDI is canonical `/_/*`; display-field typo is annotated. No remote source-access or whole-PE identity claim.
* Installed Windows and nonroot Linux: 13 technical passes each. Three RpaBlockly final-package lanes: 14/14 each and final parity true, plus separate 17 semantic assertion groups. Pinned original consumer checkout unchanged; mixed provider/CAPTCHA/network suite not run.
* Browser source contracts are frozen 4d runs with unchanged production C#; Windows reviewed initial 176+1 late NewPage timeout remains preserved, with focused and sequential full pass. Linux source lanes include required 161/reviewed 163 and six Chrome TRXs zero skip. These are not ef2d full-suite reruns.
* No external publishing/default promotion. Cursory remains opt-in; Bézier/Legacy defaults and raw `Humanize=false` unchanged. 23.12/24.10 require separate approval; no approval is inferred.
* Benchmark uses three fresh processes, 1 cold/20 warm/500 runs each; 10ms p95 is investigation target, not CI guarantee. Old RNG is unseedable; no paired comparison. Snapshots optional; not identity backups.

## Audited inputs

Literal tickets `.scratch/spybrowser-cursory-nativo/issues/23-prerelease-e-rollback.md` and `24-snapshots-distribuicao-final.md`; canonical `docs/implementation/ticket-evidence.json`; `docs/snapshot-operations.md`; `docs/release-notes-0.2.0-beta.2.md`; `docs/implementation/final-release-checklist.md`; `docs/implementation/browser-matrix-current.md`; `docs/implementation/goal-progress.md`; candidate package/installed/consumer proofs and named `distribution-checks/Program.cs` assertions.

No ledger, source, package, feed or other document was edited; no commit, publication or default promotion.
