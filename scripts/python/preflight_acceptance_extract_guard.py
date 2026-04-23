#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
from pathlib import Path
from typing import Any


TASKS_JSON = Path(".taskmaster/tasks/tasks.json")
TASKS_BACK = Path(".taskmaster/tasks/tasks_back.json")
TASKS_GAMEPLAY = Path(".taskmaster/tasks/tasks_gameplay.json")


def _today() -> str:
    return dt.date.today().strftime("%Y-%m-%d")


def _read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def _load_master_task(repo_root: Path, task_id: int) -> dict[str, Any] | None:
    path = repo_root / TASKS_JSON
    if not path.exists():
        return None
    payload = _read_json(path)
    tasks = payload.get("master", {}).get("tasks", []) if isinstance(payload, dict) else []
    for task in tasks if isinstance(tasks, list) else []:
        if isinstance(task, dict) and int(task.get("id") or 0) == int(task_id):
            return task
    return None


def _load_view_rows(repo_root: Path, task_id: int) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for rel in (TASKS_BACK, TASKS_GAMEPLAY):
        path = repo_root / rel
        if not path.exists():
            continue
        payload = _read_json(path)
        if not isinstance(payload, list):
            continue
        for item in payload:
            if isinstance(item, dict) and int(item.get("taskmaster_id") or 0) == int(task_id):
                rows.append(item)
    return rows


def _as_text_list(value: Any) -> list[str]:
    if isinstance(value, list):
        return [str(item).strip() for item in value if str(item).strip()]
    if isinstance(value, str) and value.strip():
        return [value.strip()]
    return []


def _has_refs(acceptance: list[str]) -> bool:
    return any(re.search(r"\bRefs:\s*\S+", item) for item in acceptance)


def _has_state_semantics(master: dict[str, Any], acceptance: list[str]) -> bool:
    blob = "\n".join(
        [
            str(master.get("details") or ""),
            str(master.get("description") or ""),
            "\n".join(acceptance),
        ]
    ).lower()
    return all(token in blob for token in ("empty", "failure", "completion"))


def validate(*, repo_root: Path, task_id: int) -> tuple[int, dict[str, Any]]:
    master = _load_master_task(repo_root, task_id)
    rows = _load_view_rows(repo_root, task_id)
    missing: list[str] = []
    if master is None:
        missing.append("master_task")
    if not rows:
        missing.append("view_rows")

    acceptance: list[str] = []
    test_refs: list[str] = []
    for row in rows:
        acceptance.extend(_as_text_list(row.get("acceptance")))
        test_refs.extend(_as_text_list(row.get("test_refs")))

    if not acceptance:
        missing.append("acceptance")
    if acceptance and not _has_refs(acceptance):
        missing.append("acceptance_refs")
    if not test_refs:
        missing.append("test_refs")
    if master is not None and not _has_state_semantics(master, acceptance):
        missing.append("state_semantics")

    payload = {
        "ts": dt.datetime.now(dt.timezone.utc).isoformat(),
        "action": "preflight-acceptance-extract-guard",
        "status": "ok" if not missing else "fail",
        "task_id": int(task_id),
        "missing": missing,
        "acceptance_count": len(acceptance),
        "test_refs_count": len(test_refs),
        "view_rows_count": len(rows),
        "checked_files": [TASKS_JSON.as_posix(), TASKS_BACK.as_posix(), TASKS_GAMEPLAY.as_posix()],
    }
    return (0 if payload["status"] == "ok" else 1), payload


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Deterministic acceptance preflight before obligations extraction.")
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--task-id", required=True, type=int)
    parser.add_argument("--out-json", default="")
    args = parser.parse_args(argv)

    repo_root = Path(args.repo_root).resolve()
    rc, payload = validate(repo_root=repo_root, task_id=int(args.task_id))
    out = Path(args.out_json) if args.out_json else (
        repo_root / "logs" / "ci" / _today() / "preflight-acceptance-extract-guard" / f"task-{int(args.task_id)}.json"
    )
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(
        "PREFLIGHT_ACCEPTANCE_EXTRACT_GUARD "
        f"status={payload['status']} task={payload['task_id']} missing={len(payload['missing'])} out={str(out).replace('\\\\', '/')}"
    )
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
