---
PRD-ID: PRD-lastking-T2
Title: 契约索引 — Lastking T2
Status: Locked
ADR-Refs:
  - ADR-0020
  - ADR-0023
  - ADR-0031
  - ADR-0032
  - ADR-0033
Test-Refs:
  - Game.Core.Tests/Domain/GameConfigTests.cs
  - Game.Core.Tests/Contracts/Lastking/LastkingContractsTests.cs
---

本页用于锁定 T2 契约层执行规则，防止“配置漂移导致玩法行为隐式变化”。

## Contract Inventory

| Contract Path | Responsibility | Versioned | Runtime Required |
| --- | --- | --- | --- |
| `Game.Core/Contracts/Config/enemy-config.schema.json` | enemy archetypes and stats | yes | yes |
| `Game.Core/Contracts/Config/difficulty-config.schema.json` | difficulty tier and lock policy | yes | yes |
| `Game.Core/Contracts/Config/spawn-config.schema.json` | wave cadence and channel composition | yes | yes |
| `Game.Core/Contracts/Config/pressure-normalization.config.schema.json` | pressure baseline constants | yes | yes |
| `Game.Core/Contracts/Config/config-change-audit.schema.json` | audit event payload schema | yes | optional |

## C# Contract Inventory

| Contract Path | Type | Responsibility |
| --- | --- | --- |
| `Game.Core/Contracts/DomainEvent.cs` | Event Envelope | CloudEvents-style common event envelope for core bus |
| `Game.Core/Contracts/EventTypes.cs` | Constants | EventType SSoT constants aligned with ADR-0004 |
| `Game.Core/Contracts/Guild/GuildMemberJoined.cs` | Event | Existing guild domain event example |
| `Game.Core/Contracts/Lastking/DayStarted.cs` | Event | Day phase enter event |
| `Game.Core/Contracts/Lastking/NightStarted.cs` | Event | Night phase enter event |
| `Game.Core/Contracts/Lastking/WaveSpawned.cs` | Event | Night wave spawn commit event |
| `Game.Core/Contracts/Lastking/CastleHpChanged.cs` | Event | Castle durability delta event |
| `Game.Core/Contracts/Lastking/RewardOffered.cs` | Event | Reward 3-option presentation event |
| `Game.Core/Contracts/Lastking/SaveAutosaved.cs` | Event | Day-boundary autosave success event |
| `Game.Core/Contracts/Lastking/WaveBudgetDto.cs` | DTO | Deterministic budget output for wave scheduling |
| `Game.Core/Contracts/Lastking/RewardOfferDto.cs` | DTO | Night reward options payload |
| `Game.Core/Contracts/Interfaces/IWaveBudgetPolicy.cs` | Interface | Runtime budget policy boundary |

## Event Definitions

### Events
- **DayStarted** (`core.lastking.day.started`)
  - 触发时机：白天阶段开始进入运行态。
  - 字段：`RunId`, `DayNumber`, `StartedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/DayStarted.cs`
- **NightStarted** (`core.lastking.night.started`)
  - 触发时机：白天转入夜晚防守阶段。
  - 字段：`RunId`, `DayNumber`, `NightNumber`, `StartedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/NightStarted.cs`
- **WaveSpawned** (`core.lastking.wave.spawned`)
  - 触发时机：夜晚波次生成批次提交后。
  - 字段：`RunId`, `DayNumber`, `NightNumber`, `LaneId`, `SpawnCount`, `WaveBudget`, `SpawnedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WaveSpawned.cs`
- **CastleHpChanged** (`core.lastking.castle.hp_changed`)
  - 触发时机：城堡耐久发生变化后。
  - 字段：`RunId`, `DayNumber`, `PreviousHp`, `CurrentHp`, `ChangedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/CastleHpChanged.cs`
- **RewardOffered** (`core.lastking.reward.offered`)
  - 触发时机：夜晚结算后发放三选一奖励时。
  - 字段：`RunId`, `DayNumber`, `IsEliteNight`, `IsBossNight`, `OptionA`, `OptionB`, `OptionC`, `OfferedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/RewardOffered.cs`
- **SaveAutosaved** (`core.lastking.save.autosaved`)
  - 触发时机：天开始自动存档成功后。
  - 字段：`RunId`, `DayNumber`, `SlotId`, `ConfigHash`, `SavedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/SaveAutosaved.cs`

### DTO
- **WaveBudgetDto**
  - 用途：提供夜晚预算计算结果给刷怪调度。
  - 字段：`DayNumber`, `NightNumber`, `NormalBudget`, `EliteBudget`, `BossBudget`, `ComputedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WaveBudgetDto.cs`
- **RewardOfferDto**
  - 用途：封装奖励三选一面板输入数据。
  - 字段：`DayNumber`, `IsEliteNight`, `IsBossNight`, `OptionA`, `OptionB`, `OptionC`
  - 契约位置：`Game.Core/Contracts/Lastking/RewardOfferDto.cs`

### Interface
- **IWaveBudgetPolicy**
  - 用途：约束预算策略实现边界，隔离核心计算与运行时适配层。
  - 方法：`Compute(dayNumber, nightNumber, isEliteNight, isBossNight) -> WaveBudgetDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/IWaveBudgetPolicy.cs`

## Field Constraints

- 所有经济与战斗核心数值统一为整数语义，不允许小数输入。
- 所有百分比字段必须定义上限，且上限通过 schema 显式约束。
- 所有枚举字段（night type, enemy role, reward kind）必须包含未知值拒绝策略。
- 所有时序字段必须以毫秒或秒为单位，禁止混合单位。

## Versioning and Migration

- 契约版本采用 `major.minor.patch`，并与存档元数据绑定。
- `major` 变化触发强制迁移，失败时拒绝载入并保留原存档。
- `minor` 变化允许前向兼容，必须提供字段默认值策略。
- `patch` 变化仅允许非行为性修订（注释、描述、校验补充）。

## Breaking Change Policy

- 破坏性变更必须先更新 ADR（Accepted 或 Supersede）再更新 schema。
- 破坏性变更必须新增至少一条迁移测试与一条回退测试。
- 破坏性变更必须在 `logs/ci/<YYYY-MM-DD>/` 生成独立审计摘要。

## Local Validation

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`
- `py -3 scripts/python/validate_contracts.py`
