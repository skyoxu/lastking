---
GDD-ID: GDD-LASTKING-T1-T46-M1-WIRING-AUDIT
Title: T1-T46 Minimal Feature Audit And M1 Wiring Snapshot
Status: Draft
Owner: codex
Last Updated: 2026-04-25
Encoding: UTF-8
Applies-To:
  - .taskmaster/tasks/tasks.json
  - .taskmaster/tasks/tasks_back.json
  - .taskmaster/tasks/tasks_gameplay.json
  - docs/gdd/ui-gdd-flow.md
  - docs/gdd/ui-gdd-flow.candidates.json
  - Game.Godot/Scenes/UI/MainMenu.tscn
  - Game.Godot/Scenes/UI/HUD.tscn
  - Game.Godot/Scenes/UI/SettingsPanel.tscn
  - Game.Godot/Scenes/Screens/SettingsScreen.tscn
  - Game.Godot/Scripts/UI/MainMenu.cs
  - Game.Godot/Scripts/UI/HUD.cs
  - Game.Godot/Scripts/UI/SettingsPanel.cs
  - Game.Godot/Scripts/UI/SettingsLoader.cs
  - Game.Godot/Scripts/Screens/SettingsScreen.cs
  - Game.Godot/Scripts/Security/SecurityAudit.cs
ADR-Refs:
  - ADR-0010
  - ADR-0011
  - ADR-0018
  - ADR-0019
  - ADR-0021
  - ADR-0022
  - ADR-0025
Test-Refs:
  - Game.Core.Tests/State/GameStateMachineTests.cs
  - Game.Core.Tests/State/GameStateManagerTests.cs
  - Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs
  - Game.Core.Tests/Domain/GameConfigTests.cs
  - Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd
  - Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd
  - Tests.Godot/tests/Integration/test_day_night_cycle_runtime_loop.gd
  - Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd
  - Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd
  - Tests.Godot/tests/UI/test_main_menu_settings_button.gd
  - Tests.Godot/tests/UI/test_settings_panel_logic.gd
  - Tests.Godot/tests/UI/test_settings_panel_audio_channels.gd
---

# T1-T46 最小功能审计与 M1 接线快照

## 1. 范围与判定规则

本文件参考 `newrouge` 仓的同类审计文档结构，但只审计 `lastking` 当前 `T1-T46` 的真实接线现状，并把三份任务文件视为同一批能力的三个视角：

- 主任务：`.taskmaster/tasks/tasks.json`
- Back 视图：`.taskmaster/tasks/tasks_back.json`
- Gameplay 视图：`.taskmaster/tasks/tasks_gameplay.json`

本文件的“已接入 M1”按当前仓库中已经落地的 UI surface、运行时可见反馈、场景/面板归属和验证入口来判断，不按“仅有底层服务/契约/测试存在”来判断。

判定标签如下：

- `已接入`：已经有明确 UI/可见 surface，且存在对应运行时或测试证据。
- `部分接入`：已经存在 surface 或代码入口，但覆盖面、交互闭环或证据仍不完整。
- `底层支撑`：主要以核心服务、契约、schema、门禁、审计、验证存在，不单独形成玩家 surface。
- `未接入 M1`：当前 Chapter 7 规划或代码中还没有稳定 surface ownership。

额外说明：

- 当前 `tasks.json` 中 `T41-T46` 仍为 `pending`，但它们对应的 Chapter 7 UI 接线分组、候选 surface、验证目标已经在 `docs/gdd/ui-gdd-flow.md` 与 `docs/gdd/ui-gdd-flow.candidates.json` 中建立。因此本文件同时记录“运行时已落地的表面”和“Chapter 7 已规划但仍待补线的 surface owner”。
- 本文件是 Chapter 7 的审计快照，不替代 `ui-gdd-flow.md` 作为路由编排源。

## 2. 当前 M1 实际接线骨架

按 `docs/gdd/ui-gdd-flow.md`、`docs/gdd/ui-gdd-flow.candidates.json`、现有 UI 场景脚本以及相关测试引用，当前仓库的 M1 接线骨架可归纳为六个 governed slices：

1. `MainMenu / Boot Flow（主菜单 / 启动流程）`
2. `Runtime HUD / Prompt / Outcome Surfaces（运行时 HUD / 提示 / 结果界面）`
3. `Combat Pressure / Interaction Surfaces（战斗压力 / 交互界面）`
4. `Economy / Build / Progression Panels（经济 / 建造 / 成长面板）`
5. `Save / Settings / Meta Surfaces（存档 / 设置 / 元系统界面）`
6. `Config Summary / Audit / Migration Surfaces（配置总览 / 审计 / 迁移界面）`

当前骨架的关键现实约束：

- `ui-gdd-flow.md` 已把 `T1-T40` 重组进六个 UI slice，并为每组给出 matrix、screen contracts、state matrix、candidate surface。
- `T41-T46` 已被拆为六个“Wire UI”任务，分别对应上面六个 slice，但任务状态尚未回写为 `done`。
- 仓内已存在明确的部分 UI 场景与脚本，例如 `MainMenu`、`HUD`、`SettingsPanel`、`SettingsScreen`，这意味着 Entry、Loop、Meta、Config 相关 surface 不是从零开始。
- 经济、战斗、配置治理等能力中，很多底层实现和测试已经存在，但并未都形成完整、可核验、统一 owned surface。

## 3. T1-T46 最小颗粒度功能审计

### 3.1 T1-T10

