---
GDD-ID: GDD-LASTKING-COMBAT-FEEDBACK-PRESSURE-STATE-COPY-V1
Title: Combat Feedback Pressure State Machine And Copy Table
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scripts/UI/HUD.cs
  - Game.Core/Contracts/Lastking/WaveSpawned.cs
  - Game.Core/Contracts/Lastking/CastleHpChanged.cs
  - Game.Core/Contracts/Lastking/RewardOffered.cs
---

# Combat Feedback Pressure State Machine And Copy Table

## 1. Goal

This document defines the minimum viable pressure-state model for combat feedback UI, using only existing runtime inputs.

## 2. Available Inputs

Use existing sources only:

- `WaveSpawned`
- `CastleHpChanged`
- `RewardOffered.IsBossNight` as a weak boss-night hint

There is currently no formal dedicated `PressureChanged` event contract for UI.

## 3. Pressure States

Recommended first-pass state set:

- `Stable`
- `Warning`
- `Danger`
- `Critical`

If implementation must stay even smaller, a temporary three-state subset may exist internally, but the UI spec targets four named states.

## 4. HP-Based Baseline Suggestion

Using current wall/castle HP baseline `200`:

- `Stable`: `HP > 120`
- `Warning`: `80 < HP <= 120`
- `Danger`: `40 < HP <= 80`
- `Critical`: `HP <= 40`

## 5. Wave And Boss Escalation Suggestion

- a new wave should push the battlefield prompt toward warning-level urgency even if HP is still healthy
- boss-night context should raise urgency above ordinary wave messaging
- pressure upgrades should happen fast
- pressure downgrades should happen slowly to avoid noisy label oscillation

## 6. Top-Bar Copy

Use concise summary copy:

- `Pressure: Stable`
- `Pressure: Warning`
- `Pressure: Danger`
- `Pressure: Critical`

## 7. Battlefield Prompt Copy

Suggested short battlefield prompts:

- `Wave Incoming`
- `Pressure Rising`
- `Wall Under Attack`
- `Boss Pressure`
- `Castle Critical`
- `Last Stand`

## 8. Scope Note

This is a UI-facing pressure model, not the final pressure normalization or audit model.
It should remain decoupled from post-battle scoring logic.
