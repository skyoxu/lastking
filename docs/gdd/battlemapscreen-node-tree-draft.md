---
GDD-ID: GDD-LASTKING-BATTLEMAPSCREEN-NODE-TREE-DRAFT-V1
Title: BattleMapScreen Node Tree Draft
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs
Related-Docs:
  - docs/gdd/battlemapscreen-godot-scene-restructure-recommendation.zh-CN.md
  - docs/gdd/battlemapscreen-master-wireframe.zh-CN.md
---

# BattleMapScreen Node Tree Draft

## 1. Goal

This draft proposes a target node tree for `BattleMapScreen.tscn` that:

- keeps `Main.tscn` and `ScreenNavigator` unchanged
- keeps `HUD.tscn` as the persistent feedback surface
- keeps `CombatExperienceRuntimeBridge` as a separate runtime bridge
- removes long-term dependence on the current prototype `Margin/VBox` UI stack

This is a structure draft, not a final implementation patch.

## 2. Design Intent

The target scene should separate five concerns:

1. battlefield composition
2. slot and placement overlays
3. battle actor visuals
4. scene-local feedback overlays
5. runtime bridge and timers

It should not duplicate responsibilities already owned by `HUD.tscn`.

## 3. Proposed Tree

```text
BattleMapScreen (Control)
|- SafeFrame (MarginContainer)
|  |- BattlefieldViewport (Control)
|  |  |- BattlefieldRoot (Control)
|  |  |  |- MapBaseLayer (Control)
|  |  |  |  |- TerrainBackdrop (ColorRect or TextureRect)
|  |  |  |  |- RegionInnerCastle (Control)
|  |  |  |  |- RegionOuterLeft (Control)
|  |  |  |  |- RegionOuterRight (Control)
|  |  |  |
|  |  |  |- BoundaryLayer (Control)
|  |  |  |  |- WallLeft (Control)
|  |  |  |  |- WallRight (Control)
|  |  |  |  |- SpawnEdgeGlowLeft (Control)
|  |  |  |  |- SpawnEdgeGlowRight (Control)
|  |  |  |
|  |  |  |- SlotOverlayLayer (Control)
|  |  |  |  |- InnerSlotGrid (Control)
|  |  |  |  |- LeftOuterSlotGrid (Control)
|  |  |  |  |- RightOuterSlotGrid (Control)
|  |  |  |  |- PlacementStateOverlay (Control)
|  |  |  |
|  |  |  |- MapMarkerLayer (Control)
|  |  |  |  |- EnemySpawnMarkerLeft (Control)
|  |  |  |  |- EnemySpawnMarkerRight (Control)
|  |  |  |  |- CastleAnchor (Control)
|  |  |  |
|  |  |  |- CombatActorLayer (Node2D or Control)
|  |  |  |  |- BuildingVisualRoot
|  |  |  |  |- FriendlyUnitVisualRoot
|  |  |  |  |- EnemyUnitVisualRoot
|  |  |  |  |- ProjectileVisualRoot
|  |  |  |
|  |  |  |- SelectionFeedbackLayer (Control)
|  |  |  |  |- BuildingSelectionOutlineRoot
|  |  |  |  |- LinkedUnitOutlineRoot
|  |  |  |
|  |  |  |- RangeOverlayLayer (Control)
|  |  |  |  |- DefenseRangeRoot
|  |  |  |  |- UnitBuildingRangeRoot
|  |  |  |
|  |  |  |- CombatFeedbackLayer (Control)
|  |  |  |  |- DamageNumberRoot
|  |  |  |  |- HitFlashRoot
|  |  |  |  |- PromptAnchorTop
|  |  |  |  |- PressureEventAnchor
|  |  |  |
|  |  |  |- OutcomeModalAnchor (Control)
|  |
|- RuntimeBridgeRoot (Node)
|  |- CombatExperienceRuntimeBridge (Node)
|  |- WaveTimer (Timer)
|
|- LegacyPrototypeRoot (Control) [temporary migration-only container]
```

## 4. Container Responsibilities

### 4.1 SafeFrame

Purpose:

- provides the final screen-local frame inside the screen root
- can own margins or scale behavior if needed later

Notes:

- if no extra safe framing is needed, this layer can remain extremely thin

### 4.2 BattlefieldViewport

Purpose:

- owns the visible 1584x624 battlefield presentation area inside the 1600x900 frame
- becomes the scrollable region when low horizontal resolution forces battlefield scrolling

Notes:

- top and bottom UI remain outside this scrolling responsibility because they belong to persistent HUD and bottom operation surfaces

### 4.3 BattlefieldRoot

Purpose:

- owns all battlefield-local layers in one place
- becomes the main coordination root for scene-local visuals

