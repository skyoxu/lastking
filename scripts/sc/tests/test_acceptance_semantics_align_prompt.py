#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from __future__ import annotations

import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SC_DIR = REPO_ROOT / "scripts" / "sc"
sys.path.insert(0, str(SC_DIR))

from _acceptance_semantics_align import build_prompt  # noqa: E402


class AcceptanceSemanticsAlignPromptTests(unittest.TestCase):
    def test_build_prompt_should_accept_delivery_profile_context(self) -> None:
        prompt = build_prompt(
            "Task context line",
            "delivery_profile=fast-ship\nsecurity_profile=host-safe",
        )
        self.assertIn("Delivery profile context:", prompt)
        self.assertIn("delivery_profile=fast-ship", prompt)
        self.assertIn("Task context:", prompt)
        self.assertIn("Task context line", prompt)

    def test_build_prompt_should_keep_backward_compat_without_profile_context(self) -> None:
        prompt = build_prompt("Task context line")
        self.assertIn("Task context:", prompt)
        self.assertIn("Task context line", prompt)
        self.assertNotIn("Delivery profile context:", prompt)


if __name__ == "__main__":
    unittest.main()
