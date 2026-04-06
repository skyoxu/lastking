from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from _util import repo_root, write_json, write_text


def active_task_dir(root: Path | None = None) -> Path:
    base = root.resolve() if root else repo_root()
    return base / "logs" / "ci" / "active-tasks"


def active_task_json_path(task_id: str, root: Path | None = None) -> Path:
    return active_task_dir(root) / f"task-{str(task_id).strip()}.active.json"


def active_task_md_path(task_id: str, root: Path | None = None) -> Path:
    return active_task_dir(root) / f"task-{str(task_id).strip()}.active.md"


def _repo_rel(path: Path, *, root: Path) -> str:
    try:
        return path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def _read_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    payload = json.loads(path.read_text(encoding="utf-8"))
    return payload if isinstance(payload, dict) else {}


def _normalize_llm_verdict(value: str | None) -> str:
    raw = str(value or "").strip().lower()
    if raw in {"ok", "pass", "passed"}:
        return "OK"
    if raw in {"needs fix", "needs_fix", "need fix", "fail", "failed"}:
        return "Needs Fix"
    return "Unknown"


def _resolve_path(raw: str, *, root: Path) -> Path | None:
    raw_text = str(raw or "").strip()
    if not raw_text:
        return None
    candidate = Path(raw_text)
    if not candidate.is_absolute():
        candidate = (root / candidate).resolve()
    return candidate if candidate.exists() else None


def _infer_root_from_paths(*, latest_json_path: Path, out_dir: Path) -> Path:
    candidates = [latest_json_path.resolve(), out_dir.resolve()]
    for candidate in candidates:
        parts_lower = [part.lower() for part in candidate.parts]
        for idx in range(len(parts_lower) - 1):
            if parts_lower[idx] == "logs" and parts_lower[idx + 1] == "ci":
                return Path(*candidate.parts[:idx]).resolve()
    latest_parent = latest_json_path.resolve().parent
    if latest_parent.name.startswith("sc-review-pipeline-task-"):
        return latest_parent.parent.resolve()
    out_name = out_dir.resolve().name
    if out_name.startswith("sc-review-pipeline-task-"):
        return out_dir.resolve().parent.resolve()
    return repo_root()


def _derive_step_summary(summary: dict[str, Any]) -> dict[str, str]:
    steps = summary.get("steps")
    if not isinstance(steps, list):
        return {"latest_step": "", "latest_step_status": "", "failed_step": "", "last_completed_step": ""}
    latest_step = ""
    latest_step_status = ""
    failed_step = ""
    last_completed = ""
    for item in steps:
        if not isinstance(item, dict):
            continue
        name = str(item.get("name") or "").strip()
        status = str(item.get("status") or "").strip()
        if name:
            latest_step = name
            latest_step_status = status
        if not failed_step and status == "fail":
            failed_step = name
        if status == "ok":
            last_completed = name
    return {
        "latest_step": latest_step,
        "latest_step_status": latest_step_status,
        "failed_step": failed_step,
        "last_completed_step": last_completed,
    }


def _derive_clean_state(*, summary: dict[str, Any], out_dir: Path, root: Path) -> dict[str, Any]:
    steps = summary.get("steps") if isinstance(summary.get("steps"), list) else []
    step_map = {
        str(item.get("name") or "").strip(): item
        for item in steps
        if isinstance(item, dict) and str(item.get("name") or "").strip()
    }
    test_status = str((step_map.get("sc-test") or {}).get("status") or "").strip().lower()
    acceptance_status = str((step_map.get("sc-acceptance-check") or {}).get("status") or "").strip().lower()
    llm_step = step_map.get("sc-llm-review") or {}
    llm_status = str(llm_step.get("status") or "").strip().lower()
    llm_summary_path = _resolve_path(str(llm_step.get("summary_file") or "").strip(), root=root)
    needs_fix_agents: list[str] = []
    unknown_agents: list[str] = []
    timeout_agents: list[str] = []
    if llm_summary_path is not None:
        payload = _read_json(llm_summary_path)
        results = payload.get("results") if isinstance(payload.get("results"), list) else []
        for row in results:
            if not isinstance(row, dict):
                continue
            agent = str(row.get("agent") or "").strip()
            verdict = _normalize_llm_verdict(str(((row.get("details") or {}) if isinstance(row.get("details"), dict) else {}).get("verdict") or ""))
            rc = int(row.get("rc") or 0)
            status = str(row.get("status") or "").strip().lower()
            if verdict == "Needs Fix" and agent:
                needs_fix_agents.append(agent)
            if (verdict == "Unknown" or status not in {"ok", "skipped"} or rc != 0) and agent:
                unknown_agents.append(agent)
            if rc == 124 and agent:
                timeout_agents.append(agent)
    deterministic_ok = test_status == "ok" and acceptance_status == "ok"
    llm_clean = llm_status == "ok" and not needs_fix_agents and not unknown_agents
    if deterministic_ok and llm_clean:
        state = "clean"
    elif deterministic_ok and (needs_fix_agents or unknown_agents or llm_status == "fail"):
        state = "deterministic_ok_llm_not_clean"
    elif deterministic_ok and llm_status == "skipped":
        state = "deterministic_only"
    else:
        state = "not_clean"
    return {
        "state": state,
        "deterministic_ok": deterministic_ok,
        "llm_status": llm_status,
        "llm_summary_path": _repo_rel(llm_summary_path, root=root) if llm_summary_path else "",
        "needs_fix_agents": sorted(needs_fix_agents),
        "unknown_agents": sorted(set(unknown_agents)),
        "timeout_agents": sorted(set(timeout_agents)),
    }


