# 01: Tornar o movimento substituível sem mudar o comportamento atual

**What to build:** um ponto interno de substituição da geração de movimento, mantendo a automação atual em Bézier. Extrair somente o necessário para separar trajetória e execução; o consumidor continua chamando as mesmas interfaces e obtendo os mesmos efeitos. Esta é a preparação estrutural anterior à integração Cursory, não uma reescrita do humanizer.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Uma automação local percorre movimento, hover e clique antes/depois da extração, sem mudança intencional de comportamento.
- [ ] A geração Bézier passa pelo contrato interno de trajetória; o despacho continua usando o Playwright original, sem recursão pelo wrapper.
- [ ] Tempos e posições podem ser inspecionados em testes sem navegador; a execução real permanece coberta por teste de browser.
- [ ] Assinaturas públicas, opções atuais, seleção padrão e comportamento de `Humanize=false` permanecem compatíveis.
- [ ] O contrato interno representa posições e tempos sem depender de tipos do futuro pacote Cursory, evitando dependência cíclica.
- [ ] O teste não exige coordenadas aleatórias idênticas entre execuções; usa controle determinístico de teste ou invariantes documentados.
- [ ] Recursos e estado mutável não passam a ser compartilhados globalmente entre páginas.
- [ ] Nenhum framework público de plugins, container DI ou reorganização ampla de arquivos é introduzido.
- [ ] A documentação técnica explica o ponto de substituição, o ownership do estado e como executar a regressão local.
- [ ] Testes de navegador realmente executados são distinguíveis dos não executados.

**Scope boundary:** não implementar Cursory, mudar limites de duração, corrigir semântica dos comandos ou introduzir scheduler novo neste ticket. Alterações de outros tickets não são necessárias para demonstrar esta entrega.
