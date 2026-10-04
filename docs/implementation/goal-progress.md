# Implementação do plano Cursory nativo — acompanhamento

Objetivo ativo: implementar integralmente o plano e os 24 tickets aprovados, sem reduzir o escopo a uma demonstração ou port aproximado.

## Retomada atual — `c28093f` (aceite ainda aberto)

- Estado inspecionado diretamente: HEAD `c28093f`; o patch parcial não commitado de Unicode/deadline em `HumanActions.cs` foi preservado em `.scratch/resume-human-actions-partial-preserved.patch` e removido da integração: ele falhava build com CS1061, chamando `GetRemainingBudgetMilliseconds` ainda ausente. A mudança completa continua na frente isolada, sem descartar sua evidência. Os 24 tickets existem em `.scratch/spybrowser-cursory-nativo/issues/`; os 260 critérios continuam exigindo aceite individual.
- A matriz registrada em `browser-matrix-current.md` contém execuções Windows/Linux e versões 1.61/1.63, mas é evidência histórica de source anterior, não feed final do HEAD atual. Relatórios independentes posteriores registram novos testes e pendências; seus mapeamentos também precisam ser conferidos contra os critérios originais.
- Três executores retomados em paralelo, configuração global verificada `gpt-6-luna/high`: `39f4cdb8-8ef7-4528-a976-76a4233aff19` (correções/testes 07/09/10 em worktree isolado), `96d748cf-1ff5-423a-bea7-fa4d99d97ea6` (auditoria read-only dos critérios) e `9bd29588-b7a1-4303-a903-dd7146a2c2c5` (auditoria read-only distribuição/consumidor).
- Verificação parental atual restaurada: 2/2 testes reais de wrappers/GC e Has/HasNot passaram, zero skips, TRX `artifacts/goal/resume-wrapper-doc-proof/wrapper-doc-proof.trx`. Documentação de wrappers foi alinhada à implementação e à análise de heap; a falha inicial de compilação permanece em `resume-wrapper-doc-proof.log`.
- Comando parental de wrappers: `SPYBROWSER_RUN_BROWSER_TESTS=1 dotnet test tests/SpyBrowser.Tests/SpyBrowser.Tests.csproj -c Release --filter 'FullyQualifiedName~WrapperContractMatrixTests.Frame_collections_payloads_diagnostics_and_weak_lifetimes_follow_contract|FullyQualifiedName~Wrapped_known_playwright_locator_argument_inside_options_is_unwrapped' --results-directory artifacts/goal/resume-wrapper-doc-proof --logger 'trx;LogFileName=wrapper-doc-proof.trx'`. Esta execução só prova esses dois contratos, não a matriz integral.
- Baseline e217 fornecida foi verificada novamente com `verify_previous_feed`: hashes dos pacotes, nuspec e archive exato passaram; artifact `artifacts/goal/resume-baseline-provenance.json`. Executor adicional `4634f58a-efb1-43fe-b747-29850212f933` refaz instalação preliminar com rollback correto; ainda não representa o feed final.
- Publicação externa continua proibida enquanto os direitos do dataset não forem verificados. Não se presume que esse gate impeça toda implementação/verificação local; as pendências técnicas continuam sendo trabalhadas.

### Integração da retomada — `45c7e64`

