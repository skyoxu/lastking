# lastking - Development Epics

## Epic Overview

| # | Epic Name | Goal | Dependencies | Playable Result |
| --- | --- | --- | --- | --- |
| 1 | Foundation, Boot, and Runtime Baseline | 建立可进入、可启动、可运行的产品壳层 | None | 可从主菜单稳定进入运行时 |
| 2 | Core Survival Loop and HUD Readability | 建立昼夜、胜负、基础 HUD 和结果感知 | 1 | 玩家能体验最小主循环 |
| 3 | Combat Pressure and Defensive Validation | 建立夜晚敌军压力与防线验证 | 1, 2 | 夜晚真正构成可失败的防守挑战 |
| 4 | Economy, Building, and Progression Systems | 建立准备期决策与成长系统 | 2, 3 | 白天选择会真实影响夜晚结果 |
| 5 | Meta Systems, Save, and Platform Services | 建立产品级元系统与平台能力 | 1, 2 | 体验具备保存、设置、本地化和平台完整性 |
| 6 | UI Wiring, Auditability, and Production Closure | 把现有强系统收束成完整可读玩家体验 | 2, 3, 4, 5 | M1 从“系统在”升级为“体验可用” |

---

## Epic 1: Foundation, Boot, and Runtime Baseline

### Goal
建立项目的可启动、可进入、可导出、可验证的基础壳层。

### Scope
**Includes:**
- Windows-only / Steam single-player 基线
- Godot 4.5.1 + C# 项目结构
- 主菜单与主场景入口
- 基础启动流与运行时 bootstrap
- 导出验证和启动稳定性验证

**Excludes:**
- 完整夜晚敌军系统
- 完整经济与成长
- 完整 meta 与审计面

### Dependencies
- None

### Deliverable
玩家可以从主菜单稳定进入项目，项目可在目标平台边界内启动和运行。

### Stories
- As a player, I can launch the game and reach the main menu so that the product feels like a real runnable build.
- As a player, I can start a new run from a stable entry point so that the core game can begin consistently.
- As a developer, I can validate Windows startup/export behavior so that later features build on a reliable baseline.
- As a player, I can access essential top-level navigation so that boot flow is understandable.

---

## Epic 2: Core Survival Loop and HUD Readability

### Goal
建立最小可玩的 Day/Night 生存循环，并让玩家能读懂当前状态。

### Scope
**Includes:**
- Day/Night 状态机
- 城堡 HP
- 胜利/失败条件
- 基础 HUD
- 运行时提示与结果表达的最小闭环
- 最小 onboarding 节律

**Excludes:**
- 高复杂度战斗压力表达
- 完整经济与成长深度
- 完整结果/战报 polish

### Dependencies
- Epic 1

### Deliverable
玩家能进入一局，经历昼夜循环，看到基础状态，并在失败或推进时理解当前发生了什么。

### Stories
- As a player, I can see whether it is day or night so that I know what phase of the run I am in.
- As a player, I can see castle health so that I understand survival risk.
- As a player, I can lose only when the castle is destroyed so that failure rules stay clear.
- As a player, I can reach the next day after surviving a night so that the run meaningfully progresses.
- As a player, I can receive basic prompts and outcomes so that the loop is legible.

---

## Epic 3: Combat Pressure and Defensive Validation

### Goal
让夜晚真正成为对白天准备的系统性考试。

### Scope
**Includes:**
- 波次预算与通道
- 按节奏刷怪
- 目标优先级与路径失败回退
- 友伤禁用战斗逻辑
- 夜晚压力表达
- 相机/交互压力相关反馈基础

**Excludes:**
- 完整 progression 深度
- 完整 meta 层
- 完整 config audit surfaces

### Dependencies
- Epic 1
- Epic 2

### Deliverable
夜晚防守成为真实挑战；玩家能感受到压力升级、路径后果和防线是否有效。

### Stories
- As a player, I can face escalating nightly waves so that the game tests my preparation.
- As a player, I can rely on deterministic enemy path and targeting behavior so that failures feel fair.
- As a player, I can observe when enemies are blocked and how they respond so that spatial decisions matter.
- As a player, I can read combat pressure well enough to understand where my defense is failing.
- As a player, I can survive or collapse based on my actual defensive setup rather than arbitrary chaos.

---

## Epic 4: Economy, Building, and Progression Systems

### Goal
把白天准备期真正做成有代价、有方向、有成长的策略层。

### Scope
**Includes:**
- 资源系统
- 债务与税收规则
- 建造与 footprint
- 升级与修理
- 训练队列
- 科技系统
- 奖励选择与 run 内成长

**Excludes:**
- 平台服务层
- 完整 settings/save polish
- 最终审计与迁移闭环

### Dependencies
- Epic 2
- Epic 3

### Deliverable
玩家在白天做出的经济与建设选择，会以清晰而严苛的方式影响夜晚结果。

### Stories
- As a player, I can spend gold and iron on meaningful defense decisions so that the day phase matters.
- As a player, I can enter debt and feel its restrictions so that resource mistakes have weight.
- As a player, I can place buildings with footprint consequences so that map space becomes strategic.
- As a player, I can train units and manage queues so that military planning becomes part of survival.
- As a player, I can upgrade, repair, and invest in tech so that long-run structure matters.
- As a player, I can choose nightly rewards so that each run develops differently.

---

## Epic 5: Meta Systems, Save, and Platform Services

### Goal
把核心玩法提升为产品级体验，支持连续游玩、设置调整和平台完整性。

### Scope
**Includes:**
- 自动存档
- 云存档与冲突策略
- 设置、音频、输入、本地化
- 成就
- 性能模式与性能门
- Steam 相关平台要求

**Excludes:**
- 最终 UI 收口
- 配置审计可视面全量完成
- 最终 production closure

### Dependencies
- Epic 1
- Epic 2

### Deliverable
玩家可以稳定保存、继续、调整设置，并在平台边界内获得一致产品体验。

### Stories
- As a player, my run autosaves safely so that long sessions are protected.
- As a player, I can change settings and have them persist so that the game fits my machine and preference.
- As a player, I can switch between supported languages so that UI remains usable in zh-CN and en-US.
- As a player, I can benefit from cloud save behavior consistent with account ownership rules.
- As a player, I can rely on the game’s performance preset so that longer runs remain stable.

---

## Epic 6: UI Wiring, Auditability, and Production Closure

### Goal
把现有成熟系统与治理能力收束成真实可读、可交付、可复盘的完整 M1 体验。

### Scope
**Includes:**
- 六大 UI slice 的真实 surface 收口
- MainMenu / HUD / Combat / Economy / Meta / Config Audit 统一接线
- 战报读面
- 迁移状态和配置审计状态表达
- 玩家面与治理面的最终桥接

**Excludes:**
- 全新玩法大扩展
- 新平台移植
- 重做既有 PRD 基线

### Dependencies
- Epic 2
- Epic 3
- Epic 4
- Epic 5

### Deliverable
M1 从“系统大多存在但玩家面不完整”提升为“玩家能读懂、能验证、能复盘的可交付垂直切片”。

### Stories
- As a player, I can understand boot and continue state through clear entry surfaces.
- As a player, I can read outcomes, prompts, and errors through stable runtime surfaces.
- As a player, I can manage economy and progression through dedicated panels rather than inferred behavior.
- As a player, I can understand meta state, save state, and summary state through clear UI surfaces.
- As a developer or advanced player, I can inspect config, migration, and report metadata through governed readable surfaces.
- As a team, we can treat the M1 build as a true vertical slice rather than a collection of partial systems.
