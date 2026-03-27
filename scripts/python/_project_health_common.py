#!/usr/bin/env python3
"""Common helpers for project-health scans and dashboard artifacts."""

from __future__ import annotations

import json
import html
import re
from datetime import datetime
from pathlib import Path
from typing import Any


PROJECT_HEALTH_KINDS = (
    "detect-project-stage",
    "doctor-project",
    "check-directory-boundaries",
)

TASK_FILES = ("tasks.json", "tasks_back.json", "tasks_gameplay.json")
ALLOWED_BASE_08_FILES = {"08-crosscutting-and-feature-slices.base.md"}
GODOT_PATTERN = re.compile(r"\busing\s+Godot\b|\bGodot\.", re.MULTILINE)
PRD_PATTERN = re.compile(r"\bPRD-[A-Za-z0-9_-]+\b")

KIND_LABELS = {
    "detect-project-stage": "阶段检测",
    "doctor-project": "仓库体检",
    "check-directory-boundaries": "目录边界检查",
}

STATUS_LABELS = {
    "ok": "正常",
    "warn": "警告",
    "fail": "失败",
    "unknown": "未知",
}

KV_LABELS = {
    "generated_at": "生成时间",
    "history_json": "历史记录文件",
    "latest_json": "最新记录文件",
    "task.done": "任务计数-完成",
    "task.in_progress": "任务计数-进行中",
    "task.other": "任务计数-其他",
    "check.ok": "检查项-正常",
    "check.warn": "检查项-警告",
    "check.fail": "检查项-失败",
    "boundary.violations": "边界违规数",
    "boundary.warnings": "边界警告数",
    "rules_checked": "规则总数",
    "status": "状态",
    "url": "访问地址",
    "host": "主机",
    "port": "端口",
    "pid": "进程ID",
    "started_at": "启动时间",
    "reused": "是否复用已有服务",
    "latest_file": "latest 文件",
    "run_id": "运行ID",
    "task_id": "任务ID",
    "summary_path": "summary 路径",
    "delivery_profile": "交付档位",
    "security_profile": "安全档位",
    "failed_step": "失败步骤",
    "steps.total": "步骤总数",
    "steps.ok": "步骤正常",
    "steps.warn": "步骤警告",
    "steps.fail": "步骤失败",
    "dashboard.html": "仪表盘页面",
    "dashboard.json": "仪表盘JSON",
    "stage.latest": "阶段检测latest",
    "doctor.latest": "仓库体检latest",
    "boundaries.latest": "边界检查latest",
}


def now_local() -> datetime:
    return datetime.now().astimezone()


def today_str(now: datetime | None = None) -> str:
    stamp = now or now_local()
    return stamp.strftime("%Y-%m-%d")


def timestamp_str(now: datetime | None = None) -> str:
    stamp = now or now_local()
    return stamp.strftime("%H%M%S%f")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_root(root: Path | str | None = None) -> Path:
    if root is None:
        return repo_root()
    return Path(root).resolve()


def to_posix(path: Path) -> str:
    return str(path).replace("\\", "/")


def repo_rel(path: Path, *, root: Path) -> str:
    try:
        return to_posix(path.resolve().relative_to(root.resolve()))
    except ValueError:
        return to_posix(path.resolve())


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def history_dir(root: Path, *, now: datetime | None = None) -> Path:
    return root / "logs" / "ci" / today_str(now) / "project-health"


def latest_dir(root: Path) -> Path:
    return root / "logs" / "ci" / "project-health"


def task_triplet_paths(root: Path, parent: Path) -> dict[str, Path]:
    return {name: root / parent / name for name in TASK_FILES}


def has_task_triplet(paths: dict[str, Path]) -> bool:
    return all(path.exists() for path in paths.values())


