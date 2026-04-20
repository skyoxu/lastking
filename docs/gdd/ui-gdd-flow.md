# UI GDD Flow - LASTKING Chapter 7 Wiring Board

## 1. Scope And Single Goal

This document is the Chapter 7 UI wiring board for `lastking`. It converts completed systems into player-facing flows and provides the single planning surface for future UI wiring work.

Single goal: connect the minimum playable loop of `main menu -> start run -> day preparation -> night defense -> castle damage / defeat feedback` before any polish-heavy UI work.

## 2. Player Loop Spine

The current completed systems already support this gameplay spine:

1. Launch the game and enter one canonical runtime root.
2. Load config-driven balance values.
3. Run a deterministic Day/Night state machine.
4. Spawn enemies through wave budget and channel rules.
5. Use castle HP as the only visible failure boundary.

If any of those steps has no UI surface, no feedback, or no player-readable state, the loop is not yet player-complete.

## 3. Completed Capability Inventory

Completion state comes only from `.taskmaster/tasks/tasks.json`. The two view files enrich the task with test, acceptance, and contract context.

| Capability Group | Task IDs | Player-Facing Meaning | Primary UI Need |
| --- | --- | --- | --- |
| Baseline runtime and canonical scene root | T01, T11 | Player can launch into one stable project entry and main scene | MainMenu, boot error feedback, canonical startup surface |
| Config-driven balancing | T02 | Core values come from config instead of hidden constants | Settings summary, run-start parameter preview, config-driven labels |
| Day/Night runtime flow | T03 | Player can see the current phase and transition between day and night | DayNight HUD, phase banner, timer strip |
| Wave budget and spawn channels | T04 | Night pressure, spawn cadence, and channel behavior should be explainable | Night pressure panel, spawn alerts, wave summary |
| Castle HP and defeat boundary | T07 | Player knows when the castle is being damaged and when the run is lost | Castle HP bar, damage feedback, defeat modal/report entry |

## 4. Player Flow Recomposition

### 4.1 Run Entry Flow

Player intent: start the game and understand how a run begins.

1. MainMenu shows Start, Settings, and Quit.
2. Start enters the canonical run bootstrap flow.
3. If startup fails, the player sees an explicit error surface instead of a silent stall.
4. After boot, the player immediately sees phase, timer, and castle HP.

### 4.2 Day Preparation Flow

Player intent: use the daytime window to understand readiness, timing, and strategic state.

1. HUD clearly shows that the run is in Day state.
2. A visible timer or status strip shows remaining preparation time.
3. The player can read the current strategic state from resource and status surfaces.
4. Transition into Night is clearly signaled.

### 4.3 Night Defense Flow

Player intent: understand pressure, incoming enemies, and base survival risk.

1. HUD clearly shows that the run is in Night state.
2. Night pressure or wave budget information is visible.
3. Enemy arrival and ongoing danger are expressed by alerts, counters, or a pressure surface.
4. Castle HP changes are visible immediately.

### 4.4 Defeat Boundary Flow

Player intent: understand why the run ended and what the next action is.

1. Defeat state appears only when the castle HP failure condition is actually met.
2. The defeat surface explains the reason and key run state.
3. The player can return to menu or restart from a clear entry point.

## 5. UI Wiring Matrix

| Feature | UI Surface | Player Action | System Response | Test Refs |
| --- | --- | --- | --- | --- |
| T01/T11 baseline bootstrap | MainMenu / boot surface | Launch game | Load canonical root, attach startup scene, and show visible startup failure feedback when bootstrap fails | `Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd`, `Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd`, `Tests.Godot/tests/Scenes/Smoke/test_main_scene_bindings_persist.gd` |
| T02 config-first balancing | Settings / run-start summary | Open settings or start a run | Present config-driven timing, difficulty, and budget parameters in player-readable form | `Game.Core.Tests/Tasks/Task2ConfigManagerCoverageTests.cs`, `Game.Core.Tests/Tasks/Task2ConfigIsolationGuardTests.cs` |
| T03 day/night runtime state | HUD / phase banner | Enter run and wait for transitions | Show current phase, transition banner, and remaining time | `Game.Core.Tests/Tasks/Task3DayNightStateMachineTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightAcceptanceTests.cs` |
| T04 wave budget and channels | Night HUD / pressure panel | Survive night and observe pressure | Show budget-driven pressure, cadence, and spawn-state feedback | `Game.Core.Tests/Tasks/Task4WaveBudgetAcceptanceTests.cs`, `Game.Core.Tests/Tasks/Task4WaveBudgetChannelRulesTests.cs` |
| T07 castle HP and loss condition | Castle HP bar / defeat modal | Take damage until failure | Update castle HP immediately and enter defeat only when HP reaches zero | `Game.Core.Tests/Tasks/Task7CastleHpLossBoundaryTests.cs`, `Game.Core.Tests/Tasks/Task7CastleHpAcceptanceTests.cs` |

