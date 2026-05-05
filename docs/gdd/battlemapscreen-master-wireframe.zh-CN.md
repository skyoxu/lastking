---
GDD-ID: GDD-LASTKING-BATTLEMAPSCREEN-MASTER-WIREFRAME-V1
Title: BattleMapScreen Master Wireframe Summary
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scenes/UI/HUD.tscn
---

# BattleMapScreen Master Wireframe Summary

## 1. Goal

This document summarizes the complete BattleMapScreen structure across Battle Map UI, Combat Feedback UI, and Outcome UI.

## 2. Global Frame

- Full screen baseline: `1440 x 900`
- Top status bar: `80px`
- Battlefield: `600px`
- Bottom operation bar: `220px`

## 3. Top Bar Summary

Modules:

- resources
- time and phase
- pause and speed
- relic entry
- wave placeholder
- boss placeholder
- pressure summary
- settings

## 4. Battlefield Summary

Modules:

- inner and outer battlefield regions
- permanent wall boundaries
- spawn-side weak glow and wave pulse
- placement-mode slot overlays
- battle actors
- selection and range layers
- local combat feedback prompts
- centered outcome modal area

## 5. Bottom Bar Summary

Modules:

- left: building and production commands
- center: workers, combat units, morale, talent summary, placeholders
- right: spells and skills with icon-state logic

## 6. State Rules

- placement mode and building selection are mutually exclusive
- outcome modal overlays the battlefield center
- persistent feedback remains owned by HUD
- battlefield-local feedback stays inside the battlefield scene

## 7. Implementation Intent

This summary is the handoff layer between design specs and scene implementation planning.