- `T01 / NG-0001 / GM-0101`: Establish baseline Godot 4.5.1 C# Windows project（建立 Godot 4.5.1 C# Windows 基线项目）。M1 接线：`底层支撑`。证据：`MainMenu / Boot Flow（主菜单 / 启动流程）` 依赖该基线，验证入口见 `test_project_bootstrap_editor_compile_run.gd`、`test_windows_export_startup_flow.gd`。
- `T02 / NG-0002 / GM-0102`: Implement config-first balancing system（实现配置优先的数值平衡系统）。M1 接线：`部分接入`。证据：`ui-gdd-flow.md` 已将其放入 `Config Governance And Audit（配置治理与审计）` slice；schema、config contract、reload tests 已存在，但独立 `ConfigAuditPanel` 仍主要停留在规划层。
- `T03 / NG-0003 / GM-0103`: Develop runtime state machine for Day/Night cycles（开发昼夜循环运行时状态机）。M1 接线：`部分接入`。证据：`Runtime HUD And Outcome（运行时 HUD 与结果）` 已声明其为 surface owner，`GameStateManagerTests.cs` 和 `test_day_night_cycle_runtime_loop.gd` 提供运行时证据，但 HUD 完整呈现仍待收束。
- `T04 / NG-0004 / GM-0104`: Implement wave budget and channel system（实现波次预算与通道系统）。M1 接线：`部分接入`。证据：已进入 `Combat Pressure And Interaction（战斗压力与交互）` slice，核心逻辑与 tests 存在，但战斗 surface 仍需统一 owned panel。
- `T05 / NG-0005 / GM-0105`: Create enemy spawning system with cadence（创建按节奏刷新的敌人生成系统）。M1 接线：`部分接入`。证据：与 T04 同属 combat slice；核心 deterministic tests 存在，但玩家可见 pressure/cadence surface 尚未独立固化。
- `T06 / NG-0006 / GM-0106`: Implement enemy AI with target priority and pathing（实现带目标优先级与寻路的敌人 AI）。M1 接线：`部分接入`。证据：`Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn`、`Game.Godot/Scripts/Combat/EnemyAi.cs` 存在，但尚未形成完整 combat UI 归属面。
- `T07 / NG-0007 / GM-0107`: Set up castle HP and loss condition（建立城堡生命值与失败条件）。M1 接线：`部分接入`。证据：`HUD.cs` 与 Runtime slice 说明其应可见，但完整 loss outcome 面板收口仍依赖 T42。
- `T08 / NG-0008 / GM-0108`: Develop win condition and game progression（开发胜利条件与游戏进程）。M1 接线：`部分接入`。证据：已纳入 runtime outcome slice，胜负逻辑归属明确，但完整 outcome surface 仍待统一。
- `T09 / NG-0009 / GM-0109`: Create basic UI for day/night and HP display（创建昼夜与生命值显示基础 UI）。M1 接线：`已接入`。证据：`Game.Godot/Scenes/UI/HUD.tscn`、`Game.Godot/Scripts/UI/HUD.cs` 已存在。
- `T10 / NG-0010 / GM-0110`: Integrate and test full core loop（集成并测试完整核心循环）。M1 接线：`部分接入`。证据：Chapter 7 已把它纳入 `Runtime HUD And Outcome（运行时 HUD 与结果）`，但“full core loop”仍更多体现在 contract + tests，而不是完全闭合的单一 surface。

### 3.2 T11-T20

- `T11 / NG-0011 / GM-0111`: Refine baseline bootstrap with main-scene and structure standards（完善主场景与结构标准的基线启动）。M1 接线：`部分接入`。证据：`MainMenu.tscn`、`MainMenu.cs` 存在，且 Entry slice 已明确其 surface owner。
- `T12 / NG-0012 / GM-0112`: Implement Core Resource System with Integer Safety（实现具备整数安全的核心资源系统）。M1 接线：`部分接入`。证据：`ResourceManager` 和相关 tests 存在，且已进入 `Economy And Progression（经济与成长）` panels；但资源总览 panel 尚未统一命名落地。
- `T13 / NG-0013 / GM-0113`: Design and Implement Building System with Footprint Rules（设计并实现带占地规则的建造系统）。M1 接线：`部分接入`。证据：`BuildingModeRuntime.tscn`、`BuildingModeCoreBridge.cs` 存在；Chapter 7 已给出 `BuildPanel` 候选 surface。
- `T14 / NG-0014 / GM-0114`: Implement Economy and Tax System for Residences（实现住宅经济与税收系统）。M1 接线：`部分接入`。证据：经济 slice 已建立，但玩家 visible tax/progression surface 仍待固定。
- `T15 / NG-0015 / GM-0115`: Develop Upgrade and Repair System with Constraints（开发带约束的升级与修理系统）。M1 接线：`部分接入`。证据：`BuildingUpgradeRepairRuntime.gd` 已存在，但统一 progression surface 仍在 Chapter 7 规划内。
- `T16 / NG-0016 / GM-0116`: Implement Unit Training Queue in Barracks（实现兵营单位训练队列）。M1 接线：`部分接入`。证据：`BarracksTrainingQueueBridge.cs` 存在，经济 slice 已覆盖，但专属 queue surface 尚未明示成品。
- `T17 / NG-0017 / GM-0117`: Design and Integrate Tech Tree for Unit Stats（设计并集成单位属性科技树）。M1 接线：`部分接入`。证据：经济/进程 slice 已纳入，但更偏 runtime/service 侧，surface ownership 尚待统一。
- `T18 / NG-0018 / GM-0118`: Implement Reward System with Nightly Choices（实现夜间奖励选择系统）。M1 接线：`部分接入`。证据：在 `Runtime HUD And Outcome（运行时 HUD 与结果）` 中被视为 outcome/reward entry 的一部分，但独立 reward surface 未在现有 UI 场景中显式落地。
- `T19 / NG-0019 / GM-0119`: Add Day/Night Cycle and Game Win/Lose Conditions（加入昼夜循环与游戏胜负条件）。M1 接线：`部分接入`。证据：与 T03/T07/T08 共同落在 runtime outcome slice，测试层已强，surface 收尾仍待 T42。
- `T20 / NG-0020 / GM-0120`: Integrate Combat with Friendly Fire Disabled（集成禁用友伤的战斗系统）。M1 接线：`部分接入`。证据：combat slice 已给出 `CombatHud / PressurePanel / CameraControlOverlay`，但仓内成品命名与这些候选并不完全对齐。

