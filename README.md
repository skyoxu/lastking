# lastking (Godot 4.5.1 + C#)

`lastking` is a Windows-only single-player game project built with Godot 4.5.1 and C# (.NET 8).

## Project Posture

- Delivery profile: `fast-ship`
- Security profile: `host-safe`
- Primary runtime: Windows desktop only

## Quick Start (Windows)

1. Install Godot .NET 4.5.1 and .NET 8 SDK.
2. Set Godot binary path in shell:
   - PowerShell: `$env:GODOT_BIN = "C:\Godot\Godot_v4.5.1-stable_mono_win64.exe"`
3. Restore and build:
   - `dotnet restore Game.sln`
   - `dotnet build Game.sln -c Debug`
4. Optional local hard checks:
   - `py -3 scripts/python/dev_cli.py run-local-hard-checks --godot-bin "$env:GODOT_BIN"`

## Core Repositories and Files

- Taskmaster triplet:
  - `.taskmaster/tasks/tasks.json`
  - `.taskmaster/tasks/tasks_back.json`
  - `.taskmaster/tasks/tasks_gameplay.json`
- PRD input:
  - `.taskmaster/docs/prd.txt`
  - `docs/prd/**`
- Architecture:
  - `docs/architecture/base/**`
  - `docs/architecture/overlays/PRD-lastking-T2/08/**`
- ADR index:
  - `docs/architecture/ADR_INDEX_GODOT.md`

## Commands

- Local hard checks:
  - `py -3 scripts/python/dev_cli.py run-local-hard-checks --godot-bin "$env:GODOT_BIN"`
- Gate bundle only:
  - `py -3 scripts/python/run_gate_bundle.py --mode hard --task-files .taskmaster/tasks/tasks_back.json .taskmaster/tasks/tasks_gameplay.json`
- Task review pipeline:
  - `py -3 scripts/sc/run_review_pipeline.py --task-id <id> --godot-bin "$env:GODOT_BIN"`

## Engineering Rules

- Contracts SSoT: `Game.Core/Contracts/**`
- Contract code stays BCL-only, no `Godot.*` references.
- Domain logic in `Game.Core/**`; engine integration in `Game.Godot/**`.
- Logs and evidence go to `logs/**`.
- Keep docs and tasks aligned through ADR + Base + Overlay + Task refs.