def load_tasks_payload(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    try:
        payload = read_json(path)
    except (OSError, json.JSONDecodeError):
        return {}
    if not isinstance(payload, dict):
        return {}
    return payload


def extract_tasks(payload: dict[str, Any]) -> list[dict[str, Any]]:
    candidates = []
    if isinstance(payload.get("tasks"), list):
        candidates = payload["tasks"]
    master = payload.get("master")
    if not candidates and isinstance(master, dict) and isinstance(master.get("tasks"), list):
        candidates = master["tasks"]
    return [item for item in candidates if isinstance(item, dict)]


def task_status_counts(root: Path) -> dict[str, int]:
    payload = load_tasks_payload(root / ".taskmaster" / "tasks" / "tasks.json")
    counts = {"in_progress": 0, "done": 0, "other": 0}
    for item in extract_tasks(payload):
        raw = str(item.get("status", "")).strip().lower().replace("-", "_")
        if raw in {"in_progress", "active", "working"}:
            counts["in_progress"] += 1
        elif raw in {"done", "completed", "closed"}:
            counts["done"] += 1
        else:
            counts["other"] += 1
    return counts


def overlay_indexes(root: Path) -> list[Path]:
    return sorted((root / "docs" / "architecture" / "overlays").glob("*/08/_index.md"))


def contract_files(root: Path) -> list[Path]:
    base = root / "Game.Core" / "Contracts"
    if not base.exists():
        return []
    return sorted(path for path in base.rglob("*.cs") if path.is_file())


def unit_test_files(root: Path) -> list[Path]:
    candidates = []
    for rel in ("Game.Core.Tests", "Tests"):
        base = root / rel
        if not base.exists():
            continue
        candidates.extend(path for path in base.rglob("*.cs") if path.is_file() and not path.name.endswith(".uid"))
    return sorted(set(candidates))


def record_markdown(payload: dict[str, Any]) -> str:
    lines = [
        f"# {payload['kind']}",
        "",
        f"- status: {payload.get('status', 'unknown')}",
        f"- summary: {payload.get('summary', '')}",
        f"- generated_at: {payload.get('generated_at', '')}",
    ]
    if "stage" in payload:
        lines.append(f"- stage: {payload['stage']}")
    if payload.get("history_json"):
        lines.append(f"- history_json: {payload['history_json']}")
    return "\n".join(lines).rstrip() + "\n"


def load_latest_records(root: Path) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    for kind in PROJECT_HEALTH_KINDS:
        path = latest_dir(root) / f"{kind}.latest.json"
        if path.exists():
            payload = read_json(path)
            if isinstance(payload, dict):
                records.append(payload)
    return records


def _safe(value: Any) -> str:
    return html.escape(str(value), quote=True)


def _label_for_kind(kind: str) -> str:
    cn = KIND_LABELS.get(kind, "未知模块")
    return f"{cn} ({kind})"


def _label_for_status(status: str) -> str:
    key = str(status or "unknown").lower()
    cn = STATUS_LABELS.get(key, STATUS_LABELS["unknown"])
    return f"{cn} ({key})"


def _label_for_key(key: str) -> str:
    cn = KV_LABELS.get(key)
    return f"{cn} ({key})" if cn else key


def _read_json_if_exists(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    try:
        payload = read_json(path)
    except Exception:
        return {}
    return payload if isinstance(payload, dict) else {}


def _find_latest_local_hard_checks_latest(root: Path) -> tuple[Path | None, dict[str, Any]]:
    candidates = sorted(
        (root / "logs" / "ci").glob("*/local-hard-checks-latest.json"),
        key=lambda item: item.stat().st_mtime,
        reverse=True,
    )
    if not candidates:
        return None, {}
    chosen = candidates[0]
    return chosen, _read_json_if_exists(chosen)


def _render_kv_pairs(pairs: list[tuple[str, Any]]) -> str:
    rows = []
    for key, value in pairs:
        rows.append(
            f"<li><span class=\"k\">{_safe(_label_for_key(key))}</span><code class=\"v\">{_safe(value)}</code></li>"
        )
    return "<ul class=\"kv\">" + "".join(rows) + "</ul>"


def _render_record_details(item: dict[str, Any]) -> str:
    kind = str(item.get("kind", "unknown"))
    blocks: list[str] = []

    blocks.append(
        _render_kv_pairs(
            [
                ("generated_at", item.get("generated_at", "")),
                ("history_json", item.get("history_json", "")),
                ("latest_json", item.get("latest_json", "")),
            ]
        )
    )

    if kind == "detect-project-stage":
        signals = item.get("signals", {}) if isinstance(item.get("signals"), dict) else {}
        task_counts = signals.get("task_status_counts", {})
        if isinstance(task_counts, dict):
            blocks.append(
                _render_kv_pairs(
                    [
                        ("task.done", task_counts.get("done", 0)),
                        ("task.in_progress", task_counts.get("in_progress", 0)),
                        ("task.other", task_counts.get("other", 0)),
                    ]
                )
            )
        signal_rows: list[tuple[str, Any]] = []
        for key in (
            "project_godot",
            "readme",
            "agents",
            "real_task_triplet",
            "example_task_triplet",
            "overlay_indexes",
            "contract_files",
            "unit_test_files",
        ):
            if key in signals:
                signal_rows.append((f"signal.{key}", signals.get(key)))
        if signal_rows:
            blocks.append(_render_kv_pairs(signal_rows))
    elif kind == "doctor-project":
        counts = item.get("counts", {}) if isinstance(item.get("counts"), dict) else {}
        blocks.append(
            _render_kv_pairs(
                [
                    ("check.ok", counts.get("ok", 0)),
                    ("check.warn", counts.get("warn", 0)),
                    ("check.fail", counts.get("fail", 0)),
                ]
            )
        )
        checks = item.get("checks", []) if isinstance(item.get("checks"), list) else []
        selected = [entry for entry in checks if isinstance(entry, dict) and entry.get("status") != "ok"][:8]
        if selected:
            rows = []
            for check in selected:
                rows.append(
                    f"<li><code>{_safe(check.get('id', 'unknown'))}</code> "
                    f"<span class=\"pill { _safe(check.get('status', 'unknown')) }\">{_safe(check.get('status', 'unknown'))}</span> "
                    f"<span>{_safe(check.get('summary', ''))}</span></li>"
                )
            blocks.append("<div class=\"sub\">异常检查项 (non-ok checks)</div><ul class=\"list\">" + "".join(rows) + "</ul>")
    elif kind == "check-directory-boundaries":
        violations = item.get("violations", []) if isinstance(item.get("violations"), list) else []
        warnings = item.get("warnings", []) if isinstance(item.get("warnings"), list) else []
        blocks.append(
            _render_kv_pairs(
                [
                    ("boundary.violations", len(violations)),
                    ("boundary.warnings", len(warnings)),
                    ("rules_checked", len(item.get("rules_checked", [])) if isinstance(item.get("rules_checked"), list) else 0),
                ]
            )
        )
        preview_rows = []
        for entry in (violations[:5] + warnings[:5]):
            if not isinstance(entry, dict):
                continue
            preview_rows.append(
                f"<li><code>{_safe(entry.get('rule_id', 'rule'))}</code> "
                f"<span>{_safe(entry.get('path', ''))}</span></li>"
            )
        if preview_rows:
            blocks.append("<div class=\"sub\">样例发现 (sample findings)</div><ul class=\"list\">" + "".join(preview_rows) + "</ul>")

    blocks.append(
        "<details><summary>原始记录 (raw record)</summary>"
        + f"<pre>{_safe(json.dumps(item, ensure_ascii=False, indent=2))}</pre>"
        + "</details>"
    )
    return "".join(blocks)


def _render_context_panels(root: Path) -> str:
    panels: list[str] = []

    server = _read_json_if_exists(latest_dir(root) / "server.json")
    if server:
        panels.append(
            "\n".join(
                [
                    "<section class=\"panel\">",
                    "<h3>本地服务状态 (Server)</h3>",
                    _render_kv_pairs(
                        [
                            ("status", server.get("status", "")),
                            ("url", server.get("url", "")),
                            ("host", server.get("host", "")),
                            ("port", server.get("port", "")),
                            ("pid", server.get("pid", "")),
                            ("started_at", server.get("started_at", "")),
                            ("reused", server.get("reused", "")),
                        ]
                    ),
                    "</section>",
                ]
            )
        )

    latest_path, latest_payload = _find_latest_local_hard_checks_latest(root)
    if latest_payload:
        summary_path = Path(str(latest_payload.get("summary_path", "")).strip())
        summary = _read_json_if_exists(summary_path) if summary_path else {}
        steps = summary.get("steps", []) if isinstance(summary.get("steps"), list) else []
        ok_steps = sum(1 for step in steps if isinstance(step, dict) and str(step.get("status", "")).lower() == "ok")
        fail_steps = sum(1 for step in steps if isinstance(step, dict) and str(step.get("status", "")).lower() == "fail")
        warn_steps = sum(1 for step in steps if isinstance(step, dict) and str(step.get("status", "")).lower() == "warn")
        panels.append(
            "\n".join(
                [
                    "<section class=\"panel\">",
                    "<h3>最近一次本地硬检查 (Latest Local Hard Checks)</h3>",
                    _render_kv_pairs(
                        [
                            ("latest_file", repo_rel(latest_path, root=root) if latest_path else ""),
                            ("status", latest_payload.get("status", "")),
                            ("run_id", latest_payload.get("run_id", "")),
                            ("task_id", latest_payload.get("task_id", "")),
                            ("summary_path", latest_payload.get("summary_path", "")),
                            ("delivery_profile", summary.get("delivery_profile", "")),
                            ("security_profile", summary.get("security_profile", "")),
                            ("failed_step", summary.get("failed_step", "")),
                            ("steps.total", len(steps)),
                            ("steps.ok", ok_steps),
                            ("steps.warn", warn_steps),
                            ("steps.fail", fail_steps),
                        ]
                    ),
                    "</section>",
                ]
            )
        )

    panels.append(
        "\n".join(
            [
                "<section class=\"panel\">",
                "<h3>关键产物路径 (Artifacts)</h3>",
                _render_kv_pairs(
                    [
                        ("dashboard.html", "logs/ci/project-health/latest.html"),
                        ("dashboard.json", "logs/ci/project-health/latest.json"),
                        ("stage.latest", "logs/ci/project-health/detect-project-stage.latest.json"),
                        ("doctor.latest", "logs/ci/project-health/doctor-project.latest.json"),
                        ("boundaries.latest", "logs/ci/project-health/check-directory-boundaries.latest.json"),
                    ]
                ),
                "</section>",
            ]
        )
    )

    return "".join(panels)


def dashboard_html(records: list[dict[str, Any]], *, generated_at: str, root: Path) -> str:
    overall = "ok"
    if any(item.get("status") == "fail" for item in records):
        overall = "fail"
    elif any(item.get("status") == "warn" for item in records):
        overall = "warn"

    cards = []
    for item in records:
        kind = str(item.get("kind", "unknown"))
        status = str(item.get("status", "unknown"))
        summary = str(item.get("summary", ""))
        extra = []
        if item.get("stage"):
            extra.append(f"<div class=\"meta\">stage: {item['stage']}</div>")
        if item.get("history_json"):
            extra.append(f"<div class=\"meta\">history: {item['history_json']}</div>")
        details_html = _render_record_details(item)
        cards.append(
            "\n".join(
                [
                    f"<section class=\"card {status}\">",
                    f"<h2>{_label_for_kind(kind)}</h2>",
                    f"<div class=\"badge\">{_label_for_status(status)}</div>",
                    f"<p>{_safe(summary)}</p>",
                    *extra,
                    f"<div class=\"meta\">最新记录 (latest json): {kind}.latest.json</div>",
                    f"<div class=\"details\">{details_html}</div>",
                    "</section>",
                ]
            )
        )

    context_panels = _render_context_panels(root)

    return f"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <title>项目健康仪表盘 (Project Health Dashboard)</title>
  <style>
    body {{ font-family: Segoe UI, Arial, sans-serif; background: #f4f6f8; color: #1f2933; margin: 0; }}
    main {{ max-width: 1100px; margin: 0 auto; padding: 24px; }}
    .hero {{ display: flex; justify-content: space-between; align-items: baseline; gap: 16px; }}
    .status {{ padding: 6px 12px; border-radius: 999px; font-weight: 700; text-transform: uppercase; }}
    .status.ok {{ background: #d1fae5; color: #065f46; }}
    .status.warn {{ background: #fef3c7; color: #92400e; }}
    .status.fail {{ background: #fee2e2; color: #991b1b; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; margin-top: 20px; }}
    .card {{ background: #ffffff; border: 1px solid #d2d6dc; border-left-width: 6px; border-radius: 12px; padding: 18px; box-shadow: 0 6px 20px rgba(15, 23, 42, 0.08); }}
    .card.ok {{ border-left-color: #10b981; }}
    .card.warn {{ border-left-color: #f59e0b; }}
    .card.fail {{ border-left-color: #ef4444; }}
    .card h2 {{ margin: 0 0 10px; font-size: 18px; }}
    .badge {{ display: inline-block; margin-bottom: 10px; font-size: 12px; font-weight: 700; text-transform: uppercase; }}
    .meta {{ color: #52606d; font-size: 12px; margin-top: 8px; word-break: break-all; }}
    .details {{ margin-top: 10px; }}
    .kv {{ list-style: none; padding: 0; margin: 0 0 10px; }}
    .kv li {{ display: grid; grid-template-columns: 180px minmax(0, 1fr); gap: 10px; margin: 4px 0; align-items: baseline; }}
    .kv .k {{ color: #52606d; font-size: 12px; }}
    .kv .v {{ display: inline-block; font-size: 12px; word-break: break-all; }}
    .sub {{ font-size: 12px; color: #52606d; margin: 10px 0 4px; text-transform: uppercase; letter-spacing: 0.02em; }}
    .list {{ margin: 0 0 10px 18px; padding: 0; font-size: 12px; }}
    .list li {{ margin: 4px 0; }}
    .pill {{ display: inline-block; padding: 1px 6px; border-radius: 999px; font-size: 11px; text-transform: uppercase; margin-right: 6px; }}
    .pill.warn {{ background: #fef3c7; color: #92400e; }}
    .pill.fail {{ background: #fee2e2; color: #991b1b; }}
    details {{ margin-top: 8px; }}
    summary {{ cursor: pointer; color: #334e68; }}
    pre {{ margin: 8px 0 0; padding: 10px; background: #0f172a; color: #e2e8f0; border-radius: 8px; max-height: 260px; overflow: auto; font-size: 11px; }}
    .panels {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; margin-top: 22px; }}
    .panel {{ background: #ffffff; border: 1px solid #d2d6dc; border-radius: 12px; padding: 16px; box-shadow: 0 4px 14px rgba(15, 23, 42, 0.06); }}
    .panel h3 {{ margin: 0 0 10px; font-size: 16px; }}
    .hint {{ margin-top: 20px; color: #52606d; font-size: 13px; }}
    .note {{ margin-top: 12px; background: #eef2ff; border-left: 4px solid #6366f1; padding: 12px; border-radius: 8px; font-size: 13px; line-height: 1.5; }}
  </style>
</head>
<body>
  <main>
    <div class="hero">
      <div>
        <h1>项目健康仪表盘 (Project Health Dashboard)</h1>
        <div>用于汇总本仓库的阶段检测、仓库体检、目录边界检查结果。</div>
      </div>
      <div class="status {overall}">{_label_for_status(overall)}</div>
    </div>
    <div class="meta">生成时间 (generated_at): {generated_at}</div>
    <div class="note">
      阅读说明：上方三张卡片是三类核心检查；每张卡片内可展开“原始记录 (raw record)”查看完整 JSON。
      下方面板展示本地服务状态、最近一次本地硬检查快照和关键产物路径，便于快速定位问题。
    </div>
    <div class="grid">
      {''.join(cards)}
    </div>
    <div class="panels">
      {context_panels}
    </div>
    <div class="hint">Auto-refresh is disabled. 已关闭自动刷新；请在执行 project-health 或 local-hard-check 后手动刷新页面。</div>
  </main>
</body>
</html>
"""


def refresh_dashboard(root: Path | str | None = None, *, now: datetime | None = None) -> dict[str, Any]:
    resolved_root = resolve_root(root)
    stamp = now or now_local()
    records = load_latest_records(resolved_root)
    overall = "ok"
    if any(item.get("status") == "fail" for item in records):
        overall = "fail"
    elif any(item.get("status") == "warn" for item in records):
        overall = "warn"
    payload = {
        "kind": "project-health-dashboard",
        "status": overall,
        "generated_at": stamp.isoformat(timespec="seconds"),
        "records": [
            {
                "kind": item.get("kind", ""),
                "status": item.get("status", ""),
                "summary": item.get("summary", ""),
                "stage": item.get("stage", ""),
                "latest_json": f"{item.get('kind', '')}.latest.json",
                "history_json": item.get("history_json", ""),
            }
            for item in records
        ],
    }
    latest_root = latest_dir(resolved_root)
    write_json(latest_root / "latest.json", payload)
    write_text(latest_root / "latest.html", dashboard_html(records, generated_at=payload["generated_at"], root=resolved_root))
    return payload


def write_project_health_record(
    *,
    root: Path | str | None,
    kind: str,
    payload: dict[str, Any],
    now: datetime | None = None,
) -> dict[str, str]:
    resolved_root = resolve_root(root)
    stamp = now or now_local()
    history_root = history_dir(resolved_root, now=stamp)
    latest_root = latest_dir(resolved_root)
    history_json = history_root / f"{kind}-{timestamp_str(stamp)}.json"
    latest_json = latest_root / f"{kind}.latest.json"
    latest_md = latest_root / f"{kind}.latest.md"

    record = dict(payload)
    record["kind"] = kind
    record.setdefault("generated_at", stamp.isoformat(timespec="seconds"))
    record["history_json"] = repo_rel(history_json, root=resolved_root)
    record["latest_json"] = repo_rel(latest_json, root=resolved_root)

    write_json(history_json, record)
    write_json(latest_json, record)
    write_text(latest_md, record_markdown(record))
    refresh_dashboard(resolved_root, now=stamp)
    return {
        "history_json": repo_rel(history_json, root=resolved_root),
        "latest_json": repo_rel(latest_json, root=resolved_root),
        "latest_md": repo_rel(latest_md, root=resolved_root),
        "dashboard_html": repo_rel(latest_root / "latest.html", root=resolved_root),
    }
