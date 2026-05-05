---
GDD-ID: GDD-LASTKING-BATTLEMAPSCREEN-GODOT-RESTRUCTURE-V1
Title: BattleMapScreen Godot Scene Restructure Recommendation
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Main.tscn
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scenes/UI/HUD.tscn
  - Game.Godot/Scripts/Navigation/ScreenNavigator.cs
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scripts/UI/HUD.cs
---

# BattleMapScreen Godot Scene Restructure Recommendation

## 1. Goal

This document proposes how to restructure `BattleMapScreen` in Godot without duplicating existing systems and without drifting away from current runtime ownership.

## 2. Current Stable Assets To Preserve

Keep these foundations:

- `Main.tscn` screen and navigation skeleton
- `ScreenNavigator`
- persistent `HUD`
- independent `CombatExperienceRuntimeBridge`
- existing event-driven feedback flow in `HUD.cs`

## 3. Current Prototype Debt

The current `BattleMapScreen` mixes:

- map placeholders
- runtime bridge ownership
- prototype button controls
- prototype summary and legend text

That is useful for tests and prototype flow, but not suitable as the production ownership model.

## 4. Recommended Ownership Split

`HUD` should continue to own:

- persistent top-bar state
- persistent pressure summary
- persistent resources, progression, and outcome summaries

`BattleMapScreen` should own:

- battlefield composition
- placement overlays
- selection feedback
- range overlays
- local battlefield prompts
- centered outcome modal anchor

`CombatExperienceRuntimeBridge` should remain an independent runtime bridge and test bridge.

## 5. Migration Rule

Do not remove current prototype UI instantly.
Move it into an explicitly temporary legacy subtree first, then replace it layer by layer.

## 6. Structure Direction

The target direction is a battlefield scene separated into:

- map base layer
- boundary layer
- slot overlay layer
- actor layer
- selection feedback layer
- range overlay layer
- local battle feedback layer
- outcome modal anchor
- runtime bridge root

## 7. Acceptance Criteria

- no second HUD is created inside `BattleMapScreen`
- navigation ownership stays in `Main.tscn` and `ScreenNavigator`
- bridge methods used by tests remain stable during migration
- prototype controls are treated as migration-only, not production ownership
