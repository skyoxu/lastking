---
GDD-ID: GDD-LASTKING-T1-T46-M1-WIRING-AUDIT
Title: T1-T46 Minimal Feature Audit And M1 Wiring Snapshot
Status: Draft
Owner: codex
Last Updated: 2026-04-30
Encoding: UTF-8
Applies-To:
  - .taskmaster/tasks/tasks.json
  - .taskmaster/tasks/tasks_back.json
  - .taskmaster/tasks/tasks_gameplay.json
  - docs/gdd/ui-gdd-flow.md
  - docs/gdd/ui-gdd-flow.candidates.json
  - Game.Godot/Scenes/Main.tscn
  - Game.Godot/Scripts/Main.gd
  - Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd
  - Tests.Godot/tests/Scenes/Smoke/test_runtime_ui_debug_panel_visibility.gd
  - Tests.Godot/tests/UI/test_main_scene_ui_canvas_layer.gd
ADR-Refs:
  - ADR-0010
  - ADR-0011
  - ADR-0018
  - ADR-0019
  - ADR-0021
  - ADR-0022
  - ADR-0023
  - ADR-0025
Test-Refs:
  - Tests.Godot/tests/Scenes/Smoke/test_glue_connections.gd
  - Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd
  - Tests.Godot/tests/Scenes/Smoke/test_runtime_ui_debug_panel_visibility.gd
  - Tests.Godot/tests/UI/test_main_menu_events.gd
  - Tests.Godot/tests/UI/test_main_menu_settings_button.gd
  - Tests.Godot/tests/UI/test_main_scene_ui_canvas_layer.gd
  - Tests.Godot/tests/UI/test_hud_scene.gd
  - Tests.Godot/tests/UI/test_hud_updates_on_events.gd
  - Tests.Godot/tests/UI/test_hud_config_audit_surfaces.gd
  - Tests.Godot/tests/Integration/test_screen_navigator.gd
  - Tests.Godot/tests/Integration/test_screen_navigation_flow.gd
  - Tests.Godot/tests/Integration/test_settings_event_integration.gd
  - Tests.Godot/tests/Integration/test_audio_settings_real_chain.gd
  - Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd
  - Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd
  - Game.Core.Tests/Domain/GameConfigTests.cs
---

# T1-T46 Minimal Feature Audit And M1 Wiring Snapshot

## 1. Purpose

This document records the current runtime wiring state for tasks T1 through T46.
The focus is runtime-visible ownership rather than historical planning notes.

## 2. Current Summary

As of 2026-04-30:

- T41 through T46 are all marked done.
- Main.tscn mounts runtime UI under RuntimeUi as a CanvasLayer.
- The legacy Publish Event debug menu group is hidden from the normal runtime path.
- Runtime UI no longer drifts with the world camera and leaves the window.
- Task 46 config audit, migration, and report metadata surfaces are available through runtime-facing owned UI.

Evidence:

- logs/ci/2026-04-30/single-task-chapter6-task-46/summary.json
- logs/ci/2026-04-30/gate-bundle/runs/local-060847-998310-8012/hard/summary.json
- logs/ci/2026-04-30/chapter7-ui-wiring-gate/summary.json

## 3. T41-T46 Status Snapshot

| Task | Title | Status | Runtime Judgment |
| --- | --- | --- | --- |
| T41 | Wire UI: MainMenu And Boot Flow | done | main menu and boot flow available |
| T42 | Wire UI: Runtime HUD And Outcome Surfaces | done | HUD, prompt, and outcome surfaces available |
| T43 | Wire UI: Combat Pressure And Interaction Surfaces | done | combat pressure and interaction surfaces available |
| T44 | Wire UI: Economy And Progression Panels | done | resource, build, and progression surfaces available |
| T45 | Wire UI: Save, Settings, And Meta Surfaces | done | settings and meta surfaces available |
| T46 | Wire UI: Config Audit And Migration Surfaces | done | config audit, migration, and report metadata surfaces available |

## 4. Task 46 Closure Notes

This closure also included two direct runtime fixes.

1. Runtime UI container correction.
   RuntimeUi was moved under a CanvasLayer so it is isolated from world camera movement.

2. Debug menu noise reduction.
   The Publish Event menu group is hidden while other menu actions remain clickable.

## 5. Gate Evidence

### 5.1 Chapter 6

Task 46 chapter 6 summary is complete.
The relevant fields show pipeline_clean, continue, and approval not needed.

### 5.2 Gate Bundle

Gate Bundle hard status is ok with zero failed gates in the referenced clean run.

### 5.3 Chapter 7

chapter7-ui-wiring-gate status is ok.
The required UI wiring sections and done task references are present.

### 5.4 Direct Test Coverage For This Fix

The affected runtime fix is covered by targeted tests that verify:

- RuntimeUi is a CanvasLayer.
- The hidden debug group does not break the main scene smoke path.
- Runtime screen, settings, audio, and glue routes remain usable.

## 6. Player-Facing Impact

Within the T41-T46 wiring scope, the repository now provides a valid minimal runtime experience:

- main menu and boot entry
- runtime HUD, prompts, and outcome surfaces
- combat, economy, build, progression, settings, and meta readouts
- config audit, migration status, and report metadata without requiring log inspection only

## 7. Remaining Boundary

T41-T46 should now be treated as closed.
This does not mean every larger BMAD product ambition is fully implemented.
Some wider product, meta, and expansion features still remain partial at repository level.

## 8. Final Judgment

- T41-T46 can be treated as complete.
- T46 can be treated as complete and status-synchronized.
- The M1 minimal runtime wiring loop is now runnable, verifiable, and auditable.