- Integrados `457fe8f` → `03f9ca3` (lease de contextos descartáveis e popups concorrentes), `f47b028` → `cc983ed` (validar dataset antes de efeitos do mouse) e `34d6eb2` → `45c7e64` (fallback conservador de grafemas e exceção de timeout nativo).
- Verificação parental focada: **11/11 aprovados**, zero skipped, `artifacts/goal/resume-integrated-input/integrated-input.trx`.
- Suite requerida completa Windows, browser e headed-probe habilitados: **36/36 Cursory + 145/147 Playwright**, zero skips. Duas fixtures de admissão com mocks falharam por budget/relógio sob execução conjunta; falhas preservadas em `artifacts/goal/resume-full-required/` e `resume-full-required.log`. Não é uma matriz verde. Executor `d8bd2398-cf1c-4d51-9672-26b7042d3591` investiga isolamento/seam da fixture, sem ampliar deadlines de produção.
- Lacuna concreta de orçamento identificada nesta rodada: fila consumia budget no gate, porém core criava deadline novo e passava timeout nativo configurado inteiro. Executor `7cc75c0f-79eb-42d8-9764-c368dde2ab6c` corrige lease/deadline compartilhado em `resume-shared-budget`; `c8bc5495-a3d4-4a64-8e0c-975df7229acc` completa provas Fill-close/contexto e click/options/partial-effect em frente separada.
- Integrados `356aaf6` → `98b5831` (lease/deadline compartilhado), `d0a5e64` → `c843ef8` (fixture de admissão sem pacing e na coleção de entrada temporizada), e `f66cd8f` → `3888f9c` (provas de falha após efeito/fallback sem caixa segura). Revisão parental `c75d42d` preserva options por clone e corrige foco de digitação para usar o budget restante. Testes novos verificam ambas as propriedades.
- A execução sequencial de projetos ainda expôs competição de fixtures dentro da assembly e foi encerrada pelo limite de 240s; log `resume-required-sequential.log` preservado. NativeCursoryMovement, ActionsCompatibility, DirectWrappedHelpers e FactoryStress agora usam a coleção temporizada já existente, mantendo todos os budgets/assertions e concorrência dentro de cada teste. Prova parental completa após isolamento: **159/159 Playwright aprovados**, zero skips, browser e headed-probe habilitados, `artifacts/goal/resume-required-input-isolated/required-input-isolated.trx`. Suite Cursory executada separadamente em `artifacts/goal/resume-required-cursory/`.
- A primeira prova effect-fault tinha erros da fixture (contador de sucesso e reflection de Trial nullable); eles foram corrigidos sem alterar runtime. Falha preservada em `resume-effect-budget.log`; rodada corrigida passou 11/11 em `resume-effect-budget-corrected/`.
- Auditorias de retomada são inventários de lacunas, não aceite definitivo. 23.05 permite bloqueio de distribuição explicitamente documentado; esse fato não libera publicação e não substitui qualquer gate técnico.

### Candidato de homologação `b1777b3` — ainda preliminar

