---
PRD-ID: PRD-lastking-T2
Title: 功能纵切 — Lastking T2 Core Loop
Status: Locked
ADR-Refs:
  - ADR-0011
  - ADR-0018
  - ADR-0021
  - ADR-0022
  - ADR-0031
  - ADR-0032
  - ADR-0033
Test-Refs:
  - Game.Core.Tests/State/GameStateMachineTests.cs
  - Game.Core.Tests/Domain/GameConfigTests.cs
  - Tests.Godot/tests/UI/test_hud_scene.gd
  - Tests.Godot/tests/Integration/test_backup_restore_savegame.gd
---

本页定义 Lastking T2 的执行边界：在 Windows only + Godot 4.5 + C# 条件下，交付可验证的昼夜高压塔防核心循环。

## Runtime Boundary

- In Scope：昼夜循环、刷怪压力、建造/升级/维修、训练队列、奖励三选一、胜负判定、存档恢复。
- Out of Scope：联机、手游、多平台发布、Mod/创意工坊、运行期热更新程序集。

## Domain Entities

| Entity | Responsibility | Key State | Notes |
| --- | --- | --- | --- |
| Castle | Core survival objective | hp, level | hp=0 is hard fail |
| Residence | Economy and population | tax_tick, pop_cap | tax drives gold economy |
| Mine | Secondary resource provider | iron_tick | replaces legacy wood semantics |
| Barracks | Unit production queue | queue, unlocks | one queue per barracks |
| MachineGunTower | Persistent defense | hp, dps, range | no friendly fire |
| WallSegment | Path blocking defense | hp, length | supports drag placement |
| Landmine | One-shot defense | armed/disarmed | consumed immediately on trigger |
| RangedUnit | Player army actor | hp, atk, speed | auto-combat only |
| EnemyWave | Night pressure carrier | budget, composition | elite/boss are independent budget tracks |
| RewardOffer | Post-night progression choice | pool_state | 3-choice once, no stack, no duplicate |

## Event Contracts

事件命名遵循 `${DOMAIN_PREFIX}.<entity>.<action>`，首版约定 `${DOMAIN_PREFIX}=core.lastking`。

- `core.lastking.day.started`
- `core.lastking.night.started`
- `core.lastking.wave.spawned`
- `core.lastking.castle.hp_changed`
- `core.lastking.reward.offered`
- `core.lastking.save.autosaved`

## Runtime State Machine

| State | Entry Condition | Exit Condition | Failure Path |
| --- | --- | --- | --- |
| Boot | New run or load run | Config validated | invalid config -> reject load |
| DayPhase | Boot/ResolveNight finished | 4-minute timer elapsed | fatal runtime error -> stop and log |
| NightPhase | Day timer elapsed | all scheduled waves resolved | castle hp <= 0 -> Defeat |
| ResolveNight | Night ended | reward resolved | reward pool exhausted -> gold fallback |
| Victory | survived target day | terminal | none |
| Defeat | castle hp <= 0 | terminal | none |

## Failure Paths

- Config validation failure: reject run start/load and show deterministic error code.
- Save migration failure: reject load, show repair recommendation, keep original save untouched.
- Missing contract file: fallback to built-in safe defaults and emit audit line.
- Runtime queue inconsistency: halt affected subsystem only, keep simulation deterministic.

## Acceptance Anchors

- Core loop is deterministic under fixed seed and identical config snapshot.
- Day/Night state transitions are recoverable after autosave load at day boundary.
- Reward choice applies once per qualified night and cannot be duplicated.
- Enemy targeting priority obeys `nearest blocker` fallback policy from ADR-0031.
- Elite/Boss budget isolation obeys ADR-0032, pool exhaustion fallback obeys ADR-0033.

## Canonical References

- Contracts root: `Game.Core/Contracts/Config/`
- Enemy schema: `Game.Core/Contracts/Config/enemy-config.schema.json`
- Difficulty schema: `Game.Core/Contracts/Config/difficulty-config.schema.json`
- Spawn schema: `Game.Core/Contracts/Config/spawn-config.schema.json`
- Pressure normalization schema: `Game.Core/Contracts/Config/pressure-normalization.config.schema.json`
- Core state machine tests: `Game.Core.Tests/State/GameStateMachineTests.cs`
- Config domain tests: `Game.Core.Tests/Domain/GameConfigTests.cs`
- HUD scene test: `Tests.Godot/tests/UI/test_hud_scene.gd`
- Save/restore integration test: `Tests.Godot/tests/Integration/test_backup_restore_savegame.gd`
- Triplet validation script: `scripts/python/validate_task_master_triplet.py`

## Task Mapping

- Taskmaster IDs 1-10: core loop foundation and integration.
- Taskmaster IDs 11-20: economy/building/combat systems.
- Taskmaster IDs 21-30: runtime UX/save/performance envelope.
- Taskmaster IDs 31-40: config contracts and governance.
