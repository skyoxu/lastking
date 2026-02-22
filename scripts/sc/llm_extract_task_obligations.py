#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Extract task obligations and check acceptance coverage via LLM.

Supports multi-run consensus, per-run artifacts, and deterministic hard gates:
- minimum obligations count
- each subtask must have at least one obligation source `subtask:<id>`
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

def _bootstrap_imports() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent))

_bootstrap_imports()
from _taskmaster import resolve_triplet  # noqa: E402
from _util import ci_dir, repo_root, write_json, write_text  # noqa: E402

def _run_codex_exec(*, prompt: str, out_last_message: Path, timeout_sec: int) -> tuple[int, str, list[str]]:
    exe = shutil.which("codex")
    if not exe:
        return 127, "codex executable not found in PATH\n", ["codex"]
    cmd = [
        exe,
        "exec",
        "-s",
        "read-only",
        "-C",
        str(repo_root()),
        "--output-last-message",
        str(out_last_message),
        "-",
    ]
    try:
        proc = subprocess.run(
            cmd,
            input=prompt,
            text=True,
            encoding="utf-8",
            errors="ignore",
            cwd=str(repo_root()),
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout_sec,
        )
    except subprocess.TimeoutExpired:
        return 124, "codex exec timeout\n", cmd
    except Exception as exc:  # noqa: BLE001
        return 1, f"codex exec failed to start: {exc}\n", cmd
    return proc.returncode or 0, proc.stdout or "", cmd

def _extract_json_object(text: str) -> dict[str, Any]:
    text = str(text or "").strip()
    try:
        obj = json.loads(text)
        if isinstance(obj, dict):
            return obj
    except Exception:
        pass
    m = re.search(r"\{.*\}", text, flags=re.DOTALL)
    if not m:
        raise ValueError("No JSON object found in model output.")
    obj = json.loads(m.group(0))
    if not isinstance(obj, dict):
        raise ValueError("Model output JSON is not an object.")
    return obj

def _truncate(text: str, *, max_chars: int) -> str:
    if len(text) <= max_chars:
        return text
    return text[: max_chars - 3] + "..."

def _normalize_subtasks(raw: Any) -> list[dict[str, str]]:
    if not isinstance(raw, list):
        return []
    out: list[dict[str, str]] = []
    for s in raw:
        if not isinstance(s, dict):
            continue
        sid = str(s.get("id") or "").strip()
        title = str(s.get("title") or "").strip()
        details = str(s.get("details") or "").strip()
        test_strategy = str(s.get("testStrategy") or "").strip()
        if not sid or not title:
            continue
        if details:
            details = re.sub(r"\s+", " ", details).strip()
            details = _truncate(details, max_chars=520)
        if test_strategy:
            test_strategy = re.sub(r"\s+", " ", test_strategy).strip()
            test_strategy = _truncate(test_strategy, max_chars=320)
        out.append({"id": sid, "title": title, "details": details, "testStrategy": test_strategy})
    return out

def _normalize_model_status(value: Any) -> str:
    status = str(value or "").strip().lower()
    return "ok" if status == "ok" else "fail"

def _parse_subtask_source(source: Any) -> str | None:
    text = str(source or "").strip()
    m = re.match(r"subtask\s*:\s*(.+)$", text, flags=re.IGNORECASE)
    if not m:
        return None
    sid = str(m.group(1) or "").strip()
    return sid or None

def _format_acceptance(view_name: str, acceptance: list[Any]) -> str:
    out = [f"[{view_name}] acceptance items ({len(acceptance)}):"]
    for idx, raw in enumerate(acceptance, start=1):
        text = _truncate(str(raw or "").strip(), max_chars=520)
        out.append(f"- {view_name}:{idx}: {text}")
    return "\n".join(out)


