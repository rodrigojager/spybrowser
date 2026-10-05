# Final package inspection

- Candidate: `0.2.0-beta.2.final.4d8a18c`
- Source / declared final commit: `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`
- Source archive SHA-256: `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`
- Producer manifest SHA-256: `8144b157fe26a7f000cd4728fbe19a94773fd0d203badfec9be856e0d5833727`
- `isFinal`: `true` (local technical candidate only)
- External publication: **prohibited**; upstream redistribution rights remain **UNVERIFIED**. This is not legal clearance.
- Package inspector: `PASS` for Cursory actual package contents, metadata, sources, dataset, licenses/notices and portable symbols. Its CLI does not accept `--expected-source-commit`; exact commit checked independently in all package nuspecs and Cursory inspection manifest.
- Cursory packaged source builds successfully. Exact DLL byte correspondence: **NOT PROVEN (packaged `3a93b3535312098328c5e817e5dad9430c9fbd8dcfba3fa8377f82e5df69c978`; rebuild `7aaa9e8bfb5dba1303cbec6d30a0286b6e9e1c1ea23910f93cfa0976c967cff9`)**. No binary identity claim is made.
- Runtime dependencies: Cursory has a single packaged runtime DLL and no dependency IDs. All four package identities/source commits were checked from actual nuspecs.

## Package and symbols SHA-256

- `SpyBrowser.Cli.0.2.0-beta.2.final.4d8a18c.nupkg` — `c50ad2a3381fded27fb7113bba52ee4e77f3b1e575e645cca4aed4d48cd840b5`
- `SpyBrowser.Core.0.2.0-beta.2.final.4d8a18c.nupkg` — `78a5ccedbe322f023fbef9a326a31cdfb95a14a93edfe4ed58b84ec70a1270d2`
- `SpyBrowser.Cursory.0.2.0-beta.2.final.4d8a18c.nupkg` — `595df25eb646defe3c66fa1505c2f517345476e26ca46333f1f9a473d5b8363e`
- `SpyBrowser.Playwright.0.2.0-beta.2.final.4d8a18c.nupkg` — `d5eb6a7a1098660a380675dec3a357f0070cc764457abf72723805dd76a7cb76`
- `SpyBrowser.Cli.0.2.0-beta.2.final.4d8a18c.snupkg` — `a36c62e8ffcb7817c3517e2b6d1288fc523844fd03a0fc053c985cf5e99fb8e3`
- `SpyBrowser.Core.0.2.0-beta.2.final.4d8a18c.snupkg` — `1d3f6bcd02a283f42c28806c661be5855116847b63302de42167022feff523cf`
- `SpyBrowser.Cursory.0.2.0-beta.2.final.4d8a18c.snupkg` — `2e76a9bf4fa5f48ccbfd8678d5cc4cec2cc083b17c5c4291dc8e4282e072159d`
- `SpyBrowser.Playwright.0.2.0-beta.2.final.4d8a18c.snupkg` — `ededa03871d588a85065c66e421f4fce0ab6da1781c72835999b28da5eceb559`

## Consumer projection

- `D:\SpyBrowser\artifacts\goal\final-feed-4d8a18c` contains byte-copied Core, Cursory and Playwright packages plus symbols and `manifest.json`; producer package hashes are preserved exactly.
- CLI package remains in the producer feed, not this three-library consumer projection.
