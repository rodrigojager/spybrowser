# Plano de evolução do SpyBrowser: qualidade, compatibilidade e Cursory nativo

**Status:** proposta de implementação; nenhuma alteração de runtime está sendo feita neste plano.  
**Data da análise:** 03/10/2026.  
**Objetivo:** aumentar a qualidade da interação e a coerência das sessões em automações autorizadas, preservando as interfaces e os contratos do Microsoft.Playwright, com integração real ao RpaBlockly e sem um processo adicional Node/Python para gerar movimentos.

## 1. Resumo executivo e decisões

A evolução será incremental: não criar um fork de Chromium, não reescrever o Playwright, não substituir o RpaBlockly e não adicionar scripts de mascaramento indiscriminadamente.

### Decisões principais

1. **Porte C# real do Cursory**, não uma biblioteca apenas inspirada nele: seleção de gravações, transformação geométrica, temporalidade, jitter, knots e subconjunto numérico necessário, com referência upstream fixada e testes de paridade.
2. **Uma única biblioteca nova de produção:** `SpyBrowser.Cursory`, .NET 8, LGPL-3.0-or-later, independente de Playwright. O restante da integração permanece em `SpyBrowser.Playwright`.
3. **Sem servidor/processo extra de humanização.** Os dados vêm embarcados na DLL e o algoritmo roda no processo .NET. O Playwright .NET já utiliza seu próprio driver; este plano não o remove nem promete um processo inteiro sem Node.
4. **Interfaces oficiais mantidas:** `IBrowser`, `IBrowserContext`, `IPage`, `IFrame`, `IFrameLocator`, `ILocator`, `IMouse`, `IKeyboard` etc. Os blocos existentes não recebem uma API de automação nova.
5. **Semântica antes de aparência:** `InsertText` não vira `Type`; atalhos continuam atalhos; clique duplo continua gerando clique duplo; `Fill` não vira digitação por padrão no novo modo recomendado.
6. **Playwright continua responsável pela ação final:** movimentação preparatória pode ser humanizada, mas clique, foco, preenchimento e verificações de acionabilidade continuam sendo delegados sempre que possível.
7. **Fallback conservador antes de efeitos irreversíveis:** operação sem suporte comprovado segue pelo Playwright original, com motivo diagnosticável. Nunca repetir automaticamente um clique ou preenchimento parcialmente executado.
8. **Não mudar padrões em uma correção silenciosa:** introduzir o novo comportamento opt-in em uma pré-release; promover somente após validação do RpaBlockly, com release notes e opção de rollback.
9. **Identidade real/coerente, não fingerprint aleatório:** ampliar validação e observabilidade sem adulterar mais características do navegador.
10. **Licenciamento como gate inicial:** a origem de código, dados e fixtures, a distribuição da DLL e as obrigações LGPL precisam estar documentadas antes de publicar.
11. **Ajuste de chamadas do RpaBlockly fica para depois:** conforme decisão do usuário, não ampliar agora o suporte a options só para evitar que chamadas do consumidor desativem o humanizer. Esse comportamento pode permanecer como fallback documentado e não bloqueia a entrega.

### O que este projeto não promete

- Não promete invisibilidade, eliminação de CAPTCHAs ou uma porcentagem de redução de detecção.
- Não replica os patches internos de Firefox/Juggler do Camoufox em Chrome.
- Não inclui resolução de CAPTCHAs, alterações nos solvers de outros projetos ou contorno de controles de acesso.
- Não afirma que movimento mais natural equivale a passar por uma pessoa em qualquer classificador.
- Não mede qualidade pela aprovação em um único site público de fingerprint.

---

## 2. Evidências e correções da visão atual

### 2.1 Referências examinadas

| Projeto | Referência examinada | Papel no plano |
|---|---|---|
| SpyBrowser local e `origin/main` | `e217359d19a29635f2b3b5ba54664d299fd16d36` | Baseline de implementação e API |
| RpaBlockly `main` remoto | `c2f2947c3ccd8b20f7a1cdf9c3b41fb68567b6ca` | Consumidor real, não apenas exemplo hipotético |
| Cursory Python | `5caa9d3477a1ff7b2d413978ea168a93fdc01dfc` | Referência algorítmica original |
| cursory-js | `16fff97fab05bb6b0c6753b2dc136a7692634cec` | Referência do porte e helpers de paridade |
| Camoufox desenvolvimento | `05a9f60c1b578767b23f3930fc8b4acf6fd8a787` | Referência de integração, não dependência de runtime |
| Camoufox release examinada | `v152.0.4-beta.30` | Não confundir funcionalidades da release com as do desenvolvimento |

Na implementação, resolver novamente essas referências, preservar as escolhidas no manifesto de proveniência e não atualizar silenciosamente para `main`.

### 2.2 O RpaBlockly já está integrado

A documentação atual `docs/rpablockly-integration.md` está defasada em relação ao repositório remoto examinado:

- `src/RpaFlow.Playwright/RpaFlow.Playwright.csproj` já referencia `SpyBrowser.Playwright 0.2.0-beta.1` e `Microsoft.Playwright 1.61.0`.
- O consumidor usa `net9.0`; SpyBrowser usa `net8.0`.
- `BrowserLauncher.cs` já oferece SpyBrowser, com humanização configurável e seleção padrão nos fluxos examinados.
- Cria identidade em memória, usa Chromium do Playwright, locale do runtime e fuso local normalizado.
- Usa `LaunchBrowserAsync`, **não perfil persistente**. A persistência de cookies/armazenamento é tratada pelo `StorageStatePath` no runner; isso não equivale a um perfil completo de navegador.
- Desativa `RunGpuProbe` nessa integração.
- Tem um `SpyBrowserSessionBrowser : IBrowser` para encaminhar fábricas e manter eventos/listagem de contextos.
- Testa referências de objetos, contextos fechados, humanização on/off, fuso e cancelamento.
- `LocatorActions.FillWithRuntimeAsync` chama `FillAsync` com `Timeout` explícito. O usuário decidiu ajustar posteriormente as chamadas do RpaBlockly que hoje desativam o humanizer; resolver esse ponto não pertence ao escopo obrigatório desta entrega.
- O projeto também usa o pacote original CloakBrowser. **Não introduzir nele a fachada de compatibilidade que também emite `CloakBrowser.dll`.** Manter o pacote nativo SpyBrowser lado a lado com o Cloak original.

**Consequência:** a integração não começa trocando pacotes Cloak. Deve evoluir a integração SpyBrowser existente, sem remover os outros providers.

### 2.3 Fragilidades concretas do código atual

| Evidência | Consequência / hipótese a confirmar com teste | Prioridade |
|---|---|---|
| `HumanActions.MoveMouseAsync` gera Bézier e usa delays relativos por ponto | Modelo limitado; custo das chamadas se soma ao tempo planejado | P1 |
| `HasOnlyDefaultOptions` aceita apenas ausência/null de opções | Até um `Timeout` explícito desativa humanização; objeto vazio também. Comportamento conhecido, aceito temporariamente pelo usuário | Fora do escopo obrigatório; ajuste posterior no consumidor |
| `IKeyboard.InsertTextAsync` é encaminhado para digitação | Altera a sequência de eventos esperada | P0 |
| `PressFocusedAsync` faz `Down(key)` e `Up(key)` | Combinações como `Control+A` precisam manter a semântica de `PressAsync`; não pressupor equivalência | P0 |
| `DoubleClickAsync` emite dois ciclos Down/Up comuns | Não há garantia de `dblclick` e `detail` corretos | P0 |
| `FillAsync` pode virar clique, seleção total e digitação | Não preserva todos os tipos de input, eventos, foco e custo temporal de Fill | P0 |
| `MoveToLocatorAsync` faz trial e depois clica por coordenada | O elemento pode mudar entre a verificação e a ação | P0 |
| Wrapper não trata `IFrameLocator` em `WrapValue` | Locators derivados de frames podem sair sem decoração | P0 |
| Eventos são delegados sem adaptar objetos de callback | Pages/popups/contextos recebidos por evento podem ser raw | P0 |
| Listas de contexts/locators e outros retornos não têm cobertura completa | Quebras de propagação ou identidade de referências | P0 |
| `Browser` decorado e fábricas do handle têm caminhos distintos | `Browser.NewContextAsync` pode não passar pela configuração aplicada no handle | P0 |
| RpaBlockly usa adaptador próprio para esse problema | Trabalho duplicado no consumidor e no SDK | P1, só remover após prova |
| Probe executa na primeira página disponível em alguns modos | Não deve interferir em abas restauradas ou aplicações do usuário | P1 |
| Validação compara timezone por string | Aliases como UTC/Etc/UTC podem produzir falso erro | P1 |
| Testes de navegador retornam sem executar quando flag está ausente | Resultado “passou” pode ser confundido com cobertura real de browser | P0 de CI |

