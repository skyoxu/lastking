---
GDD-ID: GDD-LASTKING-COMBAT-LOOP-V1
Title: Lastking Combat Loop GDD
Status: Draft
Owner: codex
Last Updated: 2026-05-01
Encoding: UTF-8
Applies-To:
  - .taskmaster/tasks/tasks.json
  - .taskmaster/docs/prd.txt
  - docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md
  - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
  - docs/gdd/ui-gdd-flow.md
  - docs/gdd/t1-t46-m1-wiring-audit.md
  - Game.Core/Contracts/**
  - Game.Core/Services/**
  - Game.Godot/Scripts/**
ADR-Refs:
  - ADR-0011
  - ADR-0018
  - ADR-0021
  - ADR-0022
  - ADR-0031
  - ADR-0032
  - ADR-0033
---

# Lastking Combat Loop GDD

## 1. Document Goal

This document defines the playable combat loop target for Lastking.
It links completed capabilities from T1-T46 with the planned expansion tasks T47-T53.

## 2. Target Player Experience

- Day phase focuses on economy and defense preparation.
- Night phase focuses on combat pressure and survival.
- A full loop exists from build, spawn, target, attack, damage, cleanup, and after-action feedback.

## 3. Combat Loop

Canonical runtime order:

Input -> Economy -> Queue -> Spawn -> Targeting -> Attack -> Damage -> DeathCleanup -> RewardState -> SaveSnapshot

Guardrails:

- Target priority follows ADR-0031 hierarchy intent.
- Damage flow follows base damage, modifiers, mitigation, and minimum clamp.
- Runtime cleanup must prevent sustained object growth in long sessions.

## 4. Capability Slices

1. Building placement and defense setup
2. Barracks training and deployment
3. Enemy spawn and wave scheduling
4. Enemy pathing and target selection
5. Enemy attack model and damage behavior
6. Defense building targeting and attack
7. Friendly combat unit targeting and attack
8. Death cleanup and pooling lifecycle
9. Combat visibility and after-action guidance in HUD

## 5. Completed Coverage in T1-T46

- Foundations are already in place across T01-T10, T11-T20, T21-T30, T31-T40, and T41-T46.
- Remaining combat completeness gaps are concentrated in registry, auto-attack, barracks combat units, projectiles, AoE, lifecycle hardening, and summary guidance.
- These gaps are mapped to T47-T53 with reuse-first implementation.

## 6. T47-T53 Package Intent

- T47: Combat Entity Registry And Shared Target Query
- T48: Activate MgTower Auto Attack Runtime
- T49: Spawn Barracks Units Into Battlefield Runtime
- T50: Projectile Runtime For Towers And Ranged Enemies
- T51: Area Damage Resolver And Elite Pressure Slice
- T52: Combat Lifecycle And Pooling Hardening
- T53: Battle After Action Summary And Guidance

## 7. Reuse-First Constraints

- Reuse existing contracts first; add new contracts only when evidence shows a real gap.
- Keep domain logic in Game.Core and engine adaptation in Game.Godot.
- Reuse existing UI surfaces delivered in T42-T46.

## 8. Acceptance Outcome

The loop is considered closed when:

- The player can complete one full combat cycle with meaningful outcomes.
- HUD presents pressure, outcome, and next-step guidance.
- Lifecycle churn is controlled under long-session pressure.
- Task, overlay, and evidence links remain consistent.
