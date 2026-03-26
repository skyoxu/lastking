# ADR-0033: Reward Pool Exhaustion Falls Back to Gold

- Status: Accepted
- Date: 2026-03-26

## Context

PRD defines post-elite/post-boss reward as 3-choice cards.
When configured pools are exhausted, the run must remain valid and choices must still resolve deterministically.

## Decision

1. Reward generation keeps fixed 3-choice presentation.
2. If non-gold pools are exhausted, slots fall back to gold rewards.
3. If all configured pools are exhausted, full card set becomes gold variants.
4. Fallback behavior is explicit and logged in reward telemetry.

## Consequences

- No dead-end reward state.
- Deterministic replay remains intact.
- Economy balance requires fallback value governance.

## Implementation Notes

- Include pool-state tag in reward events (`catalog_available`, `exhausted`, `gold_fallback`).
- Persist selection context with seed/day/night type to support replay.
- Keep UI copy consistent for fallback-only cards.

## References

- ADR-0004
- ADR-0029
- `docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md`