Esses itens vêm de inspeção estática. Antes de corrigir, criar testes reproduzíveis; não apresentar falhas de execução como já demonstradas.

### 2.4 Diferença real nos dados Cursory

Na inspeção:

- Cursory Python: **2.357** gravações, arquivo gzip de 637.912 bytes.
- cursory-js no commit fixado: **2.356** gravações, gzip de 637.556 bytes.
- O commit do port remove uma gravação identificada como `2281`, com longa pausa inicial.
- Parte dos textos upstream ainda diz que os datasets são idênticos. Não confiar nessa afirmação sem comparar bytes e conteúdo.

**Escolha inicial:** utilizar o dataset do cursory-js fixado como referência de produção, após validação. Registrar explicitamente a diferença para o Python; regenerar fixtures com o mesmo dataset. Não misturar resultados gerados com 2.357 gravações com execução usando 2.356.

---

## 3. Escopo organizado por retorno e risco

### P0 — obrigatório para não melhorar o mouse e piorar o produto

- Congelar baseline de API/contratos.
- Decidir/licenciar a distribuição do porte.
- Corrigir roteamento de ações que altera semântica.
- Garantir configuração consistente por caminho de criação.
- Testar propagação de wrappers, eventos e frames.
- Garantir cancelamento, encerramento e ausência de ações órfãs.
- Criar contrato de integração com o RpaBlockly real.

### P1 — pacote principal de qualidade

- Porte Cursory nativo com dados embarcados e paridade.
- Scheduler monotônico, limitado e testável.
- Diagnostics de cobertura/fallback e versões.
- Coerência mínima de idioma, fuso, viewport, plataforma e renderer.
- Snapshot opcional e comparação entre execuções.
- Documentação de atualização upstream e release reproduzível.

### P2 — somente depois dos gates

- Expandir humanização a opções especiais que tenham contrato demonstrado.
- Melhorias de rolagem baseadas em evidência e testes, não apenas mais aleatoriedade.
- Checks de workers/headers mais extensos em ambiente local controlado.
- Simplificar o adaptador do RpaBlockly quando o SDK entregar tudo que ele precisa.
- Estudar perfil persistente no RpaBlockly como projeto separado, caso haja requisito real.

### Fora do escopo

Fork Chromium, patches C++, troca para Firefox, motor anti-detect externo, geração massiva de identidades artificiais, auto-GeoIP externo, rotação de proxy, solvers de CAPTCHA, UI nova, banco de telemetria, DI container obrigatório, plugin framework, port genérico de NumPy e promessa de NativeAOT do wrapper Playwright.

---

## 4. Arquitetura mínima recomendada

### 4.1 Dependências

```text
RpaBlockly e outros consumidores
        |
        v
SpyBrowser.Playwright (integração, MIT)
        |                         |
        v                         v
SpyBrowser.Core (MIT)     SpyBrowser.Cursory (LGPL-3.0-or-later)
                                  |
                         apenas .NET BCL + dados embarcados
```

`SpyBrowser.Compatibility.CloakBrowser` e `SpyBrowser.Cli` continuam consumindo o runtime. Nenhuma dependência inversa do Cursory para Playwright/Core.

A distribuição recomendada contém uma DLL Cursory separada por dependência de pacote explícita. Não usar ILMerge, source inclusion ou copiar código LGPL para arquivos MIT. A existência de assemblies separados ajuda na organização, mas **não substitui a avaliação de conformidade LGPL**.

### 4.2 Organização de arquivos proposta

```text
src/
  SpyBrowser.Cursory/
    SpyBrowser.Cursory.csproj
    CursoryTrajectoryGenerator.cs
    TrajectoryOptions.cs
    Trajectory.cs
    TrajectoryPoint.cs
    Internal/
      TrajectoryDataset.cs
      TrajectorySelection.cs
      TrajectoryTransforms.cs
      TimingSampler.cs
      NumericCompat.cs
      Random/
        Pcg64.cs
        SeedSequence.cs
        CursoryRandom.cs
        ZigguratTables.cs
    Data/
      trajectories.json.gz
      upstream-manifest.json
    LICENSE
    COPYING
    NOTICE
    README.md
  SpyBrowser.Playwright/
    HumanActions.cs                         # fachada pública existente
    HumanInteractionOptions.cs             # evolução aditiva
    Humanization/
      MouseTrajectoryStrategy.cs           # contrato interno
      BezierTrajectoryStrategy.cs          # extrair algoritmo atual
      CursoryTrajectoryStrategy.cs         # adaptador do port puro
      TrajectoryScheduler.cs
      PageInputState.cs
      InteractionDeadline.cs
      HumanizationPolicy.cs
      ActionOptionsClassifier.cs
    Wrapping/
      PlaywrightObjectScope.cs
      PlaywrightEventBridge.cs
      ConfiguredBrowserAdapter.cs
    Diagnostics/
      HumanizationDiagnostics.cs
      RuntimeFingerprintSnapshot.cs
      SnapshotComparer.cs

tests/
  SpyBrowser.Cursory.Tests/
    Fixtures/
    DatasetTests.cs
    RandomParityTests.cs
    TrajectoryParityTests.cs
    TrajectoryInvariantTests.cs
  SpyBrowser.Tests/
    ... testes existentes ...
    HumanizationContractTests.cs
    WrapperPropagationTests.cs
    BrowserFactoryContractTests.cs
    LifecycleTests.cs
    Fixtures/Web/
  SpyBrowser.ConsumerChecks/                # executável pequeno, contrato net9

tools/
  cursory-reference/                        # apenas desenvolvimento/manutenção
    README.md
    generate-fixtures.py
    requirements.lock.txt
    dataset-diff.*                         # script simples se necessário

docs/
  humanization.md
  cursory-port.md
  compatibility.md
  rpablockly-integration.md
  diagnostics.md
  adr/
    0001-cursory-native-license-and-parity.md
    0002-playwright-semantics-and-rollout.md
```

Nomes são orientativos. Não criar uma classe por função trivial. Manter classes coesas, helpers internos e poucos tipos públicos. Antes de mover arquivos existentes, criar testes; evitar PR que mistura renomeação geral e alteração de comportamento.

### 4.3 API pública

Preservar assinaturas existentes, interfaces de retorno e escape hatches:

- `SpyBrowserLauncher.*`;
- `HumanActions`;
- `PlaywrightHumanizer.Wrap/Unwrap`;
- handles, `RawBrowser`, `RawContext`;
- `HumanInteractionOptions` e suas propriedades atuais.

Adicionar apenas opções necessárias, com nomes finais decididos no primeiro ADR:

- seleção do algoritmo: `Bezier` ou `Cursory`;
- modo de compatibilidade: `Legacy` ou `PlaywrightCompatible`;
- política de falha do gerador antes da ação: exigir algoritmo selecionado ou permitir fallback explicitamente;
- diagnósticos opcionais;
- opções Cursory agrupadas, sem expor cada detalhe de NumPy.

Exemplo conceitual, **não API já implementada**:

```csharp
Humanize = true,
HumanInteraction = new HumanInteractionOptions
{
    MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
    CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible
};
```

Não adicionar parâmetros obrigatórios nem exigir configuração por comando Playwright. `Humanize=false` não deve carregar dataset nem modificar o despacho de entrada.

