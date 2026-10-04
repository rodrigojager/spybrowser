# 20: Validar uma atualização do pacote com o consumidor RpaBlockly

**What to build:** o consumidor RpaBlockly real executa fluxos locais com um pacote candidato SpyBrowser, mantendo suas interfaces, saídas e controle de recursos. Demonstrar que a integração funciona por atualização de pacote e inicialização, sem reescrever blocos e sem corrigir agora suas chamadas que desativam o humanizer.

**Blocked by:** 10 — Interromper e serializar entradas sem deixar ações órfãs; 12 — Manter humanização em frames e coleções de locators; 14 — Automatizar popups e novas páginas sem perder o wrapper; 15 — Explicar a humanização e seus fallbacks sem expor dados; 17 — Detectar incoerências de fuso, idioma e geometria.

**Status:** ready-for-agent

- [ ] Commit do consumidor e versões de SDK/Playwright/pacotes são fixados e registrados; checkout de integração fica isolado do trabalho normal do usuário.
- [ ] Um feed NuGet local fornece o candidato; o teste não exige publicar pacote externo.
- [ ] Um consumer check .NET 9 verifica o contrato público mínimo e um subconjunto local do RpaBlockly real exercita os fluxos relevantes.
- [ ] Humanize on/off, fuso local inclusive UTC, viewport, Chromium selecionado e identidade em memória continuam corretos.
- [ ] Evento de contexto, referência retornada e Browser.Contexts coincidem; fechar contexto remove-o da coleção.
- [ ] StorageStatePath, download local, screenshot, navegação e fluxo com frame/popup preservam comportamento.
- [ ] Cancelamento de Fill fecha o contexto e interrompe a tarefa real; cancelamento do launch limpa um browser que concluiu abertura tardiamente.
- [ ] Dois jobs com identidade em memória não bloqueiam por lease de perfil nem compartilham estado de entrada.
- [ ] Options explícitas podem continuar raw: humanizar o fill atual do RpaBlockly não é critério de aceite.
- [ ] Mantém-se o adaptador atual do consumidor e os outros providers; não se adiciona a fachada que conflita com CloakBrowser.dll.
- [ ] Checks executados, skips e dependências externas não exercitadas são relatados honestamente, sem marcar o suite inteiro como validado.
- [ ] Nenhum bloco, DSL ou componente de CAPTCHA é alterado para fazer os testes passarem.

**Scope boundary:** pequenas mudanças de referência/configuração existem apenas na branch/checkout de homologação. Publicar alterações no outro repositório, remover seu adaptador ou reformular sua humanização ficam para aprovação posterior.
