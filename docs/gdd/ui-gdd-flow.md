---
GDD-ID: GDD-LASTKING-UI-WIRING-V1
Title: Lastking Chapter 7 UI Wiring Board
Status: Draft
Owner: codex
Last Updated: 2026-04-23
Encoding: UTF-8
Applies-To:
  - .taskmaster/tasks/tasks.json
  - docs/gdd/ui-gdd-flow.md
ADR-Refs:
  - ADR-0011
  - ADR-0018
  - ADR-0021
  - ADR-0022
  - ADR-0004
  - ADR-0019
  - ADR-0001
  - ADR-0006
  - ADR-0008
  - ADR-0015
  - ADR-0005
  - ADR-0023
Test-Refs:
  - Game.Core.Tests/State/GameStateMachineTests.cs
  - Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd
  - Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd
  - Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd
  - Tests.Godot/tests/Integration/test_project_bootstrap_restart_stability.gd
  - Tests.Godot/tests/Integration/test_project_bootstrap_windows_sdk_resolution.gd
  - Tests.Godot/tests/Integration/test_project_creation_csharp_mode.gd
  - Tests.Godot/tests/Integration/test_project_structure_referenced_assets.gd
---

# Lastking Chapter 7 UI Wiring Board

## 1. Design Goals

### 1.1 Experience Pillars
- Stable entry: startup, continue, and runtime entry must be explicit and recoverable.
- Readable loop: phase, pressure, resources, HP, prompts, and outcomes must be understandable from the UI alone.
- Explainable systems: config, governance, save, migration, and audit state must have visible ownership instead of hiding in logs.
- Deterministic recovery: failure, invalid action, persistence, and fallback states must be reproducible and visible.

### 1.2 Target Use
- Provide one governed planning surface for all currently completed task capabilities.
- Keep Chapter 7 focused on player-facing or operator-facing surface ownership before polish-only work.

## 2. Core Player Loop

1. Launch or continue from a stable entry surface.
2. Enter the runtime loop with readable phase, timing, pressure, and survival state.
3. Interact with combat, economy, progression, and meta systems through governed surfaces.
4. Resolve win, loss, save/load, config-governance, and migration outcomes with visible feedback.

## 3. Completed Capability Inventory

| Capability Slice | Audience | Task IDs | Player-Facing Meaning | Primary UI Need |
| --- | --- | --- | --- | --- |
| Entry And Bootstrap | player-facing | T01, T11, T21 | Show canonical startup path, valid continue behavior, and explicit startup failure recovery | MainMenu / Boot Flow |
| Core Loop State And Outcome | player-facing | T03, T07, T08, T09, T10, T18, T19, T23, T24 | Render readable phase, timer, HP, reward, prompt, and win/lose state from runtime events | HUD / Prompt / Outcome Surfaces |
| Combat Pressure And Interaction | player-facing | T04, T05, T06, T20, T22 | Render enemy pressure, targeting, combat outcomes, and camera interaction without hidden state | Combat HUD / Pressure / Camera Feedback |
| Economy Build And Progression | player-facing | T12, T13, T14, T15, T16, T17 | Render deterministic resource, build, queue, upgrade, and progression changes with clear invalid-state feedback | Resource / Build / Progression Panels |
| Meta Systems And Platform | player-facing or mixed | T25, T26, T27, T28, T29, T30 | Render persistence, localization, audio, performance, and platform status on governed player-visible surfaces | Settings / Save / Meta Surfaces |
| Config Governance And Audit | operator-facing or mixed | T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40 | Render active config, schema status, fallback policy, migration status, and audit metadata without relying on logs-only evidence | Config Summary / Audit / Migration Surfaces |

## 4. Flow Recomposition

### Entry And Bootstrap

- T01 `Establish baseline Godot 4.5.1 C# Windows project`
- T11 `Refine baseline bootstrap with main-scene and structure standards`
- T21 `Lock Windows export profile and Steam runtime startup validation`
### Core Loop State And Outcome