Evitar API pública de registro de plugins nesta fase. A estratégia pode ser interface interna testável. Caso surja requisito externo real, publicar a extensão em outra versão com contrato próprio.

---

## 5. Licenciamento e empacotamento — gate de início

### 5.1 Proveniência

Criar manifesto com:

- URLs e commits upstream;
- versões dos pacotes de referência;
- SHA-256 do gzip original, conteúdo descompactado e fixtures;
- contagem de registros e pontos;
- alterações locais e motivo;
- referência da gravação removida e mapeamento de índices;
- versões de Python/NumPy usadas para gerar resultados;
- licenças do Cursory, port, dados e helpers derivados.

Copiar integralmente avisos pertinentes: LGPL/GPL, atribuições do port, NumPy BSD-3-Clause, PCG MIT e CPython PSF quando aplicável. Não reduzir tudo a um único aviso “MIT”.

Verificar termos de redistribuição dos dados e sua cadeia de origem; não presumir que uma menção no README resolve toda a proveniência. Se a verificação for inconclusiva, bloquear publicação do pacote de dados até resolução.

### 5.2 Configuração NuGet

`Directory.Build.props` impõe MIT, autor e copyright globais. O projeto Cursory deve sobrescrever explicitamente os metadados apropriados:

- `PackageLicenseExpression=LGPL-3.0-or-later`;
- autores/atribuições corretos;
- LICENSE/NOTICE e manifesto incluídos;
- SourceLink apontando para a fonte correspondente à versão;
- fonte correspondente e instruções de build acessíveis por versão publicada;
- dados embarcados identificados como recurso, sem download no primeiro uso.

Revisar `.nuspec`/conteúdo real do `.nupkg` em teste automatizado, não apenas o `.csproj`.

### 5.3 Distribuição e direitos

Documentar como substituir/recompilar a biblioteca conforme as obrigações aplicáveis; não criar restrições contratuais incompatíveis com depuração de modificações quando exigida pela LGPL. Avaliar publicação single-file, trimming, assinatura e eventual distribuição embarcada separadamente.

O caminho padrão de homologação é framework-dependent com DLL separada. Não declarar compatibilidade jurídica universal com todas as formas de empacotamento .NET.

**Aceite:** checklist de licenças revisado, fontes correspondentes reproduzíveis e pacote sem código derivado indevidamente rotulado como MIT. Esta parte requer validação jurídica quando distribuída comercialmente; a arquitetura sozinha não é parecer legal.

---

## 6. Porte nativo do Cursory

### 6.1 Estratégia para limitar trabalho

Usar Python como origem conceitual e o port TypeScript fixado como mapa de implementação: ele já isola o subconjunto de NumPy necessário. Na análise, os arquivos principais e helpers somam aproximadamente 1.422 linhas TypeScript, incluindo tabelas; não é preciso portar NumPy inteiro.

Separar duas camadas:

1. **Port puro:** reproduz o algoritmo e produz posições/tempos. Não conhece Playwright, viewport, páginas, timers ou cliques.
2. **Adaptador de execução:** limites operacionais, ritmo, deadline, última posição e integração Playwright.

Não inserir regras de viewport, duração máxima ou clipping dentro do núcleo do port, pois isso dificulta comparar com upstream.

### 6.2 Implementação por etapas

1. Carregador e validação do dataset.
2. Primitivas numéricas e RNG.
3. Extração de features: dx/dy, deslocamento, comprimento e eficiência.
4. Ranking de candidatos por direção/distância.
5. Perturbações e escolha ponderada por directness.
6. Transformações: morph, knot e jitter.
7. Amostragem temporal e interpolação.
8. Geração completa com endpoints exatos.
9. Otimização somente depois de paridade verde.

Preservar a ordem de consumo de aleatoriedade. Não trocar `rng.normal` por ruído uniforme porque “visualmente parece igual”.

### 6.3 RNG e números: não esconder o custo

Para reproduzir a mesma seed do upstream, portar o conjunto mínimo:

- PCG64, estado/aritmética de 128 bits;
- SeedSequence;
- geração de doubles;
- amostragem inteira com rejeição;
- normal ziggurat e suas tabelas;
- escolha ponderada;
- helpers de soma/arredondamento que influenciam o resultado.

Em .NET 8, preferir `UInt128` com operações `unchecked` nos pontos que dependem de wraparound; testar shifts e overflow com vetores. Usar `BigInteger` somente na expansão de seeds quando necessário e documentar o domínio suportado. Evitar reimplementar operações por adivinhação.

Não usar `System.Random` como substituto do RNG do port e ainda afirmar paridade. O algoritmo Bézier existente pode manter seu RNG separado.

Seeds de produção: entropia do sistema, sem seed global fixa por identidade. Seeds determinísticas ficam disponíveis no port e nos testes. Preservar características da sessão não significa repetir a mesma trajetória a cada execução.

### 6.4 Paridade exigida

Três níveis distintos:

1. **RNG inteiro/bitstream:** exato, com vetores de referência.
2. **Transformações e tempos:** paridade com referência, usando tolerância documentada para ponto flutuante onde necessário.
3. **Execução no navegador:** endpoints, ordem, limites e efeitos corretos; não exigir timers de SO bit a bit.

Critérios iniciais para coordenadas: tolerância absoluta de `1e-9` pixel em fixtures comuns; confirmar empiricamente em Windows/Linux. Para domínios maiores, definir regra absoluta+relativa explícita. Não afrouxar tolerância só para deixar teste verde.

Detalhes que exigem testes:

- `np.rint`/round to even vs arredondamentos C# usados por padrão;
- precisão de `hypot`, `atan2`, `sin`, `cos`, `exp`, `sqrt`;
- somas par-a-par vs soma linear;
- ordenação e empates: regra determinística por índice, como o port TS;
- `searchsorted(side="right")`;
- inteiros de coordenadas upstream que truncam normais em certas etapas de jitter;
- tempos repetidos depois do arredondamento;
- limites inteiros e loops de rejeição;
- diferença entre comprimento do caminho e distância entre endpoints.

Não “corrigir” peculiaridades do upstream dentro do primeiro port sem registrá-las: uma alteração pode ser boa, mas deixa de ser paridade. Melhorias posteriores recebem versão, nota e novos fixtures.

### 6.5 Fixtures sem dependência em produção

- Gerar fixtures a partir das referências fixadas, com o dataset escolhido explicitamente.
- Comitar JSONs compactos e manifesto de geração.
- Incluir seeds pequenas, grandes, casos empatados separados e casos degenerados.
- Testes regulares executam apenas .NET lendo fixtures.
- Python/NumPy e eventualmente Node podem existir em uma ferramenta manual/workflow de manutenção, nunca no runtime distribuído nem como pré-requisito normal para `dotnet test`.
- Instrumentar a referência para fornecer features intermediárias quando surgir divergência; localizar o primeiro estágio divergente antes de investigar a trajetória inteira.
- Fixtures anteriores ao filtro da gravação `2281` devem ser regeneradas ou identificadas por dataset, nunca reutilizadas silenciosamente.

### 6.6 Dataset e memória

- Embarcar o gzip original; descompactar com `GZipStream` e ler com `System.Text.Json`.
- Carregar uma vez por processo com `Lazy<T>` thread-safe.
- Dados imutáveis compartilhados; RNG e buffers mutáveis por operação/instância, nunca compartilhados sem controle.
- Validar número de pontos, correspondência com tempos, valores finitos, ordem temporal, limites de tamanho e registros degenerados.
- Limitar descompactação defensivamente; erro deve identificar dataset inválido sem tentar download.
- SHA-256 e contagem verificados em testes; uma validação de integridade em runtime pode ocorrer uma vez no carregamento, não por movimento.
- Não expor arrays internos mutáveis. Retorno deve ser imutável do ponto de vista público ou ter ownership documentado.
- Não registrar conteúdo de navegação nem coletar novas gravações do usuário por padrão.

### 6.7 Casos extremos obrigatórios

