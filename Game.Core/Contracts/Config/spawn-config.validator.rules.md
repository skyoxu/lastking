# spawn-config.validator.rules

## 1. Purpose
Define runtime semantic validation rules for `spawn-config.sample.json` and future spawn configs.
These rules complement JSON Schema and cover cross-field constraints that schema alone cannot guarantee.

## 2. Scope
- Target file: `Game.Core/Contracts/Config/spawn-config*.json`
- Validation phase: startup (pre-match) and CI config checks
- Failure policy: fail closed with safe fallback (as locked in PRD)

## 3. Inputs
- `spawn-config.json`
- `difficulty-config.json`
- Optional: `enemy-config.json`

## 4. Hard-Fail Rules
Any hard-fail must trigger:
1) structured warning log
2) fallback to built-in safe default
3) telemetry marker for diagnosis

### R-001 Lane Ratio Sum
- `lane_ratio.default.left + lane_ratio.default.right == 100`
- If override exists, each override pair must also sum to 100

### R-002 Spawn Window Sum
- `active_spawn_window_percent + inactive_spawn_window_percent == 100`
- Active window must be greater than 0

### R-003 Positive Cadence
- `refresh_step_seconds >= 1`
- `top_k >= 1`

### R-004 Night Schedule Validity
- `elite_days` and `boss_days` contain only values in `[1, 15]`
- Day entries must be unique within each list
- A day cannot be both elite and boss

### R-005 Boss Constraints
- `boss_count.mode == config-driven`
- `boss_count.default >= 1` (current lock default is `2`)
- If provided, `boss_count.by_day` keys must be in `[1, 15]`
- If provided, `boss_count.by_day_and_difficulty` entries override `by_day` and `by_difficulty`

### R-006 Source Mapping Integrity
- `channel_budget_multipliers_source.normal == difficulty.budget_mult_normal`
- `channel_budget_multipliers_source.elite == difficulty.budget_mult_elite`
- `channel_budget_multipliers_source.boss == difficulty.budget_mult_boss`

### R-007 Sampling Contract
- `spawn_model == greedy_fill_plus_seeded_weighted_topk`
- `weight_formula == spawn_weight * night_type_weight`
- `night_type_weight_semantics == sampling-priority-only-not-channel-budget`
- `deterministic_tie_breaker == seeded_pseudo_random`

### R-008 Remainder Budget Policy
- `remainder_budget_policy == discard`
- Runtime must never carry remainder across ticks or waves
- If a spawn tick has no eligible candidates, tick budget must be discarded (`on_no_eligible_candidates == discard_tick_budget`)

### R-009 Deterministic Seed Contract
- `seed_formula == run_seed + day + lane + spawn_tick`
- All random branches in spawn generation must use deterministic RNG pipeline

## 5. Cross-File Rules (Spawn + Difficulty)
### R-010 Difficulty Level Resolution
- Runtime selected difficulty must exist in difficulty config
- Difficulty is locked at run start and cannot change mid-match

### R-011 Budget Multiplier Availability
- Selected difficulty level must provide all three fields:
  - `budget_mult_normal`
  - `budget_mult_elite`
  - `budget_mult_boss`

## 6. Recommended Warning-Only Rules
### W-001 Extreme Weight Skew
Warn if `max(night_type_weights) / min(night_type_weights) > 5`.

### W-002 Cadence Stress
Warn if `refresh_step_seconds < 5` because this may increase update pressure.

### W-003 Empty Tick Streak
Warn if no eligible candidates appear for more than 3 consecutive ticks.

## 7. Logging Contract
Each validation run should emit at least:
- `rule_id`
- `severity` (`hard_fail` or `warning`)
- `field_path`
- `observed_value`
- `expected_condition`
- `resolution` (`fallback` / `continue`)
- `ts`

## 8. Exit Criteria
A config is accepted only when:
- no hard-fail rules are violated
- runtime fallback was not required
- warning count is recorded for balancing review
