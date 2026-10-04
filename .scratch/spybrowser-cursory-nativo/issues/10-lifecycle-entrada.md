# 10: Interromper e serializar entradas sem deixar ações órfãs

**What to build:** movimentos Cursory, cliques e digitação funcionam juntos sem sequências conflitantes na mesma página. Cancelamento, timeout e fechamento realmente interrompem entrada futura, enquanto páginas independentes continuam operando em paralelo.

**Blocked by:** 05 — Manter o movimento correto sob atraso, escala e estado desconhecido; 08 — Clicar, dar clique duplo e passar o mouse com ação final nativa; 09 — Digitar com ritmo variável sem corromper conteúdo.

**Status:** ready-for-agent

- [ ] Um gate por página ordena operações de entrada decoradas, incluindo entrada encaminhada raw através do wrapper, sem lock global de navegador.
- [ ] Duas páginas executam em paralelo sem compartilhar cursor, RNG, teclas ou fila; teste demonstra o isolamento.
- [ ] Fila, readiness, movimento e ação consomem o mesmo orçamento, respeitando defaults observados e sua atualização pelo wrapper.
- [ ] Fechamento e navegação não dependem do gate de entrada e não causam deadlock; callbacks não rodam sob locks internos.
- [ ] Page/context/browser lifetime interrompe pausas e futuros envios; todos os faults das tarefas iniciadas são observados.
- [ ] WaitAsync cancelado não é tratado como prova de cancelamento subjacente; testes verificam ausência de eventos posteriores e cleanup real.
- [ ] Métodos próprios honram CancellationToken; o limite das interfaces Playwright sem token permanece documentado.
- [ ] Down/Up de um drag em múltiplas chamadas não mantêm um lock indefinido nem têm seus botões liberados pelo SDK indevidamente.
- [ ] Cleanup libera somente teclas/botões de que o SDK é dono, preserva a causa original e não repete ações.
- [ ] Testes abrangem fechamento durante fila, espera, movimento, clique e digitação, além de timeout imediatamente antes da ação final.
- [ ] Descarte é idempotente; teste de longa repetição não retém páginas/contextos nem acumula tarefas.
- [ ] Um fluxo integrado Cursory + click + typing termina corretamente no navegador local, sem alterar comandos do consumidor.

**Scope boundary:** acesso via CDP/Unwrap continua fora da coordenação observável e requer responsabilidade/invalidação explícita do chamador. Não prometer isolamento de entradas externas não controladas pelo SDK.
