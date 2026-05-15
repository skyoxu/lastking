#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import sys
import tempfile
import textwrap
import time
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


run_gdunit = _load_module("run_gdunit_module", "scripts/python/run_gdunit.py")


class RunGdUnitTests(unittest.TestCase):
    def test_run_cmd_failfast_should_stop_after_completion_marker_when_child_idles(self) -> None:
        with tempfile.TemporaryDirectory() as tmp_dir:
            script_path = Path(tmp_dir) / "fake_gdunit_hang.py"
            script_path.write_text(
                textwrap.dedent(
                    """
                    import sys
                    import time

                    print("Run Test Suite: res://tests/UI/test_hud_action_icon_availability_and_cooldown_mask.gd", flush=True)
                    print("Overall Summary: 3 test cases | 0 errors | 0 failures | 0 flaky | 0 skipped | 0 orphans |", flush=True)
                    print("Open HTML Report at: file://F:/Lastking/Tests.Godot/reports/report_2/index.html", flush=True)
                    time.sleep(2.0)
                    """
                ),
                encoding="utf-8",
            )

            started = time.perf_counter()
            rc, out = run_gdunit.run_cmd_failfast(
                [sys.executable, str(script_path)],
                cwd=REPO_ROOT,
                timeout=1_500,
            )
            elapsed = time.perf_counter() - started

        self.assertEqual(0, rc)
        self.assertIn("Overall Summary:", out)
        self.assertLess(elapsed, 1.2)


if __name__ == "__main__":
    unittest.main()
