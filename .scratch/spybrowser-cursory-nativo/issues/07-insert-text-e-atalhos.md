# 07: Inserir texto e executar atalhos sem alterar seus eventos

**What to build:** no modo compatível, comandos normais `InsertTextAsync` e `PressAsync` preservam a diferença entre inserir texto e pressionar teclas. Um fluxo com atalhos, modificadores e inserção de conteúdo funciona como no Playwright raw, sem mudanças na API do consumidor.

**Blocked by:** 06 — Preencher e limpar campos no modo compatível.

**Status:** ready-for-agent

- [ ] InsertText chama a operação nativa, sem converter a string em digitação por tecla.
- [ ] Press mantém a interpretação nativa de chords/modificadores; não passa a string de um atalho como se fosse uma tecla simples em Down/Up.
- [ ] Um formulário local registra eventos e conteúdo, comprovando diferenças esperadas entre InsertText, Press e Type.
- [ ] Casos incluem seleção total, navegação por teclado, Enter, Shift e combinações suportadas na matriz do produto.
- [ ] Opções explícitas e Delay são preservados; a chamada com options pode continuar raw e o objeto options não é mutado.
- [ ] Mapeamentos nos locators/pages/frames e teclado são consistentes com as interfaces atualmente suportadas.
- [ ] Exceções originais são propagadas sem embalagem reflexiva extra e com stack útil.
- [ ] Após sucesso ou falha não permanecem teclas pressionadas pelo SDK; a implementação não interfere em modificadores de que não é dona.
- [ ] Testes não digitam segredos reais nem produzem logs com o conteúdo do usuário.
- [ ] Documentação do modo compatível registra o contrato implementado e mantém a saída raw acessível.

**Scope boundary:** não humanizar atalhos por implementação manual, não reinterpretar comandos do consumidor e não ampliar options para contornar o comportamento atual do RpaBlockly. Ritmo de Type/PressSequentially pertence ao ticket 09.
