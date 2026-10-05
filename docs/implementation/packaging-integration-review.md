# Packaging integration findings before the new source cut

The 4d8a18c feed and all rejected/failed evidence remain unchanged. The newly integrated packaging changes are not yet overall release acceptance.

## Source/compiler correspondence

`final-compiler-correspondence.json` resolves the earlier IL discrepancy using the actual compiler provenance. Extracted source without a SDK pin selected SDK 10.0.401/reference pack 8.0.31; the producer used SDK 8.0.319/reference pack 8.0.22. An explicit C#-12 SDK-10 control still differed. The SDK-8 replay matched all 432 compared entries across 23 types, including method bodies, locals, exception regions, metadata and resources. No whole-PE byte-identity claim is made, and the earlier rejected report remains historical evidence.

The corresponding-source packaging fix supplies the exact SDK pin, standalone legal/build files and an independently rebuildable/repackable source tree. Actual final packages must still be generated and checked from the integrated source.

## CI integration correction

The peer project initially duplicated `Microsoft.SourceLink.GitHub` when `CI=true`: the repository already inherits a centrally versioned private build reference. A real bounded restore failed with NU1504; the failure is preserved in `artifacts/goal/corresponding-source-ci-review/ci-before-fix.log`.

The additional explicit-version reference now applies only to standalone CI builds without central package management. A subsequent real `CI=true` restore succeeded, recorded in `ci-after-fix.log`; runtime dependencies and numerical C# source files were not changed. The PowerShell build example also now constructs a not-yet-existing feed directory before resolving it.

## SourceLink URI correction

The first repaired PDB mapping used a GitHub HTML/nonexistent route. CDI presence and equal source checksums did not prove the content URI was valid. A further repair must use `https://raw.githubusercontent.com/rodrigojager/spybrowser/<full-source-sha>/<relative-source-path>` and reject the old route independently of the caller-supplied expected map. No remote accessibility or external publication is asserted; dataset redistribution rights remain unverified.

Before freezing the next candidate, review the integrated producer/tests/project/build guide and preserve all proof scope limits. Build a new local feed from the accepted full source SHA; never relabel 4d8a18c or reuse its old package bytes as the newly corrected final candidate.
