# 23: Instalar a pré-release e comprovar rollback em ambiente limpo

**What to build:** um consumidor instala os pacotes candidatos em ambiente limpo, ativa Cursory explicitamente e consegue voltar para Bézier ou desligar humanização sem mudar seus comandos ou migrar perfis. A entrega deixa o núcleo pronto para publicação autorizada, com evidências e licenças correspondentes ao binário.

**Blocked by:** 18 — Diagnosticar conflitos de browser e renderização; 20 — Validar uma atualização do pacote com o consumidor RpaBlockly; 21 — Demonstrar o ganho e o custo do novo movimento; 22 — Homologar upgrades do Playwright com evidência real de execução.

**Status:** ready-for-agent

- [ ] Pacotes são construídos e instalados via feed local em consumidor limpo, não apenas por project reference dentro da solution.
- [ ] O grafo contém a DLL Cursory separada, dados embarcados e nenhuma dependência adicional de servidor/processo de geração.
- [ ] O teste reconhece a instalação e o driver normal do Playwright; não exige erroneamente a ausência de todo Node no processo de automação.
- [ ] Conteúdo real de nupkg/nuspec, fontes correspondentes, símbolos, licenças e avisos de código/dados/helpers são verificados.
- [ ] Checklist LGPL/proveniência está concluído ou aponta bloqueio explícito de distribuição; separação em DLL não é apresentada como parecer jurídico suficiente.
- [ ] Ativar Cursory, selecionar Bézier e Humanize=false funcionam pelos mesmos comandos Playwright, sem fallback silencioso durante uma ação.
- [ ] Pin da versão anterior é demonstrado sem migração destrutiva de identidade/perfil; recursos continuam descartados corretamente.
- [ ] Releases documentam semântica do modo compatível, diferenças do legado, matriz homologada, limites do produto e rollback.
- [ ] Evidências RpaBlockly, browser contracts, paridade e benchmark são associadas ao candidato instalado.
- [ ] Nenhum default global é promovido silenciosamente; Cursory permanece opt-in até decisão explícita.
- [ ] Snapshots históricos não são dependência obrigatória desta pré-release; se já incluídos no pacote, só são anunciados como homologados após o ticket 24.
- [ ] Publicação em NuGet/GitHub externo exige aprovação operacional separada; este ticket não autoriza esse efeito por si só.

**Scope boundary:** não remover adaptadores do RpaBlockly, atualizar seus blocos nem empacotar single-file/AOT sem validação específica. O fechamento do recurso de snapshots na distribuição ocorre no ticket 24.