- T03 `Develop runtime state machine for Day/Night cycles`
- T07 `Set up castle HP and loss condition`
- T08 `Develop win condition and game progression`
- T09 `Create basic UI for day/night and HP display`
- T10 `Integrate and test full core loop`
- T18 `Implement Reward System with Nightly Choices`
- T19 `Add Day/Night Cycle and Game Win/Lose Conditions`
- T23 `Develop Runtime Speed Controls (Pause, 1x, 2x) with Timer Freeze`
- T24 `Create UI Feedback System for Invalid Actions and Errors`
### Combat Pressure And Interaction

- T04 `Implement wave budget and channel system`
- T05 `Create enemy spawning system with cadence`
- T06 `Implement enemy AI with target priority and pathing`
- T20 `Integrate Combat with Friendly Fire Disabled`
- T22 `Implement Camera and Interaction System with Edge and Keyboard Scrolling`
### Economy Build And Progression

- T12 `Implement Core Resource System with Integer Safety`
- T13 `Design and Implement Building System with Footprint Rules`
- T14 `Implement Economy and Tax System for Residences`
- T15 `Develop Upgrade and Repair System with Constraints`
- T16 `Implement Unit Training Queue in Barracks`
- T17 `Design and Integrate Tech Tree for Unit Stats`
### Meta Systems And Platform

- T25 `Build Save System with Autosave and Migration Handling`
- T26 `Integrate Steam Cloud Save with Account Binding`
- T27 `Implement Achievements System with Deterministic Unlocking`
- T28 `Set Up Localization (i18n) for zh-CN and en-US`
- T29 `Add Audio Settings for Music and SFX Channels`
- T30 `Optimize for Performance Targets (45 FPS 1% Low, 60 FPS Average)`
### Config Governance And Audit

- T02 `Implement config-first balancing system`
- T31 `Scaffold config-contract workspace on existing project`
- T32 `Implement enemy-config.schema.json with validation rules`
- T33 `Implement difficulty-config.schema.json with versioning and lock constraints`
- T34 `Implement spawn-config.schema.json for deterministic wave composition`
- T35 `Implement pressure-normalization.config.schema.json with baseline constants and range checks`
- T36 `Create sample JSON files for all configs with valid data`
- T37 `Implement config validation and fallback handling in C#`
- T38 `Integrate config governance for gameplay tuning`
- T39 `Add config hash/version to battle report metadata for auditability`
- T40 `Implement version migration rule with force migration`

## 5. UI Wiring Matrix

