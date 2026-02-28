#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Domain contracts check (template-friendly, deterministic).

This script scans `Game.Core/Contracts/**/*.cs` and validates event contract conventions:

- `public const string EventType = ...;` supports:
  - string literal (`"core.example.created"`)
  - EventTypes reference (`EventTypes.ExampleCreated`)
- Event type naming supports ADR-0004 families:
  - core.*.*
  - ui.menu.*
  - screen.*.*
- XML doc line `Domain event: <type>` should match EventType (warning).
- EventType values should be unique across Contracts.

Outputs:
  - JSON report (default): logs/ci/<YYYY-MM-DD>/domain-contracts-check/summary.json

Exit codes:
  - 0: ok (or skipped when Contracts dir not found)
  - 1: issues found
  - 2: unexpected error
"""

from __future__ import annotations

import argparse
import json
import os
import re
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Any


EVENT_TYPE_ASSIGN_RE = re.compile(
    r"\bpublic\s+const\s+string\s+EventType\s*=\s*([^;]+)\s*;",
    re.MULTILINE,
)
EVENT_TYPES_CONST_RE = re.compile(
    r"\bpublic\s+const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*\"([^\"]+)\"\s*;",
    re.MULTILINE,
)
DOC_DOMAIN_EVENT_RE = re.compile(r"\bDomain\s+event:\s*([a-z0-9._]+)\b", re.IGNORECASE)
EVENT_TYPE_LITERAL_RE = re.compile(r"\"([^\"]+)\"")
EVENT_TYPE_REF_RE = re.compile(r"EventTypes\.([A-Za-z_][A-Za-z0-9_]*)")


@dataclass(frozen=True)
class Finding:
    file: str
    event_type: str
    ok: bool
    issues: list[str]
    warnings: list[str]


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _to_posix(path: Path) -> str:
    return str(path).replace("\\", "/")


def _default_out_path(root: Path) -> Path:
    day = date.today().strftime("%Y-%m-%d")
    return root / "logs" / "ci" / day / "domain-contracts-check" / "summary.json"


def _iter_contract_files(contracts_dir: Path) -> list[Path]:
    files: list[Path] = []
    for p in contracts_dir.rglob("*.cs"):
        if not p.is_file():
            continue
        if any(seg in {"bin", "obj"} for seg in p.parts):
            continue
        files.append(p)
    return sorted(files)


def _load_event_types_map(contracts_dir: Path) -> dict[str, str]:
    event_types_file = contracts_dir / "EventTypes.cs"
    if not event_types_file.exists():
        return {}
    text = event_types_file.read_text(encoding="utf-8", errors="ignore")
    mapping: dict[str, str] = {}
    for name, value in EVENT_TYPES_CONST_RE.findall(text):
        mapping[name] = value
    return mapping


def _resolve_event_type_rhs(rhs: str, event_types_map: dict[str, str]) -> tuple[str | None, str | None]:
    expr = rhs.strip()
    literal = EVENT_TYPE_LITERAL_RE.fullmatch(expr)
    if literal:
        return literal.group(1), None

    ref = EVENT_TYPE_REF_RE.fullmatch(expr)
    if ref:
        key = ref.group(1)
        if key not in event_types_map:
            return None, f"EventType reference EventTypes.{key} not found in EventTypes.cs"
        return event_types_map[key], None

    return None, f"unsupported EventType assignment expression: {expr!r}"


def _validate_event_type(value: str, *, domain_prefix: str) -> list[str]:
    issues: list[str] = []
    s = value.strip()
    if s != value:
        issues.append("event type contains leading/trailing whitespace")

    parts = s.split(".")
    if len(parts) < 3:
        issues.append("event type must have >= 3 dot-separated segments")
        return issues

    token_re = re.compile(r"^[a-z][a-z0-9_]*$")
    for part in parts:
        if not token_re.fullmatch(part):
            issues.append(f"invalid segment: {part!r} (require [a-z][a-z0-9_]*)")

    if s.startswith("ui.menu."):
        return issues
    if s.startswith("screen."):
        return issues
    if not s.startswith(f"{domain_prefix}."):
        issues.append(f"event type prefix must be '{domain_prefix}.', 'ui.menu.', or 'screen.'")

    return issues


def main() -> int:
    ap = argparse.ArgumentParser(description="Domain contracts check (template-friendly).")
    ap.add_argument("--contracts-dir", default="Game.Core/Contracts", help="Contracts root directory (relative to repo root).")
    ap.add_argument(
        "--domain-prefix",
        default=(os.environ.get("DOMAIN_PREFIX") or "core").strip() or "core",
        help="Expected default event type prefix (default from env DOMAIN_PREFIX or 'core').",
    )
    ap.add_argument("--out", default=None, help="Output JSON path. Defaults to logs/ci/<date>/domain-contracts-check/summary.json")
    args = ap.parse_args()

    root = repo_root()
    contracts_dir = root / str(args.contracts_dir)
    out_path = Path(args.out) if args.out else _default_out_path(root)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    if not contracts_dir.exists():
        report = {
            "status": "skipped",
            "reason": f"contracts dir not found: {_to_posix(contracts_dir)}",
            "domain_prefix": args.domain_prefix,
            "findings": [],
        }
        out_path.write_text(json.dumps(report, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
        print(f"DOMAIN_CONTRACTS_CHECK status=skipped out={_to_posix(out_path)}")
        return 0

    contract_files = _iter_contract_files(contracts_dir)
    event_types_map = _load_event_types_map(contracts_dir)

    findings: list[Finding] = []
    all_event_types: dict[str, list[str]] = {}

    for cs in contract_files:
        text = cs.read_text(encoding="utf-8", errors="ignore")
        event_assignments = EVENT_TYPE_ASSIGN_RE.findall(text)
        doc_values = DOC_DOMAIN_EVENT_RE.findall(text)
        doc_value = doc_values[0].strip() if doc_values else None

        for rhs in event_assignments:
            issues: list[str] = []
            warnings: list[str] = []

            event_type, resolve_error = _resolve_event_type_rhs(rhs, event_types_map)
            if resolve_error:
                issues.append(resolve_error)
                event_type = rhs.strip()
            else:
                issues.extend(_validate_event_type(event_type, domain_prefix=args.domain_prefix))
                if doc_value and doc_value.lower() != event_type.strip().lower():
                    warnings.append(f"doc 'Domain event' mismatch: doc={doc_value!r} const={event_type!r}")

                rel = _to_posix(cs.relative_to(root))
                all_event_types.setdefault(event_type, []).append(rel)

            rel = _to_posix(cs.relative_to(root))
            findings.append(Finding(file=rel, event_type=event_type, ok=not issues, issues=issues, warnings=warnings))

    duplicate_event_types = {k: v for k, v in all_event_types.items() if len(v) > 1}
    dup_issues: list[dict[str, Any]] = []
    if duplicate_event_types:
        for event_type, files in sorted(duplicate_event_types.items()):
            dup_issues.append({"event_type": event_type, "files": files})

    issues_count = sum(1 for finding in findings if finding.issues)
    warnings_count = sum(len(finding.warnings) for finding in findings)
    status = "ok" if (issues_count == 0 and not dup_issues) else "fail"

    report = {
        "status": status,
        "domain_prefix": args.domain_prefix,
        "contracts_dir": _to_posix(contracts_dir.relative_to(root)),
        "counts": {
            "files_scanned": len(contract_files),
            "event_type_constants": len(findings),
            "issues": issues_count + (1 if dup_issues else 0),
            "warnings": warnings_count,
            "event_types_map": len(event_types_map),
        },
        "duplicate_event_types": dup_issues,
        "findings": [finding.__dict__ for finding in findings],
    }
    out_path.write_text(json.dumps(report, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")

    print(
        f"DOMAIN_CONTRACTS_CHECK status={status} events={len(findings)} "
        f"issues={report['counts']['issues']} warnings={warnings_count} out={_to_posix(out_path)}"
    )
    return 0 if status == "ok" else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001
        print(f"DOMAIN_CONTRACTS_CHECK status=fail error={exc}")
        raise SystemExit(2)

