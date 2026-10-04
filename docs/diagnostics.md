# Diagnostics and local snapshots

SpyBrowser diagnostics are advisory unless a finding is an explicit consistency error and the caller keeps `FailOnConsistencyErrors` enabled. Missing surface data is not proof of a contradiction. No GeoIP lookup, third-party evaluation service, telemetry, or additional stealth scripts are used.

## Effective expectations and findings

The validator's original `Validate(identity, policy, diagnostics)` overload remains available and derives expected values from the identity. The launch flow also validates against context values after the caller's final options callback (locale, timezone, viewport, screen, device scale factor and user agent), so intentional overrides are not judged against stale manifest defaults. Context handles expose these values additively as `EffectiveExpectations`; consumers performing a later probe can pass this record to the validator and preserve callback overrides. Browser family is compared at family level, not by literal browser version. Client Hints are only compared when explicitly available; absent/reduced hints are indeterminate. A WebGPU adapter being unavailable is not an inconsistency.

Findings use stable codes and `Information`, `Warning` or `Error` severity. Known UTC names (`UTC`, `Etc/UTC`, `Etc/GMT`, `GMT`) are aliases. Windows/IANA timezone comparison accepts an explicit platform conversion only when the converted IANA identifier matches; different IANA region identifiers are not accepted merely because they share offsets or map to a broad Windows region. Locale comparison uses BCP-47/CultureInfo normalization and compares the primary `navigator.languages` entry; later language preferences may legitimately differ. Screen dimensions are compared in CSS screen units. Viewport and DPR checks run only when those observed values are supplied; DPR differences are warnings because browser zoom can affect the observed value. `NoViewport` or unavailable measurements must remain indeterminate rather than being fabricated as a mismatch.

GPU software/hardware policy remains governed by `GpuPolicy`. Missing WebGL can warn (or error for `RequireHardware`); unavailable WebGPU alone does not. `ExperimentalMask` reports that string masking does not cover pixels, timing, WebGPU or native introspection. A canvas sample hash is an observation only, not certification of equivalent GPU behavior.

The CLI `probe` command runs the existing local Playwright probe. Snapshot persistence is disabled unless `--snapshot-dir` is provided:

```text
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --snapshot-retention 20
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --baseline ./accepted/snapshot.json
```

`--baseline` always names an explicit, read-only file; the command saves the current run as a separate unique snapshot and never promotes it to baseline. `--baseline` requires `--snapshot-dir`. Snapshots have their own schema and contain runtime versions, effective expectations, normalized browser surface fields and findings. They do not include identity IDs, profile contents, pages, cookies, storage state, typed text, proxy settings, or GPU probe error text/canvas hashes. Browser/driver updates are informational changes unless a current consistency rule independently reports a contradiction.

Writes use a same-directory temporary file followed by an atomic rename. On Unix the file mode is owner read/write (`0600`). On Windows, the file inherits the destination directory's ACL; place the directory in a user-private location. Retention applies only to generated `snapshot-*.json` files in the chosen directory and keeps the newest configured count (default 20). The explicitly selected `--baseline` is passed to retention and protected even when it is inside that directory. A per-directory exclusive lock coordinates publish/prune across concurrent processes; temporary names are unique. Corrupt or unsupported-schema files fail with an error and are not repaired or overwritten. Snapshots are optional local artifacts; deleting them does not affect profiles or identities. Concurrent runs receive unique filenames rather than sharing an IdentityId-based path.

API consumers can use `DiagnosticSnapshotStore.SaveAsync`, `ReadAsync`, and `Compare` directly. `RuntimeVersionRecord` accepts caller-supplied version strings. CLI entries use actual loaded assembly and browser version data where available; unavailable family or algorithm/dataset associations are explicitly labeled `unknown`/`not-assessed`, not inferred. A caller must opt into persistence by constructing the store and invoking `SaveAsync`.

## Validation boundaries

The diagnostics/snapshot modules and CLI groundwork are independently testable. End-to-end launch/probe integration still depends on ticket 16's isolated probes; final snapshot acceptance depends on ticket 15's safe humanization diagnostics plus tickets 17/18. Those dependencies are not represented as complete by the groundwork here. A real browser test requires a locally installed Playwright browser; no remote endpoint is used by this feature.