- Feed imutável construído por archive exato em `D:/Temp/spybrowser-feed-review-b1777b3`, versão `0.2.0-beta.2.review.b1777b3`, quatro nupkgs e símbolos vinculados ao SHA completo. `isFinal=false`, sem autorização de publicação; fontes/licenças/PDB e rebuild do Cursory incluído passaram em `resume-native-package-inspection-b1777b3.log`.
- Windows atual: required 1.61 **159 + 36 aprovados**, latest 1.63 resolvido uma vez **159 + 36 aprovados**, sem skips. Chrome e Edge headed: **17/17 contratos selecionados** cada. Resultados em `windows-b1777b3-{latest,chrome,msedge}-results`; lanes de canais não são a suíte inteira.
- Linux Ubuntu24/Xvfb source b1777b3: TRXs required 1.61 headed **159 + 36**, reviewed 1.63 headed **159 + 36**, Chrome selecionado **17/17**. A falha da primeira tentativa required (158/159) foi preservada; relatório detalhado do executor deve explicar a diferença de ambiente/reexecução antes do aceite da matriz.
- Consumidor real: descoberto que o projeto principal pinado exclui V1. O harness foi corrigido para referenciar somente seu projeto de testes `RpaFlow.Legacy.Playwright` já existente, qualificando o DTO V1, sem alterar contratos/compilação da aplicação. Reruns limpos fora da árvore SpyBrowser passaram **13/13** em Cursory, Bézier e Humanize off, incluindo parsing/compilação/execução V1 e V2 com frames. Provas compactas em `consumer-b1777b3-{v1qualified,bezier-qualified,off-qualified}-proof/`; parity final ainda false, por declaração preliminar do feed. Falhas anteriores de harness e falta de disco permaneceram nos logs. Foi removido somente o cache NuGet gerado de uma execução parental já falha; as lanes seguintes foram para C:/Temp.
- Distribuição histórica com baseline correto: Windows e Linux non-root agora têm `technicalAllPassed=true`; direitos públicos continuam BLOCKED. O PENDING de chmod em root não foi creditado como prova de permissão. Isso verifica a revisão 1e9 histórica, não o novo candidato.
- A auditoria parental encontrou novas lacunas reais antes de declarar final: defaults de lançamento não registrados no humanizer, override por página sendo sobrescrito por contexto, pointer conhecido ficando obsoleto após ação nativa/reposicionamento, e cleanup legado podendo substituir a causa inicial quando Up falha. Executores `2c8d2487-1ab2-48e9-9696-23b37ff36516`, `5e407135-67de-4219-8056-7486b51dd24b` e `410af34e-66a6-420e-8cd0-9228d37e9dac` corrigem essas fronteiras em worktrees separados. O cap do budget SDK já foi corrigido parentalmente para não exceder `ActionDeadlineMilliseconds` quando o default nativo é maior, com teste direto passando.
- Portanto `b1777b3` não será rotulado como feed final: os pacotes e as provas instaladas precisam ser refeitos após essas correções. Nada foi publicado, nem o default Cursory promovido.

## Revisão integrada anterior — `3f864d4` (runtime `06aedb1`)

- Integrados `bc963f4` (GC com evidência de heap), `dc28870` (SaveAsync instalado real), `eb8d2f7` (comparação histórica NuGet-only), `7ae11b5` (consistência efetiva) e `06aedb1` (cancelamento/timeout nativo). A tolerância DPR continua 0,01; diferenças reais não foram ocultadas como zoom não observado. Categorias GPU usam gerações allowlisted, não modelos/hash de hardware.
- Windows requerido 1.61: antes da correção lifecycle, escopo sem matriz de ações passou **15 Cursory + 113 Playwright**, sem skips. Depois da integração lifecycle, execução completa passou **139/141**, com duas falhas preservadas: início de digitação sob carga e NullReferenceException nativa na corrida NewContext/Close. Logs/TRX em `artifacts/goal/{ee46710-required-results,lifecycle-merge-required-results}/`. Não é uma matriz final verde.
- Feed preliminar arquivado de `7ae11b5`, versão `0.1.0-review.7ae11b5`: **12 checks técnicos instalados aprovados em WSL**, incluindo SaveAsync real interrompido por OS, dois processos escritores, permissão Linux, descarte e rollback que ignora snapshot schema-999 sem alterá-lo. Não há alegação de um loader inexistente no pacote anterior. Evidência em `artifacts/goal/distribution-review-7ae11b5-complete-dependencies/`; `technicalAllPassed=true`, `isFinal=false`, `declaredFinalCommit=null`, publicação bloqueada. Falhas iniciais de restore por dependências offline ausentes foram preservadas.
- Executor #44 travado foi encerrado após preservar o patch; #55 entregou correção parcial integrada. Frentes atuais: #57 helpers públicos/cores internos/lease compartilhada e digitação; #58 corrida de fechamento/fábricas; #59 observação de dataset e alocação em processos novos; #60 auditoria independente dos tickets 15–19. Todos executor `gpt-6-luna/high`.
- Contrato dos **24 tickets / 260 critérios** agora versionado. O arquivo de evidência ainda não tem aceite definitivo; testes agregados não substituem auditoria critério a critério. Feed final, consumidor RpaBlockly, comparação histórica Windows/Linux e matriz completa serão refeitos após os fechamentos de produto.

## Baseline observado

