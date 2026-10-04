# 16: Executar probes sem interferir nas páginas do usuário

**What to build:** ao solicitar diagnóstico, o usuário recebe o resultado sem perder a aba atual, navegar páginas restauradas ou confundir a página interna de probe com uma página de trabalho. A operação tem limite de tempo e libera recursos mesmo em falha.

**Blocked by:** 13 — Expor contextos consistentes em eventos e listagens; 14 — Automatizar popups e novas páginas sem perder o wrapper.

**Status:** ready-for-agent

- [ ] O probe usa página temporária dedicada e não reutiliza/navega a primeira página disponível do usuário.
- [ ] A página interna não aparece nos eventos/listagens públicas de trabalho segundo uma política explícita, preservando o acesso raw intencional.
- [ ] Preparação, registro, filtragem e fechamento da página interna são testados contra corridas com novas páginas do usuário.
- [ ] O resultado pode ser solicitado nos modos persistent, context e browser-only previstos, sem precisar alterar o fluxo de automação.
- [ ] Há timeout próprio e cleanup em finally; falha de GPU, JS, criação ou fechamento não vaza página/contexto.
- [ ] O probe considera a configuração efetiva do contexto após defaults, overrides e callbacks.
- [ ] Não é ligado automaticamente onde o consumidor optou por RunGpuProbe=false; não roda por clique.
- [ ] Testes locais verificam URL, estado e referências de abas de usuário antes/depois do diagnóstico.
- [ ] APIs indisponíveis pela origem/contexto do probe são reportadas como indisponíveis, não como prova de fingerprint incoerente.
- [ ] Caso uma origem segura ou headers sejam necessários, utiliza fixture local controlada, sem consultas públicas automáticas.
- [ ] A superfície de diagnóstico distingue resultado incompleto, timeout e findings reais, com mensagens acionáveis.

**Scope boundary:** não criar novos mecanismos de spoofing ou exigir GPU hardware em toda execução. Regras novas de coerência são entregues pelos tickets 17 e 18.
