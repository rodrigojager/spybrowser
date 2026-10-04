# 09: Digitar com ritmo variável sem corromper conteúdo

**What to build:** comandos explícitos Type/PressSequentially podem receber ritmo variável sem trocar o texto, ignorar opções ou perder o controle de duração. Um fluxo local digita conteúdo multilíngue corretamente; situações cuja composição não pode ser preservada seguem pelo Playwright original.

**Blocked by:** 07 — Inserir texto e executar atalhos sem alterar seus eventos.

**Status:** ready-for-agent

- [ ] O comportamento é ligado aos comandos de digitação do modo compatível; Fill e InsertText mantêm as semânticas nativas dos tickets anteriores.
- [ ] O texto final e sua ordem são preservados, sem erros deliberados de digitação, truncamento ou correções artificiais.
- [ ] Testes incluem acentos, emojis, surrogate pairs, marcas combinantes e scripts não latinos.
- [ ] Limites reais de grafemas/IME são documentados; casos não comprovados usam fallback antes de inserir conteúdo, não alegações de suporte universal.
- [ ] Delay explícito e outras options continuam podendo delegar integralmente à chamada original.
- [ ] Existe orçamento total de tempo; texto longo não é acelerado fora da política nem truncado para aparentar sucesso.
- [ ] Fechar a página/contexto durante digitação impede novos caracteres e observa a tarefa real.
- [ ] Exceções e falhas de foco/elemento são propagadas sem repetir texto já inserido.
- [ ] Testes de eventos e conteúdo diferenciam digitação humana, Fill e InsertText.
- [ ] Não são registrados valores digitados nos diagnósticos ou mensagens de erro adicionados pelo SDK.

**Scope boundary:** não alterar blocos do RpaBlockly, nem converter seus fills em digitação. A coordenação entre mouse e teclado concorrentes é consolidada no ticket 10.
