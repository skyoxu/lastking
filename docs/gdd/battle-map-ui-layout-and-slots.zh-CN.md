---
GDD-ID: GDD-LASTKING-BATTLEMAP-LAYOUT-SLOTS-V1
Title: Lastking Battle Map Layout And Slot Rules
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
---

# Lastking Battle Map Layout And Slot Rules

## 1. Goal

This document defines battlefield space partitioning and slot placement rules.
It does not define full AI, full pathfinding, or final balance tables.

## 2. Battlefield Dimensions

- Screen baseline: `1600 x 900`
- Battlefield usable area: `1584 x 624`
- Decorative side padding: `8px` on left and `8px` on right
- Castle interior region: `432 x 624`
- Left wall: `48px`
- Right wall: `48px`
- Left outer field: `528px`
- Right outer field: `528px`

Total width:

`528 + 48 + 432 + 48 + 528 = 1584`

## 3. Grid Rules

- Slot size: `48 x 48`
- The map uses a strict grid, not free placement
- The walls split the battlefield into three valid slot regions:
  - left outer grid: `11 x 13 = 143`
  - inner castle grid: `9 x 13 = 117`
  - right outer grid: `11 x 13 = 143`
- Total valid slots: `403`

Walls are not buildable slots.
Buildings cannot cross wall boundaries.

## 4. Region Rules

- The inner castle region is fully buildable space
- There is no separate castle model footprint occupying inner build cells
- Walls run top to bottom and are permanent hard boundaries
- Enemy attacks against the wall are treated as attacks against castle durability
- Default castle HP baseline: `200`

## 5. Building Category Placement Rules

- unit-producing buildings and traps: outer fields only
- defense buildings: allowed regions are configuration-driven
- all other buildings: inner castle only

Traps consume charges and release their slot again when fully exhausted.

## 6. Unit and Movement Constraints

- units are not player-commanded directly
- units do not cross the wall boundary
- enemies spawn from left and right side spawn bands only
- there are no top or bottom enemy entry points in this layout definition

## 7. Difficulty and Configuration Ownership

These remain configuration-owned, not hard-coded by this layout spec:

- building footprint size
- defense-building region permissions
- trap charge counts
- difficulty-based slot unlock rules
- special pre-placed non-removable map structures

## 8. Visual Expectations

- inner and outer slot zones must be visually distinct
- wall boundaries must be visually unmistakable
- build legality must be readable without trial-and-error placement spam