def _recommended_action(*, status: str, failed_step: str, repair_guide: dict[str, Any], clean_state: dict[str, Any]) -> tuple[str, str]:
    normalized = str(status or "").strip().lower()
    derived_state = str(clean_state.get("state") or "").strip().lower()
    repair_status = str(repair_guide.get("status") or "").strip().lower()
    first_fix_title = ""
    recommendations = repair_guide.get("recommendations")
    if isinstance(recommendations, list):
        for item in recommendations:
            if isinstance(item, dict):
                first_fix_title = str(item.get("title") or "").strip()
                if first_fix_title:
                    break
    if derived_state == "deterministic_ok_llm_not_clean":
        if clean_state.get("timeout_agents") or clean_state.get("unknown_agents"):
            return "needs-fix-fast", "Deterministic steps are already green, but llm_review is incomplete; rerun only the LLM closure path instead of paying for a full pipeline."
        return "needs-fix-fast", "Deterministic steps are already green, but llm_review still reports actionable findings; continue with the task-scoped Needs Fix loop."
    if normalized == "ok":
        return "continue", "Pipeline is green; continue the task or start the next task."
    if normalized == "aborted":
        return "rerun", "The latest run was aborted; start a fresh run instead of resuming frozen artifacts."
    if repair_status == "needs-approval":
        return "fork", "Repair guidance requires approval or isolation; prefer fork after reviewing the approval sidecar."
    if failed_step:
        why = f"Fix the first blocking step `{failed_step}` and resume the same run."
        if first_fix_title:
            why += f" Suggested first fix: {first_fix_title}."
        return "resume", why
    return "inspect", "Inspect summary, execution-context, and repair-guide before choosing resume or fork."


