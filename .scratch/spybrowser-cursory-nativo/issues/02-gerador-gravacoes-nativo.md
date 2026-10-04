# 02: Gerar trajetórias gravadas em uma biblioteca .NET autocontida

**What to build:** um consumidor .NET consegue instalar localmente uma biblioteca experimental e gerar uma trajetória gravada adaptada entre dois pontos, com posições e tempos reproduzíveis e sem processo adicional. Entregar um caminho estreito completo: dados embarcados, seleção seeded e transformação geométrica, geração acessível ao consumidor de demonstração e pacote com licenças corretas. A capacidade é explicitamente uma prévia de gravações, não Cursory completo.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] O pacote `SpyBrowser.Cursory` é .NET 8, independente de Playwright, com DLL separada e dependências de execução limitadas à BCL.
- [ ] Um consumidor de demonstração executa a geração a partir do pacote local; não precisa de Node/Python, serviço, download no primeiro uso ou browser.
- [ ] Dataset, origem, commits, SHA-256, contagem de gravações e licenças constam de manifesto verificável.
- [ ] A diferença entre as 2.357 gravações da referência Python e as 2.356 do port escolhido é registrada; fixtures usam exatamente o dataset escolhido.
- [ ] A seleção reproduzível utiliza o subconjunto necessário do RNG de referência, com vetores; não substitui esse RNG por System.Random fingindo paridade.
- [ ] A trajetória gerada tem origem/destino corretos, valores finitos e tempos coerentes; entrada inválida falha antes de retornar dados parciais.
- [ ] O dataset gzip é carregado uma vez, validado com limites de tamanho e mantido imutável; chamadas concorrentes não o modificam.
- [ ] O pacote não herda indevidamente MIT, autoria ou copyright globais para o código derivado; inclui avisos LGPL e demais atribuições pertinentes.
- [ ] Proveniência dos dados, fontes correspondentes e condições de redistribuição foram verificadas; pendências bloqueiam distribuição externa e aparecem no checklist.
- [ ] Há teste de conteúdo do pacote, integridade do recurso e consumo em ambiente sem dependências externas de geração.
- [ ] A API experimental e a demonstração deixam explícitos os limites desta prévia e não ativam um algoritmo Cursory incompleto no SpyBrowser.

**Scope boundary:** não integrar humanização no browser, anunciar paridade completa ou publicar em um feed externo. O subconjunto numérico só é introduzido na medida necessária para a capacidade demonstrável deste ticket.