### 3.3 T21-T30

- `T21 / NG-0021 / GM-0121`: Lock Windows export profile and Steam runtime startup validation（锁定 Windows 导出配置与 Steam 运行时启动验证）。M1 接线：`底层支撑`。证据：直接支撑 `MainMenu / Boot Flow（主菜单 / 启动流程）`，但不是单独玩家功能。
- `T22 / NG-0022 / GM-0122`: Implement Camera and Interaction System with Edge and Keyboard Scrolling（实现带边缘与键盘滚动的相机与交互系统）。M1 接线：`部分接入`。证据：combat slice 已覆盖，camera interaction smoke checks 已成为验证目标，但统一 overlay 仍待补线。
- `T23 / NG-0023 / GM-0123`: Develop Runtime Speed Controls (Pause, 1x, 2x) with Timer Freeze（开发带计时冻结的运行时速度控制：暂停、1x、2x）。M1 接线：`部分接入`。证据：已被 runtime slice 和 acceptance notes 明确要求，但实际 UI 控件归属未单独落名。
- `T24 / NG-0024 / GM-0124`: Create UI Feedback System for Invalid Actions and Errors（创建无效操作与错误反馈 UI 系统）。M1 接线：`部分接入`。证据：runtime slice 已把 invalid-action prompts 视为必显状态，但仓内尚缺一个独立命名的 prompt panel surface。
- `T25 / NG-0025 / GM-0125`: Build Save System with Autosave and Migration Handling（构建带自动保存与迁移处理的存档系统）。M1 接线：`部分接入`。证据：`GameStateManager` 中 autosave/save path 已落地，Chapter 7 也给出 `SavePanel` 候选 surface。
- `T26 / NG-0026 / GM-0126`: Integrate Steam Cloud Save with Account Binding（集成带账号绑定的 Steam 云存档）。M1 接线：`部分接入`。证据：`GameStateManager`、`SaveManagerTestBridge.cs` 中 cloud sync / conflict / ownership binding 逻辑已明显存在，但最终玩家 surface 仍待 T45。
- `T27 / NG-0027 / GM-0127`: Implement Achievements System with Deterministic Unlocking（实现确定性解锁的成就系统）。M1 接线：`未接入 M1`。证据：domain/entity 与报告目标存在，但当前 UI 文档仅在 meta slice 中原则性覆盖，没有实际已见 achievements surface。
- `T28 / NG-0028 / GM-0128`: Set Up Localization (i18n) for zh-CN and en-US（为 zh-CN 与 en-US 设置本地化）。M1 接线：`已接入`。证据：`SettingsPanel.cs`、`SettingsLoader.cs`、`Translations/README.md` 以及 `test_settings_locale*.gd` 说明语言切换已有明确 surface。
- `T29 / NG-0029 / GM-0129`: Add Audio Settings for Music and SFX Channels（加入音乐与音效通道音频设置）。M1 接线：`已接入`。证据：`SettingsPanel.cs` 与 `test_settings_panel_audio_channels.gd` 直接证明音量设置 surface 已在场。
- `T30 / NG-0030 / GM-0130`: Optimize for Performance Targets (45 FPS 1% Low, 60 FPS Average)（针对性能目标优化：1% Low 45 FPS、平均 60 FPS）。M1 接线：`部分接入`。证据：`Task30PerformanceGateBridge.cs` 已存在，Chapter 7 将其归于 meta surface，但更偏 operator-facing gate 读面，而非成熟玩家面板。

### 3.4 T31-T40

- `T31 / NG-0031 / GM-0131`: Scaffold config-contract workspace on existing project（在现有项目上搭建配置契约工作区）。M1 接线：`底层支撑`。证据：多份 `schema.json` 已在 `Game.Core/Contracts/Config/**` 下存在。
- `T32 / NG-0032 / GM-0132`: Implement enemy-config.schema.json with validation rules（实现带校验规则的 enemy-config.schema.json）。M1 接线：`底层支撑`。证据：`enemy-config.schema.json` 已存在，surface owner 在 Chapter 7 中属于 `Config Summary / Audit / Migration`。
- `T33 / NG-0033 / GM-0133`: Implement difficulty-config.schema.json with versioning and lock constraints（实现带版本与锁定约束的 difficulty-config.schema.json）。M1 接线：`底层支撑`。证据：`difficulty-config.schema.json` 已存在。
- `T34 / NG-0034 / GM-0134`: Implement spawn-config.schema.json for deterministic wave composition（为确定性波次组成实现 spawn-config.schema.json）。M1 接线：`底层支撑`。证据：schema 与 tests 已经成为治理层资产。
- `T35 / NG-0035 / GM-0135`: Implement pressure-normalization.config.schema.json with baseline constants and range checks（实现带基线常量与范围检查的 pressure-normalization.config.schema.json）。M1 接线：`底层支撑`。证据：`pressure-normalization.config.schema.json` 已存在。
- `T36 / NG-0036 / GM-0136`: Create sample JSON files for all configs with valid data（为所有配置创建含有效数据的示例 JSON 文件）。M1 接线：`部分接入`。证据：Chapter 7 已把 sample/config governance 证据面纳入 audit slice，但玩家可见面仍不强。
- `T37 / NG-0037 / GM-0137`: Implement config validation and fallback handling in C#（在 C# 中实现配置校验与回退处理）。M1 接线：`部分接入`。证据：`Config Governance And Audit（配置治理与审计）` slice 将 fallback status 视为必须 surface，但目前更多通过 tests / audit evidence 显示。
- `T38 / NG-0038 / GM-0138`: Integrate config governance for gameplay tuning（为玩法调优集成配置治理）。M1 接线：`部分接入`。证据：治理逻辑与 routing 已在 Chapter 7 文档中成型，但成品 UI panel 仍偏规划。
- `T39 / NG-0039 / GM-0139`: Add config hash/version to battle report metadata for auditability（为战报元数据加入配置哈希与版本以支持审计）。M1 接线：`部分接入`。证据：`ReportMetadataPanel` 已在 candidate surfaces 中被命名，但仓内未见同名成品场景。
- `T40 / NG-0040 / GM-0140`: Implement version migration rule with force migration（实现带强制迁移的版本迁移规则）。M1 接线：`部分接入`。证据：migration 已在 config/meta 两侧被当作 governed outcome；`MigrationStatusDialog` 已被定义为候选 surface。

