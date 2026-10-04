# 24: Homologar snapshots na distribuição final

**What to build:** o operador instala o candidato final, salva/compara snapshots opcionais e consegue desativar ou reverter o recurso sem danificar identidades e perfis. Fechar o caminho de distribuição do diagnóstico histórico, independente da homologação anterior do núcleo Cursory.

**Blocked by:** 19 — Comparar sessões por snapshots locais opcionais; 23 — Instalar a pré-release e comprovar rollback em ambiente limpo.

**Status:** ready-for-agent

- [ ] Consumidor instalado a partir de pacote executa salvar, selecionar baseline, comparar e descartar snapshots, sem acesso ao workspace de desenvolvimento.
- [ ] Schema de snapshot é independente do schema de identidade e da versão dos dados do algoritmo.
- [ ] Rollback do pacote não altera/corrompe perfil ou manifesto de identidade; snapshots de versão mais nova são ignorados ou rejeitados de forma clara conforme contrato.
- [ ] Desligar persistência de snapshots e excluir arquivos do recurso não remove storage state, cookies, perfis ou identidades.
- [ ] Falta de permissão, interrupção de escrita e sessões concorrentes são exercitadas no formato distribuído, preservando baseline anterior válida.
- [ ] Testes confirmam ausência de secrets e isolamento dos dados do recurso, inclusive após erros e rollback.
- [ ] Mudanças legítimas de versões e GPU aparecem como informação contextual, sem alterar automaticamente a baseline nem bloquear a execução.
- [ ] Manual operacional cobre ativação, comparação, retenção, exportação quando disponível e descarte seguro.
- [ ] Release checklist da entrega completa associa snapshots às evidências de compatibilidade, licenciamento e desempenho do núcleo.
- [ ] Não se publica externamente ou altera defaults globais sem aprovação específica, mesmo com todos os testes concluídos.

**Scope boundary:** não criar serviço de telemetria, migração de perfil ou política automática de identidade. Este ticket homologa o produto instalado; não reimplementa armazenamento e comparação já entregues no ticket 19.