def build_active_task_payload(
    *,
    task_id: str,
    run_id: str,
    status: str,
    out_dir: Path,
    latest_json_path: Path,
    root: Path | None = None,
) -> dict[str, Any]:
    resolved_root = root.resolve() if root else repo_root()
    summary_path = out_dir / "summary.json"
    execution_context_path = out_dir / "execution-context.json"
    repair_guide_json_path = out_dir / "repair-guide.json"
    repair_guide_md_path = out_dir / "repair-guide.md"
    summary = _read_json(summary_path)
    repair_guide = _read_json(repair_guide_json_path)
    execution_context = _read_json(execution_context_path)
    step_summary = _derive_step_summary(summary)
    clean_state = _derive_clean_state(summary=summary, out_dir=out_dir, root=resolved_root)
    recommended_action, recommended_why = _recommended_action(
        status=status,
        failed_step=step_summary["failed_step"],
        repair_guide=repair_guide,
        clean_state=clean_state,
    )
    return {
        "cmd": "active-task-sidecar",
        "task_id": str(task_id).strip(),
        "run_id": str(run_id).strip(),
        "status": str(status).strip(),
        "updated_at_utc": datetime.now(timezone.utc).isoformat(),
        "paths": {
            "latest_json": _repo_rel(latest_json_path, root=resolved_root),
            "out_dir": _repo_rel(out_dir, root=resolved_root),
            "summary_json": _repo_rel(summary_path, root=resolved_root),
            "execution_context_json": _repo_rel(execution_context_path, root=resolved_root),
            "repair_guide_json": _repo_rel(repair_guide_json_path, root=resolved_root),
            "repair_guide_md": _repo_rel(repair_guide_md_path, root=resolved_root),
        },
        "step_summary": step_summary,
        "clean_state": clean_state,
        "recommended_action": recommended_action,
        "recommended_action_why": recommended_why,
        "candidate_commands": {
            "resume": f"py -3 scripts/sc/run_review_pipeline.py --task-id {task_id} --resume",
            "fork": f"py -3 scripts/sc/run_review_pipeline.py --task-id {task_id} --fork",
            "rerun": f"py -3 scripts/sc/run_review_pipeline.py --task-id {task_id}",
            "needs_fix_fast": f"py -3 scripts/sc/llm_review_needs_fix_fast.py --task-id {task_id} --delivery-profile fast-ship --rerun-failing-only --max-rounds 1",
            "resume_summary": f"py -3 scripts/python/dev_cli.py resume-task --task-id {task_id}",
        },
        "repair_status": str(repair_guide.get("status") or "").strip(),
        "agent_review_recommended_action": str(
            ((execution_context.get("agent_review") or {}).get("recommended_action")) or ""
        ).strip(),
    }


def render_active_task_markdown(payload: dict[str, Any]) -> str:
    paths = payload.get("paths") or {}
    steps = payload.get("step_summary") or {}
    commands = payload.get("candidate_commands") or {}
    clean_state = payload.get("clean_state") or {}
    lines = [
        "# Active Task Summary",
        "",
        f"- Task id: `{payload.get('task_id')}`",
        f"- Run id: `{payload.get('run_id')}`",
        f"- Status: {payload.get('status')}",
        f"- Updated at UTC: {payload.get('updated_at_utc')}",
        f"- Latest pointer: `{paths.get('latest_json')}`" if paths.get("latest_json") else "- Latest pointer: n/a",
        f"- Pipeline out dir: `{paths.get('out_dir')}`" if paths.get("out_dir") else "- Pipeline out dir: n/a",
        f"- Latest step: {steps.get('latest_step') or 'n/a'}",
        f"- Latest step status: {steps.get('latest_step_status') or 'n/a'}",
        f"- Failed step: {steps.get('failed_step') or 'none'}",
        f"- Last completed step: {steps.get('last_completed_step') or 'none'}",
        f"- Recommended action: {payload.get('recommended_action') or 'inspect'}",
        f"- Recommended action why: {payload.get('recommended_action_why') or 'n/a'}",
        f"- Clean state: {clean_state.get('state') or 'unknown'}",
        f"- Deterministic ok: {clean_state.get('deterministic_ok')}",
        f"- Resume summary command: `{commands.get('resume_summary')}`" if commands.get("resume_summary") else "- Resume summary command: n/a",
        f"- Resume command: `{commands.get('resume')}`" if commands.get("resume") else "- Resume command: n/a",
        f"- Fork command: `{commands.get('fork')}`" if commands.get("fork") else "- Fork command: n/a",
        f"- Rerun command: `{commands.get('rerun')}`" if commands.get("rerun") else "- Rerun command: n/a",
        f"- Needs Fix command: `{commands.get('needs_fix_fast')}`" if commands.get("needs_fix_fast") else "- Needs Fix command: n/a",
    ]
    return "\n".join(lines) + "\n"


def write_active_task_sidecar(
    *,
    task_id: str,
    run_id: str,
    status: str,
    out_dir: Path,
    latest_json_path: Path,
    root: Path | None = None,
) -> tuple[Path, Path]:
    resolved_root = root.resolve() if root else _infer_root_from_paths(latest_json_path=latest_json_path, out_dir=out_dir)
    payload = build_active_task_payload(
        task_id=task_id,
        run_id=run_id,
        status=status,
        out_dir=out_dir,
        latest_json_path=latest_json_path,
        root=resolved_root,
    )
    json_path = active_task_json_path(task_id, resolved_root)
    md_path = active_task_md_path(task_id, resolved_root)
    write_json(json_path, payload)
    write_text(md_path, render_active_task_markdown(payload))
    return json_path, md_path
