# Final installed non-root Linux proof — 4d8a18c

## Provenance and execution

Consumed the producer's existing byte-exact feed at
`/mnt/d/Temp/spybrowser-feed-final-4d8a18c`; no package was rebuilt or
repacked. The feed manifest validates `isFinal: true`, candidate version
`0.2.0-beta.2.final.4d8a18c`, and both declared-final/source commit
`4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`. All four package hashes match
the manifest; the before/after package SHA256 inventories are identical.
`docs/implementation/final-installed-linux-proof.json` records every package
and symbol-package hash.

The isolated consumer ran in WSL `SpyBrowser-Ubuntu24` as `spyreview` UID
1001, with .NET SDK 8.0.319 and Playwright 1.61.0. Its worktree, NuGet cache,
and copied verification harness are under
`/home/spyreview/installed-final-4d8a18c-work`; the installed consumer ran
outside the product workspace with a separate NuGet cache. Pinned Playwright
dependencies came from `/home/spyreview/.nuget/packages`. The pinned previous
feed passed provenance verification against a fresh Linux git archive of
`e217359d19a29635f2b3b5ba54664d299fd16d36` (archive SHA256
`91f7807b1c998d556479e8456d0c256f5c7cd6659ba96e56e148a666aa0ce52f`). The
mounted repository was used only for that baseline/source verification.

## Results

`artifacts/goal/installed-final-4d8a18c-linux-proof/distribution-evidence.json`
is the detailed consumer evidence. All 13 technical consumer checks PASS,
including installed CLI/version, Cursory/Bezier/off modes, snapshot save and
compare, concurrency/schema/secrets/discard, OS crash during async write,
real non-root permission denial, GPU/context information, rollback behavior,
and official browser/driver launch. There are no consumer technical failures
or pending checks.

The dataset redistribution/license gate is BLOCKED because clearance is not
accepted. Therefore `allPassed` is false and `externalPublicationAllowed` is
false; the consumer process exits 2 for this expected rights block. Rights are
not relaxed or assumed cleared.

**Separate preserved producer-side issue:** the feed's existing
`package-audit.log` reports a packaged-versus-rebuilt Cursory DLL SHA256
mismatch (packaged `3a93b353…f69c978`, rebuilt `fcde380f…c1c5a3ad`). This
independent producer audit failure is recorded in the JSON and was not
suppressed, recast as a consumer pass, or used to change the feed.

## Reproduction

Run `artifacts/goal/installed-final-4d8a18c-linux-proof/run-distribution.sh`
through the existing `SpyBrowser-Ubuntu24` WSL distro as `spyreview`. It
bounds the Linux command to 1800 seconds, uses only the pinned local feeds,
and compares candidate package hashes before and after. Exit 2 is expected
while the rights gate remains blocked.
