---
GDD-ID: GDD-LASTKING-BATTLEMAP-SELECTION-RANGE-PLACEMENT-EFFECTS-V1
Title: Lastking Battle Map Selection, Range, Placement, And Effect Rules
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
---

# Lastking Battle Map Selection, Range, Placement, And Effect Rules

## 1. Goal

This document defines how selection, range overlays, placement-mode states, and battlefield effect emphasis should behave.

## 2. Selection Rules

- buildings are selectable
- units are not selectable
- all buildings share one outline color
- selection persists until deselection, another building selection, or placement-mode entry

Economy buildings:

- show shared outline
- show light base highlight
- do not show range overlays

Defense buildings:

- show shared outline
- show warm red clipped range overlay

Unit-producing buildings:

- show shared outline
- show cool blue clipped range overlay
- show linked already-deployed unit outlines only for units owned by that selected building

## 3. Range Overlay Rules

- use semi-transparent fill plus a clear boundary line
- defense range uses warm red family
- unit-building range uses cool blue family
- range displays are clipped by the walls
- they do not extend through the wall boundary

## 4. Placement-Mode Priority Rules

Selection mode and placement mode are mutually exclusive.

- entering placement mode cancels current building selection
- while placement mode is active, clicking an existing building does not activate selection
- instead, that space remains part of legality feedback only

## 5. Placement-State Visual Rules

Valid cells:

- highlighted using region-aware color logic

Fixed invalid cells:

- grey cell fill
- lock or cross mark

Temporary invalid cells:

- red single-cell frame
- no reason text

Walls:

- always visible
- in placement mode they become stably red
- they do not pulse

## 6. Effect Emphasis Rules

- wall pressure should feel more urgent than passive environmental effects
- spawn pulses should not overpower placement legality
- battle readability should favor actor silhouettes and feedback timing over decorative motion
