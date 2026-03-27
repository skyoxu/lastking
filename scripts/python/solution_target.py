#!/usr/bin/env python3
"""Solution target resolution helpers."""

from __future__ import annotations

from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_solution_arg(solution: str, *, root: Path | None = None) -> str:
    raw = str(solution or "").strip()
    if raw and raw.lower() != "auto":
        return raw

    resolved_root = root or repo_root()
    candidates = sorted(resolved_root.glob("*.sln"))
    if not candidates:
        return "Game.sln"

    preferred_names = (
        f"{resolved_root.name}.sln",
        f"{resolved_root.name.lower()}.sln",
        "Game.sln",
        "GodotGame.sln",
    )
    lowered = {candidate.name.lower(): candidate.name for candidate in candidates}
    for preferred in preferred_names:
        hit = lowered.get(preferred.lower())
        if hit:
            return hit

    return candidates[0].name

