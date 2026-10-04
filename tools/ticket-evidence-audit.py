#!/usr/bin/env python3
"""Bookkeeping for the 24 accepted slices; this is not a substitute for testing.

Initialize once, then record observed evidence manually for each criterion.
Validation checks completeness and references, not whether a test proves a claim.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TICKETS = ROOT / ".scratch/spybrowser-cursory-nativo/issues"
AUDIT = ROOT / "docs/implementation/ticket-evidence.json"


def contract() -> list[dict]:
    result = []
    for path in sorted(TICKETS.glob("[0-9][0-9]-*.md")):
        text = path.read_text(encoding="utf-8")
        heading = re.search(r"^# (\d+): (.+)$", text, re.M)
        if heading is None:
            raise ValueError(f"Missing ticket heading: {path}")
        blocked = re.search(r"^\*\*Blocked by:\*\* (.+)$", text, re.M)
        if blocked is None:
            raise ValueError(f"Missing dependencies: {path}")
        criteria = re.findall(r"^- \[[ x]\] (.+)$", text, re.M)
        result.append({
            "id": int(heading[1]), "title": heading[2],
            "ticket": path.relative_to(ROOT).as_posix(),
            "blockedBy": [int(n) for n in re.findall(r"(\d{2}) —", blocked[1])],
            "criteria": [
                {"id": i + 1, "requirement": value, "status": "pending", "evidence": [], "notes": ""}
                for i, value in enumerate(criteria)
            ],
        })
    if [ticket["id"] for ticket in result] != list(range(1, 25)):
        raise ValueError("Expected exactly tickets 01–24")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("init", "status", "verify"))
    args = parser.parse_args()
    accepted = contract()
    if args.action == "init":
        if AUDIT.exists():
            raise SystemExit("Refusing to overwrite existing acceptance evidence")
        AUDIT.parent.mkdir(parents=True, exist_ok=True)
        AUDIT.write_text(json.dumps({"schemaVersion": 1, "tickets": accepted}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Initialized {len(accepted)} tickets and {sum(len(t['criteria']) for t in accepted)} pending criteria")
        return
    audit = json.loads(AUDIT.read_text(encoding="utf-8"))
    if audit.get("schemaVersion") != 1 or len(audit.get("tickets", [])) != 24:
        raise SystemExit("Invalid audit schema or ticket count")
    failures, verified = [], 0
    for expected, actual in zip(accepted, audit["tickets"]):
        for key in ("id", "title", "ticket", "blockedBy"):
            if actual.get(key) != expected[key]:
                failures.append(f"Ticket {expected['id']:02}: contract changed: {key}")
        if len(actual.get("criteria", [])) != len(expected["criteria"]):
            failures.append(f"Ticket {expected['id']:02}: criterion count mismatch")
            continue
        count = 0
        for criterion, reference in zip(actual["criteria"], expected["criteria"]):
            label = f"{expected['id']:02}.{reference['id']:02}"
            if criterion.get("id") != reference["id"] or criterion.get("requirement") != reference["requirement"]:
                failures.append(f"{label}: requirement changed")
            status = criterion.get("status")
            if status not in ("pending", "verified", "blocked"):
                failures.append(f"{label}: invalid status")
            if status == "verified":
                count += 1
                if not criterion.get("evidence") or not criterion.get("notes", "").strip():
                    failures.append(f"{label}: verification needs evidence and reasoning")
                for evidence in criterion.get("evidence", []):
                    location = evidence.get("path", "") if isinstance(evidence, dict) else ""
                    path = Path(location)
                    if not location or not ((ROOT / path) if not path.is_absolute() else path).exists():
                        failures.append(f"{label}: missing evidence artifact {location!r}")
            elif args.action == "verify":
                failures.append(f"{label}: {status}")
        verified += count
        print(f"{expected['id']:02}: {count}/{len(expected['criteria'])} verified — {expected['title']}")
    print(f"Total: {verified}/{sum(len(t['criteria']) for t in accepted)} verified")
    if failures:
        print("\n".join(failures))
        raise SystemExit(1)
    if args.action == "verify":
        print("Bookkeeping complete. Independently audit evidence scope before completing the goal.")


if __name__ == "__main__":
    main()
