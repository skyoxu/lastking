#!/usr/bin/env python3
from __future__ import annotations

import datetime as dt
import json
import re
import subprocess
from collections import Counter
from pathlib import Path


def main() -> int:
    task_ids = [2, 3, 12, 13, 19]
    rounds = 3
    timeout_sec = 420

    out_dir = Path("logs") / "ci" / dt.date.today().isoformat()
    out_dir.mkdir(parents=True, exist_ok=True)
    out_json = out_dir / "sc-obligations-rerun-5tasks-x3-hardgate.json"
    out_md = out_dir / "sc-obligations-rerun-5tasks-x3-hardgate.md"

    rows: list[dict[str, object]] = []
    for round_index in range(1, rounds + 1):
        for task_id in task_ids:
            cmd = [
                "py",
                "-3",
                "scripts/sc/llm_extract_task_obligations.py",
                "--task-id",
                str(task_id),
                "--timeout-sec",
                str(timeout_sec),
            ]
            proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
            combined = "\n".join(x for x in [proc.stdout.strip(), proc.stderr.strip()] if x)
            match_status = re.search(r"status=(ok|fail)", combined)
            match_out = re.search(r"out=([^\r\n]+)", combined)
            verdict = match_status.group(1) if match_status else ("ok" if proc.returncode == 0 else "fail")
            out_path = Path(match_out.group(1).strip()) if match_out else None

            uncovered_ids: list[str] = []
            if out_path is not None:
                verdict_path = out_path / "verdict.json"
                if verdict_path.exists():
                    try:
                        verdict_obj = json.loads(verdict_path.read_text(encoding="utf-8"))
                        verdict_status = str(verdict_obj.get("status") or "").strip().lower()
                        if verdict_status in {"ok", "fail"}:
                            verdict = verdict_status
                        uncovered_ids = [str(x) for x in (verdict_obj.get("uncovered_obligation_ids") or [])]
                    except Exception:
                        pass

            rows.append(
                {
                    "round": round_index,
                    "task_id": task_id,
                    "verdict": verdict,
                    "uncovered_ids": uncovered_ids,
                }
            )
            print(f"T{task_id} r{round_index}: {verdict}, uncovered={uncovered_ids}")

    stats: dict[int, dict[str, object]] = {}
    for task_id in task_ids:
        verdict_sequence = [str(r["verdict"]) for r in rows if int(r["task_id"]) == task_id]
        uncovered_sequence = [list(r["uncovered_ids"]) for r in rows if int(r["task_id"]) == task_id]
        counts = Counter(verdict_sequence)
        if counts["ok"] >= 2:
            majority = "ok"
        elif counts["fail"] >= 2:
            majority = "fail"
        else:
            majority = "unknown"
        verdict_jitter = len(set(verdict_sequence)) > 1
        uncovered_jitter = len(set(tuple(x) for x in uncovered_sequence)) > 1

        if majority == "ok" and not verdict_jitter:
            stability = "stable_ok"
        elif majority == "fail" and not verdict_jitter:
            stability = "stable_fail"
        elif majority == "ok" and verdict_jitter:
            stability = "jitter_ok_majority"
        elif majority == "fail" and verdict_jitter:
            stability = "jitter_fail_majority"
        else:
            stability = "unknown"

        stats[task_id] = {
            "verdict_sequence": verdict_sequence,
            "uncovered_sequence": uncovered_sequence,
            "majority": majority,
            "stability": stability,
            "verdict_jitter": verdict_jitter,
            "uncovered_jitter": uncovered_jitter,
        }

    payload = {
        "meta": {
            "date": dt.date.today().isoformat(),
            "tasks": task_ids,
            "rounds": rounds,
            "phase": "hardgate-round3",
        },
        "rows": rows,
        "task_stats": stats,
    }
    out_json.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = ["# obligations rerun (5 tasks x3) hardgate", ""]
    for task_id in task_ids:
        item = stats[task_id]
        lines.append(
            f"- T{task_id}: stability={item['stability']}, majority={item['majority']}, "
            f"verdict_seq={item['verdict_sequence']}, uncovered_seq={item['uncovered_sequence']}"
        )
    out_md.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(f"WROTE {out_json}")
    print(f"WROTE {out_md}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