- origem igual ao destino: um ponto, tempo zero;
- movimento subpixel ou muito curto;
- horizontal, vertical, diagonal, negativo e mudança de quadrante;
- coordenadas não finitas: rejeição clara antes de enviar ao browser;
- frequência zero/negativa ou absurda: rejeição/limite explícito;
- directness fora de `[0,1]`;
- duração nula, tempos duplicados, dataset vazio/corrompido;
- seleção com empate;
- seed máxima, erro de overflow e reprodutibilidade;
- geração concorrente para muitas páginas;
- trajetória longa demais: limitação no adaptador, não truncamento silencioso do port;
- ausência do recurso ou falha de load: falhar na inicialização do algoritmo selecionado, ou fallback somente se explicitamente habilitado.

### 6.8 Performance

Começar com loops C# e arrays simples; o dataset é pequeno. Pré-calcular features uma vez. Evitar LINQ/alocação excessiva no hot path se o benchmark mostrar custo.

Não adicionar SIMD, pool complexo ou índice espacial no primeiro PR. Otimizar seleção top-N somente depois de demonstrar equivalência, especialmente na ordem de empates.

Medir separadamente:

- carga fria do dataset;
- geração aquecida p50/p95;
- bytes alocados por trajetória;
- memória por processo e por página;
- tempo de execução Playwright, que tende a dominar.

Orçamento inicial de investigação, não resultado prometido: geração aquecida p95 abaixo de 10 ms e sem cópia de dataset por página em uma máquina de referência registrada. Não usar esse limiar como teste flaky em runner compartilhado; benchmark é gate de revisão com hardware/contexto documentados.

---

## 7. Integração de entrada sem quebrar Playwright

### 7.1 Contrato por comando no modo recomendado

| API | Comportamento recomendado | O que não fazer |
|---|---|---|
| `Mouse.MoveAsync` | Cursory quando suportado; estado atualizado após envio | Ignorar `Steps` explicitamente pedido |
| `Locator.ClickAsync` / `Page.ClickAsync` | Movimento preparatório e ação final pelo Playwright | Clicar em coordenada obsoleta sem revalidação |
| `DblClickAsync` | Movimento preparatório e `DblClickAsync` nativo | Simular com dois cliques simples e assumir equivalência |
| `HoverAsync` | Movimento preparatório + hover final nativo | Perder acionabilidade ou opção do usuário |
| `FillAsync` | Preservar Fill nativo; eventual preparação sem alterar tipo/eventos | Converter silenciosamente para digitação |
| `ClearAsync` | Preservar Clear nativo | Implementar Ctrl+A/Backspace em qualquer elemento |
| `TypeAsync` / `PressSequentiallyAsync` | Ritmo variável somente no domínio testado | Alterar conteúdo, corromper Unicode ou descartar Delay |
| `Keyboard.InsertTextAsync` | Delegação nativa | Transformar em eventos de teclado |
| `PressAsync` | Press nativo, mantendo shortcuts/modifiers | Enviar string de chord como tecla simples |
| `Mouse.Down/Up` | Delegar e observar estado para não interferir em drag | Liberar botão que o chamador mantém pressionado |
| `WheelAsync` | Nativo na primeira versão recomendada, ou modo segmentado explicitamente escolhido/testado | Mudar padrão de eventos escondido sob promessa de compatibilidade exata |
| `DragTo`, touch, CDP e handles avançados | Raw até ter suporte explícito validado | Interceptação genérica por nome |
| navegação, requests, routing, downloads, eval | Delegação original | Reescrever expressões, payloads ou callbacks arbitrários |

Compatibilidade de tipos é preservada; movimentos adicionais mudam eventos de movimento por definição. Documentar a diferença entre **semântica da ação** e **identidade de todo o fluxo de eventos**. Quando a aplicação exigir eventos/timing exatos, usar `Humanize=false` ou os escape hatches.

### 7.2 Opções: fallback conservador, sem expansão obrigatória

**Decisão de escopo do usuário:** manter nesta entrega a possibilidade de qualquer objeto options encaminhar a chamada ao Playwright original. Não implementar um classificador complexo só para humanizar os fills com `Timeout` do RpaBlockly. Esse consumidor será ajustado posteriormente.

Suporte obrigatório inicial:

- chamadas sem options/null nos comandos explicitamente suportados;
- options presentes: execução original, preservando todos os valores, com motivo de fallback diagnosticável.

Expansão futura, não requisito desta entrega:

- objeto vazio;
- apenas `Timeout`, desde que a operação tenha deadline compartilhado implementado;
- opções comprovadamente neutras pelo contrato do método.

Se e quando essa expansão ocorrer, usar classificador por método/tipo e testes, sem presumir que todo objeto options é seguro.

Para `Force`, `Trial`, `Position`, `Modifiers`, `Button`, `ClickCount`, `Delay`, `Steps`, `NoWaitAfter` e opções novas: fallback conservador na primeira fase, salvo teste e implementação explícitos.

- `Trial=true` deve delegar o trial original, sem executar a preparação humanizada que adicionaria movimento por conta própria.
- `Timeout=0` significa sem limite Playwright; não converter para zero de orçamento restante.
- `Delay` explícito tem precedência sobre política de digitação/clique.
- Propriedade nova/desconhecida com valor não padrão => raw + motivo.
- Não modificar o objeto options fornecido; copiar as propriedades suportadas para options internos.
- Comparar com defaults da versão instalada, não tratar arbitrariamente `false` e `null` como equivalentes.

Usar handlers/whitelist por assinaturas, não só nome do método. Classificação pode ter cache de metadados por tipo; não construir um serializador genérico de options.

### 7.3 Movimento para locator e ação final

1. Resolver page raw e locator raw.
2. Iniciar deadline total e adquirir exclusão de entrada da página.
3. Executar readiness/trial suportado usando Playwright e orçamento restante.
4. Obter bounding box no sistema de coordenadas correto, considerando frames.
5. Planejar destino coerente com a posição da ação final; começar pelo ponto padrão/centro, evitando escolher ponto aleatório e depois saltar para outro ponto.
6. Gerar e executar trajetória de mouse.
7. Entregar a ação final ao `locator.ClickAsync`/`DblClickAsync`/`HoverAsync` original, com orçamento restante e options preservados.
8. Atualizar estado observado/conhecido; registrar desvio ou reposicionamento quando aplicável.

O Playwright pode reposicionar o ponteiro ou reencontrar o elemento se o layout mudar. Isso é preferível a clicar no lugar errado. Não adicionar loop ilimitado de “perseguir” o alvo.

Quando não for possível preparar com segurança — elemento fora de viewport, transformação complexa, estado desconhecido, drag ativo — pular preparação e executar o comando original. Não transformar um caso válido no Playwright em erro só porque Cursory não pode planejar o movimento.

### 7.4 Unicode e campos especiais

- Não simular erros de digitação nem modificar o texto solicitado.
- `Fill` mantém suporte nativo a inputs especiais, contenteditable, máscaras e frameworks.
- Digitação humana explícita deve preservar ordem de caracteres e composição conforme suporte real do Playwright.
- Testar emoji, surrogate pairs, marcas combinantes, acentos e scripts não latinos.
- Iterar runes evita quebrar surrogate pair, mas não resolve todos os grafemas/IME; não declarar cobertura que os testes não mostram.
- Quando dividir texto em chamadas alterar contrato de IME/composição, delegar a chamada original, registrando motivo.
- Segredos nunca aparecem em logs de humanização.
- Textos longos têm deadline; nunca truncar texto nem acelerar silenciosamente além da política para fingir sucesso.

### 7.5 Estado por página e acesso raw

`PageInputState` acompanha:

- última posição conhecida enviada com sucesso;
- origem do conhecimento (wrapper, coordenada de ação, desconhecida);
- botões/modificadores acompanhados;
- lifetime/cancelamento de page/context;
- gate de serialização de entrada.

A posição física real do cursor não é consultável de forma geral. Não fingir conhecimento. Começar com a posição inicial conhecida do Playwright somente se verificada; em estado desconhecido, posicionar/delegar sem inventar uma longa trajetória desde o centro.

Chamadas feitas via `Unwrap` escapam do tracking. Fornecer mecanismo simples de invalidar o estado de entrada por página para quem mistura raw e humanizado; documentar essa limitação. Estado desatualizado deve degradar para ação original, não arrastar a partir de posição inventada.