| Feature | UI Surface | Player Action | System Response | Test Refs |
| --- | --- | --- | --- | --- |
| Entry And Bootstrap (T01, T11, T21) | MainMenu / Boot Flow | Launch, continue, retry bootstrap, or enter a run | Show canonical startup path, valid continue behavior, and explicit startup failure recovery | `Game.Core.Tests/State/GameStateMachineTests.cs`, `Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd`, `Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd`, `Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd` |
| Core Loop State And Outcome (T03, T07, T08, T09, T10, T18, T19, T23, T24) | HUD / Prompt / Outcome Surfaces | Play a run, observe timing, rewards, prompts, and terminal transitions | Render readable phase, timer, HP, reward, prompt, and win/lose state from runtime events | `Game.Core.Tests/State/GameStateMachineTests.cs`, `Game.Core.Tests/State/GameStateManagerTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightScopeGuardTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightDesignArtifactTests.cs` |
| Combat Pressure And Interaction (T04, T05, T06, T20, T22) | Combat HUD / Pressure / Camera Feedback | Fight, observe pressure, targeting, pathing, and camera responses | Render enemy pressure, targeting, combat outcomes, and camera interaction without hidden state | `Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs`, `Game.Core.Tests/Services/WaveManagerDeterminismTests.cs`, `Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs`, `Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs` |
| Economy Build And Progression (T12, T13, T14, T15, T16, T17) | Resource / Build / Progression Panels | Spend resources, place/build, train, upgrade, repair, or pick rewards | Render deterministic resource, build, queue, upgrade, and progression changes with clear invalid-state feedback | `Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs`, `Game.Core.Tests/Engine/GameEngineCoreEventTests.cs`, `Game.Core.Tests/Services/ResourceManagerTests.cs`, `Game.Core.Tests/Services/ResourceManagerEventTests.cs` |
| Meta Systems And Platform (T25, T26, T27, T28, T29, T30) | Settings / Save / Meta Surfaces | Save, load, localize, tune audio, or inspect platform/runtime status | Render persistence, localization, audio, performance, and platform status on governed player-visible surfaces | `Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd`, `Tests.Godot/tests/Adapters/Db/test_savegame_update_overwrite_cross_restart.gd`, `Tests.Godot/tests/Adapters/Save/test_save_manager_daystart_autosave.gd`, `Tests.Godot/tests/Adapters/Db/test_savegame_persistence_cross_restart.gd` |
| Config Governance And Audit (T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40) | Config Summary / Audit / Migration Surfaces | Inspect config state, validation, governance, migration, and report metadata | Render active config, schema status, fallback policy, migration status, and audit metadata without relying on logs-only evidence | `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `Game.Core.Tests/Domain/GameConfigTests.cs`, `Tests.Godot/tests/Security/Hard/test_settings_config_security.gd`, `Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd` |

## 6. Screen And Surface Requirements

### Entry And Bootstrap
- Audience: player-facing.
- Empty state: On Windows, baseline verification requires Godot 4.5.1 .NET editor and a compatible .NET SDK to be installed and detected, with no missing-SDK warning.
- Failure state: After a clean editor restart, two consecutive runs from the canonical root must enter the same startup flow, with any setup-introduced regression causing failure.
- Completion result: Baseline verification passes only if the editor opens the canonical root, C# compilation succeeds, and the configured startup scene runs with its attached C# script.

### Core Loop State And Outcome
- Audience: player-facing.
- Empty state: Day/Night phase durations must be configuration-driven (not hardcoded): default Day=4:00 and Night=2:00; transition is allowed only when the active phase reaches its configured threshold, and transition cannot be manually forced before threshold.
- Failure state: Day/Night runtime updates must be driven by the Godot process loop (_Process and/or _PhysicsProcess) into GameStateManager.UpdateDayNightRuntime; when loop updates are paused or stopped, cycle progression must not advance.
- Completion result: Hard gate: deterministic cycle verification must include a fixed-seed forced-terminal scenario, not only natural Day15 completion paths.

### Combat Pressure And Interaction
- Audience: player-facing.
- Empty state: Wave generation preserves deterministic replay for the same day/config/seed, and wave-budget computation is isolated to wave inputs without cross-system side effects.
- Failure state: Taskmaster #6 is complete only when combat runs demonstrate target selection by the required priority chain and blocked-path fallback attacks whenever routes to priority targets are obstructed.
- Completion result: Changing only one channel budget must change that channel's input budget and alter at least one spawn decision for the same day and seed, while non-target channel outputs remain unchanged.

### Economy Build And Progression
- Audience: player-facing.
- Empty state: During long train/cancel sequences with large integer values, resource totals stay accurate integers, never negative, and without overflow/underflow.
- Failure state: Integer safety is enforced through both static scan and runtime assertion gates; any non-integer resource arithmetic fails immediately and gameplay-visible resource state remains unchanged.
- Completion result: Integer safety is enforced through both static scan and runtime assertion gates; any non-integer resource arithmetic fails immediately and gameplay-visible resource state remains unchanged.

### Meta Systems And Platform
- Audience: player-facing or mixed.
- Empty state: If no autosave file exists, load returns a deterministic failure result, shows explicit user feedback, and leaves runtime state unchanged.
- Failure state: All save writes target only the single autosave slot at user://autosave.save (Windows-safe user path); any write to an alternate slot path fails acceptance.
- Completion result: If no autosave file exists, load returns a deterministic failure result, shows explicit user feedback, and leaves runtime state unchanged.

### Config Governance And Audit
- Audience: operator-facing or mixed.
- Empty state: Loading path must be implemented by runtime file I/O or resource-backed path, and switching artifact content without code changes must change observed gameplay outputs after reload.
- Failure state: Hard gate: malformed JSON/INI input must deterministically fail with a fixed reason code, and parse failure must not advance runtime config snapshot (must keep last-known-good or documented fallback).
- Completion result: Hard gate: malformed JSON/INI input must deterministically fail with a fixed reason code, and parse failure must not advance runtime config snapshot (must keep last-known-good or documented fallback).

- Operator-facing read surfaces are allowed when player-facing interaction is not appropriate.

## 7. Screen-Level Contracts

### 7.1 MainMenu And Boot Flow
- Covered slice: Entry And Bootstrap.
- Must show: start, continue, retry bootstrap, and platform-start validation state.
- Must not hide: startup failure, continue-gate denial, or export/runtime startup issues behind logs only.
- Validation focus: boot path, continue gate, retry flow, and startup validation evidence.

### 7.2 Runtime HUD And Outcome Surfaces
- Covered slice: Core Loop State And Outcome.
- Must show: phase, timer, HP, prompts, reward entry, invalid-action prompts, speed state, and terminal outcomes.
- Must not hide: terminal or prompt state transitions that occur without visible HUD or outcome feedback.
- Validation focus: HUD state changes, prompts, reward visibility, and win/lose transitions.

### 7.3 Combat Pressure And Interaction Surfaces
- Covered slice: Combat Pressure And Interaction.
- Must show: pressure, spawn cadence, targeting, pathing fallback, combat resolution, and camera interaction state.
- Must not hide: combat pressure or targeting changes that only appear in logs or traces.
- Validation focus: combat feedback, pressure visibility, pathing fallback evidence, and camera interaction smoke checks.

### 7.4 Economy And Progression Panels
- Covered slice: Economy Build And Progression.
- Must show: resource totals, build placement state, queue state, upgrade/repair state, tech state, and progression results.
- Must not hide: invalid spend/build/progression transitions without governed feedback.
- Validation focus: resource determinism, build validation, queue behavior, and progression surface evidence.

### 7.5 Save, Settings, And Meta Surfaces
- Covered slice: Meta Systems And Platform.
- Must show: save/load status, cloud state, localization state, audio state, performance state, and platform/runtime status.
- Must not hide: persistence or settings failures that are only visible in lower-level logs.
- Validation focus: save/load flow, cloud sync, localization/audio controls, and platform status visibility.

### 7.6 Config Audit And Migration Surfaces
- Covered slice: Config Governance And Audit.
- Must show: active config, schema status, fallback status, migration state, config audit metadata, and report metadata.
- Must not hide: validation, fallback, or migration outcomes that do not surface on a governed read surface.
- Validation focus: config validation, governance, migration, and audit metadata evidence.

## 8. Screen State Matrix

| Screen Group | Entry State | Interaction State | Failure State | Recovery / Exit |
| --- | --- | --- | --- | --- |
| MainMenu And Boot Flow | show start, continue, and startup readiness before any run begins. | allow start, continue, retry bootstrap, and acknowledgement of startup state. | show startup failure, continue denial, or runtime-start validation failure explicitly. | retry bootstrap, acknowledge, or return to menu. |
| Runtime HUD And Outcome Surfaces | show no active run state until runtime data is available. | show phase, timer, HP, prompts, reward entry, invalid-action prompts, speed state, and terminal outcomes. | show prompt/terminal failure state instead of leaving the HUD stale or blank. | acknowledge outcome, continue the run, or return to menu. |
| Combat Pressure And Interaction Surfaces | show no active combat state until combat data and camera ownership are ready. | show pressure, targeting, pathing fallback, combat resolution, and camera interaction state. | show blocked targeting, missing path, or hidden pressure failure explicitly. | retry, acknowledge, or return to the governed combat-ready surface. |
| Economy And Progression Panels | show no owned economy state until resource/build/progression data is available. | show resource totals, build placement state, queue state, upgrade/repair state, tech state, and progression results. | show invalid spend/build/progression state without mutating deterministic ownership silently. | acknowledge invalid state, retry the action, or return to menu. |
| Save, Settings, And Meta Surfaces | show no persisted/platform state until save, cloud, or settings services are available. | show save/load status, cloud state, localization state, audio state, performance state, and platform/runtime status. | show persistence or settings failure instead of only writing low-level logs. | retry, acknowledge, or return to menu. |
| Config Audit And Migration Surfaces | show no active run state until config, validation, and migration data is available. | show active config, schema status, fallback status, migration state, config audit metadata, and report metadata. | show validation, fallback, or migration failure on the governed read surface. | retry, acknowledge, or return to menu after review. |

## 9. Scope And Non-Goals

- Chapter 7 covers UI or governed visible-surface ownership for every completed task in `.taskmaster/tasks/tasks.json`.
- It does not require final production polish, animation, skinning, or marketing-grade copy.

### 9.1 In Scope

- Surface ownership for startup, loop, combat, economy, meta, and governance capabilities.
- Empty state, failure state, and completion state for each major slice.
- Task alignment and validation references back to completed backlog items.

### 9.2 Non-Goals
- Final UX polish, visual theming, animation tuning, and cosmetic-only layout work.
- Replacing source-of-truth task status outside `.taskmaster/tasks/tasks.json`.

## 10. Unwired UI Feature List

- Entry And Bootstrap: define concrete scene ownership, empty/failure states, and validation evidence for T01, T11, T21.
- Core Loop State And Outcome: define concrete scene ownership, empty/failure states, and validation evidence for T03, T07, T08, T09, T10, T18, T19, T23, T24.
- Combat Pressure And Interaction: define concrete scene ownership, empty/failure states, and validation evidence for T04, T05, T06, T20, T22.
- Economy Build And Progression: define concrete scene ownership, empty/failure states, and validation evidence for T12, T13, T14, T15, T16, T17.
- Meta Systems And Platform: define concrete scene ownership, empty/failure states, and validation evidence for T25, T26, T27, T28, T29, T30.
- Config Governance And Audit: define concrete scene ownership, empty/failure states, and validation evidence for T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40.

## 11. Next UI Wiring Task Candidates

### Candidate Slice MainMenu And Boot Flow

- Matrix link: `## 5. UI Wiring Matrix row Entry And Bootstrap (T01, T11, T21)`.
- Scope: T01, T11, T21.
- UI entry: MainMenu / Boot Flow.
- Candidate type: task-shaped UI wiring spec.
- Screen group: MainMenu And Boot Flow.
- Player action: Launch, continue, retry bootstrap, or enter a run.
- System response: Show canonical startup path, valid continue behavior, and explicit startup failure recovery.
- Empty state: On Windows, baseline verification requires Godot 4.5.1 .NET editor and a compatible .NET SDK to be installed and detected, with no missing-SDK warning.
- Failure state: After a clean editor restart, two consecutive runs from the canonical root must enter the same startup flow, with any setup-introduced regression causing failure.
- Completion result: Baseline verification passes only if the editor opens the canonical root, C# compilation succeeds, and the configured startup scene runs with its attached C# script.
- Requirement IDs: `RQ-RUNTIME-INTERACTION`, `RQ-CORE-LOOP-STATE`, `RQ-ECONOMY-BUILD-RULES`.
- Validation artifact targets: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/ci/<YYYY-MM-DD>/export.log`.
- Suggested standalone surfaces: `MainMenu`, `BootStatusPanel`, `ContinueGateDialog`.
- Test refs: `Game.Core.Tests/State/GameStateMachineTests.cs`, `Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd`, `Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd`, `Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd`.
### Candidate Slice Runtime HUD And Outcome Surfaces

- Matrix link: `## 5. UI Wiring Matrix row Core Loop State And Outcome (T03, T07, T08, T09, T10, T18, T19, T23, T24)`.
- Scope: T03, T07, T08, T09, T10, T18, T19, T23, T24.
- UI entry: HUD / Prompt / Outcome Surfaces.
- Candidate type: task-shaped UI wiring spec.
- Screen group: Runtime HUD And Outcome Surfaces.
- Player action: Play a run, observe timing, rewards, prompts, and terminal transitions.
- System response: Render readable phase, timer, HP, reward, prompt, and win/lose state from runtime events.
- Empty state: Day/Night phase durations must be configuration-driven (not hardcoded): default Day=4:00 and Night=2:00; transition is allowed only when the active phase reaches its configured threshold, and transition cannot be manually forced before threshold.
- Failure state: Day/Night runtime updates must be driven by the Godot process loop (_Process and/or _PhysicsProcess) into GameStateManager.UpdateDayNightRuntime; when loop updates are paused or stopped, cycle progression must not advance.
- Completion result: Hard gate: deterministic cycle verification must include a fixed-seed forced-terminal scenario, not only natural Day15 completion paths.
- Requirement IDs: `RQ-RUNTIME-SPEED-MODES`, `RQ-RUNTIME-ERROR-FEEDBACK`, `RQ-COMBAT-QUEUE-TECH`, `RQ-COMBAT-QUEUE-TECH`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`.
- Validation artifact targets: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json`.
- Suggested standalone surfaces: `RuntimeHud`, `OutcomePanel`, `RuntimePromptPanel`.
- Test refs: `Game.Core.Tests/State/GameStateMachineTests.cs`, `Game.Core.Tests/State/GameStateManagerTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightScopeGuardTests.cs`, `Game.Core.Tests/Tasks/Task3DayNightDesignArtifactTests.cs`.
### Candidate Slice Combat Pressure And Interaction Surfaces