def _build_prompt(
    *,
    task_id: str,
    title: str,
    master_details: str,
    master_test_strategy: str,
    subtasks: list[dict[str, str]],
    acceptance_by_view: dict[str, list[Any]],
) -> str:
    sub_lines = []
    for s in subtasks:
        sid = s.get("id", "").strip()
        st = s.get("title", "").strip()
        sd = s.get("details", "").strip()
        ts = s.get("testStrategy", "").strip()
        if sid and st:
            line = f"- {sid}: {st}"
            if sd:
                line += f" :: {sd}"
            sub_lines.append(line)
            if ts:
                sub_lines.append(f"  testStrategy: {ts}")

    acceptance_blocks = []
    for view_name, acc in acceptance_by_view.items():
        acceptance_blocks.append(_format_acceptance(view_name, acc))

    schema = """
Return JSON only (no Markdown).
Schema:
{
  "task_id": "<id>",
  "status": "ok" | "fail",
  "obligations": [
    {
      "id": "O1",
      "source": "master" | "subtask:<id>",
      "kind": "core" | "godot" | "meta",
      "text": "<one falsifiable obligation>",
      "source_excerpt": "<short verbatim excerpt from the provided task text>",
      "covered": true | false,
      "matches": [
        {"view": "back|gameplay", "acceptance_index": <1-based>, "acceptance_excerpt": "<short>"}
      ],
      "reason": "<one short sentence>",
      "suggested_acceptance": ["<line1>", "<line2>"]
    }
  ],
  "uncovered_obligation_ids": ["O2", "..."],
  "notes": ["<short>", ...]
}

Rules:
- Obligations MUST be falsifiable / auditable: avoid vague statements like "works correctly".
- Avoid no-op loopholes: include at least one "must refuse / must not advance / state unchanged" obligation when applicable.
- Use ONLY the provided task text (master.details/testStrategy + subtasks title/details/testStrategy) to derive obligations.
- Each obligation MUST include source_excerpt copied verbatim from the provided task text; if you cannot cite an excerpt, do NOT include that obligation.
- Be conservative: mark covered ONLY when an acceptance item clearly implies it.
- If ANY obligation is not covered => status must be "fail".
- suggested_acceptance must be minimal and aligned to tasks_back/tasks_gameplay style (Chinese OK). Do NOT include any "Refs:" here.
- Ignore "Local demo paths" / absolute paths; they are not obligations.
"""

    master_details = _truncate(master_details or "", max_chars=8_000)
    master_test_strategy = _truncate(master_test_strategy or "", max_chars=4_000)

    return "\n".join(
        [
            "You are a strict reviewer for a Godot + C# repo.",
            "Acceptance criteria are used as SSoT for deterministic gates; they must cover all must-have obligations.",
            "",
            f"Task: T{task_id} {title}",
            "",
            "Master details:",
            master_details or "(empty)",
            "",
            "Master testStrategy:",
            master_test_strategy or "(empty)",
            "",
            "Subtasks (from tasks.json):",
            *(sub_lines or ["- (none)"]),
            "",
            "Acceptance criteria (from tasks_back/tasks_gameplay):",
            *acceptance_blocks,
            "",
            schema.strip(),
        ]
    )


def _is_view_present(view: dict[str, Any] | None) -> bool:
    return isinstance(view, dict) and isinstance(view.get("acceptance"), list)


