import copy
import importlib.util
import json
from pathlib import Path
import unittest

SCRIPT = Path(__file__).parents[1] / "check_distribution_gate.py"
spec = importlib.util.spec_from_file_location("distribution_gate", SCRIPT)
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)


class DistributionGateTests(unittest.TestCase):
    def setUp(self):
        self.operator_manifest = {"dataset": {"rightsReview": {
            "status": "OPERATOR_ATTESTED",
            "externalDistribution": "APPROVED_BY_OPERATOR",
            "evidence": "docs/implementation/external-publication-authorization.md#operator-attestation",
            "operatorAttestation": {
                "attestor": "repository operator",
                "basis": "Operator states they hold the license; no independent review claimed.",
                "statements": ["I have the license; proceed."],
                "scope": "Pinned dataset gzip and expanded JSON SHA-256 values recorded in manifest.",
                "independentLegalReview": False,
            },
        }}}
        self.verified_manifest = {"dataset": {"rightsReview": {
            "status": "VERIFIED", "externalDistribution": "APPROVED",
            "evidence": "rights-review.pdf#section-4",
        }}}

    def test_complete_operator_attestation_route_passes(self):
        self.assertIsNone(gate.refusal(self.operator_manifest, True))

    def test_existing_independent_verified_route_passes(self):
        self.assertIsNone(gate.refusal(self.verified_manifest, True))

    def test_missing_operational_approval_fails_for_both_routes(self):
        for manifest in (self.operator_manifest, self.verified_manifest):
            with self.subTest(status=manifest["dataset"]["rightsReview"]["status"]):
                self.assertIn("not been explicitly approved", gate.refusal(manifest, False))

    def test_unknown_and_inconsistent_routes_fail(self):
        cases = [
            ("UNVERIFIED", "APPROVED"),
            ("UNKNOWN", "APPROVED_BY_OPERATOR"),
            ("VERIFIED", "APPROVED_BY_OPERATOR"),
            ("OPERATOR_ATTESTED", "APPROVED"),
            ("OPERATOR_ATTESTED", "BLOCKED"),
        ]
        for status, distribution in cases:
            manifest = copy.deepcopy(self.operator_manifest)
            review = manifest["dataset"]["rightsReview"]
            review["status"], review["externalDistribution"] = status, distribution
            with self.subTest(status=status, distribution=distribution):
                self.assertIsNotNone(gate.refusal(manifest, True))

    def test_evidence_required_on_both_routes(self):
        for source in (self.operator_manifest, self.verified_manifest):
            for evidence in (None, "", "   ", 42):
                manifest = copy.deepcopy(source)
                manifest["dataset"]["rightsReview"]["evidence"] = evidence
                with self.subTest(route=manifest["dataset"]["rightsReview"]["status"], evidence=evidence):
                    self.assertIsNotNone(gate.refusal(manifest, True))

    def test_operator_attestation_metadata_must_be_complete_and_honest(self):
        fields = ("attestor", "basis", "statements", "scope", "independentLegalReview")
        for field in fields:
            manifest = copy.deepcopy(self.operator_manifest)
            del manifest["dataset"]["rightsReview"]["operatorAttestation"][field]
            with self.subTest(missing=field):
                self.assertIsNotNone(gate.refusal(manifest, True))
        mutations = [
            ("attestor", "  "), ("basis", ""), ("scope", " "),
            ("statements", []), ("statements", ["ok", " "]),
            ("independentLegalReview", True),
        ]
        for field, value in mutations:
            manifest = copy.deepcopy(self.operator_manifest)
            manifest["dataset"]["rightsReview"]["operatorAttestation"][field] = value
            with self.subTest(field=field, value=value):
                self.assertIsNotNone(gate.refusal(manifest, True))

    def test_verified_route_cannot_mix_operator_metadata(self):
        manifest = copy.deepcopy(self.verified_manifest)
        manifest["dataset"]["rightsReview"]["operatorAttestation"] = {"attestor": "operator"}
        self.assertIsNotNone(gate.refusal(manifest, True))

    def test_malformed_or_missing_review_fails_closed(self):
        for manifest in ({}, {"dataset": None}, {"dataset": {"rightsReview": None}},
                         {"dataset": {"rightsReview": "VERIFIED"}}):
            with self.subTest(manifest=manifest):
                self.assertIsNotNone(gate.refusal(manifest, True))


if __name__ == "__main__":
    unittest.main()
