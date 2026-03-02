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
| `Game.Core/Contracts/Lastking/TimeScaleChanged.cs` | Event | Runtime speed state transition event |
| `Game.Core/Contracts/Lastking/UiFeedbackRaised.cs` | Event | Core-to-UI feedback dispatch event |
| `Game.Core/Contracts/Lastking/CloudSaveSyncCompleted.cs` | Event | Cloud save sync completion event |
| `Game.Core/Contracts/Lastking/BootstrapReady.cs` | Event | Baseline bootstrap validated event |
| `Game.Core/Contracts/Lastking/ConfigLoaded.cs` | Event | Config set loaded and bound event |
| `Game.Core/Contracts/Lastking/ResourcesChanged.cs` | Event | Economy resources snapshot changed event |
| `Game.Core/Contracts/Lastking/TaxCollected.cs` | Event | Residence tax settlement event |
| `Game.Core/Contracts/Lastking/TechApplied.cs` | Event | Technology modifier committed event |
| `Game.Core/Contracts/Lastking/WindowsRuntimeValidated.cs` | Event | Windows runtime validation passed event |
| `Game.Core/Contracts/Lastking/CameraScrolled.cs` | Event | Camera position updated event |
| `Game.Core/Contracts/Lastking/AudioSettingsChanged.cs` | Event | Audio settings updated event |
| `Game.Core/Contracts/Lastking/PerfSampled.cs` | Event | Performance sampling window completed event |
| `Game.Core/Contracts/Lastking/WaveBudgetDto.cs` | DTO | Deterministic budget output for wave scheduling |
| `Game.Core/Contracts/Lastking/RewardOfferDto.cs` | DTO | Night reward options payload |
| `Game.Core/Contracts/Lastking/TimeScaleStateDto.cs` | DTO | Runtime speed state snapshot |
| `Game.Core/Contracts/Lastking/UiFeedbackDto.cs` | DTO | UI feedback payload |
| `Game.Core/Contracts/Lastking/CloudSaveSyncResultDto.cs` | DTO | Cloud save sync result payload |
| `Game.Core/Contracts/Interfaces/IWaveBudgetPolicy.cs` | Interface | Runtime budget policy boundary |
| `Game.Core/Contracts/Interfaces/ITimeScaleController.cs` | Interface | Runtime speed control boundary |
| `Game.Core/Contracts/Interfaces/IFeedbackDispatcher.cs` | Interface | UI feedback dispatch boundary |
| `Game.Core/Contracts/Interfaces/ICloudSaveSyncService.cs` | Interface | Cloud save sync boundary |

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
- **TimeScaleChanged** (`core.lastking.time_scale.changed`)
  - 触发时机：局内速度切换（暂停/1x/2x）提交后。
  - 字段：`RunId`, `PreviousScalePercent`, `CurrentScalePercent`, `IsPaused`, `ChangedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/TimeScaleChanged.cs`
- **UiFeedbackRaised** (`core.lastking.ui_feedback.raised`)
  - 触发时机：核心逻辑发出无效操作或错误反馈时。
  - 字段：`RunId`, `Code`, `MessageKey`, `Severity`, `Details`, `RaisedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/UiFeedbackRaised.cs`
- **CloudSaveSyncCompleted** (`core.lastking.cloud_save.sync.completed`)
  - 触发时机：一次云存档上传/下载结束后。
  - 字段：`RunId`, `SlotId`, `Direction`, `SteamAccountId`, `Success`, `ErrorCode`, `RemoteRevision`, `SyncedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/CloudSaveSyncCompleted.cs`
- **BootstrapReady** (`core.lastking.bootstrap.ready`)
  - 触发时机：基线工程初始化与导出预检完成后。
  - 字段：`RunId`, `ProjectRoot`, `ExportProfileReady`, `ReadyAt`
  - 契约位置：`Game.Core/Contracts/Lastking/BootstrapReady.cs`
- **ConfigLoaded** (`core.lastking.config.loaded`)
  - 触发时机：配置集加载并绑定到运行态后。
  - 字段：`RunId`, `ConfigVersion`, `ConfigHash`, `SourcePath`, `LoadedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/ConfigLoaded.cs`
- **ResourcesChanged** (`core.lastking.resources.changed`)
  - 触发时机：金币/铁矿/人口上限任一资源变化后。
  - 字段：`RunId`, `DayNumber`, `Gold`, `Iron`, `PopulationCap`, `ChangedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/ResourcesChanged.cs`
- **TaxCollected** (`core.lastking.tax.collected`)
  - 触发时机：住所税收结算 Tick 完成后。
  - 字段：`RunId`, `DayNumber`, `ResidenceId`, `GoldDelta`, `TotalGold`, `CollectedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/TaxCollected.cs`
- **TechApplied** (`core.lastking.tech.applied`)
  - 触发时机：科技节点生效并写入运行参数后。
  - 字段：`RunId`, `TechId`, `StatKey`, `PreviousValue`, `CurrentValue`, `AppliedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/TechApplied.cs`