### 3.5 T41-T46

- `T41`: `Wire UI: MainMenu And Boot Flow（接线 UI：主菜单与启动流程）`。M1 接线：`部分接入`。证据：`MainMenu.tscn`、`MainMenu.cs` 已存在；`BootStatusPanel`、`ContinueGateDialog` 在 `ui-gdd-flow.candidates.json` 已有候选定义，但文档显示任务状态仍为 `pending`。
- `T42`: `Wire UI: Runtime HUD And Outcome Surfaces（接线 UI：运行时 HUD 与结果界面）`。M1 接线：`部分接入`。证据：`HUD.tscn`、`HUD.cs` 已存在；`OutcomePanel`、`RuntimePromptPanel` 仅以 candidate 形式存在。
- `T43`: `Wire UI: Combat Interaction Surfaces（接线 UI：战斗交互界面）`。M1 接线：`部分接入`。证据：combat scripts/probe scenes 存在，但 `CombatHud`、`PressurePanel`、`CameraControlOverlay` 仍主要体现为 Chapter 7 candidate。
- `T44`: `Wire UI: Economy And Progression Panels（接线 UI：经济与成长面板）`。M1 接线：`部分接入`。证据：building/resource/training bridges 已存在，但 `ResourcePanel`、`BuildPanel`、`ProgressionPanel` 尚未以稳定成品面板形式统一。
- `T45`: `Wire UI: Save, Settings, And Meta Surfaces（接线 UI：存档、设置与元系统界面）`。M1 接线：`部分接入`。证据：`SettingsPanel.tscn`、`SettingsScreen.tscn`、autosave/cloud logic 已有；但 `SavePanel` 和完整 `RunSummaryPanel` 的集中归属尚未完全固化。
- `T46`: `Wire UI: Config Audit And Migration Surfaces（接线 UI：配置审计与迁移界面）`。M1 接线：`部分接入`。证据：config contracts、audit validators、`SecurityAudit.cs` 已存在；但 `ConfigAuditPanel`、`MigrationStatusDialog`、`ReportMetadataPanel` 仍主要是 Chapter 7 明确规划结果。
## 4. 基于 Chapter 7 的功能接线矩阵

### 4.1 当前已经建立 owner 的六个 slices

| Slice | 已绑定任务 | 当前 surface owner | 现状 |
| --- | --- | --- | --- |
| Entry And Bootstrap（入口与启动） | T01, T11, T21, T41 | `MainMenu / Boot Flow（主菜单 / 启动流程）` | `MainMenu` 已落地，BootStatus/ContinueGate 仍需统一补线 |
| Runtime HUD And Outcome（运行时 HUD 与结果） | T03, T07, T08, T09, T10, T18, T19, T23, T24, T42 | `HUD / Prompt / Outcome Surfaces（HUD / 提示 / 结果界面）` | `HUD` 已落地，Prompt/Outcome 仍偏文档化定义 |
| Combat Pressure And Interaction（战斗压力与交互） | T04, T05, T06, T20, T22, T43 | `Combat HUD / Pressure / Camera Feedback（战斗 HUD / 压力 / 相机反馈）` | combat 逻辑与 probe 已有，统一 UI shell 未完全固化 |
| Economy And Progression（经济与成长） | T12, T13, T14, T15, T16, T17, T44 | `Resource / Build / Progression Panels（资源 / 建造 / 成长面板）` | core/service 已强，surface 统一程度不足 |
| Meta Systems And Platform（元系统与平台） | T25, T26, T27, T28, T29, T30, T45 | `Settings / Save / Meta Surfaces（设置 / 存档 / 元系统界面）` | settings 已落地最明显，save/cloud/perf/achievement 仍有分散性 |
| Config Governance And Audit（配置治理与审计） | T02, T31-T40, T46 | `Config Summary / Audit / Migration Surfaces（配置总览 / 审计 / 迁移界面）` | contracts/tests 很完整，读面板和报告面仍待补齐 |

### 4.2 当前已落地或接近落地的 surface

- `MainMenu`: 已存在于 `Game.Godot/Scenes/UI/MainMenu.tscn` 与 `Game.Godot/Scripts/UI/MainMenu.cs`。
- `HUD`: 已存在于 `Game.Godot/Scenes/UI/HUD.tscn` 与 `Game.Godot/Scripts/UI/HUD.cs`。
- `SettingsPanel`: 已存在于 `Game.Godot/Scenes/UI/SettingsPanel.tscn` 与 `Game.Godot/Scripts/UI/SettingsPanel.cs`。
- `SettingsScreen`: 已存在于 `Game.Godot/Scenes/Screens/SettingsScreen.tscn` 与 `Game.Godot/Scripts/Screens/SettingsScreen.cs`。
- `Config/Audit runtime code`: `SecurityAudit.cs`、`BattleReportAuditReader.cs`、`BattleReportAuditValidator.cs` 已存在，但还未统一成单个 governed panel。

### 4.3 当前接线但仍明显不完整的 surface

