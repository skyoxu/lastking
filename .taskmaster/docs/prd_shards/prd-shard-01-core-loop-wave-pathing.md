# LASTKING PRD Shard 01 - Core Loop, Waves, Pathing

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
- Runtime state machine for Day/Night cycles and day progression.
- Normal wave budget baseline: day1=50, daily growth=120%.
- Elite and boss channels are independent from normal budget.
- Spawn conversion strategy: continuous spawning in first 80% of night, 10s cadence.
- Last 20% of night: no new spawns.
- Boss night: fixed boss count = 2.
- Enemy AI target priority when path is valid: unit > castle > armed defense > wall/gate.
- Path blocked fallback: attack nearest blocking structure, seeded tie-break for determinism.

## Out of Scope
- Economy buildings, save/cloud, achievements, localization.

## Acceptance Anchors
- Day timeline is deterministic under a fixed seed.
- Wave channels can be tuned by config independently.
- Fully blocked path does not deadlock; fallback attack always triggers.
- Boss count remains constant across difficulties.
