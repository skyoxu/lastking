# PRD-LASTKING-v1.2-LOCKED-SUMMARY

## 0. Document Purpose
- Consolidated lock summary for PRD v1.2 and config contracts.
- This file is used as Taskmaster handoff input and drift guard.
- Generated date: `2026-02-10`.

## 1. Sources of Truth
- `tasks/tasks.json` (task decomposition SSoT)
- `docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md`
- `Game.Core/Contracts/Config/difficulty-config.schema.json`
- `Game.Core/Contracts/Config/difficulty-config.sample.json`
- `Game.Core/Contracts/Config/spawn-config.schema.json`
- `Game.Core/Contracts/Config/spawn-config.sample.json`
- `Game.Core/Contracts/Config/enemy-config.schema.json`
- `Game.Core/Contracts/Config/config-change-audit.schema.json`
- `Game.Core/Contracts/Config/pressure-normalization.config.schema.json`
- `Game.Core/Contracts/Config/pressure-normalization.config.sample.json`
- `Game.Core/Contracts/Config/spawn-config.validator.rules.md`
- Runtime behavior SSoT remains `Game.Core/Contracts/** + config contracts + implementation`, while PRD/GDD are not CI gate inputs.

## 2. Product and Milestone Locks
- Platform: `Steam single-player, Windows only`
- Engine: `Godot 4.5.1 + C#`
- Match duration target: `60-90 minutes`
- Day/Night cycle: `4m + 2m`
- Victory/Failure: `Survive day 15` / `Castle HP reaches 0 only`
- Vertical slice milestone: `Day1-Day15`
- Launch platform boundaries: `keyboard/mouse only`, `no gamepad`, `no mod/workshop`, `no safe-mode startup`.

## 3. Difficulty and Unlock Locks
- Difficulty mode: `config-driven` with `>=10` levels.
- Difficulty v1: `numeric-only`
- Difficulty required fields: `enemy_hp_mult, enemy_dmg_mult, budget_mult_normal, budget_mult_elite, budget_mult_boss, resource_mult`
- Match lock: `locked-at-run-start, immutable-in-match`
- Unlock policy: `clear-to-unlock-config-driven`
- Cross-tier unlock skip: `disallowed`

## 4. Spawn and Wave Locks
- Wave channels: `normal/elite/boss independent`
- Spawn timing: `linear-uniform`, active `first 80% of night`, inactive `no new spawns`
- Spawn refresh step: `10s`
- Boss night: day `15`, boss count `config-driven (default 2)`, scaling `config-driven`
- Clone policy: `global cap 10`; clone kills counted in report; clones do not consume boss channel budget.
- Clone despawn rule: clones despawn `immediately` when parent boss dies.
- Normal mob tag baseline (config-overridable):
  - Day1-3: `basic70/fast20/heavy10/ranged0/suicide0`
  - Day4-6: `basic55/fast20/heavy15/ranged10/suicide0`
  - Day7-9: `basic45/fast20/heavy20/ranged10/suicide5`
  - Day10(Elite-night mobs): `basic35/fast20/heavy20/ranged15/suicide10`
  - Day11-14: `basic35/fast18/heavy22/ranged15/suicide10`
  - Day15(Boss-night mobs): `basic25/fast20/heavy25/ranged20/suicide10`
- Spawn algorithm: `greedy_fill + seeded_weighted_topk`
- Weight formula: `spawn_weight * night_type_weight`
- night_type_weight_semantics: `sampling-priority-only-not-channel-budget`
- Seed formula: `run_seed + day + lane + spawn_tick`
- Top-K: `5`
- Lane ratio default: `50/50`, overridable `True`
- remainder_budget: `discard`
- on_no_eligible_candidates: `discard_tick_budget`

## 5. Target Priority and Combat Locks
- Enemy target priority (path reachable): `player_units, castle, armed_defense_buildings, walls_and_gate`
- Player target priority (within range): `from-map-center-outward`
- Path block fallback remains nearest-blocking-structure by path cost.
- If no blocking structure exists after path fail, fallback target is `castle` (no idle wait).
- Gate policy: enemy cannot pass alive gate, can attack gate body, destroyed gate becomes passable.
- Player attacks remain no-friendly-fire.
- Damage pipeline order: `base_damage -> offense_defense_modifiers -> difficulty_modifiers -> armor_reduction -> min_damage_clamp_1`.
- Tick order lock: `Input -> Economy -> Queue -> Spawn -> Targeting -> Attack -> Damage -> DeathCleanup -> RewardState -> SaveSnapshot`, and reward state cannot re-enter in same tick.