- `BootStatusPanel` / `ContinueGateDialog`：文档与候选清单里有，仓内暂未看到同名成品 surface。
- `OutcomePanel` / `RuntimePromptPanel`：runtime acceptance 已要求显示，但仓内主要体现为 `HUD` 与测试约束。
- `CombatHud` / `PressurePanel` / `CameraControlOverlay`：combat 逻辑存在，UI owner 还没有完全收束成固定 scene/panel。
- `ResourcePanel` / `BuildPanel` / `ProgressionPanel`：building/resource/queue 已有桥接实现，但 panel 归属不集中。
- `SavePanel` / `RunSummaryPanel`：settings 之外的 meta surfaces 仍有明显待补线部分。
- `ConfigAuditPanel` / `MigrationStatusDialog` / `ReportMetadataPanel`：Chapter 7 已命名，但现状仍以 contract、audit、validator 为主。

## 5. 当前未完全接入 M1 的功能拆分

### 5.1 已有能力，主要缺 UI 收口

- `T02, T31-T40, T46`：配置治理、schema、migration、audit 已经很完整，主要缺统一读面板和报告面。
- `T03, T07, T08, T10, T18, T19, T23, T24, T42`：runtime/loop 能力强于当前 HUD surface 的收束程度。
- `T04, T05, T06, T20, T22, T43`：combat 核心已存在，但可视化 pressure/interaction shell 仍待明确 owner。
- `T12-T17, T44`：经济、建造、训练、升级、tech 更多是系统已在，surface 未统一。
- `T25, T26, T28, T29, T30, T45`：save/cloud/localization/audio/perf 已有部分落地面，仍需统一为 meta surface 组合。

### 5.2 仍偏底层或验证层，不应误判为玩家面已完成

- `T01, T21`：平台、基线、导出验证属于 boot/governance 基座。
- `T27`：achievements 目前更像 domain + report 资产，还未形成明确玩家 surface。
- `T30`：性能门主要是 operator-facing evidence，不等价于玩家面板已完善。
- `T31-T35`：schema 合同是核心资产，但不是独立玩家交互面。

## 6. 与 `ui-gdd-flow.md` 的关系

本文件与 `docs/gdd/ui-gdd-flow.md` 的分工如下：

- `ui-gdd-flow.md`：Chapter 7 的路由编排源，负责 slice、matrix、contracts、candidate surface 和验证目标。
- `t1-t46-m1-wiring-audit.md`：审计快照，负责回答“当前哪些已经有真实 surface，哪些只是文档 owner 已确定但仍待补线”。

从信息密度上，本文件补足了 `ui-gdd-flow.md` 没有直接给出的两类结论：

1. `MainMenu / Boot Flow（主菜单 / 启动流程）`
2. `Runtime HUD / Prompt / Outcome Surfaces（运行时 HUD / 提示 / 结果界面）`

## 6.1 逐任务 Evidence Path 表

下表按 `newrouge` 审计文档的密度补充每个 task 的最小 evidence path。这里的 evidence path 不是“唯一证据”，而是后续核查该任务时最短应先看的路径组合：

- `Primary Surface/Code`：优先看的运行时代码或 UI/核心实现入口。
- `Primary Test/Evidence`：优先看的自动化或验证入口。
- `Governance Path`：overlay / Chapter 7 / contracts 的落点。
- `Evidence Status`：证据成熟度标签，统一按以下口径判定：
  - `runtime`：已有明确运行时 surface 或主实现入口，并且存在直接测试/验证入口。
  - `partial`：已有实现或 surface 候选，但运行时闭环、统一 UI owner 或验证链还不完整。
  - `test-only`：主要证据来自测试、schema、validator、audit、gate，本身不是成熟玩家面。
  - `docs-only`：当前主要只有 Chapter 7 文档、candidate 或治理落点，缺少强代码/运行时证据。
- `Gap To Close`：从当前证据状态提升到 `runtime` 还差的最短收口动作；`runtime` 项统一记为 `None`。