- Worktree principal: `D:/SpyBrowser`.
- Baseline Git: `e217359d19a29635f2b3b5ba54664d299fd16d36`.
- Executor global verificado: `codex-account-pool / gpt-6-luna / high`.
- SDK Windows: .NET 8.0.319; .NET 9 disponível para contrato consumidor.
- Testes baseline: 38 aprovados. A suite antiga inclui gates que retornam sem browser; esse número não é prova de execução browser.
- Browser baseline realmente executado com `SPYBROWSER_RUN_BROWSER_TESTS=1`: 5 aprovados; evidência TRX em `artifacts/goal/baseline/`.
- Linux WSL Ubuntu 20.04: SDK 8.0.319 provisionado em diretório dedicado do usuário, baseline .NET executada e 5 testes reais de Chrome aprovados. Logs em `artifacts/goal/linux-baseline*.log`; TRX original na cópia Linux isolada.
- Chrome Linux observado: 154.0.8037.97. Xvfb/dependências e Chromium Playwright 1.61.0 foram provisionados para verificações posteriores. Docker daemon estava indisponível; WSL fornece o caminho Linux alternativo.

## Histórico das frentes iniciais

| Worktree | Executor/job | Tickets / limites |
|---|---|---|
| `D:/SpyBrowser-work/cursory` | executor #1 / `45b945f6-c8b8-42c9-8861-70d23e66e8d5` | 02–03: port puro, dados, licenças, paridade |
| `D:/SpyBrowser-work/actions` | executor #2 / `c37b6474-7d24-4319-9d75-82c7f8283754` | 01, 06–09: estratégia Bézier e semântica opt-in |
| `D:/SpyBrowser-work/wrappers` | executor #3 / `380cad78-592c-403e-82a8-25d521a73e15` | 11–14: factories, propagação e eventos |
| `D:/SpyBrowser-work/verification` | executor #4 / `c97f4d2c-0a35-4328-be5d-911652a2e0d1` | preparação 22; aceite após integração dos pré-requisitos |
| `D:/SpyBrowser-work/diagnostics` | executor #5 / `cbad35be-f1e1-40ac-9d04-e12d8af15014` | preparação 17–19; findings/snapshots com aceite pendente da integração 15/16 |

As alterações são integradas no worktree principal após verificação. Um retorno de executor não é, por si só, prova de que todo ticket está concluído. Critérios serão auditados contra código, execução, testes e artefatos.

## Regras de escopo preservadas

- Options que desativam humanizer no RpaBlockly continuam aceitas como fallback; esse ajuste foi adiado pelo usuário.
- Não alterar blocos/DSL/solvers CAPTCHA do consumidor.
- Nenhuma publicação externa ou promoção silenciosa de defaults faz parte da implementação local.
- DLL LGPL separada e cadeia de proveniência devem ser verificadas; não inventar parecer jurídico.
- Não prometer invisibilidade nem redução quantitativa de CAPTCHA sem experimento correspondente.

## Verificação final pendente

Nenhum dos 24 tickets foi marcado como concluído nesta etapa. São necessárias integração, auditoria de critérios, execução Windows/Linux, homologação do consumidor real, benchmark e instalação/rollback dos pacotes.

## Integração inicial e auditoria

