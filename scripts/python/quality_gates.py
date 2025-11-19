#!/usr/bin/env python3
"""
Quality gates entry for Windows (Godot+C# variant).

Current minimal implementation:
- Delegates to ci_pipeline.py `all` command, which runs:
  * dotnet tests + coverage (soft gate on coverage)
  * Godot self-check (hard gate)
  * encoding scan (soft gate)

Usage (Windows):
  py -3 scripts/python/quality_gates.py all \
    --solution Game.sln --configuration Debug \
    --godot-bin "C:\\Godot\\Godot_v4.5.1-stable_mono_win64_console.exe" \
    --build-solutions

Exit codes:
  0  all hard gates passed
  1  hard gate failed (dotnet tests or self-check)

This script is designed to be extended in Phase 13 to include
additional gates (GdUnit4 sets, smoke, perf, etc.).
"""

import argparse
import subprocess
import sys


def run_ci_pipeline(solution: str, configuration: str, godot_bin: str, build_solutions: bool) -> int:
    args = [
        "py", "-3", "scripts/python/ci_pipeline.py", "all",
        "--solution", solution,
        "--configuration", configuration,
        "--godot-bin", godot_bin,
    ]
    if build_solutions:
        args.append("--build-solutions")

    proc = subprocess.run(args, text=True)
    return proc.returncode


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_all = sub.add_parser("all", help="run all quality gates (delegates to ci_pipeline.py")
    p_all.add_argument("--solution", default="Game.sln")
    p_all.add_argument("--configuration", default="Debug")
    p_all.add_argument("--godot-bin", required=True)
    p_all.add_argument("--build-solutions", action="store_true")

    args = parser.parse_args()

    if args.cmd == "all":
        return run_ci_pipeline(args.solution, args.configuration, args.godot_bin, args.build_solutions)

    print("Unsupported command", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())