Não arredondar universalmente para inteiro CSS: CSS pixels e device pixels diferem com escala/DPR. Na primeira versão, manter coordenadas precisas e deixar Playwright/browser aplicar sua política; qualquer quantização adicional exige prova em DPR 1/1.25/1.5/2.

### 7.6 Scheduler de trajetória

- Usar relógio monotônico (`TimeProvider`/timestamp) e deadlines absolutos.
- Não somar `Task.Delay(intervalo)` ao custo de cada chamada como se o envio fosse instantâneo.
- Aguardar chamadas Playwright serialmente; não usar `Task.WhenAll` para pontos do mesmo movimento.
- Se estiver atrasado, descartar/coalescer intermediários vencidos em vez de criar rajada para “recuperar” o relógio.
- Sempre preservar o destino final, salvo cancelamento/falha/deadline.
- Sem busy wait, ajuste global de timer, prioridade de processo ou promessa de tempo real.
- Não afirmar que o ritmo planejado é o ritmo observado no DOM; medir ambos no harness.
- Ponto repetido não precisa ser reenviado se puder ser omitido sem perder semântica; conservar a pausa correspondente.

Duração mínima/máxima é aplicada no adaptador. Se redimensionar o tempo, ajustar amostragem/cadência de maneira documentada; não comprimir centenas de pontos em uma rajada. Separar a seleção da gravação do resampling, evitando que nova frequência selecione outra gravação.

Os limites atuais de 180–650 ms são comportamento legado. O preset Cursory recomendado deve respeitar mais o tempo gravado, com teto operacional inicialmente proposto de 1,5 s, a validar. Não reinterpretar silenciosamente limites explícitos de usuários antigos; criar preset/fábrica versionada de opções se necessário.

### 7.7 Concorrência, cancelamento e deadlines

- Uma sequência de entrada por página de cada vez; páginas distintas continuam paralelas.
- O gate é por page, não global ou por navegador.
- O gate deve abranger comandos raw de entrada encaminhados pelo wrapper para não intercalar `Down/Up` e movimento; CDP/raw escape hatch fica fora e é responsabilidade explícita do chamador.
- Navegação, fechamento e eventos não podem depender desse gate e causar deadlock.
- Não manter lock enquanto invoca callback do usuário.
- Não serializar uma transação drag que atravessa várias chamadas usando um lock impossível de liberar; acompanhar estado de botão e manter comandos atômicos.

As interfaces Playwright não passam `CancellationToken` para toda ação. Não afirmar que `WaitAsync(token)` cancela a ação subjacente. O SDK deve:

- honrar tokens dos métodos próprios `HumanActions`;
- vincular execução ao fechamento de page/context/browser;
- interromper pausas e próximas etapas quando o lifetime termina;
- observar falhas de operações já iniciadas;
- não deixar tasks órfãs continuarem digitando após cancelamento;
- usar um deadline total para readiness + movimento + ação, não um timeout completo em cada etapa;
- rastrear mudanças de `SetDefaultTimeout` no wrapper para calcular defaults efetivos; se houver mutação raw não observável, usar fallback/documentar explicitamente.

Ao falhar após pressionar um botão/tecla controlados pelo SDK, executar cleanup best-effort somente do estado de que ele é dono, sem sobrepor a exceção original. Nunca repetir a ação por causa de erro no cleanup.

---

## 8. Wrappers, fábricas e eventos: compatibilidade transitive de verdade

### 8.1 Separar configuração de humanização

Um browser pode precisar de defaults de identidade mesmo com `Humanize=false`.

- Centralizar criação configurada em uma implementação usada por `handle.NewContextAsync`, `handle.NewPageAsync` e `handle.Browser.NewContextAsync/NewPageAsync`.
- Evitar recursão: a fábrica configurada sempre chama o browser raw, não o adapter público.
- `RawBrowser` permanece realmente raw e bypass intencional.
- Contexts/pages com `Humanize=false` permanecem raw para preservar o contrato usado pelo RpaBlockly.
- Um adapter de browser para configuração não implica que todo context tenha de ser proxy.

Preferir reaproveitar a scope/cache existentes com handlers explícitos. Se um `ConfiguredBrowserAdapter` pequeno e tipado reduzir complexidade, usá-lo somente para `IBrowser`; não escrever manualmente centenas de membros de `IPage`.

### 8.2 Propriedades e coleções

Cobrir explicitamente:

- `IBrowser.Contexts`;
- `IBrowserContext.Pages`;
- retorno `Task<IPage>` de new page/wait/run-and-wait popup;
- `IPage.Frames`, main frame, parent/child frames;
- `IFrameLocator` e locators derivados dele;
- listas de locators de `AllAsync`;
- listas e retornos de handles, sem prometer humanização de seus métodos não implementados;
- referências de volta: `locator.Page`, `page.Context`, `context.Browser`.

A mesma instância raw deve corresponder à mesma instância pública dentro da scope. Não recriar um proxy a cada acesso. Usar caches fracos onde apropriado, com lifetime claro, e testes de identidade e coleta.

Não tentar embrulhar arbitrariamente qualquer objeto de retorno via recursão: existe risco de corromper DTOs, objetos de usuário, resultados de `EvaluateAsync` e objetos de protocolo.

### 8.3 Eventos

Eventos como `Context.Page`, `Page.Popup`, criação/fechamento de contexto devem entregar os mesmos objetos públicos das propriedades/fábricas.

Criar bridge apenas para eventos relevantes:

- preservar delegate original, assinatura, ordenação e semântica de assinatura/remoção;
- respeitar inscrições duplicadas e remoção da última ocorrência como eventos .NET;
- adaptar argumentos Playwright conhecidos e sender somente conforme o contrato;
- não engolir exceções nem transformá-las arbitrariamente;
- não assinar o mesmo bridge repetidamente;
- remover inscrições e referências no descarte;
- não invocar handlers mantendo locks internos.

Há risco de contexto ainda não estar preparado quando o evento raw chegar. Usar uma coordenação única entre fábrica e registro, preparar antes de publicar o objeto configurado e garantir emissão exatamente uma vez. Para objetos externos que não passaram pela fábrica, definir política explícita (registrar/wrap sem inventar defaults retroativos). Não usar `.Result` em handler síncrono para esperar configuração assíncrona.

### 8.4 Identidade e erros

- `Unwrap(Wrap(x))` retorna `x`.
- `Wrap(Wrap(x))` não duplica humanização.
- Argumentos Playwright passados de volta são desembrulhados quando exigido; não recursar no payload arbitrário do usuário.
- Preservar exceções Playwright e stack trace.
- Comparar operações síncronas/assíncronas, `Task<T>` e formas usadas pela versão suportada.
- Novas APIs desconhecidas continuam delegadas, e auditoria de CI avisa se retornam objetos Playwright que ainda precisam de wrapper.

---

## 9. Diagnósticos e coerência sem custo operacional alto

### 9.1 Observabilidade de humanização

Adicionar integração leve baseada em `DiagnosticSource`, `ActivitySource` ou callback/collector opcional. Escolher uma solução, não três frameworks sobrepostos.

Campos permitidos:

- método, algoritmo, modo e resultado;
- motivo de fallback;
- duração planejada vs efetiva;
- quantidade de pontos gerados/enviados/coalescidos;
- deadline excedido/cancelamento;
- versões de SpyBrowser, Playwright, browser, algoritmo e dataset.

Não registrar por padrão texto digitado, senha, token, cookie, storage state, URL completa, seletor com conteúdo pessoal ou proxy com credenciais. Coordenadas e seed detalhadas somente em diagnóstico explicitamente habilitado e com retenção curta.

Observers de diagnóstico não devem derrubar a automação se falharem. Sem observer, overhead próximo de uma checagem de habilitação; não alocar evento por ponto.

Motivos com códigos estáveis, por exemplo `humanization.unsupported-options`, `humanization.raw-insert-text`, `humanization.pointer-unknown`, `humanization.active-drag`, `trajectory.dataset-invalid`.

