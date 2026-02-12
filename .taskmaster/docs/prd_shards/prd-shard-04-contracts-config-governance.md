# LASTKING PRD Shard 04 - Config Contracts and Governance

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
- Config contract set:
  - enemy-config.schema.json
  - difficulty-config.schema.json
  - spawn-config.schema.json
  - pressure-normalization.config.schema.json
- Sample files validation:
  - difficulty-config.sample.json
  - spawn-config.sample.json
  - pressure-normalization.config.sample.json
- Pressure normalization uses explicit baseline constants and range checks.
- Difficulty is locked at run start and cannot change mid-match.
- Invalid config handling policy: reject and fallback to built-in safe default where applicable.
- Versioning and migration rule: force migration, no dual-version compatibility mode.
- Config governance: gameplay tuning via config only, no implementation hardcoding.
- Determinism constraints: same seed + same config => same wave composition timeline.
- Auditability: config hash/version appears in battle report metadata.

## Out of Scope
- Frontline gameplay content expansion outside schema boundaries.

## Acceptance Anchors
- All schemas validate samples cleanly.
- Unknown keys/invalid ranges are handled by policy and logged.
- Contracts remain backward-trackable via explicit version fields.
