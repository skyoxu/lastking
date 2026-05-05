---
DOC-ID: DOC-LASTKING-BATTLEMAPSCREEN-IMPLEMENTATION-ORDER-V1
Title: BattleMapScreen Implementation Order
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Based-On:
  - docs/gdd/battlemapscreen-epics.md
  - docs/gdd/battlemapscreen-stories.md
  - docs/gdd/battlemapscreen-gd-responsibility-split-draft.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/battlemapscreen-node-migration-map.md
---

# BattleMapScreen Implementation Order

## Goal

This document defines the recommended implementation order for the current BattleMapScreen redesign.
It exists to prevent task-order drift before task decomposition begins.

## Ordering Principles

- Land stable ownership boundaries before high-detail UI work.
- Preserve existing test-visible paths until the dedicated migration story.
- Move prototype responsibilities out of `BattleMapScreen.gd` before adding new battlefield controllers.
- Prefer delivering one readable and testable slice at a time.

## Recommended Order

### Phase 1: Frame And Ownership Baseline

1. `Story 1.1` Introduce The Stable Battle Screen Frame
2. `Story 1.2` Isolate Legacy Prototype And Runtime Ownership
3. `Story 1.3` Reduce `BattleMapScreen.gd` To A Coordinator
4. `Story 1.4` Preserve Existing Global UI Ownership And Localization

Why first:

- later stories depend on a stable scene frame
- ownership mistakes here would multiply downstream rework
- current tests and bridge behavior can still stay intact

### Phase 2: Battlefield Planning Readability

5. `Story 2.1` Build The Battlefield Region And Slot Layout
6. `Story 2.2` Implement Placement Legality Overlays
7. `Story 2.3` Implement Building Selection And Clipped Range Feedback
8. `Story 2.4` Implement Spawn-Side And Path Readability Cues

Why second:

- this is the first player-facing Battle Map UI slice
- placement and selection should not be built on top of an unstable node tree

### Phase 3: Combat Readability

9. `Story 3.1` Implement Battlefield-Local Combat Feedback Surfaces
10. `Story 3.2` Implement Pressure State Mapping With Existing Inputs
11. `Story 3.3` Implement Bottom-Bar Combat State And Action Readability
12. `Story 3.4` Replace Prototype Summary Text With Production Feedback Ownership

Why third:

- combat feedback becomes much cleaner once the battlefield layers exist
- pressure and local prompts should reuse the new ownership model rather than fight it

### Phase 4: Outcome Resolution

13. `Story 4.1` Implement Daily Settlement Outcome Modal
14. `Story 4.2` Implement Victory Outcome Modal
15. `Story 4.3` Implement Defeat Outcome Modal
16. `Story 4.4` Implement Outcome Evidence And Transition Wiring

Why last:

- outcome flow depends on stable modal anchors and clear UI ownership
- outcome presentation is easier once the battle and feedback layers already exist

## Test Migration Rule

- Keep current `Margin/VBox/...` paths alive during Phase 1 unless a story explicitly migrates tests.
- Do not delete the legacy prototype subtree before production feedback surfaces replace its behavior.
- The story that finally removes legacy prototype nodes must also own the synchronized test-path migration.

## Definition Of Done For Each Story

- The intended scene or script ownership is clear and non-duplicated.
- `HUD` global ownership is preserved where required.
- Any new controller has a narrow responsibility boundary.
- Existing tests still pass, or test updates are included in the same story.
- No new placeholder runtime contract is invented if existing fields are sufficient.

## Recommended First Task Slice

If task decomposition starts immediately, the best first implementation slice is:

1. `Story 1.2`
2. `Story 1.3`

Reason:

- this creates the migration-safe base for nearly every later UI story
- it reduces the current highest-risk concentration in `BattleMapScreen.gd`
- it preserves momentum without forcing immediate visual completion
