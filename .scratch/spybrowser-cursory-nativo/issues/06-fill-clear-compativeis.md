# 06: Preencher e limpar campos no modo compatível

**What to build:** um consumidor ativa explicitamente o modo compatível e continua usando `FillAsync` e `ClearAsync` com os efeitos nativos do Playwright. O SDK deixa de transformar implicitamente qualquer preenchimento em clique, seleção total e digitação, sem trocar os tipos públicos nem alterar silenciosamente o default existente.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A opção de modo é aditiva, com significado e limites documentados; nesta entrega a correção garantida cobre Fill/Clear, sem prometer comandos ainda não corrigidos.
- [ ] Fill/Clear delegam à implementação nativa no modo compatível e preservam parâmetros, resultado e exceções.
- [ ] Testes locais com inputs comuns, senha, number/date, textarea, contenteditable e campo controlado por JS comparam o resultado com Playwright raw.
- [ ] Campos ocultos/desabilitados e múltiplos matches mantêm acionabilidade, strictness e erros pertinentes do Playwright.
- [ ] Não são injetados Ctrl+A/Backspace ou eventos de teclado adicionais para simular Fill/Clear.
- [ ] `Timeout=0`, options vazias e options explícitas continuam válidos no caminho nativo; objetos recebidos não são modificados.
- [ ] As interfaces `IPage`, `IFrame` e `ILocator` contratadas encaminham a operação de forma consistente.
- [ ] A documentação distingue preencher de digitar e explica o modo legado disponível para comparação.
- [ ] Há teste de cancelamento por fechamento do contexto enquanto Fill aguarda um campo indisponível.
- [ ] O consumidor não precisa mudar seus blocos para instalar esta evolução.

**Scope boundary:** não expandir o classificador de options para humanizar fills do RpaBlockly, não alterar suas chamadas e não incluir humanização caractere a caractere de Fill. As demais famílias são tratadas em tickets próprios.