### 6.1.1 Entry And Bootstrap（入口与启动）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T01 | Establish baseline Godot 4.5.1 C# Windows project（建立 Godot 4.5.1 C# Windows 基线项目） | `Game.Godot/Scenes/UI/MainMenu.tscn`, `Game.Godot/Scripts/UI/MainMenu.cs` | `Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd`, `Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T11 | Refine baseline bootstrap with main-scene and structure standards（完善主场景与结构标准的基线启动） | `Game.Godot/Scenes/UI/MainMenu.tscn`, `Game.Godot/Scripts/UI/MainMenu.cs` | `Tests.Godot/tests/Scenes/Smoke/test_main_scene_smoke.gd`, `Tests.Godot/tests/Integration/test_project_bootstrap_restart_stability.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T21 | Lock Windows export profile and Steam runtime startup validation（锁定 Windows 导出配置与 Steam 运行时启动验证） | `Game.Godot/Scenes/UI/MainMenu.tscn`, `Game.Godot/Scripts/UI/MainMenu.cs` | `Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd`, `Tests.Godot/tests/Integration/test_windows_export_preset_artifact.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `test-only` | `Need a governed boot/export status surface instead of relying only on export and startup verification artifacts.` |
| T41 | Wire UI: MainMenu And Boot Flow（接线 UI：主菜单与启动流程） | `Game.Godot/Scenes/UI/MainMenu.tscn`, `Game.Godot/Scripts/UI/MainMenu.cs` | `Tests.Godot/tests/UI/test_main_menu_settings_button.gd`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need to materialize BootStatusPanel and ContinueGateDialog as stable owned surfaces with direct runtime evidence.` |

### 6.1.2 Runtime HUD And Outcome（运行时 HUD 与结果）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T03 | Develop runtime state machine for Day/Night cycles（开发昼夜循环运行时状态机） | `Game.Core/State/DayNightRuntimeStateMachine.cs`, `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/Runtime/DayNightRuntimeLoopNode.cs` | `Game.Core.Tests/State/GameStateMachineTests.cs`, `Tests.Godot/tests/Integration/test_day_night_cycle_runtime_loop.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T07 | Set up castle HP and loss condition（建立城堡生命值与失败条件） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/UI/HUD.cs` | `Game.Core.Tests/State/GameStateManagerTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need a single owned HUD/outcome surface that visibly renders HP depletion and loss transition.` |
| T08 | Develop win condition and game progression（开发胜利条件与游戏进程） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/UI/HUD.cs` | `Game.Core.Tests/State/GameStateManagerTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need a stable outcome surface that presents win progression and terminal state rather than relying on state tests alone.` |
| T09 | Create basic UI for day/night and HP display（创建昼夜与生命值显示基础 UI） | `Game.Godot/Scenes/UI/HUD.tscn`, `Game.Godot/Scripts/UI/HUD.cs` | `Tests.Godot/tests/UI/test_hud_day_night_cycle_status.gd`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `runtime` | `None` |
| T10 | Integrate and test full core loop（集成并测试完整核心循环） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/UI/HUD.cs` | `Game.Core.Tests/State/GameStateManagerTests.cs`, `Game.Core.Tests/State/GameStateMachineTests.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `partial` | `Need one governed runtime loop surface that ties state, prompts, outcomes, and transitions into a visible end-to-end flow.` |
| T18 | Implement Reward System with Nightly Choices（实现夜间奖励选择系统） | `Game.Core/Services/Reward/RewardManager.cs`, `Game.Godot/Scripts/Reward/RewardPanelRuntime.gd` | `Tests.Godot/tests/Scenes/Rewards/test_reward_panel_selection_applies_single_effect.gd`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need reward entry and confirmation flow to be fully surfaced in the runtime slice instead of remaining a side runtime panel.` |
| T19 | Add Day/Night Cycle and Game Win/Lose Conditions（加入昼夜循环与游戏胜负条件） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/UI/HUD.cs` | `Game.Core.Tests/State/GameStateManagerTests.cs`, `Tests.Godot/tests/Integration/test_day_night_cycle_runtime_loop.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need win/lose transitions and loop resolution to be rendered through a stable outcome owner instead of inferred from state changes.` |
| T23 | Develop Runtime Speed Controls (Pause, 1x, 2x) with Timer Freeze（开发带计时冻结的运行时速度控制：暂停、1x、2x） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Scripts/UI/HUD.cs` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `docs-only` | `Need explicit speed-control widgets and direct runtime evidence that pause and timer freeze are visible and owned by the HUD slice.` |
| T24 | Create UI Feedback System for Invalid Actions and Errors（创建无效操作与错误反馈 UI 系统） | `Game.Godot/Scripts/UI/HUD.cs`, `Game.Godot/Scripts/UI/MainMenu.cs` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `docs-only` | `Need a concrete prompt/error surface with runtime-triggered messages and direct validation coverage.` |
| T42 | Wire UI: Runtime HUD And Outcome Surfaces（接线 UI：运行时 HUD 与结果界面） | `Game.Godot/Scenes/UI/HUD.tscn`, `Game.Godot/Scripts/UI/HUD.cs` | `Game.Core.Tests/State/GameStateMachineTests.cs`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need OutcomePanel and RuntimePromptPanel to exist as stable surfaces with direct runtime assertions.` |

### 6.1.3 Combat Pressure And Interaction（战斗压力与交互）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T04 | Implement wave budget and channel system（实现波次预算与通道系统） | `Game.Core/Services/WaveManager.cs`, `Game.Core/Services/WaveBudgetAllocator.cs` | `Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs`, `Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `runtime` | `None` |
| T05 | Create enemy spawning system with cadence（创建按节奏刷新的敌人生成系统） | `Game.Core/Services/WaveManager.NightSpawning.cs`, `Game.Core/Services/WaveManager.cs` | `Game.Core.Tests/Services/WaveManagerDeterminismTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need spawn cadence to be surfaced through a stable combat pressure owner instead of remaining mostly service-level evidence.` |
| T06 | Implement enemy AI with target priority and pathing（实现带目标优先级与寻路的敌人 AI） | `Game.Godot/Scripts/Combat/EnemyAi.cs`, `Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn` | `Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need runtime combat UI to expose target-priority and blocked-path feedback beyond probe-only evidence.` |
| T20 | Integrate Combat with Friendly Fire Disabled（集成禁用友伤的战斗系统） | `Game.Godot/Scripts/Combat/EnemyAi.cs`, `Game.Core/Services/WaveManager.cs` | `Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need combat HUD feedback and scenario evidence showing friendly-fire-disabled behavior on governed surfaces.` |
| T22 | Implement Camera and Interaction System with Edge and Keyboard Scrolling（实现带边缘与键盘滚动的相机与交互系统） | `Game.Godot/Scripts/Combat/EnemyAiRuntimeProbe.cs`, `Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `docs-only` | `Need an actual camera-control overlay or direct scene evidence instead of candidate-only planning.` |
| T43 | Wire UI: Combat Interaction Surfaces（接线 UI：战斗交互界面） | `Game.Godot/Scripts/Combat/EnemyAi.cs`, `Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn` | `Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need CombatHud, PressurePanel, and CameraControlOverlay to be realized as stable surfaces with direct runtime checks.` |

### 6.1.4 Economy And Progression（经济与成长）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T12 | Implement Core Resource System with Integer Safety（实现具备整数安全的核心资源系统） | `Game.Core/Services/ResourceManager.cs` | `Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs`, `Game.Core.Tests/Services/ResourceManagerTests.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `runtime` | `None` |
| T13 | Design and Implement Building System with Footprint Rules（设计并实现带占地规则的建造系统） | `Game.Core/Services/Building/BuildingPlacementService.cs`, `Game.Godot/Scripts/Building/BuildingModeCoreBridge.cs` | `Game.Core/State/Building/BuildingPlacementState.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need build placement to be exposed through a governed BuildPanel instead of remaining mostly bridge/state evidence.` |
| T14 | Implement Economy and Tax System for Residences（实现住宅经济与税收系统） | `Game.Core/Services/ResourceManager.cs`, `Game.Core/Engine/Building/BuildingSystemCoreLoop.cs` | `Game.Core.Tests/Engine/GameEngineCoreEventTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need player-visible economy/tax readouts and direct progression-panel evidence rather than engine-event evidence only.` |
| T15 | Develop Upgrade and Repair System with Constraints（开发带约束的升级与修理系统） | `Game.Core/Services/Building/BuildingUpgradeRepairRuntime.cs`, `Game.Godot/Scripts/Building/BuildingUpgradeRepairRuntime.gd` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need upgrade and repair actions to be bound to a stable progression surface with direct validation.` |
| T16 | Implement Unit Training Queue in Barracks（实现兵营单位训练队列） | `Game.Core/Services/BarracksTrainingQueueRuntime.cs`, `Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need queue state and queue actions to appear on an owned panel instead of staying at runtime/bridge level.` |
| T17 | Design and Integrate Tech Tree for Unit Stats（设计并集成单位属性科技树） | `Game.Core/Services/TechTreeManager.cs`, `Game.Godot/Adapters/TechTreeManager.cs` | `docs/gdd/ui-gdd-flow.md`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need a visible tech/progression owner surface with direct evidence that unlocks and stat changes are surfaced.` |
| T44 | Wire UI: Economy And Progression Panels（接线 UI：经济与成长面板） | `Game.Godot/Scripts/Building/BuildingModeCoreBridge.cs`, `Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs` | `Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need ResourcePanel, BuildPanel, and ProgressionPanel to exist as concrete surfaces with direct scenario assertions.` |

