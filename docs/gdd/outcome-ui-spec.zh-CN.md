---
GDD-ID: GDD-LASTKING-OUTCOME-UI-V1
Title: Outcome UI Specification
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scenes/UI/HUD.tscn
---

# Outcome UI Specification

## 1. Goal

This document defines the three outcome flows used by `BattleMapScreen`:

1. daily settlement
2. victory settlement
3. defeat settlement

## 2. Outcome Types

### Daily Settlement

- triggered after each completed night, including boss nights
- continues the run
- includes a three-choice reward entry

### Victory Settlement

- triggered after defeating the final boss
- ends the run
- no additional reward-claim flow
- cannot return to live battle

### Defeat Settlement

- triggered when wall HP is `0` or below
- ends the run
- no additional reward-claim flow
- cannot return to live battle
- failure reason must clearly state: `The wall was breached`

## 3. Presentation Rules

- the outcome modal covers the center of the battlefield
- time pauses while the modal is active
- once the modal is resolved, the game continues to the next flow automatically

## 4. Required Summary Content

All outcome views should show the appropriate subset of:

- final HP
- kill count
- reward result summary
- resource snapshot

## 5. Button Rules

Victory and defeat both use:

- `Return to Main Menu`
- `Restart`

There is no `Continue Battle` path for victory or defeat.

## 6. Evidence Panel Guidance

Recommended design:

- a small expandable evidence panel inside the central outcome modal
- collapsed by default to a one-line summary
- expandable for additional runtime evidence context

## 7. Visual Tone

- daily settlement: neutral or softly warm
- victory: warm gold
- defeat: dark or cold red
