#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import datetime as dt
import json
from pathlib import Path
from typing import Any


TASKS_JSON = Path(".taskmaster/tasks/tasks.json")
TASKS_BACK = Path(".taskmaster/tasks/tasks_back.json")
TASKS_GAMEPLAY = Path(".taskmaster/tasks/tasks_gameplay.json")
UI_CANDIDATES = Path("docs/gdd/ui-gdd-flow.candidates.json")
OVERLAY_INDEX = "docs/architecture/overlays/PRD-lastking-T2/08/_index.md"
OVERLAY_REFS = [
    "docs/architecture/overlays/PRD-lastking-T2/08/_index.md",
    "docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md",
    "docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md",
    "docs/architecture/overlays/PRD-lastking-T2/08/08-Testing-T2.md",
    "docs/architecture/overlays/PRD-lastking-T2/08/08-Observability-T2.md",
    "docs/architecture/overlays/PRD-lastking-T2/08/ACCEPTANCE_CHECKLIST.md",
]


def _today() -> str:
    return dt.date.today().strftime("%Y-%m-%d")


def _read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def _write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def _load_master_tasks(repo_root: Path) -> list[dict[str, Any]]:
    payload = _read_json(repo_root / TASKS_JSON)
    tasks = payload.get("master", {}).get("tasks", []) if isinstance(payload, dict) else []
    if not isinstance(tasks, list):
        raise ValueError("tasks.json master.tasks must be a list")
    return tasks


def _load_view_tasks(repo_root: Path, rel: Path) -> list[dict[str, Any]]:
    payload = _read_json(repo_root / rel)
    if not isinstance(payload, list):
        raise ValueError(f"{rel} must be a list")
    return payload


def _max_master_id(master_tasks: list[dict[str, Any]]) -> int:
    values = [int(item.get("id", 0)) for item in master_tasks if isinstance(item.get("id"), int)]
    return max(values or [0])


def _existing_chapter7_task_ids(master_tasks: list[dict[str, Any]]) -> dict[str, int]:
    result: dict[str, int] = {}
    for item in master_tasks:
        if not isinstance(item, dict):
            continue
        details = str(item.get("details") or "")
        title = str(item.get("title") or "")
        marker = "Chapter7 Candidate:"
        if marker in details:
            candidate = details.split(marker, 1)[1].splitlines()[0].strip()
            if candidate and isinstance(item.get("id"), int):
                result[candidate] = int(item["id"])
        elif title.startswith("Wire UI: ") and isinstance(item.get("id"), int):
            result[title.removeprefix("Wire UI: ").strip()] = int(item["id"])
    return result


def _priority_for_bucket(bucket: str) -> str:
    if bucket in {"entry", "loop", "combat", "economy"}:
        return "high"
    if bucket in {"meta", "governance"}:
        return "medium"
    return "medium"


def _view_priority(master_priority: str) -> str:
    return {"high": "P1", "medium": "P2", "low": "P3"}.get(master_priority, "P2")


def _layer_for_bucket(bucket: str) -> str:
    return "adapter" if bucket in {"entry", "loop", "combat", "economy", "meta"} else "core"


