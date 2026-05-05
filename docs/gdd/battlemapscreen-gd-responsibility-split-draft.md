---
GDD-ID: GDD-LASTKING-BATTLEMAPSCREEN-GD-RESPONSIBILITY-SPLIT-DRAFT-V1
Title: BattleMapScreen.gd Responsibility Split Draft
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs
  - Game.Godot/Scenes/UI/HUD.tscn
  - Game.Godot/Scripts/UI/HUD.cs
Test-Refs:
  - Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd
  - Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd
Related-Docs:
  - docs/gdd/battlemapscreen-godot-scene-restructure-recommendation.zh-CN.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/battlemapscreen-node-migration-map.md
---

# BattleMapScreen.gd Responsibility Split Draft

## 1. Goal

This draft defines which responsibilities should remain inside `BattleMapScreen.gd` and which responsibilities should move into dedicated scene-local controllers.

The intent is to make implementation safer by:

- preserving current navigation and test entry points
- preserving the independent `CombatExperienceRuntimeBridge`
- avoiding overlap with `HUD.tscn` / `HUD.cs`
- reducing the current prototype script into a scene coordinator instead of a multi-purpose script

## 2. Current Script Reality

The current `BattleMapScreen.gd` is not only a screen script. It currently acts as all of the following at once:

- screen lifecycle script
- localization bootstrapper
- prototype debug button controller
- combat phase flow driver
- battlefield token renderer for moving enemies
- summary text formatter
- prototype legend/help text binder
- return-to-menu navigation trigger
- auto-wave timer handler

This is acceptable for a prototype, but too broad for a production-facing battlefield screen.

## 3. Current Responsibilities Inventory

| Current responsibility in `BattleMapScreen.gd` | Current implementation shape | Keep or move | Why |
| --- | --- | --- | --- |
| screen `_ready()` bootstrap | node wiring, signal binding, initial bridge reset | keep | root screen still needs a composition entry point |
| locale polling in `_process()` | reads `TranslationServer`, switches local i18n resources | partially keep | root can coordinate locale refresh, but should not own all text targets long term |
| direct bridge calls | `BuildPhase`, `SpawnEnemyWavePhase`, `ResolveCombatExchangePhase`, etc. | partially keep | root may trigger runtime flow, but domain flow details should not spread through presentation code |
| prototype button handlers | build/wave/exchange/cleanup/finish/back | move except temporary compatibility | these are legacy debug controls, not long-term battlefield ownership |
| status and summary text rendering | writes to `Margin/VBox/Status` and `Summary` | move | battlefield-local prompt and HUD already cover these concerns better |
| legend and metrics help text | binds `Legend` and `MetricsHelp` labels | remove/move | final design does not keep permanent legend/help labels |
| auto-wave timer management | toggles `WaveTimer`, updates auto-wave status | move | better isolated as temporary debug/runtime utility behavior |
| moving enemy token rendering | `GetActorSnapshots`, dynamic `ColorRect` spawn/despawn, path sampling | move | belongs to battlefield actor/debug visualization layer |
| path sampling helper | `_sample_path()` over placeholder path points | move | battlefield view concern, not root screen coordination |
| back navigation | returns to `MainMenu`, clears current screen | keep short-term | still a screen-level action until final flow is moved to a standard UI entry |
| bridge reset and summary fallback | `_try_bridge_reset`, `_try_bridge_summary`, `_call_or_fallback` | partially keep | root may keep safe bridge access wrappers during migration |

## 4. Why The Current Split Is Risky

The current script couples four layers that should evolve independently:

1. runtime simulation bridge access
2. prototype debug controls
3. battlefield visuals
4. localized text presentation

That coupling creates predictable migration risks:

- scene node moves break script references immediately
- replacing prototype labels with production overlays requires touching unrelated runtime code
- combat visualization changes are blocked by prototype button flow assumptions
- tests stay coupled to legacy node paths longer than necessary

## 5. Responsibilities That Should Stay In `BattleMapScreen.gd`

`BattleMapScreen.gd` should become the scene composition root for the battlefield screen.

It should keep only these responsibilities:

### 5.1 Scene composition and child-controller wiring

Keep:

- resolving required child nodes or controller nodes
- validating that required scene-local components exist
- connecting scene-level signals between controllers when needed

