# Revisão de upgrade: Microsoft.Playwright 1.61.0 → 1.63.0

A auditoria foi executada contra os assemblies reais, sem promover a dependência do produto. A baseline 1.61.0 permanece; a baseline 1.63.0 é separada. Versões desconhecidas continuam exigindo revisão explícita, não atualização automática no CI.

## Classificação do delta

- `IPage.FrameLocator` / `IFrame.FrameLocator`: o argumento string tornou-se opcional; retorno ainda `IFrameLocator`, encaminhado e envolvido pela mesma classificação de retornos. Não há remoção de comportamento de wrapper.
- `ILocator.Visible`: novo retorno `ILocator`, deve continuar decorado, com a mesma referência `Page`. Teste real por reflexão exercita a propriedade na 1.63 sem exigir sua presença na 1.61.
- `ILocator.WaitForFunctionAsync`, `LocatorWaitForFunctionOptions`: operação sem entrada de mouse/teclado, encaminhada sem transformação. Teste real exerce função sobre o elemento e preserva contagem.
- `IPage.DialogClosed` / `IBrowserContext.DialogClosed`: eventos novos de `IDialog`; permanecem pass-through nativo, sem afirmar adaptação do sender ou humanização dessa superfície nova. Não são eventos de criação de página/contexto. Essa limitação está explícita; não ampliar a whitelist automaticamente.
- `ScrollMode` e opções `Scroll` em Page/Frame/Locator/ElementHandle: parâmetros novos mantidos pelo encaminhamento nativo de opções explícitas. Não são expandidos pela humanização nesta entrega.
- `ScreenshotType.Webp`: enum de saída, sem transformação do SDK.
- `IAPIResponse.Timing`: DTO de diagnóstico encaminhado nativamente, sem wrapper de entrada.
- `HttpCredentialsList`, storage state `Credentials`/`Opfs`, tracing `AriaSnapshots`/`ScreenSnapshots`: campos novos encaminhados pelo Playwright; não registrar valores em relatórios/snapshots, nem inferir humanização.

## Evidência e limites

`PlaywrightApiAuditTests` registra o delta integral e compara por versão. A execução inicial da 1.63 apontou o delta e a ausência do Chromium revision 1243; isso não foi contabilizado como passe. Instalar a revisão correspondente e repetir é necessário. Os relatórios TRX/log em `artifacts/goal/` são locais e devem ser consultados para o resultado efetivo de cada execução. A revisão não afirma cobertura comportamental de todas as novas APIs; cobre os retornos de locator relevantes para a preservação do wrapper e mantém encaminhamento conservador do restante.