def _merge_refs(values: list[Any]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    for item in values:
        value = str(item).strip()
        if value and value not in seen:
            seen.add(value)
            out.append(value)
    return out


def _acceptance(candidate: dict[str, Any], test_refs: list[str]) -> list[str]:
    refs = " ".join(test_refs) if test_refs else "docs/gdd/ui-gdd-flow.md"
    screen_group = str(candidate.get("screen_group") or "Chapter 7 UI slice")
    ui_entry = str(candidate.get("ui_entry") or "UI surface")
    player_action = str(candidate.get("player_action") or "the player action").strip()
    system_response = str(candidate.get("system_response") or "the expected system response").strip()
    empty_state = str(candidate.get("empty_state") or "the documented empty state").strip()
    failure_state = str(candidate.get("failure_state") or "the documented failure state").strip()
    completion_result = str(candidate.get("completion_result") or "the documented completion result").strip()
    artifact_targets = _merge_refs(list(candidate.get("validation_artifact_targets") or []))
    requirement_ids = _merge_refs(list(candidate.get("requirement_ids") or []))
    scope_ids = [int(item) for item in candidate.get("scope_task_ids") or [] if isinstance(item, int)]
    scope_refs = str(candidate.get("scope_task_refs") or ", ".join(f"T{item:02d}" for item in scope_ids)).strip()
    has_xunit = any(ref.startswith("Game.Core.Tests/") for ref in test_refs)
    has_gdunit = any(ref.startswith("Tests.Godot/") for ref in test_refs)
    framework_clause = "applicable GdUnit and xUnit evidence"
    if has_xunit and has_gdunit:
        framework_clause = "both referenced GdUnit and xUnit suites"
    elif has_xunit:
        framework_clause = "the referenced xUnit suite and an explicit GdUnit N/A record when no GdUnit case applies"
    elif has_gdunit:
        framework_clause = "the referenced GdUnit suite and an explicit xUnit N/A record when no xUnit case applies"
    artifact_clause = (
        f" and must produce validation evidence at {', '.join(artifact_targets)}"
        if artifact_targets
        else ""
    )
    acceptance = [
        f"{screen_group} is implemented as a concrete Chapter 7 UI wiring slice using {ui_entry}, and the player action is visible end-to-end: {player_action}. Refs: {refs}",
        f"{screen_group} selects or creates the suggested standalone surfaces from docs/gdd/ui-gdd-flow.candidates.json before implementation is accepted. Refs: {refs}",
        f"{screen_group} renders the candidate system response from runtime events: {system_response}. Refs: {refs}",
        f"{screen_group} exposes the candidate empty-state rules without relying on logs-only evidence: {empty_state}. Refs: {refs}",
        f"{screen_group} exposes the candidate failure-state rules without relying on logs-only evidence: {failure_state}. Refs: {refs}",
        f"{screen_group} satisfies the completion result, keeps deterministic domain state behind existing contracts, adds no unrelated gameplay behavior{artifact_clause}: {completion_result}. Refs: {refs}",
    ]
    failure_lower = failure_state.lower()
    if "paused or stopped" in failure_lower and "state remains unchanged" not in failure_lower:
        acceptance.append(
            f"{screen_group} must assert that when _Process/_PhysicsProcess updates are paused or stopped, cycle progression does not advance and runtime phase/timer state remains unchanged. Refs: {refs}"
        )
    if requirement_ids:
        acceptance.append(
            f"{screen_group} acceptance must explicitly map every listed Requirement ID ({', '.join(requirement_ids)}) to concrete behavior, test evidence, or artifact evidence; missing any Requirement ID mapping fails acceptance. Refs: {refs}"
        )
    if scope_refs:
        acceptance.append(
            f"{screen_group} acceptance must explicitly map implementation and verification evidence to each scope item ({scope_refs}); missing any scope mapping fails acceptance, and out-of-scope gameplay changes do not count toward completion. Refs: {refs}"
        )
    acceptance.append(
        f"{screen_group} validation must include {framework_clause}, and Chapter 7 artifact evidence must record auditable pass/fail outcomes for each framework; when one framework has no applicable case, acceptance must explicitly record that framework as N/A with rationale instead of omitting it. Refs: {refs}"
    )
    return acceptance


def _master_task(task_id: int, candidate: dict[str, Any]) -> dict[str, Any]:
    screen_group = str(candidate.get("screen_group") or f"Chapter 7 Slice {task_id}")
    bucket = str(candidate.get("bucket") or "ui")
    test_refs = _merge_refs(list(candidate.get("test_refs") or []))
    requirement_ids = _merge_refs(list(candidate.get("requirement_ids") or []))
    artifact_targets = _merge_refs(list(candidate.get("validation_artifact_targets") or []))
    standalone_surfaces = _merge_refs(list(candidate.get("suggested_standalone_surfaces") or []))
    scope_ids = [int(item) for item in candidate.get("scope_task_ids") or [] if isinstance(item, int)]
    contract_boundary = str(candidate.get("contract_boundary") or "keeps deterministic domain state behind existing contracts, adds no unrelated gameplay behavior").strip()
    priority = _priority_for_bucket(bucket)
    details = "\n".join(
        [
            f"Chapter7 Candidate: {screen_group}",
            f"Source: {UI_CANDIDATES.as_posix()}",
            f"Scope: {candidate.get('scope_task_refs') or ', '.join(f'T{item:02d}' for item in scope_ids)}",
            f"UI entry: {candidate.get('ui_entry') or 'TBD'}",
            f"Candidate type: {candidate.get('candidate_type') or 'task-shaped UI wiring spec'}",
            f"Suggested standalone surfaces: {', '.join(standalone_surfaces) if standalone_surfaces else 'n/a'}",
            f"Player action: {candidate.get('player_action') or 'TBD'}",
            f"System response: {candidate.get('system_response') or 'TBD'}",
            f"Empty state: {candidate.get('empty_state') or 'TBD'}",
            f"Failure state: {candidate.get('failure_state') or 'TBD'}",
            f"Completion result: {candidate.get('completion_result') or 'TBD'}",
            f"Contract boundary: {contract_boundary}",
            f"Requirement IDs: {', '.join(requirement_ids) if requirement_ids else 'n/a'}",
            f"Validation artifact targets: {', '.join(artifact_targets) if artifact_targets else 'n/a'}",
        ]
    )
    return {
        "id": task_id,
        "title": f"Wire UI: {screen_group}",
        "description": f"Create the Chapter 7 UI wiring slice for {screen_group} from docs/gdd/ui-gdd-flow.candidates.json.",
        "details": details,
        "testStrategy": "Validate the UI slice with the referenced GdUnit/xUnit tests and Chapter 7 artifact evidence.",
        "priority": priority,
        "dependencies": scope_ids,
        "status": "pending",
        "subtasks": [],
        "overlay": OVERLAY_INDEX,
        "adrRefs": ["ADR-0010", "ADR-0011", "ADR-0019", "ADR-0025"],
        "archRefs": ["CH02", "CH06", "CH07", "CH10"],
    }


def _view_id(prefix: str, task_id: int) -> str:
    return f"GM-{100 + task_id:04d}" if prefix == "GM" else f"NG-{task_id:04d}"


def _view_task(*, prefix: str, task_id: int, candidate: dict[str, Any], owner: str, story_id: str, source_label: str) -> dict[str, Any]:
    screen_group = str(candidate.get("screen_group") or f"Chapter 7 Slice {task_id}")
    bucket = str(candidate.get("bucket") or "ui")
    test_refs = _merge_refs(list(candidate.get("test_refs") or []))
    scope_ids = [int(item) for item in candidate.get("scope_task_ids") or [] if isinstance(item, int)]
    depends_on = [_view_id(prefix, item) for item in scope_ids]
    priority = _view_priority(_priority_for_bucket(bucket))
    return {
        "id": _view_id(prefix, task_id),
        "story_id": story_id,
        "owner": owner,
        "depends_on": depends_on,
        "taskmaster_exported": prefix == "GM",
        "taskmaster_id": task_id,
        "title": f"Wire UI: {screen_group}",
        "description": f"Create the Chapter 7 UI wiring slice for {screen_group} from docs/gdd/ui-gdd-flow.candidates.json.",
        "status": "pending",
        "priority": priority,
        "layer": _layer_for_bucket(bucket),
        "adr_refs": ["ADR-0010", "ADR-0011", "ADR-0019", "ADR-0025"],
        "chapter_refs": ["CH02", "CH06", "CH07", "CH10"],
        "overlay_refs": OVERLAY_REFS,
        "labels": [source_label, "lastking", "taskmaster-view", "chapter7-ui", bucket],
        "test_refs": test_refs,
        "acceptance": _acceptance(candidate, test_refs),
        "contractRefs": _merge_refs(list(candidate.get("requirement_ids") or [])),
        "ui_wiring_candidate": {
            "source": UI_CANDIDATES.as_posix(),
            "screen_group": screen_group,
            "scope_task_ids": scope_ids,
            "ui_entry": candidate.get("ui_entry") or "",
        },
    }


def _replace_task_by_id(tasks: list[dict[str, Any]], task_id: int, replacement: dict[str, Any]) -> bool:
    for index, item in enumerate(tasks):
        if isinstance(item, dict) and item.get("id") == task_id:
            current_status = item.get("status")
            if current_status:
                replacement["status"] = current_status
            tasks[index] = replacement
            return True
    return False


def _replace_view_task_by_taskmaster_id(
    tasks: list[dict[str, Any]],
    task_id: int,
    replacement: dict[str, Any],
) -> bool:
    for index, item in enumerate(tasks):
        if isinstance(item, dict) and item.get("taskmaster_id") == task_id:
            current_status = item.get("status")
            if current_status:
                replacement["status"] = current_status
            tasks[index] = replacement
            return True
    return False


def create_tasks(*, repo_root: Path, dry_run: bool = False) -> tuple[int, dict[str, Any]]:
    sidecar_path = repo_root / UI_CANDIDATES
    if not sidecar_path.exists():
        payload = {
            "action": "create-chapter7-tasks-from-ui-candidates",
            "status": "fail",
            "reason": "missing_candidate_sidecar",
            "source": UI_CANDIDATES.as_posix(),
        }
        return 1, payload

    sidecar = _read_json(sidecar_path)
    candidates = sidecar.get("candidates", []) if isinstance(sidecar, dict) else []
    if not isinstance(candidates, list):
        raise ValueError("ui-gdd-flow.candidates.json candidates must be a list")

    tasks_json_path = repo_root / TASKS_JSON
    tasks_payload = _read_json(tasks_json_path)
    master = tasks_payload.setdefault("master", {})
    master_tasks = master.setdefault("tasks", [])
    if not isinstance(master_tasks, list):
        raise ValueError("tasks.json master.tasks must be a list")

    back_tasks = _load_view_tasks(repo_root, TASKS_BACK)
    gameplay_tasks = _load_view_tasks(repo_root, TASKS_GAMEPLAY)
    existing_by_candidate = _existing_chapter7_task_ids(master_tasks)
    next_id = _max_master_id(master_tasks) + 1
    created_task_ids: list[int] = []
    existing_task_ids: list[int] = []
    updated_task_ids: list[int] = []

    for candidate in candidates:
        if not isinstance(candidate, dict):
            continue
        screen_group = str(candidate.get("screen_group") or "").strip()
        if not screen_group:
            continue
        existing_id = existing_by_candidate.get(screen_group)
        if existing_id is not None:
            existing_task_ids.append(existing_id)
            _replace_task_by_id(master_tasks, existing_id, _master_task(existing_id, candidate))
            _replace_view_task_by_taskmaster_id(
                back_tasks,
                existing_id,
                _view_task(
                    prefix="NG",
                    task_id=existing_id,
                    candidate=candidate,
                    owner="architecture",
                    story_id="BACKLOG-LASTKING-M1",
                    source_label="backlog",
                ),
            )
            _replace_view_task_by_taskmaster_id(
                gameplay_tasks,
                existing_id,
                _view_task(
                    prefix="GM",
                    task_id=existing_id,
                    candidate=candidate,
                    owner="gameplay",
                    story_id="PRD-LASTKING-v1.2",
                    source_label="prd",
                ),
            )
            updated_task_ids.append(existing_id)
            continue
        task_id = next_id
        next_id += 1
        master_tasks.append(_master_task(task_id, candidate))
        back_tasks.append(
            _view_task(
                prefix="NG",
                task_id=task_id,
                candidate=candidate,
                owner="architecture",
                story_id="BACKLOG-LASTKING-M1",
                source_label="backlog",
            )
        )
        gameplay_tasks.append(
            _view_task(
                prefix="GM",
                task_id=task_id,
                candidate=candidate,
                owner="gameplay",
                story_id="PRD-LASTKING-v1.2",
                source_label="prd",
            )
        )
        created_task_ids.append(task_id)
        existing_by_candidate[screen_group] = task_id

    payload = {
        "ts": dt.datetime.now(dt.timezone.utc).isoformat(),
        "action": "create-chapter7-tasks-from-ui-candidates",
        "status": "ok",
        "source": UI_CANDIDATES.as_posix(),
        "dry_run": dry_run,
        "candidate_count": len(candidates),
        "created_count": len(created_task_ids),
        "created_task_ids": created_task_ids,
        "existing_task_ids": existing_task_ids,
        "updated_count": len(updated_task_ids),
        "updated_task_ids": updated_task_ids,
        "tasks_json": TASKS_JSON.as_posix(),
        "tasks_back": TASKS_BACK.as_posix(),
        "tasks_gameplay": TASKS_GAMEPLAY.as_posix(),
    }
    if not dry_run:
        _write_json(tasks_json_path, tasks_payload)
        _write_json(repo_root / TASKS_BACK, back_tasks)
        _write_json(repo_root / TASKS_GAMEPLAY, gameplay_tasks)
    return 0, payload


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Create Taskmaster triplet tasks from Chapter 7 UI GDD candidates.")
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--out-json", default="")
    args = parser.parse_args(argv)

    repo_root = Path(args.repo_root).resolve()
    rc, payload = create_tasks(repo_root=repo_root, dry_run=bool(args.dry_run))
    out = Path(args.out_json) if args.out_json else (
        repo_root / "logs" / "ci" / _today() / "chapter7-ui-task-creation" / "summary.json"
    )
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(
        "CHAPTER7_CREATE_TASKS "
        f"status={payload['status']} created={payload.get('created_count', 0)} out={str(out).replace('\\\\', '/')}"
    )
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
