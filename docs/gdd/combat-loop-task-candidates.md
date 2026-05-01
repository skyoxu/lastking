---
GDD-ID: GDD-LASTKING-COMBAT-LOOP-T47-PLUS-CANDIDATES
Title: Lastking Combat Loop T47 Plus Candidate Task Pack
Status: Draft
Owner: codex
Last Updated: 2026-05-01
Encoding: UTF-8
Applies-To:
  - docs/gdd/combat-loop-gdd.md
  - .taskmaster/tasks/tasks.json
  - docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md
  - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
  - Game.Core/Contracts/**
  - Game.Core/Services/**
  - Game.Godot/Scripts/**
ADR-Refs:
  - ADR-0018
  - ADR-0021
  - ADR-0022
  - ADR-0031
  - ADR-0032
  - ADR-0033
---

# Lastking Combat Loop T47 Plus Candidate Task Pack

## 1. Purpose

This file defines the candidate package for T47-T53 to close the combat loop.

## 2. Dependencies

- Depends on existing work from T13, T16, T17, T20, and T42-T46.
- Every task must keep references consistent across Taskmaster views and overlay pages.

## 3. Candidate Summary

- T47: unify combat entity query and shared target lookup.
- T48: enable mg tower auto-attack runtime behavior.
- T49: deploy barracks-trained units into live combat.
- T50: add projectile runtime for towers and ranged enemies.
- T51: add AoE resolver and elite pressure slice.
- T52: harden lifecycle cleanup and pooling behavior.
- T53: deliver after-action summary and player guidance.

## 4. Execution Constraints

- Reuse-first for contracts and services.
- Domain logic stays in Game.Core; Godot integration stays in Game.Godot.
- Output must include testable evidence and artifact links.

## 5. Suggested Order

1. T47
2. T48 and T49
3. T50 and T51
4. T52
5. T53

This sequence reduces risk by connecting target query and attack flow first, then closing performance and visibility.
