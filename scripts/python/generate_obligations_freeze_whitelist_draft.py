from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path


def format_task_ids(task_ids: list[int]) -> str:
    if not task_ids:
        return "-"
    return ", ".join(f"T{task_id}" for task_id in task_ids)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate obligations freeze whitelist draft from jitter summary.")
    parser.add_argument(
        "--summary",
        default="logs/ci/2026-02-15/sc-llm-obligations-jitter-batch5x3-summary.json",
        help="Input jitter summary JSON path (repo-relative).",
    )
    parser.add_argument(
        "--out-json",
        default=".taskmaster/config/obligations-freeze-whitelist.draft.json",
        help="Output whitelist draft JSON path (repo-relative).",
    )
    parser.add_argument(
        "--out-md",
        default="logs/ci/2026-02-15/sc-obligations-freeze-whitelist-draft.md",
        help="Output markdown report path (repo-relative).",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = Path(__file__).resolve().parents[2]
    source_summary = repo_root / Path(args.summary)

    data = json.loads(source_summary.read_text(encoding="utf-8"))
    task_stats = data.get("task_stats", [])

    stable_ok = sorted(task["task_id"] for task in task_stats if task.get("stability") == "stable_ok")
    stable_fail = sorted(task["task_id"] for task in task_stats if task.get("stability") == "stable_fail")
    jitter_ok_majority = sorted(task["task_id"] for task in task_stats if task.get("stability") == "jitter_ok_majority")
    jitter_fail_majority = sorted(task["task_id"] for task in task_stats if task.get("stability") == "jitter_fail_majority")

    draft_payload = {
        "schema_version": "1.0-draft",
        "generated_at": datetime.now(timezone.utc).replace(microsecond=0).isoformat(),
        "source": {
            "summary_file": str(Path(args.summary)).replace("\\", "/"),
            "method": "llm_extract_task_obligations batch_size=5 rounds=3",
        },
        "policy": {
            "default_mode": "single_run",
            "jitter_mode": "three_run_majority",
            "majority_pass_rule": "at least 2/3 runs verdict=ok",
            "majority_fail_rule": "at least 2/3 runs verdict=fail",
            "tie_rule": "mark_unknown_and_recheck",
            "freeze_invalidation": [
                "acceptance_changed",
                "master_details_changed",
                "tooling_or_prompt_changed",
            ],
        },
        "task_sets": {
            "stable_ok": stable_ok,
            "jitter_ok_majority": jitter_ok_majority,
            "jitter_fail_majority": jitter_fail_majority,
            "stable_fail": stable_fail,
        },
        "ops_recommendation": {
            "watchlist": sorted(set(jitter_fail_majority + stable_fail)),
            "auto_rerun_on_single_fail": jitter_ok_majority,
            "blocked_until_fix": sorted(set(jitter_fail_majority + stable_fail)),
            "notes": [
                "This is a draft whitelist baseline, not a permanent gate bypass.",
                "Any acceptance/content change invalidates the freeze baseline.",
            ],
        },
    }

    draft_json = repo_root / Path(args.out_json)
    draft_json.parent.mkdir(parents=True, exist_ok=True)
    draft_json.write_text(json.dumps(draft_payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    draft_md = repo_root / Path(args.out_md)
    markdown_lines = [
        "# Obligations Freeze Whitelist Draft",
        "",
        f"- Source: {str(Path(args.summary)).replace('\\', '/')}",
        "- Method: llm_extract_task_obligations, batch_size=5, rounds=3",
        "",
        f"- stable_ok ({len(stable_ok)}): {format_task_ids(stable_ok)}",
        f"- jitter_ok_majority ({len(jitter_ok_majority)}): {format_task_ids(jitter_ok_majority)}",
        f"- jitter_fail_majority ({len(jitter_fail_majority)}): {format_task_ids(jitter_fail_majority)}",
        f"- stable_fail ({len(stable_fail)}): {format_task_ids(stable_fail)}",
        "",
        "## Draft Rules",
        "- Stable OK: run once by default; rerun only when acceptance/master content changes.",
        "- Jitter OK Majority: on first fail, auto-rerun to 3 rounds and use 2/3 majority.",
        "- Jitter/Stable Fail: block acceptance until semantic gap is fixed.",
        "- Any acceptance hash change invalidates frozen baseline.",
        "",
        "## Stop-Loss Note",
        "- This whitelist is an execution-stability baseline, not a semantic-quality bypass.",
        "- Do not use whitelist to skip obligations coverage checks.",
    ]
    draft_md.write_text("\n".join(markdown_lines) + "\n", encoding="utf-8")

    print(f"wrote {draft_json}")
    print(f"wrote {draft_md}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
