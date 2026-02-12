#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Guard archived overlays from being used as active task back-link targets.

Default checks:
1) tasks.json/tasks_back.json/tasks_gameplay.json must not reference archived paths.
2) workflow/prd docs must not reference retired active path
   docs/architecture/overlays/PRD-Guild-Manager/.
3) Archived tree marker files must exist.

Optional strict check:
- --strict-git: fail when git reports working-tree changes under
  docs/architecture/overlays/_archived unless ALLOW_ARCHIVED_OVERLAY_WRITE=1.
"""

from __future__ import annotations

import argparse
import os
import subprocess
from pathlib import Path


ARCHIVED_PREFIX = 'docs/architecture/overlays/_archived/'
RETIRED_ACTIVE_PREFIX = 'docs/architecture/overlays/PRD-Guild-Manager/'


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def read_text(path: Path) -> str:
    return path.read_text(encoding='utf-8')


def check_task_files(root: Path) -> list[str]:
    errors: list[str] = []
    task_files = [
        root / '.taskmaster' / 'tasks' / 'tasks.json',
        root / '.taskmaster' / 'tasks' / 'tasks_back.json',
        root / '.taskmaster' / 'tasks' / 'tasks_gameplay.json',
    ]
    for path in task_files:
        if not path.exists():
            errors.append(f'missing task file: {path.relative_to(root)}')
            continue
        text = read_text(path)
        if ARCHIVED_PREFIX in text:
            errors.append(f'archived overlay path found in task file: {path.relative_to(root)}')
    return errors


def scan_docs_for_retired_overlay(root: Path) -> list[str]:
    errors: list[str] = []
    scan_dirs = [
        root / 'docs' / 'workflows',
        root / 'docs' / 'prd',
    ]
    text_suffixes = {'.md', '.txt', '.json', '.yml', '.yaml'}

    for scan_dir in scan_dirs:
        if not scan_dir.exists():
            continue
        for path in scan_dir.rglob('*'):
            if not path.is_file() or path.suffix.lower() not in text_suffixes:
                continue
            rel = path.relative_to(root)
            rel_str = str(rel).replace('\\', '/')
            text = read_text(path)
            if RETIRED_ACTIVE_PREFIX in text:
                errors.append(f'retired active overlay reference found: {rel_str}')
    return errors


def check_markers(root: Path) -> list[str]:
    errors: list[str] = []
    required = [
        root / 'docs' / 'architecture' / 'overlays' / '_archived' / 'README.md',
        root / 'docs' / 'architecture' / 'overlays' / '_archived' / 'PRD-Guild-Manager' / '08' / 'DEPRECATED.md',
        root / 'docs' / 'architecture' / 'overlays' / '_archived' / 'PRD-Guild-Manager' / '08' / '_index.md',
    ]
    for path in required:
        if not path.exists():
            errors.append(f'missing marker file: {path.relative_to(root)}')
    return errors


def strict_git_check(root: Path) -> list[str]:
    errors: list[str] = []
    if os.environ.get('ALLOW_ARCHIVED_OVERLAY_WRITE', '').strip() == '1':
        return errors

    cmd = ['git', 'status', '--porcelain', '--', 'docs/architecture/overlays/_archived']
    try:
        proc = subprocess.run(cmd, cwd=root, capture_output=True, text=True, check=False)
    except Exception as exc:
        errors.append(f'failed to run git status: {exc}')
        return errors

    output = (proc.stdout or '').strip()
    if output:
        errors.append('strict git check failed: archived overlays have working-tree changes')
    return errors


def main() -> int:
    ap = argparse.ArgumentParser(description='Guard archived overlays from active usage drift.')
    ap.add_argument('--strict-git', action='store_true', help='Fail when archived overlays have local git changes unless ALLOW_ARCHIVED_OVERLAY_WRITE=1.')
    args = ap.parse_args()

    root = repo_root()
    errors: list[str] = []

    errors.extend(check_task_files(root))
    errors.extend(scan_docs_for_retired_overlay(root))
    errors.extend(check_markers(root))

    if args.strict_git:
        errors.extend(strict_git_check(root))

    if errors:
        print('ARCHIVED_OVERLAY_GUARD status=fail')
        for err in errors:
            print(f'- {err}')
        return 1

    print('ARCHIVED_OVERLAY_GUARD status=ok')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
