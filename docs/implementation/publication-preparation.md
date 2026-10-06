# Publication preparation — beta.2

**State: prepared, not published.** This preparation targets tag `v0.2.0-beta.2`, package version `0.2.0-beta.2`, and global .NET SDK `8.0.319`. The release workflow includes all five package projects, including Cursory and Compatibility.CloakBrowser, and retains normal SourceLink, full-source identity and symbol/source payload behavior.

The source manifest records the `OPERATOR_ATTESTED` / `APPROVED_BY_OPERATOR` route and links to [the operator authorization record](external-publication-authorization.md#operator-attestation). Scope is the pinned `src/trajectories.json.gz` dataset with gzip SHA-256 `1bf3af168719a580e2c5d6fb439f894f9bfff72fc90147149cc531dee80b6203` and expanded JSON SHA-256 `84468e214bf23c5ec422d566f5d3412a532b95feafd584bc0d54ade33035d9b8`. The operator attestation is not independent legal review and does not invent a license identifier or substitute for a document review. Existing upstream attribution, LGPL license/NOTICE, and corresponding-source obligations remain intact.

The gate requires both an allowed evidence route and the exact environment value `SPYBROWSER_EXTERNAL_PUBLICATION_APPROVED=true`. The local candidate-feed builder remains local, emits `externalPublicationAllowed: false`, and reports the source manifest's rights status. Global default promotion remains unauthorized.

No release variable, secret, tag, package, push, or publication was created or performed. Parent review and operational approval remain required; parent must inspect whether a NuGet API key exists by name only and conduct any later release steps. See the adjacent machine-readable [`publication-preparation.json`](publication-preparation.json).