- Matrix link: `## 5. UI Wiring Matrix row Combat Pressure And Interaction (T04, T05, T06, T20, T22)`.
- Scope: T04, T05, T06, T20, T22.
- UI entry: Combat HUD / Pressure / Camera Feedback.
- Candidate type: task-shaped UI wiring spec.
- Screen group: Combat Pressure And Interaction Surfaces.
- Player action: Fight, observe pressure, targeting, pathing, and camera responses.
- System response: Render enemy pressure, targeting, combat outcomes, and camera interaction without hidden state.
- Empty state: Wave generation preserves deterministic replay for the same day/config/seed, and wave-budget computation is isolated to wave inputs without cross-system side effects.
- Failure state: Taskmaster #6 is complete only when combat runs demonstrate target selection by the required priority chain and blocked-path fallback attacks whenever routes to priority targets are obstructed.
- Completion result: Changing only one channel budget must change that channel's input budget and alter at least one spawn decision for the same day and seed, while non-target channel outputs remain unchanged.
- Requirement IDs: `RQ-CAMERA-SCROLL`, `RQ-COMBAT-QUEUE-TECH`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`, `RQ-CORE-LOOP-STATE`.
- Validation artifact targets: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json`.
- Suggested standalone surfaces: `CombatHud`, `PressurePanel`, `CameraControlOverlay`.
- Test refs: `Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs`, `Game.Core.Tests/Services/WaveManagerDeterminismTests.cs`, `Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs`, `Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs`.
### Candidate Slice Economy And Progression Panels