## 6. Economy, Queue, and Refund Locks
- Numeric precision: `integer-only (deterministic rounding rule)`
- Integer rounding pipeline: `multiply-then-divide + floor`, no round-half-up.
- Gold balance policy: `negative-allowed`
- Core resources: `gold + iron_ore only`; population cap and tech points are system counters.
- Debt spend policy when gold `< 0`: `positive-cost actions disallowed (build/upgrade/train/repair/reroll), zero-cost actions allowed`
- Debt cross-zero behavior: in-progress spend actions continue to completion; only new spend requests are blocked.
- Debt unlock threshold: spending unlocks immediately when gold returns to `>=0`.
- Debt hard guardrails: min gold `-3000`; when gold `< 0`, tax multiplier is fixed to `70%`; guardrails lift when gold `>= 0`.
- Debt failure gate: `none` (no extra defeat condition from debt duration).
- Queue on building destroyed: `cancel-and-100%-refund`
- Queue charge timing: `build/train/upgrade charged at enqueue-time`
- Upgrade destroyed refund: `0%`
- Upgrade cancel allowed/refund: `True` / `100%`
- Repair interruption refund: `0%`
- Repair debt behavior: `repair pauses immediately when gold < 0`, resumes when gold `>=0`
- Build soft limit scope: `player-global 100ms per placement`.

## 7. Reward and Report Locks
- Reward rule: `3-choice once per night, non-gold unique non-stack, insufficient non-gold -> direct gold fallback`
- Reward states: `catalog_available, exhausted, gold_fallback`
- Reward fallback: `gold` amount `600`, repeat `True`, all-options-gold `True`
- Reward popup timing: `immediately after night settlement`; day/night timer pauses until selection.
- Gold fallback scaling: `fixed 600`, not affected by difficulty multipliers.
- Battle report fields: `total_kills, kills_by_unit_type, building_losses, economy_income, tech_points_spent, max_pressure_night`
- Max pressure formula: `(35*budget_p + 25*spawn_count_p + 20*castle_damage_p + 10*blocked_path_p + 10*elite_boss_p)/100`; range `0-100 integers`; tie-breaker `later-night-wins`
- Max pressure normalization lock:
  - `budget_p=floor(100*normal_budget_used/normal_budget_ref_day)`
  - `spawn_count_p=floor(100*spawned_count/spawned_ref_day)`
  - `castle_damage_p=floor(100*castle_damage/castle_max_hp)`
  - `blocked_path_p=floor(100*blocked_seconds/night_seconds)`
  - `elite_boss_p=clamp(base_by_night + min(20, elite_spawned*2 + boss_spawned*10),0,100)`
  - `base_by_night={normal:10, elite:60, boss:70}`
- Battle report display order: `Outcome, Losses, Combat, Combat Detail, Economy, Progression`
- Battle report actionable suggestions: `disabled`
- Battle report seed: `visible + copyable`
- Battle report export: `json enabled`, includes `run_seed, config_hash, max_pressure_night, total_kills, kills_by_unit_type, building_losses, economy_income, tech_points_spent`
- Battle report export path: `logs/e2e/<YYYY-MM-DD>/`
- Battle report identity field: `player_slot_id_only` (no Steam account identifier)
- Battle report retention: `retain-30-days-auto-cleanup` (cleanup timing: `on-startup`)
- Achievement trigger lock: each achievement must bind a unique `achievement_event_id`
- Achievement event id pattern: `ACH_<DOMAIN>_<ACTION>`
- Achievement anti-cheat lock: `unofficial-config => achievements disabled`
- Achievement disable notice policy: `show-on-every-load-when-unofficial`
- Non-official config detection: `bundle-hash allowlist + schema-version support`, either check fail => unofficial
- Config hash allowlist granularity: `bundle-hash-plus-manifest`
- Achievement backfill policy: `disabled` when achievement condition changes (new runs only)

