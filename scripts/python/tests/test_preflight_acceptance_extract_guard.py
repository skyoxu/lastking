#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PYTHON_DIR = REPO_ROOT / "scripts" / "python"
if str(PYTHON_DIR) not in sys.path:
    sys.path.insert(0, str(PYTHON_DIR))


def _load_module(name: str, relative_path: str):
    path = REPO_ROOT / relative_path
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise AssertionError(f"failed to load module: {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


class PreflightAcceptanceExtractGuardTests(unittest.TestCase):
    def _write_triplet(self, root: Path, *, acceptance: list[str], test_refs: list[str]) -> None:
        tasks_dir = root / ".taskmaster" / "tasks"
        tasks_dir.mkdir(parents=True)
        (tasks_dir / "tasks.json").write_text(
            json.dumps(
                {
                    "master": {
                        "tasks": [
                            {
                                "id": 41,
                                "title": "Wire UI: MainMenu And Boot Flow",
                                "status": "pending",
                                "details": "Empty state: visible\nFailure state: visible\nCompletion result: visible",
                            }
                        ]
                    }
                },
                ensure_ascii=False,
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )
        view = [
            {
                "id": "GM-0141",
                "taskmaster_id": 41,
                "title": "Wire UI: MainMenu And Boot Flow",
                "status": "pending",
                "test_refs": test_refs,
                "acceptance": acceptance,
            }
        ]
        (tasks_dir / "tasks_gameplay.json").write_text(json.dumps(view, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (tasks_dir / "tasks_back.json").write_text(json.dumps(view, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    def test_guard_should_pass_when_acceptance_refs_and_states_are_present(self) -> None:
        module = _load_module("preflight_guard_module", "scripts/python/preflight_acceptance_extract_guard.py")
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            self._write_triplet(
                root,
                acceptance=[
                    "MainMenu exposes empty, failure, and completion states. Refs: Tests.Godot/tests/UI/test_main_menu.gd"
                ],
                test_refs=["Tests.Godot/tests/UI/test_main_menu.gd"],
            )
            rc, payload = module.validate(repo_root=root, task_id=41)

        self.assertEqual(0, rc)
        self.assertEqual("ok", payload["status"])
        self.assertEqual([], payload["missing"])

    def test_guard_should_fail_when_refs_are_missing(self) -> None:
        module = _load_module("preflight_guard_module_missing_refs", "scripts/python/preflight_acceptance_extract_guard.py")
        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            self._write_triplet(root, acceptance=["MainMenu exposes empty, failure, and completion states."], test_refs=[])
            rc, payload = module.validate(repo_root=root, task_id=41)

        self.assertEqual(1, rc)
        self.assertEqual("fail", payload["status"])
        self.assertIn("acceptance_refs", payload["missing"])
        self.assertIn("test_refs", payload["missing"])


if __name__ == "__main__":
    unittest.main()
