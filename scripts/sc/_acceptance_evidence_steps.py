#!/usr/bin/env python3
"""
Evidence-oriented acceptance-check steps.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from _step_result import StepResult
from _util import repo_root, run_cmd, today_str, write_json, write_text


def _detect_csharp_creation_mode(root: Path) -> dict[str, Any]:
    project_godot = root / "project.godot"
    project_text = project_godot.read_text(encoding="utf-8", errors="ignore") if project_godot.exists() else ""
    has_dotnet_section = "[dotnet]" in project_text
    has_assembly_name = "project/assembly_name=" in project_text
    has_solution = any(root.glob("*.sln"))
    has_csproj = any(root.glob("*.csproj"))
    is_csharp_mode = has_dotnet_section and has_assembly_name and has_solution and has_csproj
    return {
        "creation_mode_at_bootstrap": "csharp" if is_csharp_mode else "unknown",
        "conversion_required": not is_csharp_mode,
    }


def _canonical_root(path: Path | str) -> str:
    text = str(path).replace("\\", "/").rstrip("/")
    return text.lower()


def _read_text(path_value: Any) -> str:
    if not path_value:
        return ""
    path = Path(str(path_value))
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8", errors="ignore")


def _is_windows_export_config_valid(root: Path) -> bool:
    cfg = root / "export_presets.cfg"
    if not cfg.exists():
        return False
    text = cfg.read_text(encoding="utf-8", errors="ignore")
    return 'platform="Windows Desktop"' in text and 'export_path="build/' in text and '.exe"' in text


def _build_verification_records(root: Path, parsed: dict[str, Any]) -> list[dict[str, str]]:
    steps = {
        str(step.get("name", "")): step
        for step in parsed.get("steps", [])
        if isinstance(step, dict) and step.get("name")
    }
    unit_step = steps.get("unit", {})
    smoke_step = steps.get("smoke", {})
    gdunit_step = steps.get("gdunit-hard", {})
    unit_log_text = _read_text(unit_step.get("log", "")).lower()
    smoke_log_text = _read_text(smoke_step.get("log", "")).lower()
    gdunit_cmd = gdunit_step.get("cmd", []) if isinstance(gdunit_step, dict) else []
    export_test_executed = any("test_windows_export_startup_flow.gd" in str(arg) for arg in gdunit_cmd)
    canonical = _canonical_root(root)
    return [
        {
            "step": "editor_open",
            "status": "success" if int(smoke_step.get("rc", 1)) == 0 and "starting godot" in smoke_log_text else "failed",
            "canonical_root": canonical,
        },
        {
            "step": "csharp_compile",
            "status": "success" if int(unit_step.get("rc", 1)) == 0 and "run_dotnet status=" in unit_log_text and "build failed" not in unit_log_text else "failed",
            "canonical_root": canonical,
        },
        {
            "step": "startup_scene_execution",
            "status": "success" if "smoke pass (marker)" in smoke_log_text else "failed",
            "canonical_root": canonical,
        },
        {
            "step": "export_launch",
            "status": "success" if int(gdunit_step.get("rc", 1)) == 0 and export_test_executed and _is_windows_export_config_valid(root) else "failed",
            "canonical_root": canonical,
        },
    ]


def _resolve_sc_test_dir_for_run_id(root: Path, expected_run_id: str) -> tuple[str, Path]:
    ci_root = root / "logs" / "ci"
    if ci_root.exists():
        for entry in sorted(ci_root.iterdir(), key=lambda p: p.name, reverse=True):
            if not entry.is_dir():
                continue
            if len(entry.name) != 10 or entry.name[4] != "-" or entry.name[7] != "-":
                continue
            run_id_file = entry / "sc-test" / "run_id.txt"
            if not run_id_file.exists():
                continue
            value = run_id_file.read_text(encoding="utf-8", errors="ignore").strip()
            if value == expected_run_id:
                return entry.name, entry / "sc-test"
    date = today_str()
    return date, root / "logs" / "ci" / date / "sc-test"


def step_headless_e2e_evidence(out_dir: Path, *, expected_run_id: str) -> StepResult:
    root = repo_root()
    date, sc_test_dir = _resolve_sc_test_dir_for_run_id(root, expected_run_id)
    sc_test_summary = sc_test_dir / "summary.json"
    sc_test_run_id = sc_test_dir / "run_id.txt"
    e2e_dir = root / "logs" / "e2e" / date / "sc-test" / "gdunit-hard"
    e2e_run_id = e2e_dir / "run_id.txt"

    details: dict[str, Any] = {
        "date": date,
        "expected_run_id": expected_run_id,
        "sc_test_summary": str(sc_test_summary.relative_to(root)).replace("\\", "/"),
        "sc_test_run_id_file": str(sc_test_run_id.relative_to(root)).replace("\\", "/"),
        "e2e_dir": str(e2e_dir.relative_to(root)).replace("\\", "/"),
        "e2e_run_id_file": str(e2e_run_id.relative_to(root)).replace("\\", "/"),
        "gdunit_step": None,
        **_detect_csharp_creation_mode(root),
    }

    if not sc_test_summary.exists():
        write_json(out_dir / "headless-e2e-evidence.json", {**details, "error": "missing_sc_test_summary"})
        return StepResult(name="headless-e2e-evidence", status="fail", rc=1, details={**details, "error": "missing_sc_test_summary"})

    try:
        parsed = json.loads(sc_test_summary.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        write_json(out_dir / "headless-e2e-evidence.json", {**details, "error": f"invalid_sc_test_summary_json: {exc}"})
        return StepResult(
            name="headless-e2e-evidence",
            status="fail",
            rc=1,
            details={**details, "error": f"invalid_sc_test_summary_json: {exc}"},
        )

    run_id_in_summary = parsed.get("run_id") if isinstance(parsed, dict) else None
    details["run_id_in_summary"] = run_id_in_summary
    if str(run_id_in_summary or "") != expected_run_id:
        details["error"] = "run_id_mismatch"
        write_json(out_dir / "headless-e2e-evidence.json", details)
        return StepResult(name="headless-e2e-evidence", status="fail", rc=1, details=details)

    run_id_in_file = None
    if sc_test_run_id.exists():
        run_id_in_file = sc_test_run_id.read_text(encoding="utf-8", errors="ignore").strip()
    details["run_id_in_file"] = run_id_in_file
    if str(run_id_in_file or "") != expected_run_id:
        details["error"] = "run_id_file_mismatch"
        write_json(out_dir / "headless-e2e-evidence.json", details)
        return StepResult(name="headless-e2e-evidence", status="fail", rc=1, details=details)

    gd_step = None
    if isinstance(parsed, dict):
        for s in parsed.get("steps") or []:
            if isinstance(s, dict) and s.get("name") == "gdunit-hard":
                gd_step = s
                break
    details["gdunit_step"] = gd_step
    details["verification_records"] = _build_verification_records(root, parsed if isinstance(parsed, dict) else {})

    ok = True
    if not gd_step or gd_step.get("rc") != 0:
        ok = False
        details["error"] = "gdunit_step_missing_or_failed"

    if not e2e_dir.exists() or not any(e2e_dir.rglob("*")):
        ok = False
        details["error"] = details.get("error") or "e2e_dir_missing_or_empty"

    e2e_run_id_value = None
    if e2e_run_id.exists():
        e2e_run_id_value = e2e_run_id.read_text(encoding="utf-8", errors="ignore").strip()
    details["e2e_run_id_value"] = e2e_run_id_value
    if str(e2e_run_id_value or "") != expected_run_id:
        ok = False
        details["error"] = details.get("error") or "e2e_run_id_mismatch"

    write_json(out_dir / "headless-e2e-evidence.json", details)
    return StepResult(name="headless-e2e-evidence", status="ok" if ok else "fail", rc=0 if ok else 1, details=details)


def step_acceptance_executed_refs(out_dir: Path, *, task_id: int, expected_run_id: str) -> StepResult:
    out_json = out_dir / "acceptance-executed-refs.json"
    cmd = [
        "py",
        "-3",
        "scripts/python/validate_acceptance_execution_evidence.py",
        "--task-id",
        str(task_id),
        "--run-id",
        expected_run_id,
        "--out",
        str(out_json),
    ]
    rc, out = run_cmd(cmd, cwd=repo_root(), timeout_sec=120)
    log_path = out_dir / "acceptance-executed-refs.log"
    write_text(log_path, out)
    return StepResult(name="acceptance-executed-refs", status="ok" if rc == 0 else "fail", rc=rc, cmd=cmd, log=str(log_path))


def step_security_audit_evidence(out_dir: Path, *, expected_run_id: str) -> StepResult:
    out_json = out_dir / "security-audit-executed-evidence.json"
    cmd = [
        "py",
        "-3",
        "scripts/python/validate_security_audit_execution_evidence.py",
        "--run-id",
        expected_run_id,
        "--out",
        str(out_json),
    ]
    rc, out = run_cmd(cmd, cwd=repo_root(), timeout_sec=120)
    log_path = out_dir / "security-audit-executed-evidence.log"
    write_text(log_path, out)
    return StepResult(name="security-audit-executed-evidence", status="ok" if rc == 0 else "fail", rc=rc, cmd=cmd, log=str(log_path))
