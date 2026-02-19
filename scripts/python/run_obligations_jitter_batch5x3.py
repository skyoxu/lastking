#!/usr/bin/env python3
"""
Run obligations extraction in grouped rounds and persist raw jitter records.

Default shape:
- 40 tasks
- batch size 5 (8 groups)
- 3 rounds per group

This script is local utility only and does not modify CI pipeline wiring.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
from datetime import datetime
from pathlib import Path
from typing import Any


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run obligations jitter batch rounds.")
    parser.add_argument("--task-count", type=int, default=40, help="Total task count (default: 40).")
    parser.add_argument("--batch-size", type=int, default=5, help="Tasks per group (default: 5).")
    parser.add_argument("--rounds", type=int, default=3, help="Rounds per group (default: 3).")
    parser.add_argument("--start-group", type=int, default=1, help="1-based start group.")
    parser.add_argument("--end-group", type=int, default=8, help="1-based end group.")
    parser.add_argument("--timeout-sec", type=int, default=420, help="Timeout passed to llm_extract_task_obligations.py.")
    parser.add_argument(
        "--out-raw",
        default=None,
        help="Raw output JSON path. Default: logs/ci/<today>/sc-llm-obligations-jitter-batch5x3-raw-rerun-full.json",
    )
    return parser.parse_args()


def make_groups(task_count: int, batch_size: int) -> list[list[int]]:
    all_ids = list(range(1, task_count + 1))
    return [all_ids[index : index + batch_size] for index in range(0, len(all_ids), batch_size)]


def default_out_path() -> Path:
    today = datetime.now().strftime("%Y-%m-%d")
    return Path("logs") / "ci" / today / "sc-llm-obligations-jitter-batch5x3-raw-rerun-full.json"


def load_or_init_payload(path: Path, groups: list[list[int]], rounds: int, batch_size: int) -> dict[str, Any]:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {
        "date": datetime.now().strftime("%Y-%m-%d"),
        "batch_size": batch_size,
        "rounds": rounds,
        "groups": groups,
        "rows": [],
    }


def parse_out_dir(stdout_tail: str) -> Path | None:
    match = re.search(r"out=(.+)$", stdout_tail or "")
    if not match:
        return None
    out_dir = match.group(1).strip()
    if not out_dir:
        return None
    return Path(out_dir)


def read_task_outputs(task_id: int, logs_root: Path, *, fallback_rc: int, stdout_tail: str) -> tuple[dict[str, Any], dict[str, Any]]:
    parsed_dir = parse_out_dir(stdout_tail)
    task_dir = parsed_dir if parsed_dir is not None else (logs_root / f"sc-llm-obligations-task-{task_id}")
    summary_path = task_dir / "summary.json"
    verdict_path = task_dir / "verdict.json"
    if not summary_path.exists() or not verdict_path.exists():
        summary = {
            "status": "fail",
            "rc": fallback_rc if fallback_rc != 0 else 1,
            "error": "missing_task_outputs",
        }
        verdict = {
            "status": "fail",
            "uncovered_obligation_ids": [],
        }
        return summary, verdict
    summary = json.loads(summary_path.read_text(encoding="utf-8"))
    verdict = json.loads(verdict_path.read_text(encoding="utf-8"))
    return summary, verdict


def run_once(task_id: int, timeout_sec: int) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["py", "-3", "scripts/sc/llm_extract_task_obligations.py", "--task-id", str(task_id), "--timeout-sec", str(timeout_sec)],
        cwd=str(Path.cwd()),
        capture_output=True,
        text=True,
    )


def main() -> int:
    args = parse_args()

    groups = make_groups(args.task_count, args.batch_size)
    total_groups = len(groups)

    if args.start_group < 1 or args.end_group > total_groups or args.start_group > args.end_group:
        raise ValueError(f"Invalid group range: start={args.start_group}, end={args.end_group}, total={total_groups}")

    out_raw = Path(args.out_raw) if args.out_raw else default_out_path()
    out_raw.parent.mkdir(parents=True, exist_ok=True)

    payload = load_or_init_payload(out_raw, groups, args.rounds, args.batch_size)
    rows: list[dict[str, Any]] = payload.get("rows", [])

    logs_root = out_raw.parent

    for group_index in range(args.start_group, args.end_group + 1):
        task_ids = groups[group_index - 1]
        print(f"[group {group_index}/{total_groups}] task_ids={task_ids}")
        for round_index in range(1, args.rounds + 1):
            print(f"  [round {round_index}/{args.rounds}]")
            for task_id in task_ids:
                process = run_once(task_id, args.timeout_sec)
                stdout = (process.stdout or "").strip()
                stderr = (process.stderr or "").strip()
                stdout_tail = stdout.splitlines()[-1] if stdout else ""
                stderr_tail = stderr.splitlines()[-1] if stderr else ""

                summary, verdict = read_task_outputs(
                    task_id,
                    logs_root,
                    fallback_rc=process.returncode,
                    stdout_tail=stdout_tail,
                )
                uncovered_ids = verdict.get("uncovered_obligation_ids", [])
                if not isinstance(uncovered_ids, list):
                    uncovered_ids = []

                row = {
                    "group": group_index,
                    "round": round_index,
                    "task_id": task_id,
                    "cp_returncode": process.returncode,
                    "stdout_tail": stdout_tail,
                    "stderr_tail": stderr_tail,
                    "summary_status": summary.get("status"),
                    "summary_rc": summary.get("rc"),
                    "summary_error": summary.get("error"),
                    "verdict_status": verdict.get("status"),
                    "uncovered_count": len(uncovered_ids),
                    "uncovered_ids": uncovered_ids,
                }
                rows.append(row)

                print(
                    f"    T{task_id}: verdict={row['verdict_status']} "
                    f"summary_rc={row['summary_rc']} uncovered={row['uncovered_count']}"
                )

                payload["rows"] = rows
                out_raw.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(f"wrote {out_raw}")
    print(f"rows_now={len(rows)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
