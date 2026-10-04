# Installed NuGet candidate, rollback, and snapshot verification

## Reproducible invocation

Run against an existing final package feed (no source repack workaround). Linux/macOS:

```sh
python3 tools/verification/distribution-checks/run.py \
  --repository . --feed artifacts/packages --candidate-version 0.2.0-beta.1 \
  --feed-manifest artifacts/packages/distribution-manifest.json \
  --expected-source-commit 0123456789abcdef0123456789abcdef01234567 \
  --declared-final-commit 89abcdef0123456789abcdef0123456789abcdef \
  --dependency-feed "$HOME/.nuget/packages" --output artifacts/distribution-checks \
  --evidence-input artifacts/rpablockly/evidence.json \
  --evidence-input artifacts/browser-contracts/result.json \
  --evidence-input artifacts/benchmarks/report.json
```

Windows PowerShell (Python 3.12+):

```powershell
python tools/verification/distribution-checks/run.py `
  --repository . --feed artifacts/packages --candidate-version 0.2.0-beta.1 `
  --feed-manifest artifacts/packages/distribution-manifest.json `
  --expected-source-commit 0123456789abcdef0123456789abcdef01234567 `
  --declared-final-commit 89abcdef0123456789abcdef0123456789abcdef `
  --dependency-feed "$env:USERPROFILE/.nuget/packages" --output artifacts/distribution-checks `
  --evidence-input artifacts/rpablockly/evidence.json `
  --evidence-input artifacts/browser-contracts/result.json `
  --evidence-input artifacts/benchmarks/report.json
```

The same command works from PowerShell 5.1 (use backticks instead of `\\` for line continuation). Replace the example commits with exact immutable provenance values from the supplied feed. Its existing JSON manifest must contain exactly `candidateVersion`, `sourceCommit`, `declaredFinalCommit`, and `packages` (a filename-to-SHA256 map); all candidate `.nupkg` files and hashes must match exactly. The verifier never repacks in `--feed` consumer mode. Python 3.12+, .NET 8 SDK, the final local NuGet feed, and an installed Playwright-compatible Chrome/Chromium are required. The regular Microsoft.Playwright driver/browser cache is expected; a Cursory-specific helper or generation process is not. Set `SPYBROWSER_BROWSER_EXECUTABLE` if browser discovery is not automatic. The local source configuration disallows NuGet.org during consumer restore. Candidate/previous artifacts and Microsoft.Playwright plus its pinned transitive NuGet packages must be available in the supplied local feed(s); pass `--dependency-feed PATH` for each additional offline feed (the hierarchical `$HOME/.nuget/packages` cache is usable as a local source while `NUGET_PACKAGES` is redirected to the clean temp cache). No NuGet.org source is added.

`--repository` builds the previous Core/Playwright packages from pinned baseline `e217359d19a29635f2b3b5ba54664d299fd16d36` using a version different from the candidate. Alternatively supply `--previous-feed` and `--previous-version`; in this mode provenance is marked PENDING unless separately proven. This is a package install test, not a project-reference test. Consumer source/CWD, outputs and clean NuGet cache are all in a temporary directory outside the workspace.

The same generated identity manifest and profile directory are reused across candidate Cursory, Bézier, `Humanize=false`, and previous-package phases. The harness verifies the browser action count, storage-state cookie rehydration through the installed Playwright API, unchanged identity manifest, and storage-state checksum on rollback. It closes every context before changing package versions. It uses neither the Cloak compatibility package nor RpaBlockly's graph.

## Evidence interpretation and snapshot gate

Review `distribution-evidence.json`, including every status and nested command output. PASS means only that the stated local check ran. PENDING is not a pass; technical results are separately summarized by `technicalAllPassed`, `technicalFailures`, and `technicalPending`. `allPassed` and `externalPublicationAllowed` remain false while the external rights review is blocked. That expected fail-closed distribution guard is not a technical PASS or legal clearance. Evidence input paths associate existing RpaBlockly/browser-contract/parity/benchmark proof; path inclusion does not claim those proofs passed. The manifest hashes all local feed nupkg/nuspecs, runtime DLLs, source/license/notice entries and symbol packages.

Snapshot API discovery is runtime reflection against the installed SpyBrowser.Playwright assembly. If `DiagnosticSnapshotStore` is absent, report PENDING; if present, missing or mismatched public methods fail the run. The installed test exercises Save → explicit protected baseline → Read/Compare, version/GPU/context changes, two separate store instances, concurrent saves, child-process interruption during active filesystem writes, cancellation cleanup, unknown schema rejection, Linux permission denial where the OS enforces it, secret-sentinel sanitization, and safe discard. No public SDK test hook is required. Root Linux hosts that bypass chmod are explicitly PENDING rather than faked. Snapshot API privacy producer defects are tracked separately; the verifier does not patch product source. Verify snapshots are optional and profile, manifest, cookies and storage state survive discard and rollback. Version/GPU differences are informational and never auto-promote a baseline.

The consumer checks the actual supplied package only; its preliminary status is not evidence for a later final artifact. External dataset redistribution and independent rights review remain BLOCKED until explicit clearance; this expected distribution guard does not excuse technical pending/failures. No external publication or push is performed here.

## Operational use

Keep snapshot persistence disabled unless explicitly opted in by the application. Store snapshots separately from identities/profiles with restrictive owner-only permissions and short retention; export only when the SDK offers an explicit sanitized export API. Select baselines explicitly—never promote the latest snapshot automatically. To discard the feature, turn persistence off and remove only the dedicated snapshot directory; do not delete the identity root, profile, cookies, or storage state. Compare version/GPU updates as contextual changes, not as automatic incompatibility decisions. If the snapshot schema is unknown, reject it clearly and leave the baseline and profile untouched.
