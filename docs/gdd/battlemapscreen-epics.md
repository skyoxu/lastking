---
EPIC-DOC-ID: EPIC-LASTKING-BATTLEMAPSCREEN-V1
Title: BattleMapScreen Epics
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Input-Documents:
  - docs/gdd/battle-map-combat-ui-gdd.md
  - docs/gdd/battle-map-screen-ui-spec.zh-CN.md
  - docs/gdd/battle-map-ui-layout-and-slots.zh-CN.md
  - docs/gdd/battle-map-ui-legend-spawn-path-visual-spec.zh-CN.md
  - docs/gdd/battle-map-ui-selection-range-placement-effects-spec.zh-CN.md
  - docs/gdd/battlemapscreen-gd-responsibility-split-draft.md
  - docs/gdd/battlemapscreen-godot-scene-restructure-recommendation.zh-CN.md
  - docs/gdd/battlemapscreen-master-wireframe.zh-CN.md
  - docs/gdd/battlemapscreen-node-migration-map.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/combat-feedback-pressure-state-machine-and-copy.zh-CN.md
  - docs/gdd/combat-feedback-ui-field-mapping-and-pressure-minimum.zh-CN.md
  - docs/gdd/combat-feedback-ui-spec.zh-CN.md
  - docs/gdd/combat-loop-gdd.md
  - docs/gdd/combat-loop-task-candidates.md
  - docs/gdd/outcome-ui-spec.zh-CN.md
Excludes:
  - docs/gdd/ui-gdd-flow.md
  - docs/gdd/t1-t46-m1-wiring-audit.md
  - docs/gdd/bmad-epic-task-alignment.md
---

# BattleMapScreen Epics

## Scope

This document defines a standalone epic set for the current BattleMapScreen redesign and migration effort.
It does not replace the broader repository-level epic document.

## Epic List

### Epic 1: Stable Battle Screen Frame And Ownership

Deliver a stable BattleMapScreen entry surface that preserves existing HUD ownership, screen navigation, runtime bridge compatibility, localization, and migration-safe scene structure while introducing the new three-band battle frame.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR30, FR31, FR32, FR48, FR49, FR50, FR51, FR52, FR53, FR54, FR55, FR56, FR59, FR60

### Epic 2: Battlefield Planning And Spatial Readability

Deliver a battlefield that players can read and plan around through clear slot regions, wall boundaries, spawn-side signaling, placement legality, building selection, clipped ranges, and linked deployed-unit feedback.

**FRs covered:** FR7, FR8, FR9, FR10, FR11, FR12, FR13, FR14, FR15, FR16, FR17, FR18, FR19, FR20, FR21, FR22, FR23, FR24, FR25, FR26, FR27

### Epic 3: Combat Awareness And Pressure Feedback

Deliver high-signal combat readability so players can understand battle state, local urgency, pressure escalation, counts, skill readiness, and HUD-supported after-action guidance without relying on prototype summary text.

**FRs covered:** FR28, FR29, FR33, FR34, FR35, FR36, FR37, FR57, FR58

### Epic 4: Outcome Resolution And Run Transition

Deliver daily settlement, victory, and defeat outcome flows that clearly communicate result, reason, evidence, reward context, and the allowed next actions for continuing or ending the run.

**FRs covered:** FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46, FR47

## Dependency Notes

- Epic 1 should land first because it provides the stable screen frame and ownership boundaries required by the later epics.
- Epic 2 depends on Epic 1 for the final scene frame, but it does not depend on Epic 3 or Epic 4 to deliver player value.
- Epic 3 depends on Epic 1 for the stable HUD and battlefield-local feedback surfaces, but it does not depend on Epic 4.
- Epic 4 depends on Epic 1 for modal anchoring and runtime ownership clarity, but it does not require Epic 2 or Epic 3 to define outcome behavior.

## Delivery Intent

- Epic 1 is the migration and ownership baseline.
- Epic 2 is the battle-planning readability slice.
- Epic 3 is the in-combat readability slice.
- Epic 4 is the end-of-phase and end-of-run clarity slice.

## FR Coverage Map

- FR1-FR6 -> Epic 1
- FR7-FR27 -> Epic 2
- FR28-FR29 -> Epic 3
- FR30-FR32 -> Epic 1
- FR33-FR37 -> Epic 3
- FR38-FR47 -> Epic 4
- FR48-FR56 -> Epic 1
- FR57-FR58 -> Epic 3
- FR59-FR60 -> Epic 1