- Integrado `2ea9033` como `022ba02` (estratégia Bézier e comandos compatíveis).
- Integrado `4ae9a57` como `9cab3eb` (fábricas/wrappers).
- Verificação conjunta Windows Release com browser habilitado: **43 testes aprovados**, TRX em `artifacts/goal/wave1/`.
- Retornos dos executores identificaram critérios não satisfeitos: deadline total de clique, cancelamento de operação realmente em curso, corrida de evento antes de preparar contexto, frames cross-origin/nested, AllAsync, inscrição/remoção de eventos e cleanup. Novas execuções foram delegadas para correção; não são considerados concluídos por terem compilado.
- Inspeção do port inicial `6c8391c` na frente Cursory constatou paridade ainda vermelha e aritmética de rank inteira indevida. Uma frente isolada `D:/SpyBrowser-work/cursory-parity` foi delegada para corrigir paridade e legibilidade; o port incompleto não foi ativado no runtime principal.
- Falhas de localização de escopo nas frentes CI/diagnóstico foram resolvidas reenviando caminhos absolutos de tickets/plano confirmados no disco.
- O projeto real `RpaFlow.Playwright` no checkout fixado foi compilado com sucesso usando suas dependências originais. Essa baseline ainda não comprova a compatibilidade do pacote candidato.
- Correção pontual de rank no worktree `cursory-parity`: trocar divisão inteira por fração resolveu a divergência existente; **8 testes passaram** em Release. Isso não substitui os vetores, invariantes e fixtures ampliados exigidos pelo ticket 03.

## Histórico das primeiras execuções corretivas

| Executor | Job | Entrega pendente |
|---|---|---|
| #6 | `361f0213-cba7-4c41-8a5a-b5d120f6d2f6` | findings/snapshots/CLI 17–19 |
| #7 | `6ea34da1-3bfc-492d-8b5e-6f1cae137d29` | CI/skip/API audit 22 |
| #10 | `db8c32b6-ff2b-408f-8cee-429167575ace` | deadlines/lifecycle de entrada 08–10 |
| #11 | `5f469033-e2dd-43bb-920f-a579b79fe4ff` | ampliar/finalizar paridade e manutenção 03 |
| #12 | `651af8b5-9318-4697-9b49-c07efa6cd425` | finalizar coordenação/eventos 13 |
| #13 | `87a6e5b0-56c2-4ee5-9729-0d1cbe58979a` | critérios de cobertura 12/14 |
| #14 | `fed108c7-3604-4d32-bac6-e96278a13ca5` | probe isolado/timeout/browser-only/config efetiva 16 |

#9 parou por tentar ler um nome de ticket inexistente diferente do caminho solicitado; o novo envio #11 limita sua leitura ao caminho absoluto correto do ticket 03. Não houve mudanças do #9.

## Integração mais recente — `941603e`

- CI mantém 1.61.0 obrigatória também em schedule/manual e adiciona latest resolvido uma vez. A auditoria 1.63.0 é separada e revisada; desconhecidos continuam exigindo revisão. Execução Windows anterior: 45 testes em 1.61 e 45 em 1.63; Linux headed/Chrome e Windows Edge foram realmente exercitados. Esses resultados não substituem a matriz final após todas as features.
- Paridade nativa integrada: 160 trajetórias, três fixtures intermediárias instrumentadas no JS fixado, RNG/tails/rejeição, invariantes e concorrência. Algoritmo decomposto em componentes legíveis; licença/proveniência de dados não foi presumida resolvida.
- Cursory opt-in integrado com estado por página, anchor explícito quando posição desconhecida, relógio monotônico, coalescing e testes DPR 1/1.25/1.5/2.
- Após resolver merges, **82 testes passaram em Windows e Linux**. A integração seguinte de paridade completa/probes/benchmark passou com **84 testes Windows (69 + 15, contadores TRX inspecionados)**; o total Linux deve ser confirmado nos respectivos artefatos, sem reutilizar a contagem Windows; logs `artifacts/goal/windows-integrated-wave3.log` e `linux-integrated-wave3.log`. TRX usam nomes únicos para não sobrescrever a outra assembly.
- Bugs encontrados e corrigidos no principal: retorno genérico de SelectOptionAsync perdido pelo gate, observação raw de mouse omitida, hashes de fixtures/licenças dependentes de CRLF, PowerShell 5.1 sem ArgumentList, demonstração NuGet reutilizando cache de candidato anterior.
- Demonstração pura instalada do NuGet: `windows-package-demo-verified-wave3.log`, exit 0. Verifica nupkg/nuspec, LGPL/helpers, gzip, PDB, rebuild das fontes extraídas, ausência de dependências de runtime fora da BCL e execução com PATH restrito/sem processos filhos observados. Não equivale à homologação de toda a distribuição SDK.
- Benchmark comparativo real executado, exit 0: `windows-motion-quality-wave3.log` e `artifacts/goal/motion-quality-wave3/`. Resultados precisam de auditoria de escopo e associação ao candidato final.
- Redistribuição externa continua explicitamente bloqueada pela revisão independente de direitos do dataset; o checklist não é parecer jurídico. Feed local e fontes correspondentes são mantidos para homologação técnica.

