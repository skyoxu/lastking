---
GDD-ID: GDD-LASTKING-BATTLEMAP-UI-SPEC-V1
Title: Lastking BattleMapScreen UI Specification
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs
  - Game.Godot/Scenes/UI/HUD.tscn
  - Game.Godot/Scripts/UI/HUD.cs
---

# Lastking BattleMapScreen UI Specification

## 1. Goal

This document defines the top-level UI structure for `BattleMapScreen`.
The goal is to move from a prototype combat screen to a production-oriented three-band battle interface.

## 2. Frame Baseline

- Primary layout baseline: `1600 x 900`
- Top status bar height: `80px`
- Battlefield usable area: `1584 x 624`
- Decorative side padding: `8px` on left and `8px` on right
- Bottom operation bar height: `196px`
- Main battlefield width is aligned with the gameplay canvas inside the `1584px` usable band
- On narrow displays, only the battlefield area may scroll horizontally
- Vertical scrolling is not part of the design target

## 3. Three Main Areas

`BattleMapScreen` is divided into:

1. Top Status Bar
2. Center Battlefield
3. Bottom Operation Bar

## 4. Top Status Bar

The top bar owns global run state.
It should include:

- resources using current project naming
- day and hour readout
- day/night state
- pause / 1x / 2x controls
- relic button entry
- wave placeholder
- boss placeholder
- pressure summary
- settings entry

Time format is fixed as:

`Day 3 | 18h | Night`

## 5. Center Battlefield

The center section is the main battle surface.
It contains:

- castle interior and outer defense spaces
- walls
- enemy pressure edges
- slot overlays in placement mode
- units, enemies, buildings, and projectiles
- battle-local prompts and feedback
- centered outcome modal coverage when needed

## 6. Bottom Operation Bar

The bottom bar is split into three sections:

- left: buildings and production
- center: battle counts, morale, talent summary, reserved state slots
- right: spells and skills

Rules:

- counts use `current/max`
- morale is shown as a number placeholder first
- skills use icon states for available, unavailable, and cooldown

## 7. Existing Data Reuse

Use existing contracts and runtime outputs first.
Do not create new fields unless the current pipeline cannot support the UI goal.
Wave and boss detailed UI fields may remain placeholders until formal runtime fields exist.

## 8. Acceptance Criteria

- the screen follows a stable top / center / bottom split
- the top bar is concise and global
- the battlefield owns battle-local readability and overlays
- the bottom bar owns player actions and local counters
- only the battlefield can scroll horizontally on narrow displays
