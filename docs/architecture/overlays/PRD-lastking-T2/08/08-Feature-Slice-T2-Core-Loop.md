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
  - Game.Core.Tests/Contracts/Lastking/LastkingContractsTests.cs
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

事件命名在 T2 锁定为 `core.lastking.<entity>.<action>`，禁止在本页继续使用模板变量。

- `core.lastking.day.started`
- `core.lastking.night.started`
- `core.lastking.wave.spawned`
- `core.lastking.castle.hp_changed`
- `core.lastking.reward.offered`
- `core.lastking.save.autosaved`

## 契约定义（Core Loop）

### 事件
- **DayStarted** (`core.lastking.day.started`)
  - 触发时机：白天阶段进入运行态时。
  - 字段：`RunId`, `DayNumber`, `StartedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/DayStarted.cs`
- **NightStarted** (`core.lastking.night.started`)
  - 触发时机：白天结束转入夜晚时。
  - 字段：`RunId`, `DayNumber`, `NightNumber`, `StartedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/NightStarted.cs`
- **WaveSpawned** (`core.lastking.wave.spawned`)
  - 触发时机：夜晚刷怪批次提交后。
  - 字段：`RunId`, `DayNumber`, `NightNumber`, `LaneId`, `SpawnCount`, `WaveBudget`, `SpawnedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WaveSpawned.cs`
- **CastleHpChanged** (`core.lastking.castle.hp_changed`)
  - 触发时机：城堡耐久发生变化后。
  - 字段：`RunId`, `DayNumber`, `PreviousHp`, `CurrentHp`, `ChangedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/CastleHpChanged.cs`
- **RewardOffered** (`core.lastking.reward.offered`)
  - 触发时机：夜晚结算奖励三选一展示时。
  - 字段：`RunId`, `DayNumber`, `IsEliteNight`, `IsBossNight`, `OptionA`, `OptionB`, `OptionC`, `OfferedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/RewardOffered.cs`
- **SaveAutosaved** (`core.lastking.save.autosaved`)
  - 触发时机：天开始自动存档成功后。
  - 字段：`RunId`, `DayNumber`, `SlotId`, `ConfigHash`, `SavedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/SaveAutosaved.cs`

### DTO
- **WaveBudgetDto**
  - 用途：夜晚预算计算输出。
  - 字段：`DayNumber`, `NightNumber`, `NormalBudget`, `EliteBudget`, `BossBudget`, `ComputedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WaveBudgetDto.cs`
- **RewardOfferDto**
  - 用途：奖励三选一面板输入。
  - 字段：`DayNumber`, `IsEliteNight`, `IsBossNight`, `OptionA`, `OptionB`, `OptionC`
  - 契约位置：`Game.Core/Contracts/Lastking/RewardOfferDto.cs`

### 接口
- **IWaveBudgetPolicy**
  - 用途：预算计算策略边界。
  - 方法：`Compute(dayNumber, nightNumber, isEliteNight, isBossNight) -> WaveBudgetDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/IWaveBudgetPolicy.cs`

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
- `T22` 镜头交互要求边缘滚屏 + 键盘滚屏同时生效，且镜头严格受地图边界约束。
- `T23` 速度档位 `Pause/1x/2x` 切换时，波次计时和关键运行时计时器必须冻结/恢复一致。
- `T28` 语言切换至少覆盖 `zh-CN/en-US`，切换后界面文本即时生效并持久化。
- `T29` 音频设置至少覆盖 `Music/SFX` 两通道，变更即时生效并持久化。

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

## Execution Slices (P0)

### Slice A — Economy/Build/Combat (`T11-T20`)

- Scope: 项目结构稳固、整数资源系统、建筑占地/堵路、税收、升级维修互斥、训练队列、科技数值、无友伤战斗接入。
- Key tasks: `T11`, `T12`, `T13`, `T14`, `T15`, `T16`, `T17`, `T18`, `T19`, `T20`.
- Contract anchor:
  - `Game.Core/Contracts/Lastking/WaveBudgetDto.cs`
  - `Game.Core/Contracts/Lastking/WaveSpawned.cs`
  - `Game.Core/Contracts/Lastking/CastleHpChanged.cs`
- Failure focus:
  - 路径被完全堵死但目标不可达；
  - 维修/升级并发违规；
  - 经济整数溢出或负债停工规则偏离。

### Slice B — Runtime UX/Save/Platform (`T21-T30`)

- Scope: Windows 导出与启动、镜头与交互、速度档位冻结、错误提示、本地存档与迁移、云存档绑定、成就、i18n、音频、性能门禁。
- Key tasks: `T21`, `T22`, `T23`, `T24`, `T25`, `T26`, `T27`, `T28`, `T29`, `T30`.
- Contract anchor:
  - `Game.Core/Contracts/Lastking/SaveAutosaved.cs`
  - `Game.Core/Contracts/DomainEvent.cs`
  - `Game.Core/Contracts/EventTypes.cs`
- Failure focus:
  - 暂停时计时器未冻结；
  - 存档迁移失败路径不一致；
  - 云端冲突分支无审计。
  - `T22` 边缘滚屏阈值和键盘输入叠加后出现镜头抖动或越界；
  - `T23` 暂停后仍有计时推进，或恢复后时间尺度不一致；
  - `T28` 语言切换后文案未刷新或未持久化；
  - `T29` 音频通道参数未即时应用或重启后丢失。

### Slice C — Config Governance (`T31-T40`)

- Scope: 配置契约脚手架、敌军/难度/刷怪/压力归一化 schema、sample 配置、运行时校验与回退、版本迁移规则。
- Key tasks: `T31`, `T32`, `T33`, `T34`, `T35`, `T36`, `T37`, `T38`, `T39`, `T40`.
- Contract anchor:
  - `Game.Core/Contracts/Config/enemy-config.schema.json`
  - `Game.Core/Contracts/Config/difficulty-config.schema.json`
  - `Game.Core/Contracts/Config/spawn-config.schema.json`
  - `Game.Core/Contracts/Config/pressure-normalization.config.schema.json`
- Failure focus:
  - 配置缺失未回退内置安全默认；
  - 契约版本迁移策略未强制执行；
  - 报告缺少 config hash/version 审计字段。