- **WindowsRuntimeValidated** (`core.lastking.windows_runtime.validated`)
  - 触发时机：Windows 导出配置与 Steam 启动路径验证通过后。
  - 字段：`RunId`, `SteamAppId`, `StartupPassed`, `ValidationScope`, `ValidatedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WindowsRuntimeValidated.cs`
- **CameraScrolled** (`core.lastking.camera.scrolled`)
  - 触发时机：边缘滚屏或键盘滚屏导致镜头位置更新后。
  - 字段：`RunId`, `PositionX`, `PositionY`, `InputMode`, `ScrolledAt`
  - 契约位置：`Game.Core/Contracts/Lastking/CameraScrolled.cs`
- **AudioSettingsChanged** (`core.lastking.audio_settings.changed`)
  - 触发时机：音乐/音效音量设置提交后。
  - 字段：`RunId`, `MusicVolumePercent`, `SfxVolumePercent`, `ChangedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/AudioSettingsChanged.cs`
- **PerfSampled** (`core.lastking.perf.sampled`)
  - 触发时机：性能采样窗口结束并产出指标后。
  - 字段：`RunId`, `SceneId`, `AverageFps`, `Low1PercentFps`, `SampleCount`, `SampledAt`
  - 契约位置：`Game.Core/Contracts/Lastking/PerfSampled.cs`

### DTO
- **WaveBudgetDto**
  - 用途：提供夜晚预算计算结果给刷怪调度。
  - 字段：`DayNumber`, `NightNumber`, `NormalBudget`, `EliteBudget`, `BossBudget`, `ComputedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/WaveBudgetDto.cs`
- **RewardOfferDto**
  - 用途：封装奖励三选一面板输入数据。
  - 字段：`DayNumber`, `IsEliteNight`, `IsBossNight`, `OptionA`, `OptionB`, `OptionC`
  - 契约位置：`Game.Core/Contracts/Lastking/RewardOfferDto.cs`
- **TimeScaleStateDto**
  - 用途：承载运行时速度状态快照。
  - 字段：`RunId`, `CurrentScalePercent`, `IsPaused`, `UpdatedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/TimeScaleStateDto.cs`
- **UiFeedbackDto**
  - 用途：承载 UI 反馈数据（键值与严重级别）。
  - 字段：`Code`, `MessageKey`, `Severity`, `Details`
  - 契约位置：`Game.Core/Contracts/Lastking/UiFeedbackDto.cs`
- **CloudSaveSyncResultDto**
  - 用途：承载云存档同步结果。
  - 字段：`SlotId`, `Direction`, `Success`, `ErrorCode`, `RemoteRevision`, `SyncedAt`
  - 契约位置：`Game.Core/Contracts/Lastking/CloudSaveSyncResultDto.cs`

### Interface
- **IWaveBudgetPolicy**
  - 用途：约束预算策略实现边界，隔离核心计算与运行时适配层。
  - 方法：`Compute(dayNumber, nightNumber, isEliteNight, isBossNight) -> WaveBudgetDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/IWaveBudgetPolicy.cs`
- **ITimeScaleController**
  - 用途：约束局内速度切换接口边界。
  - 方法：`SetScale(runId, currentScalePercent, isPaused) -> TimeScaleStateDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/ITimeScaleController.cs`
- **IFeedbackDispatcher**
  - 用途：约束核心到表现层反馈分发边界。
  - 方法：`Publish(feedback) -> UiFeedbackDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/IFeedbackDispatcher.cs`
- **ICloudSaveSyncService**
  - 用途：约束云存档同步边界。
  - 方法：`Sync(runId, slotId, direction, steamAccountId) -> CloudSaveSyncResultDto`
  - 契约位置：`Game.Core/Contracts/Interfaces/ICloudSaveSyncService.cs`

## Retrospective Review (6-point Checklist)

- 审查范围：Game.Core/Contracts 下所有 `.cs` 契约文件（含本次新增）。
- 自动化补充：`py -3 scripts/python/validate_contracts.py`、`py -3 scripts/python/check_domain_contracts.py`。

| Checkpoint | Result | Notes |
| --- | --- | --- |
| EventType 命名符合 ADR-0004 | pass | 统一为 `core.*.*` |
| XML 注释完整（summary/remarks） | pass | Event 契约具备 `<summary>` 与 `<remarks>` |
| EventType 常量与 EventTypes SSoT 一致 | pass | 常量全部引用 `EventTypes.<Name>` |
| BCL-only（无 Godot 依赖） | pass | 未引入 `Godot.*` |
| 字段类型明确（无 dynamic） | pass with caveat | `DomainEvent.Data` 为历史兼容字段（`[Obsolete] object?`），新增契约均为强类型 |
| Overlay 08 回链完整 | pass | 本页已登记全部新增契约路径 |

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