### 9.2 Coerência incremental

Ampliar checks existentes, priorizando dados já disponíveis:

| Check | Tratamento inicial |
|---|---|
| Fuso configurado vs observado | Canonicalizar aliases conhecidos; erro apenas em divergência inequívoca |
| Locale principal vs `navigator.languages` | Normalização BCP-47 e comparação tolerante documentada |
| Viewport/tela/escala | Comparar no mesmo espaço de unidades; considerar zoom/NoViewport |
| Família de browser/UA | Avisar conflito claro, não exigir igualdade de strings de versão |
| UA/platform/Client Hints | Diferenciar indisponível, reduzido por privacidade e conflitante |
| Software vs hardware rendering | Reaproveitar política GPU existente |
| WebGL/WebGPU | Normalização conservadora; ausência não é incoerência por si só |
| Overrides experimentais | Aviso explícito de alcance limitado, sem prometer stealth |
| Defaults efetivos do contexto | Validar configuração após callbacks e overrides do usuário, não apenas manifesto base |

Não consultar automaticamente serviços de GeoIP ou endpoints públicos. Validar relação proxy/país apenas se o usuário fornecer expectativa explícita; IP não revela com certeza idioma ou fuso de uma pessoa.

Não transformar todo aviso em bloqueio. Erros de configuração inequívocos podem falhar; dados indisponíveis e mudanças naturais geram warning/information. Manter `FailOnConsistencyErrors` funcional.

### 9.3 Contextos criados pelo browser-only

Hoje o caminho `LaunchBrowserAsync` não executa o mesmo probe completo de persistent/context launch. Não basta ampliar a classe de validator: escolher quando executá-la nos contexts criados pelas fábricas.

- Checks baratos de configuração antes de abrir o browser/context.
- Probe de superfície opt-in, ou no CLI doctor/probe, com timeout próprio curto.
- Não ligar GPU probe automaticamente no RpaBlockly, que o desliga intencionalmente.
- Usar página dedicada temporária, fechada em `finally`, sem roubar foco/navegar uma aba existente.
- Se precisarmos de origem segura/headers/workers, usar harness local controlado; não atribuir ausência de API em `about:blank` a defeito de identidade.
- Evitar que a aba interna de diagnóstico seja confundida com página do usuário em eventos/listagens; definir e testar a ordem de configuração/publicação.
- Não rodar probe pesado a cada clique.

### 9.4 Snapshot e comparação histórica

Adicionar snapshot JSON separado do manifesto de identidade, sem mudar o schema de identidade só para incluir diagnóstico:

- schema próprio;
- versões e características normalizadas;
- algoritmo/dataset selecionados;
- findings com severidade;
- timestamp e contexto de execução quando persistido.

Persistência opt-in, atomic write, permissões restritas, retenção curta configurável. Separar baseline aceito de snapshot atual; não substituir baseline silenciosamente. Snapshots concorrentes de contexts descartáveis não podem disputar um arquivo global por `IdentityId`.

Comparar mudanças relevantes, não usar um hash único como veredito. Atualização legítima de Chrome/GPU/driver deve aparecer como informação contextual, não bloqueio automático.

O hash atual do probe gráfico é uma amostra limitada, não uma certificação da GPU. Não classificá-lo como prova de equivalência de fingerprint.

---

## 10. Integração e homologação do RpaBlockly

### 10.1 Mudanças mínimas na primeira entrega

1. Referenciar a nova pré-release de `SpyBrowser.Playwright` em branch de integração.
2. Manter `Microsoft.Playwright 1.61.0` no primeiro teste, depois testar uma versão mais nova aprovada.
3. Passar o preset/modo Cursory compatível no `BrowserLauncher` existente.
4. Manter `SpyBrowserHumanize` como chave de ativação/desativação.
5. Não mexer nos blocos, `RpaContext.Page`, receitas de locator ou DSL de fluxos.
6. Manter timezone local/normalização UTC, viewport, storage state e políticas atuais.
7. Manter o adapter `SpyBrowserSessionBrowser` inicialmente; só removê-lo em PR separado se os testes de fábrica/eventos do SDK substituírem toda a sua função.
8. Manter CloakBrowser, Chromium, Firefox e WebKit existentes sem alteração.

### 10.2 Preenchimento com Timeout — ajuste adiado por decisão do usuário

O runtime passa `LocatorFillOptions.Timeout`, e isso hoje força execução raw. **Não resolver essa questão nesta entrega do SpyBrowser.** As chamadas do RpaBlockly que desativam o humanizer serão revistas posteriormente no próprio consumidor.

Essa decisão não dispensa preservar a semântica dos comandos: no modo recomendado, Fill nativo e digitação por tecla são operações diferentes. Ao revisar o consumidor, escolher o comando conforme os eventos realmente necessários, sem simplesmente remover timeouts e perder cancelamento/limites operacionais.

Não adicionar testes que exijam que o fill atual do RpaBlockly seja humanizado. Testar apenas seu funcionamento e cancelamento, mantendo o fallback permitido e diagnosticável. Nenhuma alteração dos blocos é necessária para entregar o porte nativo.

### 10.3 Contratos de regressão obrigatórios

- `Humanize=true/false` e detecção de wrapper nos mesmos pontos usados atualmente.
- Evento `Browser.Context` retorna a mesma referência de `NewContextAsync`.
- `Browser.Contexts` inclui/exclui contexts corretamente após fechar.
- `context.Pages`, eventos de popup e frames propagam humanização quando habilitada.
- `Intl` retorna fuso esperado, inclusive UTC/Etc/UTC e Windows/IANA.
- `FillWithRuntimeAsync` cancelado fecha o contexto e não continua executando entrada.
- Cancelamento do launch limpa browser que terminou de abrir depois do cancelamento.
- `StorageStatePath` de entrada e gravação posterior continuam funcionando.
- Downloads, screenshots, espera por navegação e campos do fluxo mantêm saídas.
- Flows V1/V2 com frames continuam usando `IFrameLocator`/`ILocator` oficiais.
- Dois jobs independentes não compartilham cursor/RNG/estado mutável.
- A identidade em memória `rpablockly` usada em browser-only não adquire um lease de perfil que bloqueie jobs paralelos.
- Recursos capturados após fechamento não mantêm contextos vivos indefinidamente.

Não executar testes que envolvam CAPTCHA de terceiros nem alterar os componentes de resolução existentes no outro repositório para validar este pacote.

### 10.4 Teste inter-repositório reproduzível

- No SpyBrowser: pequeno consumer check net9 baseado no contrato público de RpaBlockly, sem copiar sua aplicação inteira.
- Em CI de integração/manual: checkout do RpaBlockly em commit fixado e uso de feed NuGet local com a pré-release construída.
- Executar o subconjunto de checks que usa fixtures locais. Registrar comando, SDK e versões.
- Se o suite principal depender de serviços externos, extrair testes locais no RpaBlockly em um PR explícito em vez de marcar tudo como aprovado.
- Testar também uma branch mais recente periodicamente, mas não deixar `main` flutuante tornar a release irreproduzível.
- Não instalar a fachada CloakBrowser no consumidor que já referencia o fornecedor original.

---

## 11. Testes: o que prova cada camada

### 11.1 Unitários do port

- Integridade e validade do dataset.
- Vetores RNG, incluindo rejeições e limites.
- Features/ranking/empates/weighted choice.
- Morph/knot/jitter com fixtures intermediárias.
- Tempos e coordenadas finais por seed.
- Endpoints, finitude, contagem consistente e tempos não decrescentes.
- Não mutação do dataset durante geração.
- Uso concorrente e ausência de RNG compartilhado.
- Múltiplos seeds fixos em milhares de casos de invariantes; reproduzir falha com seed no relatório.

### 11.2 Unitários da integração

