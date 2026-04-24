#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import sys
import unittest
from pathlib import Path
from unittest import mock
import uuid


REPO_ROOT = Path(__file__).resolve().parents[3]
SC_DIR = REPO_ROOT / "scripts" / "sc"
if str(SC_DIR) not in sys.path:
    sys.path.insert(0, str(SC_DIR))

import _llm_review_engine as engine  # noqa: E402


class LlmReviewEngineStaleOutputGuardTests(unittest.TestCase):
    def _args(self) -> argparse.Namespace:
        return argparse.Namespace(
            self_check=False,
            dry_run_plan=False,
            task_id=None,
            auto_commit=False,
            commit=None,
            model_reasoning_effort="low",
            claude_agents_root="",
            security_profile="host-safe",
            agents="code-reviewer",
            agent_timeouts="",
            timeout_sec=30,
            agent_timeout_sec=10,
            llm_backend="codex-cli",
            prompts_only=False,
            strict=False,
            diff_mode="summary",
            base="origin/main",
            uncommitted=True,
            review_profile="default",
            review_template="",
            no_acceptance_semantic=True,
            semantic_gate="warn",
            prompt_max_chars=32000,
            prompt_budget_gate="warn",
            threat_model="singleplayer",
            skip_agent_prompts=True,
            delivery_profile="fast-ship",
        )

    def test_should_not_consume_stale_output_when_exec_fails(self) -> None:
        out_dir = REPO_ROOT / "logs" / "ci" / "test-tmp" / f"llm-review-stale-output-{uuid.uuid4().hex}"
        out_dir.mkdir(parents=True, exist_ok=True)
        stale_text = "Verdict: Needs Fix\n"
        try:
            def fake_run_codex_exec(**kwargs):  # type: ignore[no-untyped-def]
                output_path = Path(kwargs["output_last_message"])
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_text(stale_text, encoding="utf-8")
                return 1, "Error: launcher failure", ["codex", "exec"]

            with (
                mock.patch.object(engine, "apply_delivery_profile_defaults", side_effect=lambda value: value),
                mock.patch.object(engine, "build_parser") as parser_mock,
                mock.patch.object(engine, "validate_args", return_value=[]),
                mock.patch.object(engine, "ci_dir", return_value=out_dir),
                mock.patch.object(engine, "resolve_agents", return_value=["code-reviewer"]),
                mock.patch.object(engine, "parse_agent_timeout_overrides", return_value={}),
                mock.patch.object(engine, "build_diff_context", return_value="diff"),
                mock.patch.object(engine, "build_threat_model_context", return_value=""),
                mock.patch.object(engine, "build_security_profile_context", return_value=""),
                mock.patch.object(engine, "summary_base", return_value={"status": "ok"}),
                mock.patch.object(engine, "write_json") as write_json_mock,
                mock.patch.object(engine, "write_text"),
                mock.patch.object(engine, "agent_prompt", return_value=("prompt", {"agent_prompt_source": None})),
                mock.patch.object(engine, "apply_prompt_budget", side_effect=lambda prompt, max_chars: (prompt, {"truncated": False})),
                mock.patch.object(engine, "run_codex_exec", side_effect=fake_run_codex_exec),
            ):
                parser_mock.return_value.parse_args.return_value = self._args()
                rc = engine.main()

            self.assertEqual(0, rc)
            payload = write_json_mock.call_args_list[-1].args[1]
            results = payload.get("results") or []
            self.assertEqual(1, len(results))
            first = results[0]
            self.assertEqual("skipped", first.get("status"))
            self.assertEqual(1, int(first.get("rc") or 0))
            details = first.get("details") if isinstance(first.get("details"), dict) else {}
            self.assertIn("verdict", details)
            self.assertIsNone(details.get("verdict"))
        finally:
            if out_dir.exists():
                import shutil
                shutil.rmtree(out_dir, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
