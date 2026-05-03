# Decision Log: Task 54 Residual Needs Fix (2026-05-03)

## Decision
- Keep `T54` as `done` for the scoped acceptance: `Play -> BattleMapScreen` route and minimum interactive combat loop are in place with integration evidence.

## Why Now
- User required immediate player-visible improvement over abstract combat summary flow.
- Existing runtime slices (T47-T53) were technically complete but lacked direct map/scene presentation.

## Residual Needs Fix
- Unify two representations into one:
  - Map-side moving enemy tokens (`BattleMapScreen.gd`)
  - Bridge-side combat entities and counters (`CombatExperienceRuntimeBridge.cs`)
- Add deterministic assertions that moving map entities and combat counters remain synchronized.

## Consequences
- Current build is playable and demonstrable.
- Full production-grade consistency still requires one more unification pass.

## Evidence
- `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- `Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`
- `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`