- Fallback raw com options presentes, sem perder ou modificar propriedades.
- `Timeout=0`, objeto vazio e opções desconhecidas continuam aceitos pelo caminho nativo. Expansão de humanização com options não é gate desta entrega.
- Classificação detalhada por assinatura somente quando o suporte a options for expandido.
- Deadline compartilhado e fila de entrada contando no orçamento.
- Scheduler com tempo fake, atrasos, skips e cancelamento.
- Fallback antes de qualquer ação, nunca depois de click parcial.
- Estado desconhecido, invalidação após raw, button down ativo.
- Descarte idempotente, falha de cleanup preservando erro original.
- `Humanize=false` não inicializa Cursory.

### 11.3 Browser tests locais

Criar fixture web local servida apenas em loopback, com recursos determinísticos:

- botões com contadores e registro de eventos;
- dupla interação distinguindo `click`, `dblclick`, `detail`;
- inputs text/password/number/date, textarea/contenteditable e campo controlado por JS;
- eventos keyboard/input/change para distinguir Fill, Type, InsertText e Press;
- botões ocultos, desabilitados, overlays, elementos removidos e que se deslocam;
- frames aninhados, cross-origin local usando outra porta, popup;
- página com scroll, limites de viewport, coordenadas fracionárias e diferentes DPRs;
- links com navegação, download local e nova aba;
- campo grande cancelado durante digitação;
- context/page fechando durante fila, geração, movimento e ação final.

Comparar Playwright raw com o modo compatível:

- resultado e efeitos da ação;
- ausência de cliques duplicados;
- erros relevantes e timeout/cancelamento;
- referências/eventos/coleções;
- diferenças de mouse esperadas explicitamente separadas das regressões.

Não exigir igualdade de timestamps reais. Registrar tolerâncias e considerar carga do runner.

### 11.4 Matriz de CI

PRs:

- Windows e Linux;
- baseline Playwright 1.61.0;
- unitários, contratos e browser tests locais;
- restore/build/test/pack em Release;
- instalação explícita de Chromium/dependências para não depender de imagem do runner.

Programado ou pré-release:

- versão estável mais recente de Playwright resolvida uma única vez por workflow e registrada;
- Chrome instalado e Edge Windows quando disponível/provisionado;
- headless e headed com Xvfb em Linux, lembrando que Xvfb não é GPU;
- net9 consumer check e integração RpaBlockly pinada;
- compatibilidade de publicação framework-dependent; single-file somente se validado técnica e juridicamente.

Não multiplicar todas as combinações em todo PR. Usar camadas obrigatórias e lanes programadas para controlar custo.

Tests de browser não executados devem aparecer como **skipped** ou job não executado, nunca passar por `return` silencioso. Separar traits/projetos se o framework atual dificultar skip dinâmico.

### 11.5 Avaliação de qualidade de movimento

Relatório local com baseline Bézier, Cursory e, se houver consentimento, amostras humanas de referência:

- duração;
- comprimento/deslocamento;
- distribuição de intervalos;
- velocidade/aceleração com método e unidades declarados;
- quantidade de overshoot/pausas;
- jitter observado no DOM;
- taxa de conclusão das tarefas e custo de execução.

Isso demonstra alteração do comportamento, não indetectabilidade. Não inventar um “stealth score”. Um benchmark real em ambientes autorizados deve separar impacto de IP, conta, browser e carga.

---

## 12. Sequência de implementação por PRs pequenos

### PR 0 — baseline, ADRs e licenças

**Entregas:** documento de contrato, API baseline, referências pinadas, manifestos/licenças, plano de fixtures, atualização da documentação RpaBlockly.  
**Aceite:** decisão LGPL e distribuição revisadas; testes de contrato identificados; nenhum comportamento alterado.  
**Dependências:** nenhuma.

### PR 1 — testes de regressão e correções semânticas

**Entregas:** testes para InsertText, Press/chords, DblClick, Fill/inputs, cancelamento; modo `PlaywrightCompatible` explícito.  
**Aceite:** operações delegadas preservam efeitos e tipos; legacy permanece selecionável; não há repetição de ações.  
**Dependências:** PR 0.

### PR 2 — scope, factories e propagação

**Entregas:** unificação das fábricas, cobertura de frames/coleções, bridges de eventos relevantes.  
**Aceite:** mesma referência entre evento/lista/retorno; Humanize=false raw nos contexts/pages; fechamento remove referências; RpaBlockly adapter ainda funciona sem duplicação de eventos.  
**Dependências:** PR 1; pode ser dividido em factories e events para revisão.

### PR 3 — biblioteca Cursory: dados e núcleo numérico

**Entregas:** novo projeto/pacote, dataset embarcado, RNG/helpers e testes de fixtures.  
**Aceite:** licenças corretas no pacote; bitstream de referência validado; nenhuma dependência Playwright/Node/Python em runtime.  
**Dependências:** PR 0. Pode avançar paralelamente a PR 1/2 sem tocar no decorator.

### PR 4 — algoritmo completo e paridade

**Entregas:** seleção, transforms, timing, geração, invariantes e benchmarks.  
**Aceite:** referência comparada com dataset correspondente; divergências documentadas; Windows/Linux verdes; não mutação/concorrência validadas.  
**Dependências:** PR 3.

### PR 5 — estratégias e scheduler

**Entregas:** extrair Bézier, integrar Cursory opt-in, estado por página, scheduler monotônico, deadlines, cancellation/lifetime.  
**Aceite:** endpoint correto, sem rajadas artificiais de recuperação, nenhuma task órfã, sem servidor extra, fallback somente antes de efeitos.  
**Dependências:** PR 1/2/4.

### PR 6 — diagnósticos e coerência de baixo custo

**Entregas:** eventos de cobertura, versões, normalização fuso/locale, snapshots opt-in, probes isolados e CLI.  
**Aceite:** sem secrets/logs detalhados por padrão; sem endpoints externos; warnings não viram erros arbitrariamente; callbacks/options efetivos considerados.  
**Dependências:** PR 2/5.

### PR 7 — integração RpaBlockly e homologação

**Entregas:** pré-release local, consumer check net9, branch do RpaBlockly com nova versão/preset, evidências dos checks.  
**Aceite:** fluxos locais e testes atuais passam; cancelamento e contextos preservados; nenhum bloco reescrito.  
**Dependências:** PR 5, diagnósticos mínimos de PR 6.

### PR 8 — release e simplificação posterior

**Entregas:** release notes, matriz homologada, documentação de upgrade/rollback, pacote completo e fontes correspondentes.  
**Aceite:** gates de publicação completos. Promoção do preset recomendado somente com decisão explícita.  
**Depois:** PR separado para remover adapter redundante do RpaBlockly, se comprovado.

---

## 13. Estimativa realista e controle de esforço

Estimativas em dias de engenharia concentrada de uma pessoa familiarizada com C#/Playwright; não são prazo contratual e não incluem tempo externo de revisão jurídica/publicação.

| Pacote de trabalho | Faixa inicial |
|---|---:|
| Baseline, licenças/proveniência e desenho de fixtures | 1–2 dias |
| Porte puro Cursory e helpers com paridade | 3–6 dias |
| Contratos de ações, wrapper/factories/eventos | 3–6 dias |
| Scheduler, concorrência e lifecycle | 2–4 dias |
| Diagnósticos/coerência mínima | 1–3 dias |
| Integração RpaBlockly, CI, empacotamento e docs | 2–4 dias |

**Ordem de grandeza total: 12–25 dias de engenharia**, com risco concentrado em eventos/wrappers e semântica, não no tamanho do dataset. Parte dos trabalhos pode ser paralela, mas integração não desaparece.

“Pouco esforço” é relativo a manter um fork de navegador. Um port fiel com compatibilidade real não deve ser vendido como mudança de algumas horas.

Para reduzir o primeiro lote sem sacrificar qualidade:

- entregar primeiro Cursory opt-in + contratos de mouse/teclado/factories usados pelo consumidor;
- manter opções avançadas e operações não comprovadas em raw;
- adiar snapshot histórico sofisticado, checks extensos de worker e remoção do adapter RpaBlockly;
- não portar toda a interface pública Python: apenas geração necessária e helpers internos;
- não buscar otimizações antes dos benchmarks;
- não buscar exatidão temporal entre sistemas operacionais;
- reutilizar fixtures upstream, mas somente após corrigir a correspondência de dataset.

