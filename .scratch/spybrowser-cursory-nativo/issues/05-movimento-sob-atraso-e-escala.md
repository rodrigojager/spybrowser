# 05: Manter o movimento correto sob atraso, escala e estado desconhecido

**What to build:** a mesma automação de movimento permanece correta quando o transporte está lento, a página usa outra escala ou o chamador alterna operações raw e humanizadas. O tempo planejado não vira uma rajada de eventos atrasados e o SDK não inventa uma posição inicial conhecida.

**Blocked by:** 04 — Executar Cursory por Mouse.MoveAsync com ativação explícita.

**Status:** ready-for-agent

- [ ] O scheduler usa instantes absolutos e coalesce intermediários vencidos, em vez de somar latência a cada pausa ou recuperar atraso com rajadas ilimitadas.
- [ ] O destino final é preservado salvo cancelamento, deadline ou erro real; não há alteração silenciosa do destino solicitado.
- [ ] Redimensionar duração considera amostragem e frequência sem selecionar involuntariamente outra gravação; os limites não modificam o núcleo do port.
- [ ] Testes com tempo/controlador fake exercitam atrasos, pausas repetidas, fim do orçamento e fechamento durante espera.
- [ ] Testes reais cobrem movimento curto/subpixel, bordas e DPR 1, 1.25, 1.5 e 2; não há arredondamento universal indevido para inteiro CSS.
- [ ] A posição por página só é atualizada após envio confirmado; estado desconhecido usa estratégia segura documentada.
- [ ] Existe mecanismo simples de invalidar estado ao misturar acesso raw e humanizado, com demonstração de uso.
- [ ] Botão já pressionado pelo chamador é detectado quando observável; preparação inadequada é evitada sem liberar esse botão.
- [ ] Dataset é compartilhado de forma imutável, enquanto estado de cursor/RNG é isolado; não há cópia de dados por página.
- [ ] Benchmark registra carga fria, geração aquecida, alocações e ritmo recebido pelo DOM; máquina, versões e limitações são registradas.
- [ ] Não se utiliza busy wait, prioridade global de processo nem modificação de timers do sistema.

**Scope boundary:** não afirmar tempo real ou equivalência exata entre ritmo planejado e DOM. A coordenação completa entre famílias de entrada fica no ticket 10.
