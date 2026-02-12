#!/usr/bin/env python3
"""
Validate lock consistency between PRD machine appendix and config contracts.

Scope (locked checks):
- Boss count policy is config-driven (default 2)
- Difficulty unlock is clear-to-unlock and no cross-tier skip
- Audit writer source is script-only
- Spawn fallback and weight semantics are locked
- Debt cross-zero and unlock threshold are locked
- Clone policy / damage order / reward popup timing are locked
- PRD and LOCKED-SUMMARY lock lines stay aligned

Outputs:
- logs/ci/<YYYY-MM-DD>/config-contract-sync-check.json

Usage (Windows):
  py -3 scripts/python/config_contract_sync_check.py
  py -3 scripts/python/config_contract_sync_check.py --strict
  py -3 scripts/python/config_contract_sync_check.py --out logs/ci/2026-02-10/config-contract-sync-check.json
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import sys
from pathlib import Path
from typing import Any


EXPECTED_VALUES = {
    "prd_unlock_policy": "clear-to-unlock-config-driven-no-cross-tier-skip",
    "prd_difficulty_unlock_policy": "clear-to-unlock-config-driven",
    "prd_boss_count_policy": "config-driven",
    "prd_boss_count_default": 2,
    "prd_audit_source": "script-only",
    "prd_spawn_no_eligible": "discard_tick_budget",
    "prd_night_weight_semantics": "sampling-priority-only-not-channel-budget",
    "difficulty_unlock_policy": "clear-to-unlock-config-driven",
    "audit_writer_source": "script-only",
    "spawn_boss_mode": "config-driven",
    "debt_in_progress_policy": "continue-until-complete",
    "debt_unlock_threshold": "gold-at-least-zero",
    "build_soft_limit_scope": "player-global",
    "reward_popup_timing": "immediately-after-night-settlement",
    "reward_popup_pause": "pause-day-night-timer-until-choice",
    "reward_gold_fallback_scaling": "fixed-600-not-affected-by-difficulty",
    "path_fail_primary": "nearest-blocking-structure-by-path-cost",
    "path_fail_when_no_blocker": "attack-castle",
    "path_fail_gate_policy": "enemy-cannot-pass-alive-gate-can-attack-gate-destroyed-passable",
    "clone_cap_scope": "global",
    "clone_cap_max": 10,
    "clone_kills_counted": True,
    "clone_budget_accounting": "not-counted-as-boss-channel-budget",
    "integer_pipeline": "multiply-then-divide",
    "integer_rounding": "floor",
    "integer_bankers_rounding": False,
    "damage_pipeline_order": [
        "base_damage",
        "offense_defense_modifiers",
        "difficulty_modifiers",
        "armor_reduction",
        "min_damage_clamp_1",
    ],
    "summary_fallback_castle": "fallback target is `castle` (no idle wait)",
    "summary_gate_policy": "enemy cannot pass alive gate, can attack gate body, destroyed gate becomes passable",
    "summary_clone_policy": "Clone policy: `global cap 10`; clone kills counted in report; clones do not consume boss channel budget.",
    "summary_damage_pipeline": "Damage pipeline order: `base_damage -> offense_defense_modifiers -> difficulty_modifiers -> armor_reduction -> min_damage_clamp_1`.",
    "summary_debt_cross_zero": "Debt cross-zero behavior: in-progress spend actions continue to completion; only new spend requests are blocked.",
    "summary_debt_unlock": "Debt unlock threshold: spending unlocks immediately when gold returns to `>=0`.",
    "summary_build_soft_limit": "Build soft limit scope: `player-global 100ms per placement`.",
    "summary_reward_popup": "Reward popup timing: `immediately after night settlement`; day/night timer pauses until selection.",
    "summary_reward_gold_scaling": "Gold fallback scaling: `fixed 600`, not affected by difficulty multipliers.",
}


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def today_str() -> str:
    return dt.date.today().strftime("%Y-%m-%d")


def default_output_path(root: Path) -> Path:
    return root / "logs" / "ci" / today_str() / "config-contract-sync-check.json"


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def parse_machine_appendix_json(prd_path: Path) -> dict[str, Any]:
    """Parse JSON block under '## 16. Machine-Readable Appendix (JSON)'."""

    text = prd_path.read_text(encoding="utf-8")
    heading_pattern = r"^##\s+16\.\s+Machine-Readable Appendix \(JSON\)\s*$"
    heading_match = re.search(heading_pattern, text, flags=re.MULTILINE)
    if not heading_match:
        raise ValueError(f"Cannot find machine appendix heading in {prd_path.as_posix()}")

    remaining = text[heading_match.end():]
    block_match = re.search(r"```json\s*\n(.*?)\n```", remaining, flags=re.DOTALL)
    if not block_match:
        raise ValueError(f"Cannot find JSON code fence under machine appendix in {prd_path.as_posix()}")

    return json.loads(block_match.group(1))


def record_check(results: list[dict[str, Any]], check_id: str, ok: bool, actual: Any, expected: Any) -> None:
    results.append(
        {
            "check_id": check_id,
            "ok": ok,
            "actual": actual,
            "expected": expected,
        }
    )


def run_checks(root: Path, prd_path: Path, summary_path: Path) -> list[dict[str, Any]]:
    difficulty_schema = load_json(root / "Game.Core/Contracts/Config/difficulty-config.schema.json")
    difficulty_sample = load_json(root / "Game.Core/Contracts/Config/difficulty-config.sample.json")
    spawn_schema = load_json(root / "Game.Core/Contracts/Config/spawn-config.schema.json")
    spawn_sample = load_json(root / "Game.Core/Contracts/Config/spawn-config.sample.json")
    audit_schema = load_json(root / "Game.Core/Contracts/Config/config-change-audit.schema.json")
    rules_text = (root / "Game.Core/Contracts/Config/spawn-config.validator.rules.md").read_text(encoding="utf-8")
    summary_text = summary_path.read_text(encoding="utf-8")
    prd_json = parse_machine_appendix_json(prd_path)

    locked = prd_json["locked_constraints"]
    results: list[dict[str, Any]] = []

    record_check(
        results,
        "prd.difficulty.unlock_policy",
        locked["difficulty"].get("unlock_policy") == EXPECTED_VALUES["prd_unlock_policy"],
        locked["difficulty"].get("unlock_policy"),
        EXPECTED_VALUES["prd_unlock_policy"],
    )
    record_check(
        results,
        "prd.difficulty_unlock_policy",
        locked.get("difficulty_unlock_policy") == EXPECTED_VALUES["prd_difficulty_unlock_policy"],
        locked.get("difficulty_unlock_policy"),
        EXPECTED_VALUES["prd_difficulty_unlock_policy"],
    )
    record_check(
        results,
        "prd.difficulty_unlock_cross_tier_skip",
        locked.get("difficulty_unlock_cross_tier_skip") is False,
        locked.get("difficulty_unlock_cross_tier_skip"),
        False,
    )

    record_check(
        results,
        "prd.boss_night.boss_count_policy",
        locked["boss_night"].get("boss_count_policy") == EXPECTED_VALUES["prd_boss_count_policy"],
        locked["boss_night"].get("boss_count_policy"),
        EXPECTED_VALUES["prd_boss_count_policy"],
    )
    record_check(
        results,
        "prd.boss_night.boss_count_default",
        locked["boss_night"].get("boss_count_default") == EXPECTED_VALUES["prd_boss_count_default"],
        locked["boss_night"].get("boss_count_default"),
        EXPECTED_VALUES["prd_boss_count_default"],
    )

    record_check(
        results,
        "prd.config_change_audit_source",
        locked.get("config_change_audit_source") == EXPECTED_VALUES["prd_audit_source"],
        locked.get("config_change_audit_source"),
        EXPECTED_VALUES["prd_audit_source"],
    )

    record_check(
        results,
        "prd.budget_to_spawn.on_no_eligible_candidates",
        locked["budget_to_spawn"].get("on_no_eligible_candidates") == EXPECTED_VALUES["prd_spawn_no_eligible"],
        locked["budget_to_spawn"].get("on_no_eligible_candidates"),
        EXPECTED_VALUES["prd_spawn_no_eligible"],
    )
    record_check(
        results,
        "prd.budget_to_spawn.night_type_weight_semantics",
        locked["budget_to_spawn"].get("night_type_weight_semantics")
        == EXPECTED_VALUES["prd_night_weight_semantics"],
        locked["budget_to_spawn"].get("night_type_weight_semantics"),
        EXPECTED_VALUES["prd_night_weight_semantics"],
    )

    debt_guardrails = locked.get("debt_guardrails", {})
    record_check(
        results,
        "prd.debt_guardrails.in_progress_spend_actions_when_gold_below_zero",
        debt_guardrails.get("in_progress_spend_actions_when_gold_below_zero")
        == EXPECTED_VALUES["debt_in_progress_policy"],
        debt_guardrails.get("in_progress_spend_actions_when_gold_below_zero"),
        EXPECTED_VALUES["debt_in_progress_policy"],
    )
    record_check(
        results,
        "prd.debt_guardrails.unlock_threshold",
        debt_guardrails.get("unlock_threshold") == EXPECTED_VALUES["debt_unlock_threshold"],
        debt_guardrails.get("unlock_threshold"),
        EXPECTED_VALUES["debt_unlock_threshold"],
    )

    record_check(
        results,
        "prd.build_soft_limit_scope",
        locked.get("build_soft_limit_scope") == EXPECTED_VALUES["build_soft_limit_scope"],
        locked.get("build_soft_limit_scope"),
        EXPECTED_VALUES["build_soft_limit_scope"],
    )
    record_check(
        results,
        "prd.reward_popup_timing",
        locked.get("reward_popup_timing") == EXPECTED_VALUES["reward_popup_timing"],
        locked.get("reward_popup_timing"),
        EXPECTED_VALUES["reward_popup_timing"],
    )
    record_check(
        results,
        "prd.reward_popup_pause",
        locked.get("reward_popup_pause") == EXPECTED_VALUES["reward_popup_pause"],
        locked.get("reward_popup_pause"),
        EXPECTED_VALUES["reward_popup_pause"],
    )
    record_check(
        results,
        "prd.reward_gold_fallback_scaling",
        locked.get("reward_gold_fallback_scaling") == EXPECTED_VALUES["reward_gold_fallback_scaling"],
        locked.get("reward_gold_fallback_scaling"),
        EXPECTED_VALUES["reward_gold_fallback_scaling"],
    )

    path_fail_fallback = locked.get("path_fail_fallback", {})
    record_check(
        results,
        "prd.path_fail_fallback.primary",
        path_fail_fallback.get("primary") == EXPECTED_VALUES["path_fail_primary"],
        path_fail_fallback.get("primary"),
        EXPECTED_VALUES["path_fail_primary"],
    )
    record_check(
        results,
        "prd.path_fail_fallback.when_no_blocker",
        path_fail_fallback.get("when_no_blocker") == EXPECTED_VALUES["path_fail_when_no_blocker"],
        path_fail_fallback.get("when_no_blocker"),
        EXPECTED_VALUES["path_fail_when_no_blocker"],
    )
    record_check(
        results,
        "prd.path_fail_fallback.gate_policy",
        path_fail_fallback.get("gate_policy") == EXPECTED_VALUES["path_fail_gate_policy"],
        path_fail_fallback.get("gate_policy"),
        EXPECTED_VALUES["path_fail_gate_policy"],
    )

    clone_policy = locked.get("boss_clone_policy", {})
    record_check(
        results,
        "prd.boss_clone_policy.cap_scope",
        clone_policy.get("cap_scope") == EXPECTED_VALUES["clone_cap_scope"],
        clone_policy.get("cap_scope"),
        EXPECTED_VALUES["clone_cap_scope"],
    )
    record_check(
        results,
        "prd.boss_clone_policy.cap_max",
        clone_policy.get("cap_max") == EXPECTED_VALUES["clone_cap_max"],
        clone_policy.get("cap_max"),
        EXPECTED_VALUES["clone_cap_max"],
    )
    record_check(
        results,
        "prd.boss_clone_policy.kills_counted_in_report",
        clone_policy.get("kills_counted_in_report") == EXPECTED_VALUES["clone_kills_counted"],
        clone_policy.get("kills_counted_in_report"),
        EXPECTED_VALUES["clone_kills_counted"],
    )
    record_check(
        results,
        "prd.boss_clone_policy.boss_budget_accounting",
        clone_policy.get("boss_budget_accounting") == EXPECTED_VALUES["clone_budget_accounting"],
        clone_policy.get("boss_budget_accounting"),
        EXPECTED_VALUES["clone_budget_accounting"],
    )

    rounding_policy = locked.get("integer_rounding_policy", {})
    record_check(
        results,
        "prd.integer_rounding_policy.pipeline",
        rounding_policy.get("pipeline") == EXPECTED_VALUES["integer_pipeline"],
        rounding_policy.get("pipeline"),
        EXPECTED_VALUES["integer_pipeline"],
    )
    record_check(
        results,
        "prd.integer_rounding_policy.rounding",
        rounding_policy.get("rounding") == EXPECTED_VALUES["integer_rounding"],
        rounding_policy.get("rounding"),
        EXPECTED_VALUES["integer_rounding"],
    )
    record_check(
        results,
        "prd.integer_rounding_policy.bankers_rounding",
        rounding_policy.get("bankers_rounding") == EXPECTED_VALUES["integer_bankers_rounding"],
        rounding_policy.get("bankers_rounding"),
        EXPECTED_VALUES["integer_bankers_rounding"],
    )
    record_check(
        results,
        "prd.damage_pipeline_order",
        locked.get("damage_pipeline_order") == EXPECTED_VALUES["damage_pipeline_order"],
        locked.get("damage_pipeline_order"),
        EXPECTED_VALUES["damage_pipeline_order"],
    )

    difficulty_props = difficulty_schema.get("properties", {})
    record_check(
        results,
        "difficulty.schema.unlock_policy.enum",
        difficulty_props.get("unlock_policy", {}).get("enum", [None])[0]
        == EXPECTED_VALUES["difficulty_unlock_policy"],
        difficulty_props.get("unlock_policy", {}).get("enum", [None])[0],
        EXPECTED_VALUES["difficulty_unlock_policy"],
    )
    record_check(
        results,
        "difficulty.schema.allow_cross_tier_skip.const",
        difficulty_props.get("allow_cross_tier_skip", {}).get("const") is False,
        difficulty_props.get("allow_cross_tier_skip", {}).get("const"),
        False,
    )

    record_check(
        results,
        "difficulty.sample.unlock_policy",
        difficulty_sample.get("unlock_policy") == EXPECTED_VALUES["difficulty_unlock_policy"],
        difficulty_sample.get("unlock_policy"),
        EXPECTED_VALUES["difficulty_unlock_policy"],
    )
    record_check(
        results,
        "difficulty.sample.allow_cross_tier_skip",
        difficulty_sample.get("allow_cross_tier_skip") is False,
        difficulty_sample.get("allow_cross_tier_skip"),
        False,
    )

    spawn_night_schedule = spawn_schema.get("properties", {}).get("night_schedule", {})
    spawn_boss_count = spawn_night_schedule.get("properties", {}).get("boss_count", {})
    record_check(
        results,
        "spawn.schema.night_schedule.required_has_boss_count",
        "boss_count" in spawn_night_schedule.get("required", []),
        spawn_night_schedule.get("required", []),
        "contains boss_count",
    )
    record_check(
        results,
        "spawn.schema.boss_count.mode.enum",
        spawn_boss_count.get("properties", {}).get("mode", {}).get("enum", [None])[0]
        == EXPECTED_VALUES["spawn_boss_mode"],
        spawn_boss_count.get("properties", {}).get("mode", {}).get("enum", [None])[0],
        EXPECTED_VALUES["spawn_boss_mode"],
    )

    sample_boss_count = spawn_sample.get("night_schedule", {}).get("boss_count", {})
    record_check(
        results,
        "spawn.sample.boss_count.mode",
        sample_boss_count.get("mode") == EXPECTED_VALUES["spawn_boss_mode"],
        sample_boss_count.get("mode"),
        EXPECTED_VALUES["spawn_boss_mode"],
    )
    record_check(
        results,
        "spawn.sample.boss_count.default",
        sample_boss_count.get("default") == 2,
        sample_boss_count.get("default"),
        2,
    )

    record_check(
        results,
        "audit.schema.writer_source.const",
        audit_schema.get("properties", {}).get("writer_source", {}).get("const")
        == EXPECTED_VALUES["audit_writer_source"],
        audit_schema.get("properties", {}).get("writer_source", {}).get("const"),
        EXPECTED_VALUES["audit_writer_source"],
    )

    record_check(
        results,
        "rules.spawn.r005_contains_mode_rule",
        "boss_count.mode == config-driven" in rules_text,
        "boss_count.mode == config-driven" in rules_text,
        True,
    )
    record_check(
        results,
        "rules.spawn.r005_contains_default_rule",
        "boss_count.default >= 1" in rules_text,
        "boss_count.default >= 1" in rules_text,
        True,
    )

    record_check(
        results,
        "summary.path_fail_fallback_castle",
        EXPECTED_VALUES["summary_fallback_castle"] in summary_text,
        EXPECTED_VALUES["summary_fallback_castle"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.path_fail_gate_policy",
        EXPECTED_VALUES["summary_gate_policy"] in summary_text,
        EXPECTED_VALUES["summary_gate_policy"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.clone_policy",
        EXPECTED_VALUES["summary_clone_policy"] in summary_text,
        EXPECTED_VALUES["summary_clone_policy"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.damage_pipeline_order",
        EXPECTED_VALUES["summary_damage_pipeline"] in summary_text,
        EXPECTED_VALUES["summary_damage_pipeline"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.debt_cross_zero",
        EXPECTED_VALUES["summary_debt_cross_zero"] in summary_text,
        EXPECTED_VALUES["summary_debt_cross_zero"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.debt_unlock_threshold",
        EXPECTED_VALUES["summary_debt_unlock"] in summary_text,
        EXPECTED_VALUES["summary_debt_unlock"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.build_soft_limit_scope",
        EXPECTED_VALUES["summary_build_soft_limit"] in summary_text,
        EXPECTED_VALUES["summary_build_soft_limit"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.reward_popup_timing",
        EXPECTED_VALUES["summary_reward_popup"] in summary_text,
        EXPECTED_VALUES["summary_reward_popup"] in summary_text,
        True,
    )
    record_check(
        results,
        "summary.reward_gold_scaling",
        EXPECTED_VALUES["summary_reward_gold_scaling"] in summary_text,
        EXPECTED_VALUES["summary_reward_gold_scaling"] in summary_text,
        True,
    )

    return results


def main() -> int:
    parser = argparse.ArgumentParser(description="Check lock consistency across PRD and config contracts.")
    parser.add_argument(
        "--prd",
        default="docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md",
        help="Path to PRD file with machine appendix JSON",
    )
    parser.add_argument(
        "--out",
        default="",
        help="Optional output report path (default: logs/ci/<YYYY-MM-DD>/config-contract-sync-check.json)",
    )
    parser.add_argument(
        "--summary",
        default="docs/prd/PRD-LASTKING-v1.2-LOCKED-SUMMARY.md",
        help="Path to LOCKED-SUMMARY markdown file",
    )
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Reserved for future expansion; currently equivalent to default behavior.",
    )
    args = parser.parse_args()

    root = repo_root()
    prd_path = root / args.prd
    summary_path = root / args.summary
    out_path = Path(args.out) if args.out else default_output_path(root)
    if not out_path.is_absolute():
        out_path = root / out_path

    try:
        checks = run_checks(root, prd_path, summary_path)
    except Exception as exc:  # fail closed
        out_path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "timestamp": dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "status": "error",
            "error": str(exc),
            "report": [],
        }
        out_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"ERROR: {exc}")
        print(f"Report: {out_path.as_posix()}")
        return 1

    failed = [item for item in checks if not item["ok"]]
    status = "pass" if not failed else "fail"

    payload = {
        "timestamp": dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "status": status,
        "total_checks": len(checks),
        "failed_checks": len(failed),
        "report": checks,
    }
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(f"Status: {status.upper()}")
    print(f"Checks: {len(checks)} | Failed: {len(failed)}")
    print(f"Report: {out_path.as_posix()}")
    if failed:
        for item in failed:
            print(
                f"- {item['check_id']}: actual={item['actual']!r}, expected={item['expected']!r}",
                file=sys.stderr,
            )
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