- Matrix link: `## 5. UI Wiring Matrix row Economy Build And Progression (T12, T13, T14, T15, T16, T17)`.
- Scope: T12, T13, T14, T15, T16, T17.
- UI entry: Resource / Build / Progression Panels.
- Candidate type: task-shaped UI wiring spec.
- Screen group: Economy And Progression Panels.
- Player action: Spend resources, place/build, train, upgrade, repair, or pick rewards.
- System response: Render deterministic resource, build, queue, upgrade, and progression changes with clear invalid-state feedback.
- Empty state: During long train/cancel sequences with large integer values, resource totals stay accurate integers, never negative, and without overflow/underflow.
- Failure state: Integer safety is enforced through both static scan and runtime assertion gates; any non-integer resource arithmetic fails immediately and gameplay-visible resource state remains unchanged.
- Completion result: Integer safety is enforced through both static scan and runtime assertion gates; any non-integer resource arithmetic fails immediately and gameplay-visible resource state remains unchanged.
- Requirement IDs: `RQ-COMBAT-QUEUE-TECH`, `RQ-COMBAT-QUEUE-TECH`, `RQ-ECONOMY-BUILD-RULES`, `RQ-ECONOMY-BUILD-RULES`, `RQ-ECONOMY-BUILD-RULES`, `RQ-ECONOMY-BUILD-RULES`.
- Validation artifact targets: `logs/unit/<YYYY-MM-DD>/coverage.json`.
- Suggested standalone surfaces: `ResourcePanel`, `BuildPanel`, `ProgressionPanel`.
- Test refs: `Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs`, `Game.Core.Tests/Engine/GameEngineCoreEventTests.cs`, `Game.Core.Tests/Services/ResourceManagerTests.cs`, `Game.Core.Tests/Services/ResourceManagerEventTests.cs`.
### Candidate Slice Save, Settings, And Meta Surfaces

