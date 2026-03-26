# ADR-0032: Independent Wave Budget Channels (Normal / Elite / Boss)

- Status: Accepted
- Date: 2026-03-26

## Context

PRD defines daily pressure growth while preserving special-night semantics:
- Normal waves scale by daily budget growth.
- Elite and Boss are special channels and must not consume normal budget weight.

## Decision

1. Use independent channels: `normal`, `elite`, `boss`.
2. `normal` channel follows base growth curve (current baseline 120% day-over-day).
3. `elite` and `boss` channels are computed independently and merged at compose phase.
4. Boss night default count remains fixed at 2 and is not auto-scaled by difficulty.

## Consequences

- Better control over pressure pacing and readability.
- Clear balancing handles for content tuning in config files.
- Requires channel-aware spawn planner and telemetry breakdown.

## Implementation Notes

- Pipeline order: resolve active channels -> compute channel budgets -> compose spawn plan.
- Record budget contributions per channel in battle report.
- Keep deterministic seed behavior stable across replays.

## References

- ADR-0030
- `docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md`
