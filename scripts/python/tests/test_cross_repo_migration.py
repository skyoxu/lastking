"""Regression tests for cross-repository migration reconciliation."""
import copy
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_cross_repo_migration as migration


class CrossRepoMigrationReconciliationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for rel, text in [
            ("shared.py", "print('shared')\n"),
            ("adapted.py", "print('adapted')\n"),
            ("validation.py", "print('validation')\n"),
        ]:
            path = self.root / rel
            path.write_text(text, encoding="utf-8")
        self.manifest = {
            "schema_version": migration.SCHEMA_VERSION,
            "source": {
                "repo": "owner/source",
                "pr": 7,
                "merge_commit": "a" * 40,
                "changed_file_count": 3,
                "changed_files": ["shared.py", "source-business.cs", "source-adapted.py"],
            },
            "target": {"repo": "owner/target"},
            "entries": [
                {
                    "source_path": "shared.py",
                    "classification": "copy_exact",
                    "target_paths": ["shared.py"],
                    "source_blob_sha": migration._git_blob_sha(self.root / "shared.py"),
                    "rationale": "shared control plane",
                },
                {
                    "source_path": "source-business.cs",
                    "classification": "business_only_drop",
                    "rationale": "source-only business behavior",
                },
                {
                    "source_path": "source-adapted.py",
                    "classification": "adapt_target_native",
                    "target_paths": ["adapted.py"],
                    "validation_paths": ["validation.py"],
                    "rationale": "target-native equivalent",
                },
            ],
        }

    def test_valid_manifest_accepts_exact_adapted_and_dropped_files(self):
        self.assertEqual([], migration.validate_manifest(self.manifest, self.root))

    def test_unclassified_source_file_is_rejected(self):
        doc = copy.deepcopy(self.manifest)
        doc["entries"].pop()
        errors = migration.validate_manifest(doc, self.root)
        self.assertTrue(any("unclassified source files" in item for item in errors))
        self.assertTrue(any("entry count" in item for item in errors))

    def test_copy_exact_drift_is_rejected(self):
        (self.root / "shared.py").write_text("print('drift')\n", encoding="utf-8")
        errors = migration.validate_manifest(self.manifest, self.root)
        self.assertTrue(any("copy_exact drift" in item for item in errors))

    def test_adapted_entry_requires_validation_and_drop_cannot_target(self):
        doc = copy.deepcopy(self.manifest)
        doc["entries"][1]["target_paths"] = ["adapted.py"]
        doc["entries"][2]["validation_paths"] = []
        errors = migration.validate_manifest(doc, self.root)
        self.assertTrue(any("business_only_drop must not declare target_paths" in item for item in errors))
        self.assertTrue(any("adapt_target_native requires validation_paths" in item for item in errors))


if __name__ == "__main__":
    unittest.main()
