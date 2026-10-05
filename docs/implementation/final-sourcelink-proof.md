# Independent final SourceLink / Portable-PDB audit

**Finding: `MISSING_OR_WRONG_MAPPING` — final-feed SourceLink requirement is not met.** Inspection of the actual four production Portable PDBs in the final-feed `.snupkg` files found **no** SourceLink custom-debug-information record (CDI kind GUID `cc110556-a091-4d38-9fec-25ab9a351a6a`) in any PDB. A nuspec repository commit, a readable repository URL, PDB/BSJB validity, or matching document checksums cannot substitute for the absent SourceLink JSON mapping. **Release parent: this is a genuine feed/build gap; producing a newly source-bound feed with SourceLink configured and re-running the relevant validations is required.** No package/source rebuild was attempted here.

## Actual artifacts inspected

Source identity: full commit `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`; repository `https://github.com/rodrigojager/spybrowser.git`; frozen Git-archive SHA-256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4`. The archive hash was independently recomputed, and the final commit object exists locally. Source documents were compared to the exact archive bytes produced with the recorded CRLF Git archive settings; **no line-ending normalization was needed**.

| Package | `.nupkg` SHA-256 | `.snupkg` SHA-256 | Actual Portable PDB SHA-256 | PDB documents |
|---|---|---|---|---:|
| SpyBrowser.Cli | `c50ad2a3381fded27fb7113bba52ee4e77f3b1e575e645cca4aed4d48cd840b5` | `a36c62e8ffcb7817c3517e2b6d1288fc523844fd03a0fc053c985cf5e99fb8e3` | `65f47da56af04524ba69cfcd31bbca13b105677d83a124acaecdd52660a3a98b` | 6 |
| SpyBrowser.Core | `78a5ccedbe322f023fbef9a326a31cdfb95a14a93edfe4ed58b84ec70a1270d2` | `1d3f6bcd02a283f42c28806c661be5855116847b63302de42167022feff523cf` | `786d039a8b4274264ec6b5b58196bd42e49880f9647bb8808363cea4b030a96c` | 15 |
| SpyBrowser.Cursory | `595df25eb646defe3c66fa1505c2f517345476e26ca46333f1f9a473d5b8363e` | `2e76a9bf4fa5f48ccbfd8678d5cc4cec2cc083b17c5c4291dc8e4282e072159d` | `68d5da35788bef58b73a24da3a5b83a726fc1516ce3f824cc00f07ba3d36fccf` | 14 |
| SpyBrowser.Playwright | `d5eb6a7a1098660a380675dec3a357f0070cc764457abf72723805dd76a7cb76` | `ededa03871d588a85065c66e421f4fce0ab6da1781c72835999b28da5eceb559` | `e3eca06fa0a05e6ef2e792d1c94da92e45b9edb78227a01db3946eb023e96eef` | 36 |

The four `.snupkg` hashes and PDB hashes were cross-checked against `docs/implementation/final-package-inspection.json` and `docs/implementation/final-installed-windows-proof.json`; the PDB bytes themselves were extracted from the actual symbol packages and inspected independently with a .NET 8 `System.Reflection.Metadata` BCL helper outside the repository. All 71 PDB `Document` records, SHA-256 algorithm GUIDs, document checksums, and embedded-source CDI flags are recorded individually in [`artifacts/goal/final-sourcelink-proof/final-sourcelink-proof.json`](../../artifacts/goal/final-sourcelink-proof/final-sourcelink-proof.json).

## Source-document correspondence and boundaries

Of 71 documents, **58 non-generated source documents all SHA-256-match the corresponding bytes in the exact final source archive (58/58)**. The remaining **13** are compiler-generated `obj` documents excluded from Git-hosted source mapping; **one** (the Core `RegexGenerator.g.cs`) has EmbeddedSource CDI (`0e8a571b-6926-466e-b4ad-8ab04611f5fe`). Its embedded payload was identified but not decompressed or independently content-hashed. The source-document matches establish local version correspondence only; they do **not** cure the missing SourceLink CDI.

The expected commit-versioned raw URL template, if configured, is `https://raw.githubusercontent.com/rodrigojager/spybrowser/4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b/{documentPath}`. This is a proposed versioned pattern, **not a URL found in the PDB**. No network fetch was made: remote accessibility/publication is unverified and is not claimed. External publication remains unauthorized; upstream rights remain `UNVERIFIED`.

## Validation and scope

- Inspected **all four actual production PDBs** and enumerated every PDB custom-debug-information row by CDI kind GUID; SourceLink CDI matches: **0/4**.
- Verified SHA-256 for **all 71 document records** against the corresponding exact-archive file when source-controlled; source correspondence: **58/58**; generated/excluded: **13**; CRLF/LF-normalization discrepancies: **0**.
- Helper: `D:/Temp/ownhelper` (outside the repository); SDK/runtime `8.0.319` / .NET 8; no external helper dependencies. No full solution tests, network requests, source changes, feed changes, ledger changes, commits, publication, or heavy builds were performed.

Machine-readable per-document evidence, raw artifact hashes, classifications, limitations, and decision: [`final-sourcelink-proof.json`](../../artifacts/goal/final-sourcelink-proof/final-sourcelink-proof.json).
