# lastking Getting Started (Godot + C#, Windows)

This guide is the minimal bootstrap for local development.

## Prerequisites

- Windows 10/11
- Godot .NET 4.5.1
- .NET SDK 8.x
- Python 3 (`py -3` available)

## Environment

PowerShell example:

```powershell
$env:GODOT_BIN = "C:\Godot\Godot_v4.5.1-stable_mono_win64.exe"
```

## Build and Run

```powershell
dotnet restore Game.sln
dotnet build Game.sln -c Debug
```

Optional headless prewarm:

```powershell
& "$env:GODOT_BIN" --headless --path . --quit
```

## Validation

Repo hard gates:

```powershell
py -3 scripts/python/dev_cli.py run-local-hard-checks --godot-bin "$env:GODOT_BIN"
```

Task-scoped review pipeline:

```powershell
py -3 scripts/sc/run_review_pipeline.py --task-id <id> --godot-bin "$env:GODOT_BIN"
```

## Notes

- Put logs and evidence under `logs/**`.
- Keep contracts in `Game.Core/Contracts/**` and avoid `Godot.*` references in contract code.
