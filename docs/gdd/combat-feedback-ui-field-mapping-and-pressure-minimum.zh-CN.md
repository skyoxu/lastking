---
GDD-ID: GDD-LASTKING-COMBAT-FEEDBACK-FIELD-MAPPING-PRESSURE-MIN-V1
Title: Combat Feedback UI Field Mapping And Minimum Pressure Guidance
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Applies-To:
  - Game.Core/Contracts/Lastking/**
  - Game.Godot/Scripts/UI/HUD.cs
---

# Combat Feedback UI Field Mapping And Minimum Pressure Guidance

## 1. Goal

This document maps the current combat feedback UI needs to existing contracts and identifies where placeholders are still required.

## 2. Directly Reusable Contracts

Stable reusable contracts include:

- `DayStarted`
- `NightStarted`
- `WaveSpawned`
- `CastleHpChanged`
- `ResourcesChanged`
- `TimeScaleChanged`
- `TimeScaleStateDto`
- `RewardOffered`
- `UiFeedbackRaised`
- `TaxCollected`
- `TechApplied`
- `PerfSampled`
- `ConfigLoaded`

## 3. Top-Bar Mapping Summary

Directly mappable now:

- `Gold`, `Iron`, `PopulationCap`
- `DayNumber`
- `NightNumber`
- current phase via day/night start events
- `CurrentHp`
- current speed state and pause state

Placeholder-first for now:

- current wave index
- formal boss runtime UI fields
- exact in-battle hour field as a stable contract value

## 4. Bottom-Bar Mapping Summary

Placeholder-first for now:

- workers current/max
- combat units current/max
- morale value source
- skill cooldown runtime fields

Reusable now:

- tax feedback via `TaxCollected`
- tech feedback via `TechApplied`
- reward summaries via `RewardOffered`

## 5. Pressure Conclusion

There is no formal dedicated pressure event for UI yet.
However, the current HUD already has a small practical foundation using:

- `WaveSpawned`
- `CastleHpChanged`

This is enough to support a first pressure state machine without inventing a new contract immediately.
