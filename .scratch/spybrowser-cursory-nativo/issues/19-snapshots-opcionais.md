# 19: Comparar sessões por snapshots locais opcionais

**What to build:** um operador salva um snapshot da sessão e compara uma execução posterior com uma baseline escolhida explicitamente. O relatório mostra mudanças de versões e características sem sobrescrever o perfil, coletar segredos ou bloquear automaticamente uma atualização legítima.

**Blocked by:** 15 — Explicar a humanização e seus fallbacks sem expor dados; 17 — Detectar incoerências de fuso, idioma e geometria; 18 — Diagnosticar conflitos de browser e renderização.

**Status:** ready-for-agent

- [ ] A capacidade é opt-in e produz snapshot JSON com schema próprio, independente do manifesto de identidade e dos dados de perfil.
- [ ] Inclui versões, algoritmo/dataset, características normalizadas e findings, sem conteúdo de páginas, cookies ou textos digitados.
- [ ] API/CLI permitem salvar, escolher baseline e comparar, com demonstração completa em duas execuções locais.
- [ ] A baseline aceita não é substituída automaticamente pelo snapshot mais recente.
- [ ] Escrita é atômica; permissões e retenção têm comportamento documentado e testes nas plataformas suportadas.
- [ ] Sessões concorrentes com mesmo IdentityId em modo descartável não disputam um único arquivo global nem sobrescrevem resultados.
- [ ] Schema desconhecido/corrompido gera erro útil e não altera o perfil ou a baseline existente.
- [ ] Comparação apresenta mudanças de campos e severidade contextual, não apenas um hash ou stealth score.
- [ ] Atualizações legítimas do browser/GPU/driver são informativas salvo contradição real demonstrada por regras existentes.
- [ ] Desativar snapshots elimina persistência adicional; excluir snapshots não afeta login/perfil/identidade.
- [ ] Testes incluem falta de permissão, interrupção de escrita, concorrência e ausência de valores secretos-sentinela.

**Scope boundary:** sem banco de dados, serviço externo, coleta contínua compulsória ou bloqueio de sessão por mudança de fingerprint. A homologação de instalação/rollback dessa capacidade será concluída no ticket 24.
