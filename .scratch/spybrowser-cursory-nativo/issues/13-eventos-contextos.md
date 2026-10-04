# 13: Expor contextos consistentes em eventos e listagens

**What to build:** um consumidor acompanha a criação e o fechamento de contextos usando eventos e Browser.Contexts e encontra os mesmos objetos retornados pelas fábricas. Contextos são anunciados uma única vez, já preparados, e removidos após fechar.

**Blocked by:** 11 — Criar contextos configurados por qualquer entrada pública.

**Status:** ready-for-agent

- [ ] O objeto recebido no evento de contexto é ReferenceEquals ao retornado por NewContextAsync e ao item da coleção correspondente.
- [ ] Contexto criado por NewPageAsync segue o mesmo contrato de registro, preparação e publicação.
- [ ] Cada contexto é anunciado uma vez; corrida entre criação e fechamento não publica um contexto já encerrado indevidamente.
- [ ] A coleção deixa de conter o contexto após fechamento e não expõe uma lista mutável interna.
- [ ] Assinatura, remoção e inscrições duplicadas preservam semântica de eventos .NET; sender e argumentos são adaptados somente conforme contrato explícito.
- [ ] Nenhum callback do usuário é executado segurando lock de registro.
- [ ] O bridge não bloqueia callback síncrono com Result/Wait para aguardar preparação assíncrona.
- [ ] Contextos externos/raw que não passaram pela fábrica têm política documentada; não recebem promessa de defaults aplicados retroativamente.
- [ ] Erros de handlers mantêm o contrato compatível com a implementação subjacente, sem serem engolidos arbitrariamente.
- [ ] Descarte remove bridges e referências de longa vida; testes de repetição e fechamento cobrem vazamentos e deadlocks.
- [ ] O adaptador existente do RpaBlockly continua utilizável, sem eventos duplicados decorrentes da nova implementação do SDK.

**Scope boundary:** não remover o adaptador do consumidor neste ticket. O foco é contexto/browser; eventos e esperas de páginas/popups são tratados no ticket 14.