- Matrix link: `## 5. UI Wiring Matrix row Meta Systems And Platform (T25, T26, T27, T28, T29, T30)`.
- Scope: T25, T26, T27, T28, T29, T30.
- UI entry: Settings / Save / Meta Surfaces.
- Candidate type: task-shaped UI wiring spec.
- Screen group: Save, Settings, And Meta Surfaces.
- Player action: Save, load, localize, tune audio, or inspect platform/runtime status.
- System response: Render persistence, localization, audio, performance, and platform status on governed player-visible surfaces.
- Empty state: If no autosave file exists, load returns a deterministic failure result, shows explicit user feedback, and leaves runtime state unchanged.
- Failure state: All save writes target only the single autosave slot at user://autosave.save (Windows-safe user path); any write to an alternate slot path fails acceptance.
- Completion result: If no autosave file exists, load returns a deterministic failure result, shows explicit user feedback, and leaves runtime state unchanged.
- Requirement IDs: `RQ-I18N-LANG-SWITCH`, `RQ-AUDIO-CHANNEL-SETTINGS`, `RQ-PERF-GATE`, `RQ-SAVE-MIGRATION-CLOUD`, `RQ-SAVE-MIGRATION-CLOUD`, `RQ-SAVE-MIGRATION-CLOUD`.
- Validation artifact targets: `logs/ci/<YYYY-MM-DD>/save-migration/report.json`, `logs/ci/<YYYY-MM-DD>/steam-cloud/report.json`, `logs/ci/<YYYY-MM-DD>/achievements/report.json`, `logs/e2e/<YYYY-MM-DD>/settings/summary.json`.
- Suggested standalone surfaces: `SettingsMenu`, `SavePanel`, `RunSummaryPanel`.
- Test refs: `Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd`, `Tests.Godot/tests/Adapters/Db/test_savegame_update_overwrite_cross_restart.gd`, `Tests.Godot/tests/Adapters/Save/test_save_manager_daystart_autosave.gd`, `Tests.Godot/tests/Adapters/Db/test_savegame_persistence_cross_restart.gd`.
### Candidate Slice Config Audit And Migration Surfaces

