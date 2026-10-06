# ef2d0ea local final-feed and package proof

**Result:** the accepted immutable source `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0` produced a new local final-verification feed. Actual nuspecs and symbols were audited; Cursory's packaged corresponding source was inspected, rebuilt under its packaged SDK pin, and compared to the packaged assembly. This is technical local verification only—not publication, rights clearance, or release acceptance.

## Producer feed

Official command: `python tools/verification/build_candidate_feed.py --repository D:/SpyBrowser --source-commit ef2d0eaa35a5bf1950acf75a342590b95e15c9e0 --version 0.2.0-beta.2.final.ef2d0ea --output D:/Temp/spybrowser-feed-final-ef2d0ea --final`.

- Source archive SHA-256: `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`; SDK 8.0.319.
- Producer manifest SHA-256: `689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`.
- Package nupkg hashes: CLI `d0ee0ae2d6fe850c12f3bdc3aacdb4c34c0f951324ad6a4b41a45bfbabed0790`; Core `7c3f64b2fb65cc4c706ce2f4d2b39f553e3f548e210384d4692e51fd6fd1e194`; Cursory `b042e0c8643b00b375bbf20d1d65ee458b990472592f064bb5a741e581ce3f10`; Playwright `17965e54e3828a8d11c8686afafaf768653629ee2f68ea5dbd3e03de9a28363b`.
- Symbol package snupkg hashes: CLI `ac988bcd318c356d6491fb4264c7918c0dcdeffa8b94e43052b0eba76b8ceffc`; Core `99c6266757d555556667a3cf7e3f93d934f658950119234f4d6e7ef075466277`; Cursory `f84bb3f47468addf20b214b8733c4b4d655ef44844bb69552687025d00b021f1`; Playwright `e0de433c945c0af8f5c44b56d998561a8abda3305dbce5453430f9a6cf2a5f73`.
- All four actual nuspec IDs, versions, full repository commit and repository URL passed. Actual PDB audit: 4 PDBs, 71 documents, 58 source-controlled SHA-256 checksums matched, 13 generated documents excluded. SourceLink CDI maps `/_/*` to the raw GitHub URL containing the full frozen SHA. Remote source access was not tested.

## Actual package contents and correspondence

`tools/package-demo/verify_package.py --feed artifacts/goal/package-ef2d0ea-inspection --project D:/SpyBrowser` passed, as did an independent all-package nuspec/runtime inspection (`artifacts/goal/package-ef2d0ea-inspection/package-audit.json`). Cursory contains only its own runtime DLL, no runtime dependencies and no Playwright assembly; its standalone source pin is SDK 8.0.319 with roll-forward disabled. Its LGPL/GPL and third-party license payload, notices, README, build guide, data and source are present; the unrelated repository-wide Playwright notice is absent. The actual packaged project embeds the data resource once. All 11 packaged C# source files match the frozen source archive after CRLF/LF normalization.

The extracted **actual package source tree** built successfully using its `source/global.json` pin. The package assembly and rebuilt assembly are not whole-file byte-identical (SHA-256 `3cf6ecc4552eaba6d53b9be28d86c42b1843585e2134098e5b256d987e236d8f` vs. `fd6b11b11cfffc1003a4cc34b5009bd962d824c2de248faa0bcb1256bc2fab40`). The SDK-8 BCL checker found **0 differing entries across 432 entries / 23 types**, covering metadata/signatures/attributes, constants, generics, parameters, every method body/IL, locals, max-stack/init-locals, exception regions and embedded-resource hashes. No whole-PE identity claim is made. Build log, checker, full comparison and machine-readable details are under `artifacts/goal/final-compiler-correspondence/ef2d0ea/`.

## Consumer-ready projection and boundaries

`artifacts/goal/final-feed-ef2d0ea/` contains exactly byte-verified Core, Cursory and Playwright nupkgs plus the current consumer `manifest.json` and `consumer-ready.json`. It projects the same producer packages; it is not a rebuild. The producer feed at `D:/Temp/spybrowser-feed-final-ef2d0ea` retains all four packages and symbols for consumers needing CLI too.

The producer manifest keeps `externalPublicationAllowed: false` and `datasetRights: UNVERIFIED`. License notices and attribution are package evidence, not a legal opinion or dataset redistribution-rights clearance. No repository source was modified for this proof and no commit was created. The preserved 4d8a18c feed and its historic/preliminary artifacts were not changed or relabeled.
