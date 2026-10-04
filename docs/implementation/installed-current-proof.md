# Installed-current verification at `1e9c3e9`

## Scope and provenance

The installed consumer consumed the existing preliminary feed at
`D:/Temp/spybrowser-feed-review-1e9c3e9`, version `0.1.0-review.1e9c3e9`,
source commit `1e9c3e9f0e2ec8c24960fee47b355b2a05d6fd56`. Its manifest pins that
full commit, declares `isFinal: false` and `declaredFinalCommit: null`, and
pins the exact package hashes. The consumer verified these pins and installed
the supplied `.nupkg` files; the candidate feed was not rebuilt or repacked.
This is preliminary evidence only.

The expected rollback package is the distinct `0.1.0-baseline.e217359`
artifact from `e217359d19a29635f2b3b5ba54664d299fd16d36`. The parent evidence
recorded its package files under a temporary `/tmp/spybrowser-distribution-check-*/previous-feed`.
That temporary feed no longer exists. An exhaustive check of
`D:/Temp` for `SpyBrowser.*.0.1.0-baseline.e217359.nupkg` also found no files.
Do not substitute the available `0.1.0-review.7ae11b5` feed: it is not the
pinned pre-snapshot rollback package. Runs below use it only for candidate
execution/ACL diagnostics, and its schema-999 rollback failure is not a valid
result about the pinned e217 package. No candidate/source repack or baseline
source rebuild was performed.

## Checks performed

The Windows and WSL consumers each restored the actual candidate from the
local feed into an isolated NuGet cache and ran the real package with Chrome
and the official Microsoft.Playwright 1.61.0 driver. On both operating
systems, Cursory, Bézier and `Humanize=false` launches, native click behavior,
identity-manifest stability, snapshot round-trip, explicit-baseline comparison,
version/context/GPU informational changes, two independent concurrent writer
processes, unknown-schema rejection, cancellation cleanup, protected-baseline
hash, and discard without changing identity/profile/storage-state all passed.
The comparison correctly reports `webgl1.renderer-category` as informational;
model/adapter name changes intentionally are not raw-string comparison output.
The SDK `SaveAsync` child was terminated by the OS during a real temporary-file
write on each OS. One orphan temp file remained after each forced termination;
this is reported, not misrepresented as SDK cleanup or recovery. Separately,
cancellation removed its SDK-managed temporary file.

- Linux: genuine non-root `chmod` denial made installed SDK `SaveAsync` fail;
  protected baseline remained unchanged.
- Windows: a temporary directory received an explicit current-user NTFS deny
  ACE (`icacls`, SID from `whoami`); installed SDK `SaveAsync` failed, the
  baseline hash was unchanged, and the deny ACE was removed before directory
  cleanup.
- Package-install browser/driver use passed on both operating systems.
- The candidate `SpyBrowser.Cli` package was installed as a .NET tool from the
  local candidate feed only and `spybrowser version` reported the exact
  candidate version on Windows and Linux. No network package source was used.

## Evidence and gate result

- `artifacts/goal/installed-current-proof/distribution-evidence.json` — WSL run.
- `artifacts/goal/installed-current-proof/windows/distribution-evidence.json`
  — Windows run.
- Each run records 11 PASS, 1 FAIL, 1 PENDING and 1 BLOCKED status. The FAIL is
  the deliberately wrong `7ae11b5` previous-package/schema-999 comparison; the
  PENDING records that caller-supplied feed provenance is not the pinned e217
  baseline; BLOCKED is the external Cursory dataset rights gate.
- Both artifacts retain `isFinal: false`, `technicalAllPassed: false`,
  `allPassed: false`, and `externalPublicationAllowed: false`. These are partial
  diagnostic runs, not final proof or permission to publish.

The accepted rerun is blocked until the actual local
`0.1.0-baseline.e217359` Core and Playwright `.nupkg` files are supplied. Then
rerun the same package-only command on Linux and Windows with that feed and
`--previous-version 0.1.0-baseline.e217359`. Do not use `--declared-final-commit`.
External publication/push stays prohibited until the independent dataset-rights
review clears it.

## Local validation

- `python -m py_compile tools/verification/distribution-checks/run.py` — passed.
- `git diff --check` — passed.
- Windows and Linux actual installed-package consumer runs described above —
  candidate checks passed; pinned e217 rollback remains blocked as described.