## 8. UX, Save, i18n, and Permissions Locks
- Default display mode: `fullscreen`
- Input: `keyboard rebinding enabled`, `mouse sensitivity enabled`
- UI scale presets: `80/100/125/150`
- Accessibility (v1): `no high-contrast`, `no color-blind mode`
- Audio channels (v1): `music + sfx`
- Custom seed start: `supported`
- Time controls: `pause, 1x, 2x`
- Pause semantics: `full-freeze-all-timers` (repair/queue/spawn tick/night timer all frozen)
- Performance mode: `preset-required` for low-spec stability
- Performance mode fixed toggles: `shadows=off, post_processing=off, particle_density=50%, healthbar_animation=off`
- Performance mode switch timing: `in-match allowed`, and persisted to global setting
- Tutorial mode: `3-step onboarding (housing -> barracks -> survive first night)`
- Tutorial skip/replay: `allowed` / `settings-menu-available`
- Starter protection: `none`
- Save policy: `single autosave only, no overwrite by manual save`
- Save scope: `steam-account`
- Steam Cloud save size limit: `5MB` per save
- Autosave corruption recovery: `fallback-to-last-valid-day-snapshot-if-exists-else-new-run`
- Non-official config save policy: `allow-official-save`
- External-test gate for non-official save: `dirty-save-only`
- External-test gate trigger: `BUILD_CHANNEL in {external_test, beta}`
- Dirty-save slot policy: `single-slot + red unofficial label`
- Dirty-save policy: `no-cloud-sync` and `achievements-disabled`
- Save migration policy: `forced migration with pre-backup`; on failure `reject-load-and-suggest-delete-save`
- Save migration semver policy: `major=forced-migrate`, `minor/patch=auto-compatible-migrate`
- Defeat flow: `show battle report then exit to menu`
- Quick restart entry: `disabled`
- i18n languages: `zh-CN, en-US`
- Reserved next languages: `ru-RU, es-419, pt-BR, de-DE`
- i18n switching/persistence: `in-match supported` / `remember-last-selection`
- i18n key freeze: `not mandatory`, covered by regression checks
- Cloud conflict policy: `binary-choice-local-or-cloud-local-default` (no diff fields)
- Cloud conflict default selection setting: `user-configurable in settings`
- Cloud sync failure fallback: `continue-local-and-retry-later` with backoff `30s -> 2m -> 10m` (cap 10m)
- Config edit permissions: `all-except-guest`
- Config hot reload in match: `disabled` (new config applies on new run only)

## 9. Config Safety Locks
- Config change audit policy: `mandatory-audit-log-and-review-for-core-config`.
- Core config audit scope: `difficulty, spawn, enemy`.
- Required audit fields: `actor, timestamp, file, change_summary`.
- Audit log path: `logs/ci/<YYYY-MM-DD>/config-change-audit.jsonl`.
- Audit schema path: `Game.Core/Contracts/Config/config-change-audit.schema.json`.
- Audit writer source: `script-only` (manual edit forbidden).
- Config version mismatch policy: `reject-load-and-block-match`
- Config load failure policy: `reject-load-and-block-match`
- Validation baseline: `spawn-config.validator.rules.md` (R-001..R-011).
- Allowlist governance (current phase): `open-write, no extra approval` (high risk)
- Allowlist dual approval: `disabled` (kept unchanged)
- Config hash algorithm: `sha256-rfc8785-canonical-json`
- Canonical JSON spec: `rfc8785`
- Allowlist signature algorithm: `ed25519`
- Allowlist public key source: `embedded-readonly-resource`
- Allowlist rollback policy: `signed-only, max recent 3, audit-required`
- Allowlist rollback audit log path: `logs/ci/<YYYY-MM-DD>/allowlist-rollback.jsonl`

## 10. Difficulty KPI Baseline (Adjustable Targets)
- Performance baseline: `Avg >= 60 FPS`, `1% Low >= 45 FPS`, `logic P95 <= 16.6ms`, `logic P99 <= 22.0ms`, `cold start <= 3.0s` at `1024x768`.

| Difficulty ID | Target Win Rate |
|---|---|
| story | 85%-95% |
| casual | 75%-85% |
| easy | 65%-75% |
| normal | 60%-70% |
| advanced | 45%-55% |
| hard | 25%-35% |
| expert | 15%-25% |
| nightmare | 8%-15% |
| hell | 3%-8% |
| impossible | 1%-3% |

## 11. PRD Seal Criteria (Recommended Lock)
- `all-p0-decisions-cleared`
- `prd-locked-summary-and-config-contracts-consistent`
- `daily-gate:30-seeded-runs-pass(normal/hard/impossible=10/10/10)`
- `release-gate:100-seeded-runs-pass(10-difficulties*10)`
- `hotfix-gate:30-seeded-runs-impacted-difficulties-covered`
- `day1-day15-end-to-end-playable-without-blocker`
- `battle-report-min-fields-all-present`
- `taskmaster-preflight-config-hash-allowlist-generated`
- `post-freeze-behavior-change-requires-adr-prd-summary-and-tests`
- `post-freeze-behavior-change-budget:not-enforced`
- `post-freeze-behavior-change-test-id:not-required`
- `release-blockers-section:not-required`
- `v1.2-freeze-date:not-required`

## 12. i18n Release Hard Gate
- Critical HUD texts must have no truncation and no overlap in both `zh-CN` and `en-US`.
- i18n hard gate validation: `auto-screenshot-diff-plus-manual-spotcheck`.

## 13. Deferred and Drift Guard
- Reward schema policy: `defer-to-task-execution-phase`
- Any lock change must update PRD machine appendix, this summary, and related schema/sample contracts in the same change set.
- Behavior-scope changes require ADR update/supersede before implementation tasks proceed.