### 6.1.5 Save, Settings, And Meta（存档、设置与元系统）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T25 | Build Save System with Autosave and Migration Handling（构建带自动保存与迁移处理的存档系统） | `Game.Core/State/GameStateManager.cs`, `Game.Godot/Adapters/Save/SaveManagerTestBridge.cs` | `Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd`, `Tests.Godot/tests/Adapters/Save/test_save_manager_daystart_autosave.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T26 | Integrate Steam Cloud Save with Account Binding（集成带账号绑定的 Steam 云存档） | `Game.Core/Services/SaveManagerCloudSyncWorkflow.cs`, `Game.Godot/Adapters/Save/SaveManagerTestBridge.cs` | `Tests.Godot/tests/UI/test_save_conflict_prompt_branch_selection.gd`, `Tests.Godot/tests/UI/test_save_conflict_prompt_blocks_overwrite.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `partial` | `Need a stable cloud-sync/conflict owner surface instead of bridge and prompt tests as the primary evidence.` |
| T27 | Implement Achievements System with Deterministic Unlocking（实现确定性解锁的成就系统） | `Game.Core/Services/AchievementRuntimeServices.cs`, `Game.Godot/Adapters/Achievements/AchievementRuntimeTestBridge.cs` | `Tests.Godot/tests/Scenes/Achievements/test_achievement_manager_unlocks_on_day_threshold.gd`, `Game.Core/Domain/Entities/Achievement.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `partial` | `Need a governed achievements/readout surface so unlock evidence is not limited to tests and adapters.` |
| T28 | Set Up Localization (i18n) for zh-CN and en-US（为 zh-CN 与 en-US 设置本地化） | `Game.Godot/Scripts/UI/SettingsPanel.cs`, `Game.Godot/Scripts/UI/SettingsLoader.cs`, `Game.Godot/Scripts/Localization/LocalizationManager.gd` | `Tests.Godot/tests/UI/test_settings_locale.gd`, `Tests.Godot/tests/UI/test_settings_locale_persist.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T29 | Add Audio Settings for Music and SFX Channels（加入音乐与音效通道音频设置） | `Game.Godot/Scripts/UI/SettingsPanel.cs` | `Tests.Godot/tests/UI/test_settings_panel_audio_channels.gd`, `Tests.Godot/tests/UI/test_settings_panel_logic.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T30 | Optimize for Performance Targets (45 FPS 1% Low, 60 FPS Average)（针对性能目标优化：1% Low 45 FPS、平均 60 FPS） | `Game.Godot/Adapters/Performance/Task30PerformanceGateBridge.cs`, `Game.Godot/Scripts/Perf/PerformanceTracker.cs` | `Game.Core/Services/PerformanceGateVerdictService.cs`, `Game.Core/Services/PerformancePhaseEvidenceService.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `test-only` | `Need an operator-facing performance status surface if this capability must be treated as a governed visible panel rather than a gate-only artifact.` |
| T45 | Wire UI: Save, Settings, And Meta Surfaces（接线 UI：存档、设置与元系统界面） | `Game.Godot/Scenes/UI/SettingsPanel.tscn`, `Game.Godot/Scenes/Screens/SettingsScreen.tscn`, `Game.Godot/Scripts/UI/SettingsPanel.cs` | `Tests.Godot/tests/UI/test_settings_panel_scene.gd`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need SavePanel and RunSummaryPanel to be stabilized as owned surfaces alongside the already-real settings UI.` |

### 6.1.6 Config Governance And Audit（配置治理与审计）

| Task | Title | Primary Surface/Code | Primary Test/Evidence | Governance Path | Evidence Status | Gap To Close |
| --- | --- | --- | --- | --- | --- | --- |
| T02 | Implement config-first balancing system（实现配置优先的数值平衡系统） | `Game.Core/Services/ConfigManager.cs`, `Game.Core/Services/ConfigValidationPipeline.cs` | `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T31 | Scaffold config-contract workspace on existing project（在现有项目上搭建配置契约工作区） | `Game.Core/Contracts/Config/`, `Game.Core/Services/ConfigManager.cs` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need a governed read surface that exposes config-contract workspace presence and ownership rather than only file-system evidence.` |
| T32 | Implement enemy-config.schema.json with validation rules（实现带校验规则的 enemy-config.schema.json） | `Game.Core/Contracts/Config/enemy-config.schema.json`, `Game.Core/Services/EnemyConfigRuntimeResolver.cs` | `Game.Core/Contracts/Config/enemy-config.schema.json`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need enemy config validation results to be visible through an owned audit surface instead of schema and resolver evidence alone.` |
| T33 | Implement difficulty-config.schema.json with versioning and lock constraints（实现带版本与锁定约束的 difficulty-config.schema.json） | `Game.Core/Contracts/Config/difficulty-config.schema.json`, `Game.Core/Services/ConfigManager.cs` | `Game.Core/Contracts/Config/difficulty-config.schema.json`, `Game.Core.Tests/Domain/GameConfigTests.cs` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need difficulty lock/version outcomes to surface on a governed config read panel rather than only in contracts and tests.` |
| T34 | Implement spawn-config.schema.json for deterministic wave composition（为确定性波次组成实现 spawn-config.schema.json） | `Game.Core/Contracts/Config/spawn-config.schema.json`, `Game.Core/Services/WaveManager.cs` | `Game.Core/Contracts/Config/spawn-config.schema.json`, `Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need spawn composition validation and loaded values to appear on an audit surface instead of staying in schema and integration evidence.` |
| T35 | Implement pressure-normalization.config.schema.json with baseline constants and range checks（实现带基线常量与范围检查的 pressure-normalization.config.schema.json） | `Game.Core/Contracts/Config/pressure-normalization.config.schema.json`, `Game.Core/Services/PressureNormalizationConfigContractValidator.cs` | `Game.Core/Contracts/Config/pressure-normalization.config.schema.json`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need range-check verdicts and active normalization values to be exposed on a config audit surface.` |
| T36 | Create sample JSON files for all configs with valid data（为所有配置创建含有效数据的示例 JSON 文件） | `Game.Core/Contracts/Config/*.sample.json` | `Game.Core/Contracts/Config/spawn-config.sample.json`, `Game.Core/Contracts/Config/difficulty-config.sample.json` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `test-only` | `Need sample-config coverage and selection evidence to be surfaced as governed artifacts instead of static sample files only.` |
| T37 | Implement config validation and fallback handling in C#（在 C# 中实现配置校验与回退处理） | `Game.Core/Services/ConfigValidationPipeline.cs`, `Game.Core/Services/ConfigPolicyRouter.cs` | `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `Tests.Godot/tests/Security/Hard/test_balance_config_validation_audit.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `runtime` | `None` |
| T38 | Integrate config governance for gameplay tuning（为玩法调优集成配置治理） | `Game.Core/Services/ConfigApplicationGuards.cs`, `Game.Core/Services/ConfigManager.cs` | `Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd`, `Tests.Godot/tests/Adapters/Config/test_balance_config_single_source.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `partial` | `Need gameplay-facing or operator-facing panels that show which active config snapshot is governing runtime behavior.` |
| T39 | Add config hash/version to battle report metadata for auditability（为战报元数据加入配置哈希与版本以支持审计） | `Game.Core/Services/BattleReportAuditReader.cs`, `Game.Core/Services/BattleReportAuditValidator.cs`, `Game.Core/Contracts/Config/config-change-audit.schema.json` | `Game.Core.Tests/Domain/GameConfigTests.cs`, `docs/gdd/ui-gdd-flow.md` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need report metadata to be surfaced through a stable report/audit panel instead of reader-validator evidence only.` |
| T40 | Implement version migration rule with force migration（实现带强制迁移的版本迁移规则） | `Game.Core/Services/ConfigValidationPipeline.cs`, `Game.Core/State/GameStateManager.cs` | `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `Tests.Godot/tests/Adapters/Db/test_savegame_update_overwrite_cross_restart.gd` | `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`, `docs/gdd/ui-gdd-flow.md` | `partial` | `Need migration failure and force-migration status to be owned by a visible migration dialog or audit panel.` |
| T46 | Wire UI: Config Audit And Migration Surfaces（接线 UI：配置审计与迁移界面） | `Game.Godot/Scripts/Security/SecurityAudit.cs`, `Game.Core/Services/BattleReportAuditReader.cs`, `Game.Core/Services/BattleReportAuditValidator.cs` | `Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd`, `docs/gdd/ui-gdd-flow.candidates.json` | `docs/gdd/ui-gdd-flow.md`, `docs/architecture/overlays/PRD-lastking-T2/08/_index.md` | `partial` | `Need ConfigAuditPanel, MigrationStatusDialog, and ReportMetadataPanel to exist as stable owned surfaces with direct runtime validation.` |

## 7. 结论

按当前仓库现状，`T1-T46` 可分成四层：

1. 已有明确成品 surface 的层：
   - `MainMenu`、`HUD`、`SettingsPanel`、`SettingsScreen`，对应 Entry、Runtime、Meta 的部分能力。
2. 已有能力但仍待 UI 收口的层：
   - runtime outcome、combat interaction、economy/progression、save/meta、config audit 六大 slice 中的大多数任务。
3. 主要属于 contract / governance / validation 的层：
   - `T01`、`T21`、`T30`、`T31-T40` 的大部分合同、审计、迁移、schema 能力。
4. Chapter 7 已经明确路由，但状态未回写完成的层：
   - `T41-T46` 六个 UI wiring 任务。

如果后续要继续对齐 `newrouge` 的审计密度，下一步最有价值的不是继续扩写本文件，而是让 Chapter 7 顶层编排器能自动产出以下字段：

- 每个 slice 的实际 scene/script owner；
- 每个 candidate surface 的已落地/未落地状态；
- 每个任务的 evidence path；
- `T41-T46` 的任务状态回写条件。
