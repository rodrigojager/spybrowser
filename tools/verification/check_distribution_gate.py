#!/usr/bin/env python3
"""Fail closed before external publication; this check is not legal clearance."""
import argparse
import json
import os
from pathlib import Path


def refusal(manifest: dict, approved: bool) -> str | None:
    if not approved:
        return "External publication has not been explicitly approved."
    review = manifest.get("dataset", {}).get("rightsReview", {})
    if review.get("status") != "VERIFIED" or review.get("externalDistribution") != "APPROVED":
        return "Dataset redistribution remains blocked: independent rights review is unverified."
    if not isinstance(review.get("evidence"), str) or not review["evidence"].strip():
        return "Independent rights review must provide an auditable evidence reference."
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=Path(__file__).resolve().parents[2] / "src/SpyBrowser.Cursory/Data/upstream-manifest.json")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    reason = refusal(manifest, os.environ.get("SPYBROWSER_EXTERNAL_PUBLICATION_APPROVED") == "true")
    if reason:
        print("BLOCKED: " + reason)
        return 2
    print("Publication prerequisites recorded; this tool does not supply legal clearance.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
