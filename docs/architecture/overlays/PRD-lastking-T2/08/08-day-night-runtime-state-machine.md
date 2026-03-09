---
review_status: approved
reviewed_by: game-architect
reviewed_at: 2026-03-09
---

# Day/Night Runtime State Machine

## Day/Night State Diagram
- States: `Day`, `Night`, `Terminal`.
- Start state: `Day` at `Day1`.
- Legal transitions: `Day -> Night`, `Night -> Day` (until Day15), `Night -> Terminal` (when Day15 completes).

## Transition Conditions
- Day duration threshold: `240` seconds.
- Night duration threshold: `120` seconds.
- Day counter rules: starts at `1`, increments by `1` only on `Night -> Day` transition, never exceeds `15`.
- Terminal rule: after Night phase of Day15 reaches threshold, transition to `Terminal` exactly once.
- Update source: process-loop driven updates only; no progression when updates are inactive.

## Downstream Event Consumers
- Core tests consume checkpoint and terminal signals for deterministic validation.
- UI integration tests consume observable day/phase values for HUD rendering.
- Runtime manager emits deterministic checkpoint and terminal signals for downstream systems.