Do not let the root script become the implementation body of those child controllers.

### 5.2 Runtime bridge access boundary

Keep:

- holding the reference to `CombatExperienceRuntimeBridge`
- performing safe capability checks such as `has_method()` during migration
- exposing a thin coordination path for scene-local controllers that need bridge data

Do not let `BattleMapScreen.gd` become the place that implements combat rules or event interpretation already owned by contracts or `HUD.cs`.

### 5.3 Screen-level mode arbitration

Keep:

- switching between neutral mode, selection mode, and placement mode
- enforcing the agreed rule that placement mode cancels selection mode
- routing cancel/back requests to the correct active sub-controller

This is one of the few responsibilities that naturally belongs to the top-level battlefield screen.

### 5.4 Screen exit handoff

Keep:

- requesting menu return
- asking `ScreenNavigator` to clear the current screen

This is still screen ownership, even if the visible back affordance later moves into a different UI surface.

### 5.5 Temporary migration compatibility shell

Keep temporarily:

- enough compatibility logic to support current integration tests
- enough wiring to let legacy prototype controls call the bridge until their replacements exist

This compatibility shell should be explicitly treated as temporary and removable.

## 6. Responsibilities That Should Move Out Of `BattleMapScreen.gd`

### 6.1 Prototype debug flow controls

Move out:

- build button flow
- wave button flow
- exchange button flow
- cleanup button flow
- finish button flow
- auto-wave toggle flow

Recommended destination:

- a temporary `BattleRuntimeDebugController.gd`
- or a `LegacyPrototypePanelController.gd` attached under `LegacyPrototypeRoot`

Reason:

These behaviors are not the long-term player-facing battlefield command model. They are migration and test support.

### 6.2 Battlefield actor visualization

Move out:

- actor token creation/destruction
- actor snapshot polling
- path-progress to position conversion
- temporary debug enemy visuals

Recommended destination:

- `BattlefieldActorView.gd`

Reason:

Actor drawing and actor view refresh belong to the battlefield visual layer, not the root screen.

### 6.3 Battlefield-local prompt and summary presentation

Move out:

- `Status` label updates
- `Summary` label formatting
- any future local battlefield prompt text

Recommended destination:

- `BattleLocalFeedbackController.gd`

Reason:

Local prompt presentation should be swappable without touching bridge coordination or navigation.

### 6.4 Permanent legend/help text

Move out or remove:

- legend text binding
- metrics help text binding

Recommended destination:

- remove from final production flow
- if still needed during migration, keep only inside `LegacyPrototypePanelController.gd`

Reason:

The agreed UI direction encodes battlefield meaning directly in overlays and visuals, not in a persistent text legend block.

### 6.5 Placement overlay behavior

Move out:

- valid slot highlight logic
- fixed invalid grey/lock logic
- temporary invalid red-frame logic
- inner/outer placement tinting
- wall red blocking highlight

Recommended destination:

- `PlacementOverlayController.gd`

Reason:

Placement visualization and slot legality feedback are a distinct subsystem and will grow quickly.

### 6.6 Building selection and range visualization

Move out:

- building outline state
- linked deployed-unit outline state
- range ring display and clipping behavior

Recommended destination:

- `BattleSelectionController.gd`

Reason:

Selection rules and range rendering have their own state transitions and should not live in the root script.

### 6.7 Outcome modal ownership

Move out:

- daily settlement modal presentation
- victory modal presentation
- defeat modal presentation
- pause/unpause around outcome modal display

Recommended destination:

- `BattleOutcomeOverlayController.gd`

Reason:

Outcome presentation is modal UI ownership, not root coordinator body logic.

### 6.8 Localization target binding for local scene text

Move out gradually:

- title label updates
- legacy prototype button label updates
- legacy legend/help label updates

Recommended destination:

- temporary `BattleSceneLocalizationBinder.gd`
- or keep inside the legacy prototype controller until those nodes are deleted

Reason:

The root script may still notice locale changes, but should not remain the owner of every localized text field.

## 7. Recommended Script Split

The following split is conservative and directly aligned with the existing node-tree draft.

