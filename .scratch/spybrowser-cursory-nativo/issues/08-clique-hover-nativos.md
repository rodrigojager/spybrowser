# 08: Clicar, dar clique duplo e passar o mouse com ação final nativa

**What to build:** uma automação usa Click, DblClick e Hover com movimento preparatório, mas continua contando com o Playwright para executar e validar a ação final. A entrega funciona com Bézier imediatamente e utiliza qualquer estratégia de movimento já selecionada sem manter implementações separadas de clique.

**Blocked by:** 01 — Tornar o movimento substituível sem mudar o comportamento atual; 06 — Preencher e limpar campos no modo compatível.

**Status:** ready-for-agent

- [ ] Preparação e ação final usam objetos raw internamente, evitando recursão/dobro de humanização.
- [ ] Clique final usa o locator ou operação nativa correspondente; uma bounding box antiga não é usada para substituir cegamente a ação.
- [ ] DblClick produz os eventos nativos esperados, inclusive dblclick/detail, sem assumir que dois ciclos simples são equivalentes.
- [ ] O destino preparatório é coerente com o ponto da ação final; não há salto extra deliberado de alvo aleatório para centro.
- [ ] Testes locais cobrem alvo estável, alvo movido/removido, overlay, input, hover e links com navegação.
- [ ] Um contador demonstra que sucesso, falha e fallback nunca repetem clique/duplo clique depois de efeitos parciais.
- [ ] Opções como Force, Trial, Position, Modifiers e Button seguem pelo caminho original quando não suportadas; Trial não provoca preparação adicional do SDK.
- [ ] Deadline total considera readiness, preparação e ação; timeout esgotado não é convertido em Timeout=0, que significa ilimitado.
- [ ] Se a preparação não for segura, a operação nativa permanece utilizável, com fallback anterior a efeitos irreversíveis.
- [ ] O modo compatível documenta movimentos adicionais e a preservação da semântica da ação, sem prometer identidade de todos os eventos.

**Scope boundary:** não implementar drag/touch e não perseguir indefinidamente elementos móveis. O teste deste ticket não depende de Cursory; integração conjunta e lifecycle são validados no ticket 10.
