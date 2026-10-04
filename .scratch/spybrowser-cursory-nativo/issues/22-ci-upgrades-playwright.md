# 22: Homologar upgrades do Playwright com evidência real de execução

**What to build:** o mantenedor consegue verificar uma atualização do Playwright em Windows/Linux e saber quais testes realmente rodaram. O pipeline alerta para novas superfícies não cobertas sem quebrar silenciosamente o uso normal do SDK.

**Blocked by:** 10 — Interromper e serializar entradas sem deixar ações órfãs; 12 — Manter humanização em frames e coleções de locators; 14 — Automatizar popups e novas páginas sem perder o wrapper.

**Status:** ready-for-agent

- [ ] A lane obrigatória usa a versão-base 1.61.0 e instala explicitamente Chromium/dependências nas plataformas suportadas.
- [ ] A lane de versão estável mais recente resolve a versão uma vez por workflow, registra o valor e o reutiliza nos jobs relacionados.
- [ ] Matriz cobre build, testes de contratos, navegador local e empacotamento; testes não executados são skipped/job não executado, nunca sucesso por return silencioso.
- [ ] Headless é exercitado regularmente; headed/Xvfb, Chrome e Edge Windows têm lanes adicionais proporcionais ao custo, com disponibilidade explícita.
- [ ] Relatórios registram versões reais do browser, SDK e Playwright, evitando atribuir resultados a uma versão não instalada.
- [ ] Auditoria da superfície identifica métodos/options novos e retornos Playwright ainda sem adaptação; desconhecidos continuam delegados, não parcialmente interceptados.
- [ ] Testes incluem frames, eventos, popups, identidade de referências, semântica de ações e lifecycle já implementados.
- [ ] Nenhum browser test depende de sites externos ou resultados de CAPTCHA.
- [ ] Falhas intermitentes não são escondidas por retries que transformem regressões persistentes em sucesso; evidências de falha permanecem acessíveis.
- [ ] Atualizar a versão mínima do pacote é decisão posterior, não consequência automática de uma lane latest verde.
- [ ] A documentação permite reproduzir localmente cada lane e entende que Xvfb não fornece GPU hardware.

**Scope boundary:** não elevar o framework alvo ou adicionar suporte NativeAOT como parte do upgrade Playwright. Testes novos devem acompanhar cada feature; este ticket consolida a matriz, não adia todos os testes até o final.
