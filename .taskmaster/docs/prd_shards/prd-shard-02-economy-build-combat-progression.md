# LASTKING PRD Shard 02 - Economy, Build, Combat, Progression

## Global Locked Constraints
- Platform: Steam single-player, Windows only, Godot 4.5.1 + C#.
- Match duration target: 60-90 minutes.
- Day/Night: 4 min Day + 2 min Night, Day1-Day15.
- Win: survive Day15. Lose: castle HP = 0.
- Friendly fire: disabled for all player attacks.
- Config-first balancing: no hardcoded balance values.

## Task Generation Rules
- Generate exactly 10 top-level tasks for this shard.
- Keep each top-level task complexity <= 6.
- Each task must include concrete acceptance criteria and test strategy.
- Use Windows-compatible command and pipeline assumptions.


## In Scope
- Initial resources: gold/iron/pop-cap = 800/150/50.
- Economy: residence provides tax every 15s (+gold) and population.
- Core buildings: castle, residence, mine, barracks, MG tower, wall, mine trap.
- Building footprint rules: castle=4, barracks=2, MG tower=1, wall=1.
- Wall supports drag-line build mode, can fully block path with gate one-way logic.
- Upgrade rules: max level=5, no level skipping.
- Upgrade cannot run with repair simultaneously; upgrade completion restores full HP.
- Repair economy: total full repair cost = 50% of build cost (gold only), incremental over time.
- Unit training queue constraint: one queue per barracks, cancellable with 100% refund.
- Tech tree affects unit stats only (atk speed %, damage, production speed %, range, HP, cost %).
- Reward system: nightly 3-choice reward, one trigger per night, separate pools by night type.
- Reward fallback: if pool exhausted, grant gold only.

## Out of Scope
- Cloud save transport details and schema governance internals.

## Acceptance Anchors
- Economy and queues are integer-safe (no floating-point drift in resources).
- Cancel and refund policies are deterministic and auditable.
- No hidden hardcoded unlock path; unlocks are config driven.
