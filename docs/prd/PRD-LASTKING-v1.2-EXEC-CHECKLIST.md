# PRD-LASTKING-v1.2-EXEC-CHECKLIST

## 0. Purpose
- This checklist is the execution gate for implementing `PRD-LASTKING-v1.2-GAMEDESIGN`.
- Goal: convert locked design rules into deterministic implementation tasks with minimal drift.
- Scope: runtime behavior, config contracts, validation artifacts, and Taskmaster preflight.
- Note: PRD/GDD are planning artifacts; runtime authority remains contracts/config/code.

## 1. Preflight Gate
- [ ] Confirm source files are aligned:
  - `docs/prd/PRD-LASTKING-v1.2-GAMEDESIGN.md`
  - `docs/prd/PRD-LASTKING-v1.2-LOCKED-SUMMARY.md`
  - `Game.Core/Contracts/Config/*.schema.json`
  - `Game.Core/Contracts/Config/*.sample.json`
- [ ] Confirm no unresolved P0 decisions remain.
- [ ] Confirm current lock set includes:
  - seed visible/copyable/exportable
  - Steam-account save scope
  - binary cloud conflict choice
  - non-official config achievement disable
  - in-match config hot reload disabled

## 2. Config Contract Gate
- [ ] Difficulty contract locked:
  - linear unlock chain, no cross-tier skip
  - 10+ levels, numeric-only v1
- [ ] Spawn contract locked:
  - independent channels (normal/elite/boss)
  - weighted Top-K seeded generation
  - lane split config-driven
- [ ] Enemy contract locked:
  - tag semantics and day ranges
  - elite/boss consistency checks
- [ ] Pressure normalization contract locked:
  - `pressure-normalization.config.schema.json`
  - `pressure-normalization.config.sample.json`
  - mandatory references: `normal_budget_ref_day`, `spawned_ref_day`
- [ ] Config governance contract locked:
  - hash allowlist granularity = `bundle-hash-plus-manifest`
  - non-official detection = bundle hash allowlist + schema version support

## 3. Runtime Determinism Gate
- [ ] Tick order implemented exactly:
  - `Input -> Economy -> Queue -> Spawn -> Targeting -> Attack -> Damage -> DeathCleanup -> RewardState -> SaveSnapshot`
- [ ] Reward state machine cannot re-enter in the same tick.
- [ ] Integer-only pipeline with floor rounding end-to-end.
- [ ] Pause semantics fully frozen:
  - queue timer, repair timer, spawn tick, night timer
  - reward popup timer, tutorial timer, hint expiry timer

## 4. Economy and Queue Gate
- [ ] Enqueue-time charging is enforced for build/train/upgrade.
- [ ] Debt behavior is enforced:
  - negative gold allowed
  - positive-cost new actions blocked
  - zero-cost actions allowed
  - in-progress actions continue
  - repair pauses when gold < 0, resumes when gold >= 0
- [ ] No additional debt-based fail condition exists.

## 5. Combat and Pathing Gate
- [ ] Path-fail fallback implemented:
  - nearest blocking structure by path cost
  - no blocker => attack castle
- [ ] Gate policy implemented:
  - enemy cannot pass alive gate
  - enemy can attack gate
  - destroyed gate becomes passable
- [ ] Boss clone lifecycle locked:
  - global cap 10
  - despawn immediately on boss death (same tick)

## 6. Save, Cloud, and Migration Gate
- [ ] Save scope = Steam account isolation.
- [ ] Auto-save behavior locked:
  - single slot
  - trigger at day start
  - not overwritable by manual save
- [ ] Migration strategy locked:
  - major => forced migrate
  - minor/patch => auto-compatible migrate
  - migrate failure => reject load and suggest deleting save
- [ ] Cloud conflict UI locked:
  - binary local/cloud choice only
  - default preselect configurable in settings
  - no diff field view

## 7. Achievement and Integrity Gate
- [ ] Achievement IDs follow `ACH_<DOMAIN>_<ACTION>`.
- [ ] Each achievement binds a unique event ID.
- [ ] Non-official config disables achievements.
- [ ] Disable notice appears on every load when unofficial config is detected.

## 8. Battle Report and Forensics Gate
- [ ] Battle report fields complete and ordered.
- [ ] `run_seed` is visible and copyable.
- [ ] Export JSON enabled with minimum fields:
  - `run_seed`
  - `config_hash`
  - `max_pressure_night`
  - `total_kills`
  - `kills_by_unit_type`
  - `building_losses`
  - `economy_income`
  - `tech_points_spent`
- [ ] Export path fixed:
  - `logs/e2e/<YYYY-MM-DD>/`
- [ ] Identity export policy:
  - export `player_slot_id` only
  - do not export Steam account identifier

## 9. Performance and UX Gate
- [ ] Performance mode preset exists (required for launch).
- [ ] Fixed toggles in performance mode:
  - shadows off
  - post-processing off
  - particle density = 50%
  - healthbar animation off
- [ ] Baseline targets remain locked:
  - Avg FPS >= 60
  - 1% low >= 45
  - logic frame P95 <= 16.6ms
  - logic frame P99 <= 22.0ms

## 10. Taskmaster Preflight Gate
- [ ] Generate and freeze config hash allowlist before task decomposition.
- [ ] Allowlist must include manifest and full package hash.
- [ ] If preflight artifacts missing, block Taskmaster decomposition.
- [ ] Required preflight output:
  - `logs/ci/<YYYY-MM-DD>/config-hash-allowlist.json`
  - `logs/ci/<YYYY-MM-DD>/config-hash-allowlist.manifest.json`

## 11. Validation Gate (Local)
- [ ] Run contract sync check:
  - `py -3 scripts/python/config_contract_sync_check.py`
- [ ] Save report:
  - `logs/ci/<YYYY-MM-DD>/config-contract-sync-check.json`
- [ ] Confirm no lock drift before implementation tasks.

## 12. Stop-Loss Rules
- Any behavior change touching locked rules must update all four in one change set:
  1) `PRD-LASTKING-v1.2-GAMEDESIGN.md`
  2) `PRD-LASTKING-v1.2-LOCKED-SUMMARY.md`
  3) related `schema/sample` contracts
  4) validation rules/check scripts if lock keys changed
- If not fully synced, do not start implementation tasks.
