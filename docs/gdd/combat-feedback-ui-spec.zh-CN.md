---
GDD-ID: GDD-LASTKING-COMBAT-FEEDBACK-UI-V1
Title: Combat Feedback UI Specification
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/UI/HUD.tscn
  - Game.Godot/Scripts/UI/HUD.cs
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
---

# Combat Feedback UI Specification

## 1. Goal

This document defines the combat readability layer across the persistent HUD, battlefield-local prompts, and bottom interaction summaries.

## 2. Top Status Bar Rules

Reuse existing contracts first.
If a field does not exist formally yet, use a placeholder instead of inventing a new runtime field just for UI.

Directly reusable now:

- resources
- day and night identity
- HP
- pause / 1x / 2x state

Placeholder-first now:

- current wave label
- formal boss label

## 3. Battlefield Instant Feedback

The instant combat read layer should prioritize short, clear, high-signal feedback such as:

- damage numbers
- hit flashes
- wall-under-attack emphasis
- wave arrival prompts
- boss-pressure prompts
- pressure state prompts

Damage numbers should be optionally toggleable in settings.

## 4. Bottom Feedback Rules

- counts use `current/max`
- morale is a numeric placeholder first
- spells and skills use icons
- unavailable skills use `50%` opacity
- cooldown visuals should use a Warcraft-style radial clock mask

## 5. Win/Lose Readability

- defeating the final boss is victory
- wall HP reaching `0` or below is defeat

## 6. Pressure Readability

Use a two-level communication model:

- top bar: stable summary label
- battlefield center/top: short event-driven prompts