- Matrix link: `## 5. UI Wiring Matrix row Config Governance And Audit (T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40)`.
- Scope: T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40.
- UI entry: Config Summary / Audit / Migration Surfaces.
- Candidate type: task-shaped UI wiring spec.
- Screen group: Config Audit And Migration Surfaces.
- Player action: Inspect config state, validation, governance, migration, and report metadata.
- System response: Render active config, schema status, fallback policy, migration status, and audit metadata without relying on logs-only evidence.
- Empty state: Loading path must be implemented by runtime file I/O or resource-backed path, and switching artifact content without code changes must change observed gameplay outputs after reload.
- Failure state: Hard gate: malformed JSON/INI input must deterministically fail with a fixed reason code, and parse failure must not advance runtime config snapshot (must keep last-known-good or documented fallback).
- Completion result: Hard gate: malformed JSON/INI input must deterministically fail with a fixed reason code, and parse failure must not advance runtime config snapshot (must keep last-known-good or documented fallback).
- Requirement IDs: `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CONFIG-CONTRACT-GOV`, `RQ-CORE-LOOP-STATE`.
- Validation artifact targets: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json`, `logs/ci/<YYYY-MM-DD>/config-governance/report.json`.
- Suggested standalone surfaces: `ConfigAuditPanel`, `MigrationStatusDialog`, `ReportMetadataPanel`.
- Test refs: `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `Game.Core.Tests/Domain/GameConfigTests.cs`, `Tests.Godot/tests/Security/Hard/test_settings_config_security.gd`, `Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd`.

