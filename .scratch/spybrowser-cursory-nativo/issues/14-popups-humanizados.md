# 14: Automatizar popups e novas páginas sem perder o wrapper

**What to build:** páginas obtidas por eventos ou espera de popup podem ser automatizadas com a mesma política da sessão. Eventos, retornos de espera e context.Pages representam a mesma página pública, sem dupla humanização.

**Blocked by:** 13 — Expor contextos consistentes em eventos e listagens.

**Status:** ready-for-agent

- [ ] Context.Page e Page.Popup entregam objetos decorados quando habilitado e raw quando desabilitado, conforme contrato público.
- [ ] WaitForPopup e RunAndWaitForPopup retornam a mesma instância pública correspondente aos eventos e a context.Pages.
- [ ] Uma aplicação local abre popup e executa uma ação nele pelos comandos Playwright normais, comprovando a propagação.
- [ ] Múltiplos popups concorrentes e fechamento imediato não confundem identidade, page hints ou lifetime.
- [ ] Handler adicionado/removido, inscrição duplicada e descarte seguem a semântica de eventos definida no ticket anterior.
- [ ] Não há callbacks sob locks nem espera síncrona por trabalho assíncrono no bridge.
- [ ] Coleções removem páginas fechadas e não mantêm wrappers de páginas mortas indefinidamente.
- [ ] Eventos sem payload Playwright relevante e callbacks arbitrários não são reescritos genericamente.
- [ ] Exceções e timeout das APIs de espera mantêm o contrato do Playwright.
- [ ] Os testes cobrem Unwrap e ativação/desativação de humanização, sem presumir que todos os métodos de um objeto são humanizados.

**Scope boundary:** não alterar o conteúdo de popups, navegação ou comportamento da aplicação testada. As páginas internas de diagnóstico ainda são responsabilidade do ticket 16.