### Dependências finais delegadas (gpt-6-luna/high; histórico da onda anterior)

| Executor | Job | Escopo |
|---|---|---|
| #24 | `da585631-252a-4e7f-84bd-a31bd9f65f1a` | observação privada de humanização, ticket 15 |
| #25 | `0ef81537-240a-4530-af18-4405a5aff780` | concluir findings/snapshots/CLI, tickets 17–19 |
| #26 | `15b8fa34-92d2-4691-9152-61433130b430` | completar consumidor NuGet/Rpa real, ticket 20 |
| #27 | `cbdc1ee5-ff63-4795-abe7-58d0f53729a8` | harness instalado, rollback e snapshots distribuídos, 23/24 |
| #28 | `bdb51d42-4a61-44d0-a85d-8456c2d8cf5e` | defeitos restantes de deadline/defaults/cancelamento público, 10 |
| #29 | `9c067a34-08a5-4696-a828-455aebb74603` | auditoria read-only de contratos 06–09/11–14 |

### Integração `31b2dbc` e auditoria de lacunas

- Integrados consumidor `6009e72` → `e768d3f`, lifecycle `baaf290` → `8bf2ec6`, diagnóstico privado `f39967e` → `5caf68f` e snapshots/CLI `891b973` → `2ba48c5`. Merges preservaram defaults de contexto, filtro por identidade dos probes e expectativas efetivas.
- A primeira execução combinada falhou em quatro mocks de página com `Context` nulo; `31b2dbc` corrigiu os doubles para o contrato oficial. Os seis testes focados de diagnóstico passaram com browser habilitado.
- A segunda execução combinada tem **15/15 Cursory + 86/87 Playwright**, não aprovação integral: DPR 2 excedeu o deadline Cursory durante execuções concorrentes. Falha preservada em `windows-integrated-wave4-fixed.log` e TRX; precisa de investigação/validação controlada, não retry silencioso nem aumento do budget de produção.
- Auditoria read-only de oito tickets encontrou **24 PASS, 45 PARTIAL e 13 MISSING** em 82 critérios. Resultados em `artifacts/goal/audit-contract-slices.{md,json}`; outros tickets ainda precisam de auditoria individual.
- Novas frentes isoladas a partir de `31b2dbc`: `direct-actions-final` (lease das APIs públicas), `action-contract-matrix` (06–09), `wrapper-contract-matrix` (11–14), `probe-contract-final` (16), `consumer-flows-final` (V1/V2 locais e contratos do plano §10). Auditorias read-only complementares de 01–05/21 e 15/17–19 estão em execução. `distribution-checks` continua construindo o harness instalado/rollback (23/24).

### Integração `066abff` e validação focada Windows/Linux

