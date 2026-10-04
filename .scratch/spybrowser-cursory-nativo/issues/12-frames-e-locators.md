# 12: Manter humanização em frames e coleções de locators

**What to build:** uma automação que navega por frames e obtém locators em coleções continua recebendo objetos Playwright com a mesma humanização da página de origem. A propagação é verificável em frames aninhados, inclusive cross-origin local, sem mexer nos blocos do consumidor.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] IFrameLocator e locators derivados mantêm o page hint e a scope de humanização corretos.
- [ ] Main frame, parent/child frames e listas de frames retornam referências consistentes para o mesmo objeto raw.
- [ ] Coleções de locators, incluindo AllAsync, mantêm a decoração nos elementos retornados.
- [ ] Referências de volta como locator.Page e page.Context respeitam a cache existente e não criam uma nova camada de wrapper a cada acesso.
- [ ] Wrap(Wrap(x)) não duplica humanização e Unwrap(Wrap(x)) devolve o objeto original.
- [ ] Testes em frames aninhados e cross-origin local executam uma ação e comprovam que ela passa pela política esperada.
- [ ] Argumentos Playwright conhecidos são desembrulhados ao retornar ao driver quando necessário; payloads arbitrários do usuário não sofrem recursão/modificação.
- [ ] Resultados de Evaluate/DTOs e objetos não contratados continuam intactos; não se tenta embrulhar todo retorno genericamente.
- [ ] `Humanize=false` e os escape hatches continuam operando sem decoração indevida.
- [ ] Descarte de frames/pages não cria retenção indefinida na cache; uso de referências fracas e ownership estão documentados.
- [ ] Handles não recebem promessa de humanização de métodos não implementados; a documentação distingue propagação de suporte a ações.

**Scope boundary:** este ticket não muda semântica de Click/Fill/Type nem resolve eventos de popup. Usa os handlers disponíveis para demonstrar propagação, podendo ser implementado em paralelo às correções desses handlers.
