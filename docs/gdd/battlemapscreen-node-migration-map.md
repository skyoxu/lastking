---
GDD-ID: GDD-LASTKING-BATTLEMAPSCREEN-NODE-MIGRATION-MAP-V1
Title: BattleMapScreen Node Migration Map
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs
Test-Refs:
  - Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd
  - Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd
Related-Docs:
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/battlemapscreen-godot-scene-restructure-recommendation.zh-CN.md
---

# BattleMapScreen Node Migration Map

## 1. Goal

This document maps current `BattleMapScreen.tscn` nodes to the proposed target structure.

It is intentionally conservative:

- preserve working runtime/test entry points first
- move prototype UI second
- remove legacy nodes last

## 2. Migration Table

| Current Node | Current Role | Proposed Destination | Action | Notes |
| --- | --- | --- | --- | --- |
| `BattleMapScreen` | screen root | `BattleMapScreen` | keep | remains the screen scene root |
| `Background` | battlefield background root | `BattlefieldRoot/MapBaseLayer/TerrainBackdrop` | move/rename | stop using it as a catch-all parent for runtime visuals |
| `Path` | placeholder path visual | `BattlefieldRoot/MapMarkerLayer` or `PathingVisualLayer` | move | keep only as a temporary debug visual if still useful |
| `PlayerCastle` | placeholder castle marker | `BattlefieldRoot/MapMarkerLayer/CastleAnchor` or map art replacement | move/rename | should become marker/anchor, not final castle UI |
| `EnemySpawnB` | right spawn band marker | `BattlefieldRoot/MapMarkerLayer/EnemySpawnMarkerRight` | keep/rename later | current runtime uses a full-height right spawn band |
| `EnemySpawnB` | right spawn band marker | `BattlefieldRoot/MapMarkerLayer/EnemySpawnMarkerRight` | keep/rename later | current runtime uses a full-height right spawn band |
| `Margin` | prototype UI root | `LegacyPrototypeRoot/Margin` | move | keep only during migration |
| `Margin/VBox/Title` | prototype title label | `LegacyPrototypeRoot/...` | keep-temporary | remove after HUD-driven title/state is no longer needed |
| `Margin/VBox/Status` | prototype status text | `LegacyPrototypeRoot/...` | keep-temporary | replace with battlefield prompt + HUD feedback |
| `Margin/VBox/Controls` | prototype control row | `LegacyPrototypeRoot/...` | keep-temporary | may remain as debug/dev-only controls until production actions exist |
| `Margin/VBox/Controls/BuildBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | test path currently depends on this button |
| `Margin/VBox/Controls/WaveBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | test path currently depends on this button |
| `Margin/VBox/Controls/AutoWaveBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | production replacement likely moves elsewhere or disappears |
| `Margin/VBox/Controls/ExchangeBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | test path currently depends on this button |
| `Margin/VBox/Controls/CleanupBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | test path currently depends on this button |
| `Margin/VBox/Controls/FinishBtn` | prototype debug action | `LegacyPrototypeRoot/...` | keep-temporary | test path currently depends on this button |
| `Margin/VBox/Controls/BackBtn` | return action | `LegacyPrototypeRoot/...` or future battlefield utility layer | keep-temporary | final ownership may move once production back flow is defined |
| `Margin/VBox/Summary` | prototype metrics summary | `LegacyPrototypeRoot/...` | keep-temporary | replace with proper local battlefield feedback and HUD summaries |
| `Margin/VBox/Legend` | prototype legend text | remove after replacement | deprecate | final legend logic should be encoded in battlefield visuals and placement overlays |
| `Margin/VBox/MetricsHelp` | prototype metrics text | remove after replacement | deprecate | final metrics help should not remain as persistent scene text |
| `CombatExperienceRuntimeBridge` | runtime/test bridge | `RuntimeBridgeRoot/CombatExperienceRuntimeBridge` | move | preserve node name if tests or scripts rely on it |
| `WaveTimer` | auto-wave timer | `RuntimeBridgeRoot/WaveTimer` | move | keep exported/connected behavior intact |

## 3. Script Coupling Notes

### 3.1 BattleMapScreen.gd current direct node dependencies

Current script directly references:

- `Status`
- `Summary`
- `Legend`
- `MetricsHelp`
- button row nodes
- `CombatExperienceRuntimeBridge`
- `WaveTimer`
- `Background`

This means migration should not move all nodes at once without either:

- preserving temporary compatibility paths, or
- updating the script in the same change set

### 3.2 Recommended safe migration pattern

Recommended pattern:

1. add new containers first
2. move low-risk visual placeholders first
3. keep prototype UI subtree intact under `LegacyPrototypeRoot`
4. update script references only after container introduction is stable

## 4. Test Compatibility Map

### 4.1 Tests currently coupled to node paths

`test_battle_map_screen_runtime_flow.gd` currently expects:

- `Margin/VBox/Controls/BuildBtn`
- `Margin/VBox/Controls/WaveBtn`
- `Margin/VBox/Controls/ExchangeBtn`
- `Margin/VBox/Controls/CleanupBtn`
- `Margin/VBox/Controls/FinishBtn`
- `Margin/VBox/Summary`
- `Margin/VBox/Status`

Meaning:

- these paths should remain available until tests are updated
- moving them under `LegacyPrototypeRoot` would require synchronized test updates

### 4.2 Tests currently coupled to bridge behavior

`test_combat_experience_runtime_flow.gd` currently expects:

- `CombatExperienceRuntimeBridge` to remain instantiable
- methods such as `RunCompleteCombatExperienceForTest`, `GetSummary`, and others to remain available
- `HUD` to receive and show event-driven feedback

Meaning:

- do not rename or absorb bridge responsibilities into scene-only scripts during node migration

## 5. Recommended Migration Phases

### Phase 1: Structural Introduction

- add `SafeFrame`
- add `BattlefieldViewport`
- add `BattlefieldRoot`
- add `RuntimeBridgeRoot`
- add `LegacyPrototypeRoot`

No behavior changes yet.

### Phase 2: Non-UI Visual Moves

Move:

- `Background`
- `Path`
- `PlayerCastle`
- `EnemySpawnB`

Goal:

- battlefield visuals are no longer mixed with prototype text UI

### Phase 3: Runtime Bridge Move

Move:

- `CombatExperienceRuntimeBridge`
- `WaveTimer`

Goal:

- runtime bridge becomes presentation-independent

### Phase 4: Legacy Prototype Isolation

Move:

- `Margin`

into:

- `LegacyPrototypeRoot`

Goal:

- make the prototype subtree explicit and temporary

### Phase 5: Production Overlay Replacement

Replace subtree responsibilities with new battlefield layers:

- selection
- placement
- battle prompts
- outcome modal

### Phase 6: Test Path Update and Prototype Removal

Only after production overlays are in place:

- update tests
- remove legacy prototype subtree

## 6. Reuse Rules

To avoid rebuilding systems that already exist:

- keep top status and persistent feedback in `HUD`
- keep screen switching in `ScreenNavigator`
- keep event consumption in `HUD.cs`
- keep runtime bridge test entry points unchanged as long as tests depend on them

## 7. Acceptance Criteria

- Every current node has a defined keep/move/deprecate outcome
- Runtime bridge nodes are explicitly separated from battlefield presentation layers
- Prototype controls are not mistaken for final production ownership
- Existing tests have a documented compatibility path during migration
- The plan minimizes drift between current implementation, tests, and design specs
