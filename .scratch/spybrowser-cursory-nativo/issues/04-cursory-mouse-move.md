# 04: Executar Cursory por Mouse.MoveAsync com ativação explícita

**What to build:** uma aplicação seleciona Cursory na inicialização do SpyBrowser e usa o mesmo `IMouse.MoveAsync` para executar o movimento nativo .NET no navegador. A entrega inclui geração, execução e comprovação local ponta a ponta, sem mudar comandos do consumidor.

**Blocked by:** 01 — Tornar o movimento substituível sem mudar o comportamento atual; 03 — Completar o algoritmo Cursory com paridade upstream.

**Status:** ready-for-agent

- [ ] A seleção é aditiva e opt-in; a configuração anterior continua usando seu comportamento documentado.
- [ ] `Mouse.MoveAsync` sem options usa Cursory quando selecionado e alcança o destino numa página local de teste.
- [ ] Options presentes continuam podendo delegar integralmente ao Playwright original; nenhum valor explícito é descartado.
- [ ] A integração usa o port puro por um adaptador interno, sem cópia de algoritmo ou dependência inversa de Cursory para Playwright.
- [ ] O executor usa ordem serial, relógio monotônico e limites explícitos; nenhuma chamada é disparada em paralelo para pontos do mesmo movimento.
- [ ] Um teste confirma o endpoint e a sequência observável no browser; outro confirma que não há um processo adicional de geração.
- [ ] `Humanize=false` não carrega o dataset nem modifica a execução raw. O driver normal do Playwright é reconhecido, não confundido com sidecar Cursory.
- [ ] Falha de configuração/dataset aparece claramente antes da ação; fallback de algoritmo só ocorre conforme opção explícita, nunca após execução parcial.
- [ ] Cancelar um método próprio ou fechar a página interrompe novos envios e observa falhas pendentes.
- [ ] Selecionar Bézier novamente funciona sem mudar o código das ações e sem migração de perfil.
- [ ] A documentação distingue modo experimental, parâmetros do port e limites operacionais de execução.

**Scope boundary:** não expandir suporte a options para resolver o RpaBlockly, não alterar defaults globais e não antecipar a integração completa de todas as famílias de comandos. Stress temporal e geometria avançada são tratados no ticket 05.