Se um gate de paridade falhar, não substituir o algoritmo por aproximação sem renomear/documentar. Isolar a divergência e reestimar antes de aumentar escopo.

---

## 14. Riscos e respostas práticas

| Risco | Prevenção / resposta |
|---|---|
| Dataset/fixture de versões diferentes | Hash e manifesto amarrados; regeneração explícita |
| Licença LGPL confundida com MIT global | Override por projeto, inspeção do nupkg e NOTICE completo |
| Novo runtime depende de servidor extra | Port BCL puro, teste de dependências e smoke sem executáveis adicionais no PATH |
| Confundir ausência de Node extra com ausência do driver Playwright | Documentação e smoke test reconhecendo processo normal do Playwright |
| Seed igual, trajetória diferente sem explicação | Vetores RNG/intermediários antes do teste final |
| Empates mudam com CPU | Ordenação determinística, fixtures próprias para empates |
| .NET arredonda diferente | Helpers compatíveis e tolerâncias justificadas |
| Recurso embarcado pesado por página | Lazy único imutável, benchmark de memória |
| APIs novas do Playwright saem raw inadvertidamente | Auditoria automática de interfaces/retornos e allowlist |
| Fallback executa ação duas vezes | Decisão antes da ação; estado de execução e teste com contador |
| Elemento muda durante movimento | Ação final por locator nativo; não clicar por box antigo |
| Humanização quebra Fill/InsertText/chords | Modo compatível nativo; testes de eventos e campos |
| Cancelamento apenas deixa de esperar | Lifetime integrado, close interrompe sequência, observar operação real |
| Drag externo é liberado pelo SDK | Ownership de botões e fallback durante drag; cleanup só do próprio estado |
| Custo de transporte cria burst de mouse | Scheduler monotônico, coalescing e limite de pontos |
| Page criada via frame/popup perde wrapper | Cache e testes de propagação/event bridge |
| Eventos geram deadlock ou leak | Sem await bloqueante em handler, sem callbacks sob lock, unsubscribe no disposal |
| Probes afetam aba do usuário | Aba interna dedicada, ordem de publicação e cleanup em finally |
| UTC/Etc/UTC gera falso erro | Canonicalização e testes alinhados com RpaBlockly |
| Mudança legítima de GPU/Chrome bloqueia operação | Snapshot informativo, não auto-bloqueio |
| Logs capturam dados pessoais | Allowlist de campos, redaction e testes de ausência |
| RpaBlockly perde outros browsers | PR restrito ao provider SpyBrowser; testes das seleções atuais |
| Pacotes Cloak entram em conflito | Manter pacote nativo; proibir fachada duplicada no grafo |
| Atualização do port vira projeto de pesquisa | Proveniência, módulo pequeno, teste diferencial e changelog por upstream |
| Humanização se torna muito lenta | Medir tarefa real, presets explícitos, orçamento e comparação com baseline |

---

## 15. Critérios finais de aceite e evidências exigidas

### Compatibilidade

- [ ] Compila consumidor existente com alterações apenas de versão/opções de inicialização quando necessárias.
- [ ] Interfaces oficiais continuam em todos os pontos contratados.
- [ ] Raw escape hatches continuam disponíveis.
- [ ] Callbacks, frames, popups, lists e factories testados com identidade de referência.
- [ ] `Fill`, `InsertText`, `Press`, `DblClick` mantêm semântica documentada no novo modo.
- [ ] Opções explícitas nunca são descartadas silenciosamente; execução raw permanece válida e ampliar sua humanização não é requisito.
- [ ] Nenhum teste de browser não executado é reportado como validação real.

### Cursory

- [ ] Biblioteca C# sem processo/servidor extra.
- [ ] Dataset embarcado, versionado, íntegro e licenciado.
- [ ] RNG e algoritmo comparados com referência adequada.
- [ ] Peculiaridades/diferenças upstream registradas.
- [ ] Concorrência, invariantes e casos extremos cobertos.
- [ ] Humanize=false não paga custo de carga dos dados.

### Operação

- [ ] Nenhuma ação duplicada em fallback ou timeout.
- [ ] Cancelar/fechar interrompe entrada futura e libera recursos.
- [ ] Páginas independentes continuam paralelas.
- [ ] Scheduler não tenta recuperar atraso com bursts ilimitados.
- [ ] Não há coleta externa de fingerprint nem telemetria compulsória.
- [ ] Diagnósticos não vazam texto, cookie, token ou senha.

### Consumidor e release

- [ ] RpaBlockly fixado passa nos checks locais atuais e novos.
- [ ] `StorageStatePath`, timezone local, Chromium selecionado e Humanize on/off preservados.
- [ ] Não houve mudança nos blocos nem nos componentes de CAPTCHA.
- [ ] Matriz de versões/OS homologada e registrada.
- [ ] Pacote/símbolos/fontes correspondentes e licenças conferidos.
- [ ] Release notes descrevem mudança de semântica/preset e rollback.
- [ ] Não há alegação quantitativa de redução de CAPTCHA sem experimento autorizado que a sustente.

Artefatos mínimos da entrega: resultados de testes, relatório diferencial Cursory, manifesto de versões/dataset, relatório de benchmark, evidência RpaBlockly e checklist de licenças.

---

## 16. Rollout, rollback e manutenção

### Rollout

1. Publicar pré-release com algoritmo novo opt-in e defaults antigos preservados.
2. Homologar o preset `PlaywrightCompatible + Cursory` no RpaBlockly por configuração de inicialização.
3. Corrigir regressões sem mexer nos blocos do consumidor.
4. Publicar versão com preset recomendado claramente identificado.
5. Alterar o default global somente em release anunciada, com decisão baseada nos gates, nunca por atualização silenciosa de patch.

Na primeira pré-release, legacy ainda existe para comparar. Bugs de segurança/ações duplicadas não devem ser perpetuados apenas por chamar o modo de legacy: documentar correções obrigatórias separadamente das diferenças intencionais.

### Rollback

- Configuração para selecionar Bézier sem mudar comandos.
- Configuração para desligar humanização totalmente.
- Pin da versão anterior do pacote no consumidor.
- Não há migração destrutiva de perfil/identidade nessa entrega.
- Snapshots separados e versionados não impedem abrir uma identidade com versão anterior.
- Não reverter silenciosamente algoritmo durante uma ação já iniciada.

### Manutenção upstream

Ao atualizar Cursory:

1. Ler diff/changelog/licenças, não copiar HEAD cegamente.
2. Atualizar manifesto e hashes.
3. Comparar dataset e índices.
4. Regenerar fixtures em ambiente fixado.
5. Rodar testes diferenciais por estágio.
6. Rodar browser contracts e RpaBlockly checks.
7. Medir performance e comportamento.
8. Registrar mudança de algoritmo/dataset nas release notes.

Ao atualizar Playwright:

1. Resolver versão uma vez e registrar.
2. Comparar superfície de interfaces/options.
3. Manter desconhecidos em raw.
4. Rodar baseline e nova versão.
5. Homologar consumidor antes de elevar a versão mínima.

Não fixar versões antigas para sempre: segurança do browser/SDK continua sendo requisito. Atualização de .NET e framework do consumidor deve ser tratada em trilha própria, evitando misturá-la com o porte do algoritmo.

---

## 17. Resultado esperado, com limites claros

O resultado será um SpyBrowser que:

- mantém o uso normal de comandos Playwright no RpaBlockly;
- executa Cursory nativamente em .NET, sem sidecar de geração;
- possui fronteiras pequenas e compreensíveis entre algoritmo, despacho, wrapper e diagnóstico;
- reduz falhas de semântica e de propagação que hoje prejudicam a automação;
- melhora o modelo de movimento e sua execução temporal;
- identifica configurações contraditórias sem adicionar novas máscaras frágeis;
- permite atualizar o port com fixtures e proveniência, em vez de depender da memória de quem o implementou.

**Isso melhora qualidade técnica, previsibilidade e naturalidade de interação. Não transforma Chrome convencional em um motor nativamente modificado como o Camoufox.** Essa limitação fica explícita no produto e na documentação.
