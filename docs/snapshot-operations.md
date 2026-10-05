# Optional local snapshots: operator manual

Snapshots are diagnostics, not identity backups or a compatibility lock. Persistence is off unless explicitly requested. See [diagnostics](diagnostics.md) for the field allowlist and [installed checks](installed-distribution-checks.md) for failure/rollback contracts. Final installed-candidate acceptance is recorded separately in the [release checklist](implementation/final-release-checklist.md); examples alone are not proof of that acceptance.

## Activate, retain, and compare

Use a dedicated, owner-private directory **outside the identity/profile root**. On Unix, saved files use `0600`; on Windows, they inherit the directory ACL. Restrict directory access before use. Xvfb is a display, not hardware-GPU evidence.

```text
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --snapshot-retention 20
spybrowser probe my-identity --headless --snapshot-dir ./diagnostics --snapshot-retention 20 --baseline ./accepted/snapshot.json
```

Select the baseline file explicitly. Each run saves a different current snapshot; no automatic promotion occurs. Retention applies to generated `snapshot-*.json` files, keeps the configured newest count, and protects the selected baseline. Keep accepted baselines outside the pruning directory when practical. Version/GPU differences are contextual information, not an instruction to replace the baseline or block the browser.

Applications opt in by constructing `DiagnosticSnapshotStore` and invoking `SaveAsync`; use `ReadAsync` and `Compare` for explicit comparison. Snapshot schema, identity schema, and Cursory dataset revision are independent.

## Failure and rollback

Permission denial must not replace a valid baseline. Concurrent processes use distinct files and coordinate publish/prune. A managed cancellation removes its temporary write; an OS-killed writer cannot run `finally` and may leave a temporary file, but the selected baseline must remain byte-identical. Do not interpret that as automatic startup cleanup.

Corrupt/unknown-schema snapshots are rejected clearly and not rewritten. Do not change the schema by hand. The pinned pre-snapshot package has no reader: rollback opens the same identity/profile/storage state and leaves optional newer snapshots untouched. This is an ignore contract, not an assertion that the old package parses the new schema.

## Disable, discard, and export

Omit `--snapshot-dir` (and `--baseline`) or stop calling the store to disable persistence. Delete only the dedicated snapshot directory after confirming its path. Never delete identity manifests, profiles, cookies, or storage-state files to discard diagnostics.

There is no dedicated snapshot-export API in this candidate. If operationally copying a sanitized JSON snapshot, first verify the SDK's allowlisted fields and restrictive destination permissions; do not export raw probe errors, full renderer strings/hardware hashes, identity/profile data, cookies, tokens, passwords, selectors, or URLs. A snapshot is not a telemetry upload request. No external collection service or profile migration is part of this feature.
