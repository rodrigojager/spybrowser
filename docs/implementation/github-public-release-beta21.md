# Publicação GitHub beta2.1

A publicação externa foi autorizada pelo operador, que declarou possuir a licença do dataset. A autorização posterior para tornar **todo o repositório SpyBrowser público, incluindo seu histórico**, foi explícita. `OPERATOR_ATTESTED` não é revisão jurídica independente. Nenhum pacote foi publicado no NuGet ou no registry GitHub Packages.

## Entrega verificável

- Release pública: https://github.com/rodrigojager/spybrowser/releases/tag/v0.2.0-beta.2.1
- Fonte congelada/tag: `7983680083dd66aea2ff39a753533d02d8de2491`.
- Workflow de recuperação: `db44394`, executando checkout do **tag imutável**, não reempacotando a fonte de `main`.
- Execução de publicação bem-sucedida: https://github.com/rodrigojager/spybrowser/actions/runs/37444936875
- Cinco nupkg, cinco snupkg e dois TRX públicos. SDK produtor `8.0.319`.
- TRX nativo: 37 executados/passaram, zero falhas/erros/skips.
- TRX produto: 179 executados/passaram de 180; um teste de foco headed explicitamente não habilitado. Zero falhas/erros. Não equivale a cobertura headed/GPU real.
- Auditoria independente leu os cinco PDBs reais, conferiu mappings canônicos SourceLink e baixou **62 documentos controlados por fonte sem autenticação**. SHA-256 dos bytes recebidos coincidiu exatamente com os checksums dos PDBs; 16 documentos gerados excluídos de forma explícita.
- Os três pacotes consumidos pelo RpaBlockly foram baixados sem autenticação, tiveram SHA-256 e nuspec/version/commit conferidos. Restore em cache NuGet novo em D, build completo e execução dos checks focados foram bem-sucedidos; DLLs efetivamente usadas pelo consumidor coincidem byte a byte com as contidas nesses pacotes.

Projeção detalhada: `docs/implementation/github-public-release-beta21.json`. Raw downloads, logs, auditoria e retry de um timeout TLS preservados em `artifacts/goal/resumed-e09/` e `D:/Temp/spybrowser-beta21-public-audit/`. Nenhuma transformação de newline foi aceita como correspondência remota.

## Histórico preservado

A beta2 permanece imutável. Seu build Windows estava correto em relação ao archive CRLF local, mas os PDBs não tinham os mesmos checksums dos bytes LF públicos do GitHub. A beta2.1 corrige essa origem com archive independente de `core.autocrlf` e política LF para C#. Não altera algoritmos, defaults ou bytes de fixtures/licenças.

O primeiro workflow beta2 falhou na seleção do SDK; o primeiro workflow beta2.1 falhou pela falta da habilitação explícita de testes/browser instalado. Ambos foram preservados, não chamados de sucesso. O workflow atual instala o navegador e executa os testes como pré-requisito de publicação; resultados TRX são distribuídos como assets da release, sem apagar artifacts antigos para contornar quota.

Os feeds ef2 e todos os relatórios históricos de aceite continuam com seus hashes e decisões originais. A nova publicação não os relabela como testes desta release, nem concede garantias universais de desempenho, providers ou sites.
