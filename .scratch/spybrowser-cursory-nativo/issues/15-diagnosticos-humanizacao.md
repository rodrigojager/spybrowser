# 15: Explicar a humanização e seus fallbacks sem expor dados

**What to build:** uma aplicação consegue observar se uma interação usou Cursory, outro algoritmo ou Playwright original e por quê. O relatório é leve e opcional, com informação operacional suficiente para depurar sem coletar o conteúdo da automação.

**Blocked by:** 04 — Executar Cursory por Mouse.MoveAsync com ativação explícita; 07 — Inserir texto e executar atalhos sem alterar seus eventos; 08 — Clicar, dar clique duplo e passar o mouse com ação final nativa; 09 — Digitar com ritmo variável sem corromper conteúdo.

**Status:** ready-for-agent

- [ ] Uma única superfície leve de observação é escolhida e documentada, sem dependência obrigatória de um backend de telemetria.
- [ ] Um fluxo local demonstra registros de ação humanizada, comando nativo por semântica, options não suportadas, cancelamento e erro.
- [ ] Eventos incluem método, modo, algoritmo, resultado e motivos estáveis; medidas de duração/pontos são incluídas onde disponíveis.
- [ ] Versões de SpyBrowser, Playwright, navegador, algoritmo e dataset são obtidas e associáveis ao relatório.
- [ ] Não são registrados texto digitado, senha, cookies, tokens, storage state, URLs completas ou seletores potencialmente pessoais por padrão.
- [ ] Testes com valores-sentinela comprovam ausência de informações proibidas, inclusive em caminhos de erro.
- [ ] Coordenadas/seed detalhadas são opt-in explícito se oferecidas; não existe coleta de novas gravações de mouse do usuário por padrão.
- [ ] Um observer com falha não derruba nem repete a automação.
- [ ] Sem observer, o hot path não aloca evento por ponto nem carrega dados adicionais desnecessários.
- [ ] Fallback raw por options permanece comportamento aceito; o relatório não o apresenta como erro a corrigir obrigatoriamente no consumidor.
- [ ] Fill nativo por política não é anunciado como digitação humanizada e métricas não viram um stealth score.

**Scope boundary:** sem serviço externo, banco de dados ou dashboard novo. Snapshots persistidos pertencem ao ticket 19; aqui a entrega já é demonstrável pela observação de uma execução.
