# 11: Criar contextos configurados por qualquer entrada pública

**What to build:** criar um contexto ou uma página pelo handle SpyBrowser ou pelo IBrowser público aplica a mesma configuração efetiva de identidade. O consumidor deixa de depender de uma rota especial de criação, sem perder o acesso raw ou forçar humanização quando ela está desligada.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] NewContext/NewPage pelo handle e pelo browser público passam pela mesma fábrica configurada, que chama o browser raw sem recursão.
- [ ] Locale, fuso, viewport, escala, user agent e timeouts são aplicados de acordo com precedência documentada de defaults, opções e callbacks.
- [ ] Objetos options fornecidos pelo consumidor não são modificados inadvertidamente nem perdem campos não relacionados à identidade.
- [ ] `Humanize=false` mantém contexts/pages raw, conforme o contrato esperado pelo RpaBlockly; configuração de identidade não depende da humanização.
- [ ] RawBrowser permanece um bypass intencional e claramente documentado.
- [ ] Testes locais comparam propriedades observadas pelos dois caminhos de criação, com e sem override explícito.
- [ ] Contexts descartáveis não adquirem lease de perfil persistente nem bloqueiam jobs que usam a mesma identidade em memória.
- [ ] Erro durante criação/configuração fecha os recursos já criados sem mascarar a exceção original.
- [ ] API pública existente e propriedade de recursos/descarte permanecem compatíveis.
- [ ] A implementação aproveita o mecanismo atual com extensão pequena, sem reescrever manualmente toda a API de páginas.

**Scope boundary:** identidade entre eventos/listagens será concluída no ticket 13. Não remover o adaptador atual do RpaBlockly, adicionar perfis persistentes a ele ou habilitar GPU probe automaticamente.
