# 17: Detectar incoerências de fuso, idioma e geometria

**What to build:** o operador executa o diagnóstico pela API/CLI e recebe findings corretos sobre fuso, idioma e dimensões efetivas. Aliases legítimos e diferenças de unidade deixam de gerar falsos erros, enquanto contradições claras ficam identificáveis.

**Blocked by:** 16 — Executar probes sem interferir nas páginas do usuário.

**Status:** ready-for-agent

- [ ] Fuso configurado e observado são comparados com normalização documentada; UTC/Etc/UTC e conversões Windows/IANA têm testes.
- [ ] Comparação não considera fusos equivalentes apenas porque têm o mesmo offset num instante; regras/aliases são tratados conservadoramente.
- [ ] Locale principal e navigator.languages usam normalização BCP-47, preservando diferenças de preferência legítimas.
- [ ] Viewport, screen e escala são comparados em unidades consistentes, com casos de DPR/zoom e NoViewport documentados.
- [ ] O esperado deriva das opções efetivas do contexto, não somente do manifesto base que callbacks podem ter alterado.
- [ ] Findings possuem códigos/severidade e aparecem tanto no resultado de API quanto na apresentação CLI pertinente.
- [ ] Ausência de dado gera resultado indeterminado/aviso adequado; não vira contradição automaticamente.
- [ ] FailOnConsistencyErrors continua funcionando para erros inequívocos; warnings não bloqueiam lançamentos arbitrariamente.
- [ ] Fixtures locais demonstram ao menos um caso coerente, um alias legítimo e um conflito por categoria.
- [ ] Não há GeoIP automático nem inferência obrigatória de idioma/fuso pelo proxy.
- [ ] Um cenário equivalente ao timezone local usado pelo RpaBlockly passa sem exigir habilitar probes por padrão no consumidor.

**Scope boundary:** sem migração de perfis e sem alterar as características apresentadas pelo navegador para silenciar findings. Snapshot histórico é uma capacidade separada.
