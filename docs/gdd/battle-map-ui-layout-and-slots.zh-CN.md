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

- Battlefield area: `1440 x 600`
- Castle interior region: `400 x 600`
- Left wall: `20px`
- Right wall: `20px`
- Left outer field: `500px`
- Right outer field: `500px`

Total width:

`500 + 20 + 400 + 20 + 500 = 1440`

## 3. Grid Rules

- Slot size: `50 x 50`
- The map uses a strict grid, not free placement
- The walls split the battlefield into three valid slot regions:
  - left outer grid: `10 x 12 = 120`
  - inner castle grid: `8 x 12 = 96`
  - right outer grid: `10 x 12 = 120`
- Total valid slots: `336`

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