## 6. Screen Contracts

### 6.1 MainMenu

- Must expose a stable path into a real run.
- Must expose settings and quit.
- Must display startup failure feedback when bootstrap cannot continue.

### 6.2 In-Run HUD

- Must display phase, castle HP, and timing state together.
- Day and Night transitions must be visible.
- Night pressure cannot be hidden in logs only.

### 6.3 Defeat Surface

- Must explain the failure reason.
- Must match the actual castle HP failure boundary.
- Must provide a clear return path.

## 7. Explainability And Prompt Contract

- Critical transitions: Day to Night, Night to Day, Defeat.
- Critical danger: castle HP loss, rising pressure, startup failure.
- Prompt style should be explicit and actionable rather than atmospheric-only.

## 8. Flow-Level State And Feedback Contract

- Empty state: before entering a run, show MainMenu instead of a blank scene.
- Failure state: startup failure, invalid config, and defeat must all have visible surfaces.
- Completion state: once in a run, HUD information is visible and coherent.

## 9. Unwired UI Feature List

The following completed systems are still not wired into a full player-facing UI loop:

- T02: config-driven balancing has no governed settings or readable run summary surface.
- T03: day/night runtime exists, but lacks a formal HUD and transition presentation layer.
- T04: wave budget and channel logic exist, but lack a formal pressure surface for players.
- T07: castle HP loss boundary exists, but lacks a formal defeat modal/report surface.
- T01/T11: startup and canonical main scene exist, but the menu information architecture and player-readable failure feedback are incomplete.

## 10. Next UI Wiring Task Candidates

1. MainMenu plus run bootstrap entry with visible startup failure handling.
2. In-run HUD vertical slice for phase, timer, and castle HP.
3. Night pressure panel for wave budget, spawn cadence, and incoming danger.
4. Defeat and report surface for castle HP loss boundary.
5. Config summary or settings surface for player-readable run parameters.

## 11. Current Implementation Audit

- The repo already contains runtime, config, state machine, wave, and castle HP capabilities.
- The repo previously had no governed `docs/gdd/ui-gdd-flow.md` and no Chapter 7 orchestrator.
- The highest-value next wiring work is MainMenu, HUD, and Defeat surfaces.

## 12. Flow Validation Matrix

| Flow | Automated Validation | Manual Validation | Evidence / Output |
| --- | --- | --- | --- |
| Run Entry | `Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd`, `Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd` | Launch game, enter the main scene, verify a visible start entry and no hidden startup failure | GdUnit results and startup logs under `logs/ci/<date>/**` |
| Day/Night HUD | `Game.Core.Tests/Tasks/Task3DayNightStateMachineTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightAcceptanceTests.cs` | Start a run, wait for transition, verify phase and timer surfaces update | xUnit logs and HUD smoke evidence |
| Night Pressure | `Game.Core.Tests/Tasks/Task4WaveBudgetAcceptanceTests.cs`, `Game.Core.Tests/Tasks/Task4WaveBudgetChannelRulesTests.cs` | Observe one night and confirm pressure indicators move with system state | xUnit logs and optional HUD screenshots |
| Castle HP / Defeat | `Game.Core.Tests/Tasks/Task7CastleHpLossBoundaryTests.cs`, `Game.Core.Tests/Tasks/Task7CastleHpAcceptanceTests.cs` | Take castle damage and verify HP feedback plus defeat only at zero HP | xUnit logs and defeat UI evidence |

## 13. Formal GDD And UX Distillation Rules

This document is a Chapter 7 wiring board, not the final screen-by-screen UX spec. A flow can move into formal GDD or UX docs only when:

- At least one automated validation covers the main path.
- At least one manual script verifies the player can interpret the state and feedback.
- Key player-facing feedback is visible on a real UI surface rather than logs only.

## 14. Immediate UI Integration Backlog

- MainMenu information architecture and startup failure copy.
- In-run HUD for phase, timer, and castle HP.
- Night pressure and enemy arrival surfaces.
- Defeat modal and return-to-menu flow.
- Config summary/settings read-only surface.

## 15. Validation Plan

- After every `ui-gdd-flow.md` update, run `py -3 scripts/python/validate_chapter7_ui_wiring.py`.
- After every orchestrator or CLI change, run `py -3 scripts/python/dev_cli.py run-chapter7-ui-wiring --delivery-profile fast-ship --self-check`.
- Before submission, run `py -3 scripts/python/run_gate_bundle.py --mode hard --task-files .taskmaster/tasks/tasks_back.json .taskmaster/tasks/tasks_gameplay.json`.

## 16. Open Questions

- Whether the day preparation surface should evolve together with future building-system tasks.
- Whether night pressure should live in a persistent HUD strip or a staged drawer/panel.
- Whether defeat should go directly to a report surface or first show a short defeat summary.