| Script | Ownership | Scope |
| --- | --- | --- |
| `BattleMapScreen.gd` | composition root | scene wiring, mode arbitration, bridge access boundary, screen exit |
| `BattleRuntimeDebugController.gd` | temporary legacy/debug flow | prototype button row, auto-wave timer, test-era phase triggers |
| `BattlefieldActorView.gd` | battlefield visuals | actor snapshot polling, temporary enemy token rendering, future actor view root wiring |
| `BattleLocalFeedbackController.gd` | battlefield-local textual/visual prompt layer | transient local prompts, debug summary while production UI is incomplete |
| `PlacementOverlayController.gd` | placement mode | slot tinting, invalid markers, wall blocking visuals |
| `BattleSelectionController.gd` | selection mode | building outline, linked unit outline, range overlay coordination |
| `BattleOutcomeOverlayController.gd` | outcome modal layer | daily/victory/defeat modal flow and pause handoff |

Not every controller must be created immediately. The important point is the ownership boundary.

## 8. What `BattleMapScreen.gd` Should Explicitly Not Own

To avoid future drift, `BattleMapScreen.gd` should explicitly not own:

- top status bar resource/day/time labels
- persistent pressure summaries already covered by `HUD`
- persistent resource/build/progression panels already covered by `HUD`
- domain event parsing that already exists in `HUD.cs`
- long-term battle outcome text composition already covered by `HUD.cs`
- combat rule execution details beyond thin bridge invocation
- permanent slot rules data definitions

## 9. Migration-Safe Sequence

### Phase 1: Freeze current ownership intent

Do first:

- keep current node paths working
- keep current button-driven test flow working
- document which parts are temporary legacy responsibilities

### Phase 2: Extract debug/prototype ownership without changing behavior

Do next:

- move button handlers and auto-wave timer logic into `BattleRuntimeDebugController.gd`
- move summary/status/legend/help writes into a legacy panel controller
- keep `BattleMapScreen.gd` delegating to that controller

This phase gives the largest clarity gain with the smallest runtime risk.

### Phase 3: Extract battlefield actor view

Do next:

- move `_render_actor_tokens()`
- move `_sample_path()`
- move placeholder token node ownership out of `Background`

After this phase, the root script stops acting as a visual renderer.

### Phase 4: Introduce production controllers for selection, placement, and local prompts

Do next:

- add `PlacementOverlayController.gd`
- add `BattleSelectionController.gd`
- add `BattleLocalFeedbackController.gd`

These should target the new battlefield layer tree rather than the prototype `Margin/VBox` subtree.

### Phase 5: Remove legacy prototype subtree after test migration

Only do after:

- tests no longer depend on `Margin/VBox/...` paths
- production local prompt and outcome overlays exist
- debug flow has an explicit keep-or-delete decision

## 10. Test Compatibility Notes

Current tests depend on the legacy button row and summary/status labels.

Because of that, the recommended order is:

1. preserve old paths first
2. extract logic behind those paths second
3. update tests third
4. delete legacy subtree last

The bridge contract should remain stable throughout migration:

- `ResetForInteractiveRun()`
- `BuildPhase()`
- `TrainFriendlyUnitPhase()`
- `SpawnEnemyWavePhase()`
- `ResolveCombatExchangePhase()`
- `CleanupDeadUnitsPhase()`
- `PublishOutcomePhase()`
- `GetSummary()`
- `AdvanceSimulation()`
- `GetActorSnapshots()`

## 11. Direct Implementation Guidance

If only one implementation-oriented decision is taken from this draft, it should be this:

`BattleMapScreen.gd` should be reduced to a coordinator, and the first extraction target should be the legacy prototype debug/button flow.

That first extraction is the highest-value move because it:

- reduces the widest responsibility cluster immediately
- preserves current tests with minimal change
- makes later selection/placement/outcome implementation much cleaner
- avoids mixing long-term battle UI work with temporary prototype flow code

## 12. Acceptance Criteria

This responsibility split draft is satisfied when:

- `BattleMapScreen.gd` is treated as a composition root, not a catch-all script
- prototype debug controls are recognized as temporary ownership
- battlefield visualization is separated from root coordination
- placement and selection have their own future controller ownership
- `HUD` remains the owner of persistent global combat feedback panels
- migration can proceed without breaking current bridge-based tests early
