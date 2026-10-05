# Final installed Windows distribution proof — 4d8a18c

**Result:** all 13 technical checks PASS (0 pending, 0 failed). Overall result is intentionally **not passing**: the external dataset distribution/right-clearance check is BLOCKED (1). Runner exit 2 is the expected fail-closed rights result, not technical clearance. No publication or default promotion occurred.

- Final candidate: `0.2.0-beta.2.final.4d8a18c`, source and declared-final commit `4d8a18c5d2c21e9339cdadf4b606fbaaedfaa15b`; `isFinal: true`.
- Producer source archive SHA256 `69f2afbeebfb98a0f6e636ae49dafa4895ab3e5d6d46220a1cb840921da96ce4` matched a fresh CRLF Git archive. Every candidate nupkg and snupkg matched the feed manifest before and after consumption; the manifest SHA256 remained `8144b157fe26a7f000cd4728fbe19a94773fd0d203badfec9be856e0d5833727`. No repack was performed.
- The actual package-only consumer ran on Windows 10.0.19045 x64 in `D:/Temp/spybrowser-installed-final-4d8a18c-windows/.../consumer`, outside the repository, with no project reference. The runner's isolated consumer/profile temp tree was removed afterward. The global NuGet cache was not cleared. Evidence retains only hashes and safe fixture metadata, not cookie values or real user data.
- Verified and recorded the pinned `e217359d19a29635f2b3b5ba54664d299fd16d36` previous-feed provenance, package/nuspec hashes, candidate nuspec and packaged-source hashes, and actual portable Source PDB hashes. SourceLink repository/source-archive provenance is recorded; a SourceLink CDI mapping hash was not established and is not claimed.

## Thirteen technical checks

1. Previous baseline package provenance — PASS.
2. Offline installed CLI package/version — PASS.
3. Candidate Cursory launch — PASS.
4. Candidate Bézier launch — PASS.
5. Candidate `Humanize=false` launch — PASS.
6. Installed snapshot save, explicit baseline/compare, concurrency, schema/secrets, discard and profile/storage preservation — PASS.
7. OS-terminated SDK save during active temp-file write with baseline preserved — PASS.
8. Enforced Windows NTFS ACL denial — PASS.
9. Informational GPU/context change; no automatic baseline promotion — PASS.
10. Previous-package Bézier rollback — PASS.
11. Real previous package preserves/ignores the newer schema-999 snapshot fixture — PASS.
12. Previous-package `Humanize=false` — PASS.
13. Browser executable and official Microsoft.Playwright driver — PASS.

Full command output, statuses, package/nuspec/runtime/source/license/symbol hashes and redacted-safe metadata are in [`artifacts/goal/installed-final-4d8a18c-windows-proof/distribution-evidence.json`](../../artifacts/goal/installed-final-4d8a18c-windows-proof/distribution-evidence.json). Machine-readable summary and Source PDB hashes are in `final-installed-windows-proof.json`. This proof is strictly for the installed final feed, not the unchanged main runtime, and does not grant legal clearance.
