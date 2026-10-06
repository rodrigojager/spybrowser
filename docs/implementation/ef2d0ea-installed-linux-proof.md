# Installed non-root Linux proof — ef2d0ea

## Provenance and runtime

Consumed the producer-owned, byte-exact feed at
`/mnt/d/Temp/spybrowser-feed-final-ef2d0ea`; no packages were rebuilt or
repacked. Its manifest SHA256 is
`689d8073ab245ae88eb71438f88fbbc7f2b133ad66eb1a39f1361835a8af336c`.
The manifest declares candidate `0.2.0-beta.2.final.ef2d0ea`, source and
final commit `ef2d0eaa35a5bf1950acf75a342590b95e15c9e0`, and `isFinal: true`.
All four packages and four symbol packages match their declared hashes; the
before/after candidate package SHA256 inventories are identical. The declared
source archive hash `2022745ab0934089916ee2ccea335b9de3d3104275fdb8655f86b397c3d63119`
was independently reproduced from the full source commit with Git archive
CRLF settings (`core.autocrlf=true`, `core.eol=crlf`). SourceLink remote access
was not verified, and external publication is prohibited.

The installed consumer ran outside the repository in WSL `SpyBrowser-Ubuntu24`
as `spyreview` UID 1001 on Ubuntu 24.04, using SDK 8.0.319, Playwright 1.61.0,
and cached Google Chrome for Testing 153.0.8010.12. Its working directory is
`/home/spyreview/installed-ef2d0ea-work`; pinned package dependencies came
from `/home/spyreview/.nuget/packages` and browser cache from
`/home/spyreview/.cache/ms-playwright`. The pinned previous feed passed
independent provenance checks against a fresh archive of
`e217359d19a29635f2b3b5ba54664d299fd16d36` (archive SHA256
`91f7807b1c998d556479e8456d0c256f5c7cd6659ba96e56e148a666aa0ce52f`).

## Results

`artifacts/goal/installed-ef2d0ea-linux-proof/distribution-evidence.json`
contains full command and consumer evidence. All 13 technical statuses PASS,
with zero technical failures or pending checks:

1. Previous feed provenance.
2. Offline installed CLI package/version.
3. Installed Cursory mode.
4. Installed Bezier mode.
5. Installed off mode.
6. Snapshot save, explicit baseline compare, concurrency, schema/secret
   handling, and discard lifecycle.
7. Actual OS termination during an active SDK write; baseline preserved.
8. Actual non-root Linux permission denial.
9. GPU/context information is informational.
10. Rollback to the previous Bezier package.
11. Previous package leaves the newer schema-999 optional snapshot byte-exact
    while preserving identity/profile/storage state.
12. Previous-package off mode.
13. Installed browser executable and official Playwright driver launch.

The sole blocked gate is external dataset redistribution rights (`UNVERIFIED`).
Thus the consumer runner's result is the expected rights-only exit 2;
`technicalAllPassed` is true, but overall `allPassed` and
`externalPublicationAllowed` are false. No baseline promotion or publication
is authorized. The detailed evidence is kept separate from the proof summary
`artifacts/goal/installed-ef2d0ea-linux-proof/proof.json`.

## Reproduction

On `SpyBrowser-Ubuntu24` as `spyreview`, run the command recorded in
`proof.json`. It uses the existing candidate feed and pinned previous feed,
passes `--repository /mnt/d/SpyBrowser` for Git safe-directory/source
verification, runs from the isolated worktree, and is bounded by a 1800-second
Linux timeout. Feed hashes are checked before and after. The tooling scripts
were copied from the unchanged checkout at the exact frozen source commit;
no product source was edited.
