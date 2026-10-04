# 21: Demonstrar o ganho e o custo do novo movimento

**What to build:** um mantenedor executa um comparativo reproduzível entre Bézier e Cursory e recebe evidência do comportamento observado e do custo operacional. O experimento mede a execução real, não somente a trajetória ideal gerada em memória.

**Blocked by:** 10 — Interromper e serializar entradas sem deixar ações órfãs; 15 — Explicar a humanização e seus fallbacks sem expor dados.

**Status:** ready-for-agent

- [ ] Mesmas tarefas locais, distâncias, contexto e configuração de browser são usadas para comparar os dois algoritmos.
- [ ] Relatório registra duração planejada/observada, comprimento/deslocamento, intervalos, velocidade/aceleração com método e unidades, pausas e conclusão das tarefas.
- [ ] Custo de carga fria, geração aquecida, alocações e memória por processo/página é separado do custo de despacho Playwright.
- [ ] Condições do ambiente, hardware, OS, browser, Playwright, algoritmo e dataset são registradas.
- [ ] Casos incluem movimentos curtos/longos, alvo estável, transporte lento e múltiplas páginas independentes.
- [ ] Seeds controladas permitem repetir a geração; timestamps reais usam tolerâncias e não são prometidos como reprodutíveis bit a bit.
- [ ] O orçamento inicial de geração é avaliado em máquina de referência; runner compartilhado não produz falha flaky por um limiar rígido sem contexto.
- [ ] Relatório diferencia regressão de correção funcional, custo adicional e diferença esperada de ritmo.
- [ ] Não se usa percentual de CAPTCHA, indetectabilidade ou stealth score como conclusão sem experimento específico autorizado.
- [ ] Não são coletados movimentos reais de pessoas sem consentimento; amostras humanas adicionais não são requisito para concluir o ticket.
- [ ] O procedimento tem um comando/fluxo documentado e resultado legível sem necessidade de serviço externo.

**Scope boundary:** correções extensas identificadas pelo benchmark viram trabalho explícito, não otimização irrestrita. Não testar contra controles de terceiros nem prometer que um algoritmo é indistinguível de uma pessoa.
