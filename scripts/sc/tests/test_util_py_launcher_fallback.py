#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from __future__ import annotations

import sys
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
SC_DIR = REPO_ROOT / "scripts" / "sc"
if str(SC_DIR) not in sys.path:
    sys.path.insert(0, str(SC_DIR))

import _util as util  # noqa: E402


class RunCmdPyLauncherFallbackTests(unittest.TestCase):
    def test_run_cmd_should_translate_py_launcher_to_sys_executable(self) -> None:
        cwd = REPO_ROOT
        captured: dict[str, object] = {}

        class FakeProc:
            def __init__(self, cmd: list[str], **kwargs: object) -> None:
                captured["cmd"] = cmd
                captured["kwargs"] = kwargs
                self.returncode = 0

            def communicate(self, timeout: int | None = None) -> tuple[str, str]:
                captured["timeout"] = timeout
                return ("ok\n", "")

        with mock.patch("subprocess.Popen", side_effect=lambda *args, **kwargs: FakeProc(*args, **kwargs)):  # type: ignore[arg-type]
            rc, out = util.run_cmd(["py", "-3", "scripts/python/run_dotnet.py", "--help"], cwd=cwd, timeout_sec=12)

        self.assertEqual(0, rc)
        self.assertEqual("ok\n", out)
        launched = captured.get("cmd")
        self.assertIsInstance(launched, list)
        self.assertGreaterEqual(len(launched), 2)
        self.assertEqual(str(Path(sys.executable).resolve()), launched[0])
        self.assertEqual("scripts/python/run_dotnet.py", launched[1])

    def test_run_cmd_should_keep_non_py_launcher_command_unchanged(self) -> None:
        cwd = REPO_ROOT
        captured: dict[str, object] = {}

        class FakeProc:
            def __init__(self, cmd: list[str], **kwargs: object) -> None:
                captured["cmd"] = cmd
                self.returncode = 0

            def communicate(self, timeout: int | None = None) -> tuple[str, str]:
                return ("ok\n", "")

        with mock.patch("subprocess.Popen", side_effect=lambda *args, **kwargs: FakeProc(*args, **kwargs)):  # type: ignore[arg-type]
            rc, _out = util.run_cmd(["git", "status", "--short"], cwd=cwd, timeout_sec=8)

        self.assertEqual(0, rc)
        self.assertEqual(["git", "status", "--short"], captured.get("cmd"))


if __name__ == "__main__":
    unittest.main()
