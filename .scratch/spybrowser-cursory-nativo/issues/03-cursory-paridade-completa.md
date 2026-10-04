# 03: Completar o algoritmo Cursory com paridade upstream

**What to build:** o consumidor da biblioteca nativa gera trajetórias Cursory completas, com seleção, temporalidade e transformações correspondentes à referência fixada. Completar o caminho já demonstrável do ticket 02, não criar pacotes separados de matemática nem portar NumPy inteiro.

**Blocked by:** 02 — Gerar trajetórias gravadas em uma biblioteca .NET autocontida.

**Status:** ready-for-agent

- [ ] Geração completa cobre ranking de candidatos, escolha ponderada, directness, morph, knots, jitter, amostragem temporal e endpoints finais.
- [ ] PCG64, SeedSequence e as distribuições efetivamente usadas reproduzem os vetores de referência; wraparound e rejeições têm testes específicos.
- [ ] Fixtures finais e intermediárias identificam commits, dataset e ferramentas de geração; fixtures incompatíveis com o dataset não são reutilizadas.
- [ ] RNG inteiro/bitstream é comparado exatamente; coordenadas usam tolerância justificada, inicialmente 1e-9 pixel em fixtures comuns; tolerâncias não são afrouxadas sem análise.
- [ ] Empates de ordenação têm regra determinística documentada; diferenças legítimas de CPU/upstream são isoladas em casos próprios.
- [ ] Arredondamento, soma, tempos duplicados e peculiaridades de coordenadas inteiras do upstream são preservados ou explicitamente versionados como divergência.
- [ ] Testes cobrem distância zero, subpixel, todos os quadrantes, parâmetros inválidos, seeds-limite e milhares de casos determinísticos de invariantes.
- [ ] Chamadas concorrentes não compartilham RNG mutável nem alteram o dataset; memória não cresce com uma cópia completa por geração.
- [ ] Testes cotidianos executam apenas .NET; regenerar referências pode usar ferramentas de desenvolvimento pinadas, nunca dependências de runtime.
- [ ] Um relatório diferencial localiza divergências por estágio e registra resultados Windows/Linux.
- [ ] Há procedimento reproduzível de atualização upstream e benchmark inicial de carga fria, geração aquecida e alocações, sem otimização prematura.
- [ ] O pacote experimental consumido externamente à solution executa o algoritmo completo e mantém as licenças/fontes correspondentes.

**Scope boundary:** limites de viewport, scheduling Playwright, limites operacionais de duração e cliques não entram no núcleo puro. Não corrigir peculiaridades upstream silenciosamente nem afirmar exatidão de timers do navegador.