### 4.4 MapBaseLayer

Purpose:

- terrain backdrop
- inner/outer region background separation
- non-interactive map composition

Should contain:

- castle/outer region visual grounding
- no runtime summaries
- no button controls

### 4.5 BoundaryLayer

Purpose:

- wall visuals
- spawn edge glow visuals
- placement-mode wall highlight state

Should contain:

- persistent wall boundary visuals
- left/right weak red spawn edge glow
- stronger pulse state at wave start

### 4.6 SlotOverlayLayer

Purpose:

- placement-mode grid overlays
- valid/invalid/temporary-invalid slot feedback
- warm inner zone / cool outer zone color coding

Should contain:

- no permanent slot-heavy clutter in normal play
- all placement-mode coverage behavior

### 4.7 MapMarkerLayer

Purpose:

- scene anchors and non-unit battlefield markers
- spawn marker placeholders while art is still evolving
- castle anchor or castle marker if needed by selection/highlight logic

### 4.8 CombatActorLayer

Purpose:

- visual ownership root for all battle participants
- keeps buildings, units, enemies, and projectiles out of background-only layers

Should contain:

- visual children that correspond to battlefield runtime entities
- not long-form labels or prototype summaries

### 4.9 SelectionFeedbackLayer

Purpose:

- selected building outline
- linked unit highlight outline

Rules:

- units remain non-selectable
- linked units are highlighted only when related building selection requires it

### 4.10 RangeOverlayLayer

Purpose:

- area/range feedback for defensive buildings and unit-producing buildings
- supports wall-clipped display logic

Rules:

- economy buildings do not create range overlays

### 4.11 CombatFeedbackLayer

Purpose:

- damage number visuals
- hit flashes
- short event prompts inside battlefield center/top area

Notes:

- this is local battle readability feedback
- this does not replace persistent `HUD` status panels

### 4.12 OutcomeModalAnchor

Purpose:

- central modal anchor for daily settlement, victory, and defeat overlays

Rules:

- modal appears centered over battlefield
- battlefield pauses when outcome modal is active

### 4.13 RuntimeBridgeRoot

Purpose:

- isolates scene runtime bridge nodes from presentation hierarchy
- keeps `CombatExperienceRuntimeBridge` and `WaveTimer` grouped together

Benefits:

- easier testing
- easier future replacement of runtime bridge internals without touching battlefield layout layers

### 4.14 LegacyPrototypeRoot

Purpose:

- temporary container only during migration
- receives old `Margin/VBox` prototype controls while the new layers are introduced incrementally

Rules:

- do not build new production UI under this container
- remove after migration is complete

## 5. What Should Stay Out of BattleMapScreen

The following responsibilities should stay in `HUD.tscn` / `HUD.cs`:

- top status bar labels
- resource summary consumption
- persistent pressure summary label
- persistent config/migration/report metadata panels
- persistent outcome summary text that already consumes domain events

The following responsibilities should stay in `Main.tscn` and navigation:

- screen switching
- menu routing
- overlay fade transition

## 6. Suggested Script Ownership

Recommended ownership after migration:

- `BattleMapScreen.gd`
  - high-level scene orchestration only
- `CombatExperienceRuntimeBridge.cs`
  - runtime bridge / test bridge only
- future optional scripts
  - `BattlefieldView.gd`
  - `PlacementOverlayController.gd`
  - `BattleSelectionController.gd`
  - `BattleOutcomeOverlayController.gd`

This document does not require immediate script splitting, but the node tree is designed to make that split safe later.

## 7. Compatibility Constraints

The structure must preserve these migration constraints:

- `BattleMapScreen.tscn` must remain instantiable by `ScreenNavigator`
- `CombatExperienceRuntimeBridge` must remain callable by existing tests
- battlefield summary/runtime logic should remain available until equivalent HUD or overlay replacements exist

## 8. Recommended Migration Order

1. Introduce `BattlefieldRoot`, `RuntimeBridgeRoot`, and `LegacyPrototypeRoot`
2. Move `Background`, marker nodes, and path placeholders under new battlefield layers
3. Move `CombatExperienceRuntimeBridge` and `WaveTimer` under `RuntimeBridgeRoot`
4. Leave old `Margin/VBox` under `LegacyPrototypeRoot`
5. Rebuild scene-local overlays layer by layer
6. Remove `LegacyPrototypeRoot` only after replacement coverage is complete

## 9. Acceptance Criteria

- The proposed tree does not duplicate `HUD` responsibilities
- Runtime bridge nodes remain independent from presentation layers
- Prototype UI is clearly marked as migration-only
- Battlefield layers separate map, boundaries, slots, actors, selection, range, feedback, and outcome responsibilities
- The target structure remains compatible with current screen navigation
