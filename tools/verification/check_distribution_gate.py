#!/usr/bin/env python3
"""Fail closed before external publication; this check is not legal clearance."""
import argparse
import json
import os
from pathlib import Path


def refusal(manifest: dict, approved: bool) -> str | None:
    if not approved:
        return "External publication has not been explicitly approved."
    dataset = manifest.get("dataset")
    if not isinstance(dataset, dict):
        return "Dataset rights review is missing or malformed."
    review = dataset.get("rightsReview", {})
    if not isinstance(review, dict):
        return "Dataset rights review is missing or malformed."
    status = review.get("status")
    distribution = review.get("externalDistribution")
    evidence = review.get("evidence")
    if not isinstance(evidence, str) or not evidence.strip():
        return "Dataset rights review must provide a nonempty auditable evidence reference."
    if status == "VERIFIED" and distribution == "APPROVED":
        if "operatorAttestation" in review:
            return "Independent VERIFIED approval must not be mixed with an operator-attested route."
        return None
    if status == "OPERATOR_ATTESTED" and distribution == "APPROVED_BY_OPERATOR":
        attestation = review.get("operatorAttestation")
        required = ("attestor", "basis", "statements", "scope", "independentLegalReview")
        if not isinstance(attestation, dict) or any(key not in attestation for key in required):
            return "Operator-attested approval requires complete attestation metadata."
        if not isinstance(attestation["attestor"], str) or not attestation["attestor"].strip():
            return "Operator attestation must identify the attestor."
        if not isinstance(attestation["basis"], str) or not attestation["basis"].strip():
            return "Operator attestation must state its basis."
        if not isinstance(attestation["scope"], str) or not attestation["scope"].strip():
            return "Operator attestation must specify its scope."
        statements = attestation["statements"]
        if not isinstance(statements, list) or not statements or any(
                not isinstance(item, str) or not item.strip() for item in statements):
            return "Operator attestation must include nonempty statements."
        if attestation["independentLegalReview"] is not False:
            return "Operator attestation must not claim independent legal review."
        return None
    return "Dataset rights approval route is unknown or internally inconsistent."


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=Path(__file__).resolve().parents[2] / "src/SpyBrowser.Cursory/Data/upstream-manifest.json")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    reason = refusal(manifest, os.environ.get("SPYBROWSER_EXTERNAL_PUBLICATION_APPROVED") == "true")
    if reason:
        print("BLOCKED: " + reason)
        return 2
    print("Publication prerequisites recorded; operator attestation is not independent legal clearance and this tool does not supply legal clearance.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