## 12. Copy And Accessibility

- Visible text should remain explicit and actionable.
- Failure messages must tell the player or operator what happened and what to do next.
- Do not rely on color only to convey terminal, invalid, or route-selection state.

## 13. Test And Acceptance

- Chapter 7 validation must keep `## 5. UI Wiring Matrix`, `## 10. Unwired UI Feature List`, and `## 11. Next UI Wiring Task Candidates` intact.
- Evidence should resolve back to xUnit, GdUnit, smoke, or CI outputs already referenced by task views.
- Any new UI slice should add or name a concrete validation path before implementation.

### MainMenu And Boot Flow
- Requirement IDs: `RQ-RUNTIME-INTERACTION`, `RQ-CORE-LOOP-STATE`, `RQ-ECONOMY-BUILD-RULES`.
- Expected artifacts: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/ci/<YYYY-MM-DD>/export.log`.
- Evidence fields: platform, profile, status, duration_ms.

### Runtime HUD And Outcome Surfaces
- Requirement IDs: `RQ-RUNTIME-SPEED-MODES`, `RQ-RUNTIME-ERROR-FEEDBACK`, `RQ-COMBAT-QUEUE-TECH`, `RQ-CORE-LOOP-STATE`.
- Expected artifacts: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json`.
- Evidence fields: speed_mode, timers_frozen, resume_tick, status, error_code, message_key, displayed, duration_ms.
- Overlay acceptance notes: T23 速度档位 Pause/1x/2x 切换时，波次计时和关键运行时计时器必须冻结/恢复一致。

### Combat Pressure And Interaction Surfaces
- Requirement IDs: `RQ-CAMERA-SCROLL`, `RQ-COMBAT-QUEUE-TECH`, `RQ-CORE-LOOP-STATE`.
- Expected artifacts: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/unit/<YYYY-MM-DD>/coverage.json`, `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json`.
- Evidence fields: camera_mode, edge_threshold_px, keyboard_vector, clamped.
- Overlay acceptance notes: T22 镜头交互要求边缘滚屏 + 键盘滚屏同时生效，且镜头严格受地图边界约束。

### Economy And Progression Panels
- Requirement IDs: `RQ-COMBAT-QUEUE-TECH`, `RQ-ECONOMY-BUILD-RULES`.
- Expected artifacts: `logs/unit/<YYYY-MM-DD>/coverage.json`.

### Save, Settings, And Meta Surfaces
- Requirement IDs: `RQ-I18N-LANG-SWITCH`, `RQ-AUDIO-CHANNEL-SETTINGS`, `RQ-PERF-GATE`, `RQ-SAVE-MIGRATION-CLOUD`.
- Expected artifacts: `logs/ci/<YYYY-MM-DD>/save-migration/report.json`, `logs/ci/<YYYY-MM-DD>/steam-cloud/report.json`, `logs/ci/<YYYY-MM-DD>/achievements/report.json`, `logs/e2e/<YYYY-MM-DD>/settings/summary.json`.
- Evidence fields: save_version, migration_path, result, error_code, account_id_hash, sync_direction, conflict_policy, achievement_id.
- Overlay acceptance notes: T28 语言切换至少覆盖 zh-CN/en-US，切换后界面文本即时生效并持久化。

### Config Audit And Migration Surfaces
- Requirement IDs: `RQ-CONFIG-CONTRACT-GOV`, `RQ-CORE-LOOP-STATE`.
- Expected artifacts: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`, `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json`, `logs/ci/<YYYY-MM-DD>/config-governance/report.json`.
- Evidence fields: config_hash, schema_version, fallback_used, status.
- Overlay acceptance notes: Enemy targeting priority obeys nearest blocker fallback policy from ADR-0031.


## 14. Task Alignment

- Completed task count currently expected by Chapter 7: 40.
- Chapter 7 uses `.taskmaster/tasks/tasks.json` as the completion-state SSoT.
- View files remain enrichment sources for test refs, acceptance, labels, and contract context.

