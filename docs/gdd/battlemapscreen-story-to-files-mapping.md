---
DOC-ID: DOC-LASTKING-BATTLEMAPSCREEN-STORY-FILE-MAPPING-V1
Title: BattleMapScreen Story To Files Mapping
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Based-On:
  - docs/gdd/battlemapscreen-stories.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/battlemapscreen-gd-responsibility-split-draft.md
---

# BattleMapScreen Story To Files Mapping

## Goal

This document maps the current BattleMapScreen stories to the most likely code and test touchpoints.
It is intended to make task decomposition concrete before implementation starts.

## Core Runtime And Scene Files

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`
- `Game.Godot/Scenes/UI/HUD.tscn`
- `Game.Godot/Scripts/UI/HUD.cs`
- `Game.Godot/Scenes/Main.tscn`
- `Game.Godot/Scripts/Navigation/ScreenNavigator.cs`
- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`
- `Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd`

## Story Mapping

### Story 1.1: Introduce The Stable Battle Screen Frame

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scenes/Main.tscn`
- `Game.Godot/Scenes/UI/HUD.tscn`

Possible secondary files:

- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`

### Story 1.2: Isolate Legacy Prototype And Runtime Ownership

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`
- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`

Possible secondary files:

- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`
- `Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd`

### Story 1.3: Reduce BattleMapScreen.gd To A Coordinator

Primary files:

- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattleRuntimeDebugController.gd`
- `Game.Godot/Scripts/Screens/LegacyPrototypePanelController.gd`

Possible secondary files:

- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`

### Story 1.4: Preserve Existing Global UI Ownership And Localization

Primary files:

- `Game.Godot/Scenes/UI/HUD.tscn`
- `Game.Godot/Scripts/UI/HUD.cs`
- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`

Possible secondary files:

- `Game.Godot/Localization/en-US.json`
- `Game.Godot/Localization/zh-CN.json`

### Story 2.1: Build The Battlefield Region And Slot Layout

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattlefieldView.gd`

Possible secondary files:

- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`

### Story 2.2: Implement Placement Legality Overlays

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/PlacementOverlayController.gd`

Possible secondary files:

- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`

### Story 2.3: Implement Building Selection And Clipped Range Feedback

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattleSelectionController.gd`

Possible secondary files:

- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`

### Story 2.4: Implement Spawn-Side And Path Readability Cues

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattlefieldActorView.gd`

Possible secondary files:

- `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`

### Story 3.1: Implement Battlefield-Local Combat Feedback Surfaces

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattleLocalFeedbackController.gd`

Possible secondary files:

- `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`
- `Game.Godot/Scripts/UI/HUD.cs`

### Story 3.2: Implement Pressure State Mapping With Existing Inputs

Primary files:

- `Game.Godot/Scripts/UI/HUD.cs`

Possible secondary files:

- `Game.Godot/Scenes/UI/HUD.tscn`
- `Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd`

### Story 3.3: Implement Bottom-Bar Combat State And Action Readability

Primary files:

- `Game.Godot/Scenes/UI/HUD.tscn`
- `Game.Godot/Scripts/UI/HUD.cs`
- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Possible secondary files:

- `Game.Godot/Localization/en-US.json`
- `Game.Godot/Localization/zh-CN.json`

### Story 3.4: Replace Prototype Summary Text With Production Feedback Ownership

Primary files:

- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/UI/HUD.cs`

Possible secondary files:

- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`

### Story 4.1: Implement Daily Settlement Outcome Modal

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Likely new files:

- `Game.Godot/Scripts/Screens/BattleOutcomeOverlayController.gd`

Possible secondary files:

- `Game.Godot/Scripts/UI/HUD.cs`

### Story 4.2: Implement Victory Outcome Modal

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/Screens/BattleOutcomeOverlayController.gd`

Possible secondary files:

- `Game.Godot/Scripts/UI/HUD.cs`

### Story 4.3: Implement Defeat Outcome Modal

Primary files:

- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/Screens/BattleOutcomeOverlayController.gd`

Possible secondary files:

- `Game.Godot/Scripts/UI/HUD.cs`

### Story 4.4: Implement Outcome Evidence And Transition Wiring

Primary files:

- `Game.Godot/Scripts/Screens/BattleOutcomeOverlayController.gd`
- `Game.Godot/Scripts/UI/HUD.cs`
- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`

Possible secondary files:

- `Game.Godot/Scripts/Navigation/ScreenNavigator.cs`
- `Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd`

## Task Decomposition Rule

When appending tasks, each task should reference:

- one primary story
- one primary code owner file group
- any required test file updates

Avoid tasks that span all controller files at once.
Prefer one controller or one ownership slice per task.