- Integrei `0ced5ca` → `4749f49` (Has/HasNot não mutantes), `4e87c24` → `255973f` (projeção privada de snapshots), `88a921d` → `9b6f69d`, `ce38c33` → `9de5cfb` e `4bdb064` → `066abff` (evidência nativa). O relatório benchmark retido é vinculado à fonte limpa histórica `ce38c33`, não ao candidato final.
- Verificação parental em `066abff`: **15 Cursory + 19 Playwright = 34 executados/passados, zero skipped/falhas, em cada sistema Windows e Linux**. Escopo: paridade/invariantes nativos, oito testes de movimento, nove snapshot-store e dois probe-cleanup. TRX inspecionados em `artifacts/goal/parent-native-current/` e `artifacts/goal/linux-native-diagnostics-current/`; logs `windows-native-diagnostics-current.log` e `linux-native-diagnostics-current.log`. Não é a suíte integral nem a matriz final.
- SDK WSL está disponível em `/home/rodrigo/.dotnet-spybrowser`; usar PATH explícito. Falha anterior de cópia Windows devido a testhost foi preservada em `windows-native-resize-final.log`; execução em output isolado com `--artifacts-path` passou. Um baseline de ações permaneceu travado por cerca de uma hora: somente seu testhost foi encerrado, com identificação preservada em `hung-action-baseline-process.log`, sem tratá-lo como pass.
- Wrapped closed-context continua retido, apesar de controles raw coletados; a regressão GC continua intacta. Quatro casos de ações/lifecycle continuam aguardando correção e execução. A projeção privada foi reforçada, mas provenance real, observer default-off e CLI instalado completo ainda têm lacunas.
- Frentes gpt-6-luna/high: #44 lifecycle/router (`lifecycle-router-final`); #46 harness instalado (`distribution-final`); #47 retenção wrappers (`wrapper-retention-final`); #48 recorder/provenance/CLI (`monitor-evidence-final`); #49 consistency 17/18 (`consistency-evidence-final`); #50 comparação histórica NuGet (`historical-motion-comparison`). Todos os caminhos são absolutos, separados dos SHAs; novas tarefas exigem `cd` explícito por comando e reads/edits absolutos.
- Rights da dataset continuam não verificados, publicação externa bloqueada. Gate explícito `24fa468` mantém release fail-closed. Feed histórico e consumidor anteriores não comprovam candidato final; ainda faltam package linkage, rerun Rpa V1/V2, rollback/snapshots instalados e required/latest Windows/Linux/headed/channel final.

### Integração `d79c085` e continuação de lacunas

- Integrados `a2e89e0` → `24acdcd` (harness/proveniência do feed) e `321d5ec` → `d79c085` (proveniência runtime real por handle, recorder tolerante e projeção de comparação). Verificação parental browser-enabled focada: **18/18, zero skips/falhas**, TRX em `artifacts/goal/parent-provenance/`, log `windows-provenance-parent.log`. Ainda não é execução instalada ou aceite integral.
- Revisão direta do harness encontrou evidência de interrupção inválida: filho escrevia um FileStream próprio, não `DiagnosticSnapshotStore.SaveAsync`, e o pai removia os temporários manualmente. Também há FAIL incondicional para GPU/context após sucesso do snapshot. Essa verificação não é creditada; #54 (`distribution-installed-final`) recebeu correção e execução real de feed preliminar explicitamente não-final.
- #47 parou após reproduzir retenção, sem encontrar root; `dotnet-dump 8.0.547301` foi instalado localmente com sucesso em `.scratch/dotnet-dump/`, e #51 retomou tracing/correção em `wrapper-retention-final`. #49/#50 pararam por nomes de plan/ticket inventados; #53 consistency e #52 histórico retomaram com caminhos canônicos corrigidos. `.scratch/spybrowser-cursory-nativo/plan.md` é somente apontador para o plano aprovado em `docs/plans/`, não especificação nova.
- Permanecem dependências: #44 lifecycle/router, #51 wrappers/retention, #52 comparação histórica, #53 consistency e #54 snapshots/distribuição instalada. Feed final, consumidor real, matriz completa e evidência dos 260 critérios ainda não foram fechados.

Nenhum retorno ou contagem isolada de testes encerra os 24 tickets. Os 260 critérios continuam sujeitos à auditoria individual em `ticket-evidence.json`; integração final, pacote instalado, consumidor/rollback e matriz após todas as features permanecem necessários.