def _render_report(obj: dict[str, Any]) -> str:
    task_id = str(obj.get("task_id") or "")
    status = str(obj.get("status") or "")
    uncovered = obj.get("uncovered_obligation_ids") or []
    obligations = obj.get("obligations") or []
    lines: list[str] = []
    lines.append("# sc-llm-extract-task-obligations report")
    lines.append("")
    lines.append(f"- task_id: {task_id}")
    lines.append(f"- status: {status}")
    lines.append(f"- uncovered: {len(uncovered) if isinstance(uncovered, list) else 'unknown'}")
    lines.append("")
    if isinstance(obligations, list) and obligations:
        lines.append("## Obligations")
        lines.append("")
        for o in obligations:
            if not isinstance(o, dict):
                continue
            oid = str(o.get("id") or "").strip()
            covered = bool(o.get("covered"))
            text = str(o.get("text") or "").strip()
            excerpt = str(o.get("source_excerpt") or "").strip()
            src = str(o.get("source") or "").strip()
            kind = str(o.get("kind") or "").strip()
            lines.append(f"- {oid} covered={covered} kind={kind} source={src}: {text}")
            if excerpt:
                lines.append(f"  - excerpt: {excerpt}")
            if not covered:
                sug = o.get("suggested_acceptance") or []
                if isinstance(sug, list) and sug:
                    for s in sug[:2]:
                        ss = str(s or "").strip()
                        if ss:
                            lines.append(f"  - suggest: {ss}")
    return "\n".join(lines).strip() + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description="sc-llm-extract-task-obligations (obligations vs acceptance coverage)")
    ap.add_argument("--task-id", default=None, help="Taskmaster id (e.g. 17). Default: first status=in-progress task.")
    ap.add_argument("--timeout-sec", type=int, default=360, help="codex exec timeout in seconds (default: 360).")
    ap.add_argument("--max-prompt-chars", type=int, default=80_000, help="Max prompt size (default: 80000).")
    ap.add_argument("--consensus-runs", type=int, default=1, help="Run N rounds and use majority status (default: 1).")
    ap.add_argument("--min-obligations", type=int, default=0, help="Deterministic hard gate: minimum obligations count (default: 0).")
    ap.add_argument("--round-id", default="", help="Optional run id suffix for output directory isolation.")
    args = ap.parse_args()

    try:
        triplet = resolve_triplet(task_id=str(args.task_id) if args.task_id else None)
    except Exception as exc:  # noqa: BLE001
        print(f"SC_LLM_OBLIGATIONS status=fail error=resolve_triplet_failed exc={exc}")
        return 2

    out_dir_name = f"sc-llm-obligations-task-{triplet.task_id}"
    if str(args.round_id or "").strip():
        out_dir_name += f"-round-{str(args.round_id).strip()}"
    out_dir = ci_dir(out_dir_name)
    title = str(triplet.master.get("title") or "").strip()
    details = str(triplet.master.get("details") or "").strip()
    test_strategy = str(triplet.master.get("testStrategy") or "").strip()
    subtasks = _normalize_subtasks(triplet.master.get("subtasks"))

    acceptance_by_view: dict[str, list[Any]] = {}
    if _is_view_present(triplet.back):
        acceptance_by_view["back"] = list((triplet.back or {}).get("acceptance") or [])
    if _is_view_present(triplet.gameplay):
        acceptance_by_view["gameplay"] = list((triplet.gameplay or {}).get("acceptance") or [])

    summary: dict[str, Any] = {
        "cmd": "sc-llm-extract-task-obligations",
        "task_id": triplet.task_id,
        "title": title,
        "status": None,
        "subtasks_total": len(subtasks),
        "views_present": sorted(acceptance_by_view.keys()),
        "out_dir": str(out_dir.relative_to(repo_root())).replace("\\", "/"),
        "error": None,
    }

    if not acceptance_by_view:
        summary["status"] = "fail"
        summary["error"] = "no_views_present"
        write_json(out_dir / "summary.json", summary)
        write_text(out_dir / "report.md", _render_report({"task_id": triplet.task_id, "status": "fail"}))
        print(f"SC_LLM_OBLIGATIONS status=fail reason=no_views_present out={out_dir}")
        return 1

    prompt = _build_prompt(
        task_id=str(triplet.task_id),
        title=title,
        master_details=details,
        master_test_strategy=test_strategy,
        subtasks=subtasks,
        acceptance_by_view=acceptance_by_view,
    )
    prompt = _truncate(prompt, max_chars=int(args.max_prompt_chars))
    prompt_path = out_dir / "prompt.md"
    last_msg_path = out_dir / "output-last-message.txt"
    trace_path = out_dir / "trace.log"
    write_text(prompt_path, prompt)

    runs = max(1, int(args.consensus_runs))
    run_results: list[dict[str, Any]] = []
    run_verdicts: list[dict[str, Any]] = []
    cmd_ref: list[str] | None = None
    for i in range(1, runs + 1):
        run_last = out_dir / f"output-last-message-run-{i:02d}.txt"
        run_trace = out_dir / f"trace-run-{i:02d}.log"
        rc, trace_out, cmd = _run_codex_exec(prompt=prompt, out_last_message=run_last, timeout_sec=int(args.timeout_sec))
        write_text(run_trace, trace_out)
        if cmd_ref is None:
            cmd_ref = cmd
        last_msg = run_last.read_text(encoding="utf-8", errors="ignore") if run_last.exists() else ""
        parsed_obj: dict[str, Any] | None = None
        err: str | None = None
        if rc != 0 or not last_msg.strip():
            err = "codex_exec_failed_or_empty"
        else:
            try:
                parsed_obj = _extract_json_object(last_msg)
            except Exception as exc:  # noqa: BLE001
                err = f"invalid_json:{exc}"
        run_status = _normalize_model_status((parsed_obj or {}).get("status")) if parsed_obj else "fail"
        run_results.append({"run": i, "rc": rc, "status": run_status, "error": err})
        if parsed_obj:
            run_verdicts.append({"run": i, "status": run_status, "obj": parsed_obj})
            write_json(out_dir / f"verdict-run-{i:02d}.json", parsed_obj)

    ok_votes = sum(1 for r in run_results if r["status"] == "ok")
    fail_votes = runs - ok_votes
    status = "ok" if ok_votes > fail_votes else "fail"
    selected = next((v for v in run_verdicts if v["status"] == status), run_verdicts[0] if run_verdicts else None)
    obj: dict[str, Any] = dict((selected or {}).get("obj") or {"task_id": str(triplet.task_id), "status": "fail", "obligations": []})
    obj["status"] = status

    obligations = obj.get("obligations") or []
    if not isinstance(obligations, list):
        obligations = []
    det_missing: list[str] = []
    if int(args.min_obligations) > 0 and len(obligations) < int(args.min_obligations):
        det_missing.append(f"DET_MIN_OBLIGATIONS<{int(args.min_obligations)}")
    if subtasks:
        required = [str(s["id"]) for s in subtasks]
        covered_sources = {sid for sid in (_parse_subtask_source((o or {}).get("source")) for o in obligations if isinstance(o, dict)) if sid}
        for sid in required:
            if sid not in covered_sources:
                det_missing.append(f"DET_SUBTASK_SOURCE:{sid}")
    if det_missing:
        status = "fail"
        obj["status"] = "fail"
        uncovered = obj.get("uncovered_obligation_ids") or []
        if not isinstance(uncovered, list):
            uncovered = []
        obj["uncovered_obligation_ids"] = uncovered + [x for x in det_missing if x not in uncovered]
        notes = obj.get("notes") or []
        if not isinstance(notes, list):
            notes = []
        obj["notes"] = notes + [f"deterministic_hard_gate: {x}" for x in det_missing]

    summary["rc"] = 0 if run_verdicts else 1
    summary["cmdline"] = cmd_ref or []
    summary["consensus_runs"] = runs
    summary["consensus_votes"] = {"ok": ok_votes, "fail": fail_votes}
    summary["run_results"] = run_results
    summary["status"] = status
    if not run_verdicts:
        summary["error"] = "all_runs_failed_or_invalid"
    write_json(out_dir / "summary.json", summary)
    write_json(out_dir / "verdict.json", obj)
    write_text(out_dir / "report.md", _render_report(obj))
    write_text(last_msg_path, json.dumps(obj, ensure_ascii=False, indent=2) + "\n")
    write_text(trace_path, f"consensus_runs={runs}\nok_votes={ok_votes}\nfail_votes={fail_votes}\n")

    ok = status == "ok"
    print(f"SC_LLM_OBLIGATIONS status={'ok' if ok else 'fail'} out={out_dir}")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
