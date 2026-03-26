# ADR-0031: Path Blocking Fallback to Nearest Blocker

- Status: Accepted
- Date: 2026-03-26

## Context

In the lastking runtime, pathing can become temporarily unreachable due to dynamic wall placement.
The game design requires enemies to remain active pressure sources instead of idling indefinitely.

## Decision

1. When the direct path to target is blocked, enemy AI enters blocker-attack fallback mode.
2. Fallback target is selected by nearest reachable blocking structure on the current lane.
3. AI periodically reevaluates path availability and returns to primary objective once route is restored.

## Consequences

- Avoids idle enemy states in blocked topology.
- Preserves deterministic pressure for day/night pacing.
- Increases need for clear state transitions and telemetry in AI loop.

## Implementation Notes

- Track states: `Pathing -> BlockerAttack -> Reevaluate -> Pathing`.
- Keep deterministic tie-breaker using seed/day/tick dimensions.
- Emit contract events through `Game.Core/Contracts/**` when fallback state changes are externally observable.

## References

- ADR-0030
- `docs/architecture/overlays/PRD-lastking-T2/08/08-day-night-runtime-state-machine.md`
