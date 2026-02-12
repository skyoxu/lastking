---
PRD-ID: PRD-lastking-T2
Title: 测试索引 — Lastking T2
Status: Locked
ADR-Refs:
  - ADR-0005
  - ADR-0025
Test-Refs:
  - Game.Core.Tests/State/GameStateMachineTests.cs
  - Game.Core.Tests/Domain/GameConfigTests.cs
  - Tests.Godot/tests/UI/test_hud_scene.gd
  - Tests.Godot/tests/Integration/test_backup_restore_savegame.gd
---

本页把需求、任务、测试、日志工件绑定到同一张执行表，避免“任务完成但无可审计证据”。

## Test Layers

- Domain Layer (xUnit): deterministic logic, state machine, config parsing.
- Scene Layer (GdUnit4): UI visibility, signals, load/save interaction.
- Integration Layer: save migration and runtime continuity.

## Requirement-to-Test Mapping

| Requirement ID | Taskmaster IDs | Primary Tests | Expected Logs |
| --- | --- | --- | --- |
| RQ-CORE-LOOP | 1-10 | `Game.Core.Tests/State/GameStateMachineTests.cs` | `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json` |
| RQ-ECONOMY-COMBAT | 11-20 | `Game.Core.Tests/Domain/GameConfigTests.cs` | `logs/unit/<YYYY-MM-DD>/coverage.json` |
| RQ-UX-SAVE-PERF | 21-30 | `Tests.Godot/tests/UI/test_hud_scene.gd`, `Tests.Godot/tests/Integration/test_backup_restore_savegame.gd` | `logs/e2e/<YYYY-MM-DD>/` |
| RQ-CONFIG-GOVERNANCE | 31-40 | `Game.Core.Tests/Domain/GameConfigTests.cs` | `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json` |

## Test Execution Matrix (Windows)

- `dotnet test --collect:"XPlat Code Coverage"`
- `py -3 scripts/python/run_gdunit.py --project Tests.Godot --add tests/UI --add tests/Integration --timeout-sec 480`
- `py -3 scripts/python/validate_task_master_triplet.py`
- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`

## Evidence Policy

- 每次变更至少更新 1 条 Test-Refs 指向现存测试文件。
- 每次 CI 至少产出 1 份可机器读取 JSON 摘要用于审计。
- Refactor 阶段不允许空 `test_refs` 映射。
