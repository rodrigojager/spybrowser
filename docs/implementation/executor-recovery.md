# Executor recovery — 2026-10-05

## Incident and scope

Executor #35 (`04bb370d-2d46-4b92-8dbe-f1a4e0390285`, PID 112468) remained alive but blocked on `wsl.exe --terminate SpyBrowser-Ubuntu24`. Its descendant command had been blocked for over two hours; the approximately fifteen-hour UI elapsed time did not represent productive work. A five-second process sample showed no CPU change. No current-cut Linux proof had been produced.

The blocked command also used PowerShell syntax directly in Bash and lacked an effective bounded client cleanup. Old project-owned `id` and `echo alive` WSL probes remained orphaned. These were project-specific infrastructure faults, not product acceptance failures.

## Actions and direct verification

1. Saved process identities and command lines in `artifacts/goal/executor-recovery/processes-before.json`.
2. Terminated only executor #35's verified process tree and two verified project-owned orphan probe branches. The parent received the expected failed-subagent notification. `termination.log` and `processes-after.json` preserve the cleanup result; other Pi/Codex projects were not terminated.
3. Bounded `wsl --list --verbose` (12 s) and a distro-scoped echo (15 s) still timed out; only their newly created client trees were terminated. See `bounded-probes.json`.
4. Detected that WslService was running but unresponsive. The unelevated session could not inspect HCS VMs. Ubuntu and Docker shared the subsystem, so no global recovery was attempted without a decision.
5. The user explicitly authorized global WSL restart, including Ubuntu/Docker interruption. Elevated recovery operated on **WslService only**, with bounded status waits and verified service-process identity if forced cleanup was needed. It did not restart hns/vmcompute or delete/unregister any distro.
6. Independently verified recovery: `wsl --list --verbose` returned in **0.13 s**; `SpyBrowser-Ubuntu24` launched under non-root `spyreview` and completed echo/id/SDK/disk/date checks in **7.31 s**. See `post-restart-probes.json` and `wsl-admin-recovery.log`.

Ubuntu and Docker were stopped after recovery; they were not automatically started.

## Resumption

- Executors #37–39 completed bounded criterion audits, writing separate reports rather than competing for the canonical ledger.
- Executor #40 resumed current-cut Linux homologation, with command deadlines and owned-client cleanup. A downloaded, isolated SDK and successful source build are retained. Initial required and reviewed runs passed 37 Cursory tests and 176 of 177 Playwright cases; one explicitly gated headed probe was not executed in those runs. This is **not** yet a zero-skip final Linux gate.
- Independent review #43 approved 57 proposed criterion promotions; #44 applied them without changing immutable requirement fields. The parent reopened 13.11 after the focused input review identified an unproven RpaBlockly adapter duplicate-event assertion. Current consumer-event proof is assigned separately, not inferred from aggregate success.
- Package feed remains preliminary/source-bound to `5fa99de`; subsequent HEAD `d574f6b` changes only test fixture boundaries. No repacking, publication, legal clearance, default promotion or final acceptance occurred during recovery.

New tasks use explicit tool deadlines, retained start/end/error evidence and smaller ownership boundaries. A running UI card is never treated as proof of productive work or successful acceptance.
