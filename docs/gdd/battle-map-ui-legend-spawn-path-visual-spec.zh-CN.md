---
GDD-ID: GDD-LASTKING-BATTLEMAP-LEGEND-SPAWN-PATH-VISUAL-V1
Title: Lastking Battle Map Legend, Spawn, Path, And Slot Visual Rules
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
---

# Lastking Battle Map Legend, Spawn, Path, And Slot Visual Rules

## 1. Goal

This document defines battlefield-local visual meaning for:

- team and zone readability
- spawn-side readability
- path understanding through behavior rather than path arrows
- placement-mode overlays

## 2. Legend Strategy

This design does not use a large permanent RTS-style legend panel.
Instead, the battlefield itself communicates meaning through:

- wall visuals
- weak red spawn-edge glow
- building selection outlines
- range overlays
- placement-mode slot colors
- grey invalid cells and red temporary-invalid frames

## 3. Spawn Side Rules

- the leftmost outer slot column defines the left spawn band
- the rightmost outer slot column defines the right spawn band
- spawn distribution is highest near vertical center and falls off toward top and bottom edges
- that probability logic stays in backend logic and is not visualized as a heat map
- both sides remain weakly glowing red during normal play
- at wave start, the spawn edges pulse strongly for `3s - 5s`, then fall back to weak glow

## 4. Path Readability Rules

Enemy pathing is not shown with arrows or visible route lines.
Players read intent from enemy behavior:

- enemies move from spawn side toward the nearest wall
- if an attackable target enters range, enemies redirect toward it
- after destroying the target, they resume wall pressure
- interior economy structures are not treated as enemy attack targets

## 5. Selection And Linked Unit Feedback

- buildings are selectable
- units are not selectable
- all buildings use one shared selection outline color
- economy buildings: outline plus light base glow only
- defense buildings: outline plus clipped range overlay
- unit-producing buildings: outline plus clipped range overlay plus linked already-deployed unit outlines

Only units linked to the selected building should highlight.
Not all units of the same type.

## 6. Placement Overlay Rules

Placement mode appears directly on slot regions, not as a separate small legend box.

- only valid slot regions receive coverage
- inner zone uses warm color language
- outer zones use cool color language
- walls turn clearly red in placement mode
- valid cells highlight
- fixed invalid cells become grey with lock or cross marks
- temporary invalid cells use red single-cell frames
- temporary invalid means occupancy by building or current unit presence
