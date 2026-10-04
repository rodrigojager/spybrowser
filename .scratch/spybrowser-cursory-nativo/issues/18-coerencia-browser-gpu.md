# 18: Diagnosticar conflitos de browser e renderização

**What to build:** o relatório operacional diferencia o navegador efetivamente iniciado, características declaradas e capacidades gráficas observadas. O usuário recebe avisos de conflitos claros e dos limites das máscaras experimentais, sem que o SDK tente falsificar mais superfícies.

**Blocked by:** 16 — Executar probes sem interferir nas páginas do usuário.

**Status:** ready-for-agent

- [ ] Família/versão real do navegador e versão Playwright aparecem no diagnóstico com origem explícita.
- [ ] Conflitos claros entre família de browser, UA e plataforma são identificados sem exigir igualdade literal de strings.
- [ ] Client Hints ausentes ou reduzidos por privacidade são diferenciados de valores comprovadamente contraditórios.
- [ ] Políticas GPU existentes continuam detectando software/hardware conforme sua capacidade real, com testes para renderer indisponível.
- [ ] Dados de WebGL/WebGPU passam por comparação conservadora; ausência de WebGPU não prova inconsistência.
- [ ] Overrides experimentais exibem aviso sobre strings, pixels, timing e contextos não abrangidos, sem promessa de invisibilidade.
- [ ] O hash gráfico existente não é descrito como certificação de equivalência de GPU.
- [ ] API/CLI apresentam findings com severidade/códigos e testes locais de caso coerente, conflito e informação indisponível.
- [ ] Atualização legítima de Chrome, GPU ou driver não bloqueia operação automaticamente.
- [ ] Nenhum dado é enviado a terceiros para avaliação e nenhum argumento/script stealth novo é adicionado para esconder findings.
- [ ] Defaults e callbacks efetivos são considerados para evitar acusar como erro um override intencional documentado.

**Scope boundary:** não construir um banco universal de fingerprints nem um detector definitivo de automação. Não ativar GPU probe automaticamente no RpaBlockly.
