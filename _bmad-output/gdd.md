---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]
inputDocuments:
  []
documentCounts:
  briefs: 0
  research: 0
  brainstorming: 0
  projectDocs: 0
workflowType: 'gdd'
lastStep: 14
project_name: 'lastking'
user_name: 'skyo'
date: '2026-04-27'
game_type: 'tower-defense'
game_name: 'lastking'
---

# lastking - Game Design Document

**Author:** skyo
**Game Type:** Tower Defense / Strategy Hybrid
**Target Platform(s):** Windows (Steam single-player)

---

## Executive Summary

### Game Name

lastking

### Core Concept

`lastking` 的原始 PRD 指向一个以 `Day 1 ~ Day 15` 为纵向战役结构的单机塔防/防守型策略游戏：玩家在白天进行资源分配、建造、维修、训练与科技决策，在夜晚承受按预算、通道和夜型规则驱动的敌军压力，目标是在城堡生命值归零前撑过第 15 天。其核心体验不是高速操作，而是“白天做准备、夜晚验证决策”的闭环压力管理。

但结合 `docs/gdd/t1-t46-m1-wiring-audit.md` 的当前证据，M1 版本还不能完全实现 PRD 预期的完整体验。当前更准确的产品定义应是：一个已具备核心状态机、波次、资源、部分 HUD、设置、存档和配置治理能力，但大量玩家面仍只完成部分接线的可玩垂直切片。因此本阶段 GDD 应优先描述“当前可被玩家实际串联游玩的最小闭环”，而不是默认把所有 PRD 能力都视为已完整上线。

在这个 M1 可玩闭环中，玩家能够从主菜单进入游戏，查看基础 HUD，经历白天/夜晚循环，承受敌军波次，使用一部分资源、建造、训练、奖励与设置相关能力，并在胜负条件、提示反馈、进度面板、战斗交互壳层、配置审计面等位置体验到不同程度的降级或缺口。换言之，M1 当前最真实的设计目标不是“完成版城堡防守体验”，而是“验证 Day/Night 核心循环是否已经有足够多的真实接线，可以支撑下一轮 UI 收口与体验闭环修正”。

### Target Audience

- 偏中核的 PC 塔防/策略玩家
- 能接受 `60-90` 分钟单局时长的 Steam 单机玩家
- 愿意处理资源、队列、建造、科技和夜晚压力协同的玩家
- 在当前 M1 阶段，也包括能够容忍 partial UI 接线并帮助验证系统闭环的内部体验者

### Unique Selling Points (USPs)

- **Daytime planning and nighttime proof under locked deterministic rules**  
  许多防守游戏通过怪物数量和视觉 spectacle 制造紧张感，`lastking` 更强调白天做出的经济、建造、募兵与科技选择，是否真能在夜晚压力下成立。

- **System-governed difficulty, spawning, and auditability**  
  难度、刷怪、压力评分、迁移与配置安全并不是松散调参，而是通过显式契约、校验和审计链路治理，形成更可验证的系统身份。

- **Castle-defense loop with economy debt pressure instead of pure expansion fantasy**  
  债务、退款、队列中断、夜晚压力与唯一失败条件共同构成更严苛的生存决策空间，而不是轻量扩张幻想。

- **M1 as a truthful vertical slice instead of a fake feature-complete façade**  
  当前里程碑的价值在于如实暴露“系统强、表面弱、接线缺口明确”的现状，便于后续按证据修正体验闭环。

### Game Type

**Type:** Tower Defense
**Framework:** This GDD uses the `tower-defense` template with type-specific sections for wave pressure, placement strategy, economy/resources, upgrades, and difficulty scaling.

### M1 Current Playable Player Flow

1. **启动与入口阶段**：玩家从 `MainMenu` 进入，能够接触到基础启动入口与设置入口，但 `BootStatusPanel`、`ContinueGateDialog` 这类启动治理面仍未稳定落地。
2. **白天准备阶段**：玩家进入局内后，可通过现有系统进行资源管理、部分建造、升级、训练、科技相关操作；但这些能力更多是系统已存在，统一的 `ResourcePanel / BuildPanel / ProgressionPanel` 仍未完全成型。
3. **夜晚防守阶段**：夜晚波次、预算、敌军生成、目标优先级与基础战斗逻辑已有较强实现，HUD 可显示部分运行时状态；但 `CombatHud`、`PressurePanel`、`CameraControlOverlay` 等战斗壳层还不完整。
4. **夜间结算与奖励阶段**：奖励系统已具备核心逻辑和局部运行时面板，但奖励入口、确认、反馈与下一日衔接仍未完全收束到统一 outcome flow。
5. **局内辅助与元系统阶段**：设置、本地化、音频和部分存档能力相对最成熟；但云存档冲突、战报展示、成就可视化、配置审计与迁移状态更偏向测试/治理证据，而非成熟玩家面。
6. **一局结束阶段**：PRD 的真实目标是撑到 Day 15 或在城堡 HP 归零时失败；但 M1 当前更像是“可跑通核心循环并暴露缺口”的验证版，一局结束体验在 outcome 展示与收束方面仍未完全达标。

### Why This Type

本项目与 `Tower Defense` 最匹配，原因是其主信号集中在：

- `waves`：夜晚敌军按预算、通道和 schedule 生成；
- `placement`：白天建造与占地规则是核心准备手段；
- `strategy/resources/planning`：资源、债务、训练队列、升级和科技都服务于长期防守规划；
- `survival pressure`：玩家目标不是探索地图，而是在多日防守压力下维持城堡与经济体系。

它同时具有明显的 `strategy` 特征，但当前最强的玩法框架仍然是“建造准备 + 波次防守 + 夜晚压力验证”，所以在 BMAD 类型分类中应以 `tower-defense` 为主，而把策略深度视为其混合属性。

### M1 Experience Boundary

当前 M1 不能被定义为“按 PRD 完整可交付的正式可玩版本”，而应被定义为：

- **可验证主循环**：Day/Night 状态机、波次、战斗、基础 HUD、设置、部分存档已可串联；
- **可验证系统正确性**：大量 contract、audit、schema、migration 与 gate 已经建立；
- **不可视为完整玩家体验**：Prompt/Outcome、Combat UI、Economy/Progression panel、Save/Meta summary、Config audit surfaces 仍大量处于 partial/test-only/docs-only 状态。

因此，后续 GDD 与玩家游玩动线设计都应以“如何在现有已接线能力上形成最小可玩闭环，并明确缺口处的降级体验”为主线。

---

## Target Platform(s)

### Primary Platform

Windows PC via Steam (single-player only)

### Platform Considerations

- Release scope is locked to `Windows only` and `Steam single-player`.
- Match target is `60-90 minutes`, so the platform assumes desk-based, longer-form sessions rather than short mobile bursts.
- Runtime support is built around `keyboard/mouse only`; no gamepad support is planned for launch.
- Platform services expected by the PRD include `Steam Cloud`, `achievements`, autosave, and account-scoped save ownership.
- Performance expectations are governed by the locked baseline: `Avg >= 60 FPS`, `1% Low >= 45 FPS`, with low-spec stability supported by a required performance preset.
- Launch boundaries explicitly exclude `mod/workshop` and `safe-mode startup`.

### Control Scheme

- Primary input: `keyboard + mouse`
- Supported interaction style:
  - mouse-driven UI navigation
  - keyboard/mouse camera and runtime control
  - in-match time controls: `pause, 1x, 2x`
  - settings support for key rebinding and mouse sensitivity
- Launch does not support controller-first interaction.

---

## Target Audience

### Demographics

- Primary audience: PC strategy/tower-defense players on Steam
- Likely age band: late-teen to adult players who are comfortable reading layered UI and managing multiple systems at once
- Current M1 practical audience: players or internal evaluators who tolerate partial UI wiring and can validate systems despite incomplete presentation layers

### Gaming Experience

Core-to-midcore players with room for lower-difficulty onboarding.

These players are expected to handle:
- day/night pacing pressure
- resource debt and recovery rules
- building and queue planning
- upgrade and tech tradeoffs
- multi-night survival adaptation

### Genre Familiarity

The game assumes at least light familiarity with tower-defense and strategy conventions.

Players do not need to be experts at the lowest difficulties, but the design expects them to understand:
- defensive preparation before enemy pressure
- lane/wave escalation
- economy-versus-survival tradeoffs
- progression choices that influence later nights

### Session Length

`60-90 minutes` per run.

This is a sit-down PC session structure, not a short-session format. The intended pattern is one focused run with repeated day/night decision cycles.

### Player Motivations

This audience is likely drawn by:
- surviving an increasingly hostile 15-day defense arc
- turning daytime planning into nighttime payoff
- balancing economy, defense, and recruitment under pressure
- seeing deterministic systems produce fair but demanding outcomes
- climbing from approachable difficulties into harder optimization-driven runs

---

## Goals and Context

### Project Goals

- **Ship a playable Day1-Day15 defense loop**  
  Deliver a full run structure that can move from `DayStart` to `DefeatReport / Day15 victory` without blocker-level breaks in the core runtime flow.

- **Validate system-first tower-defense design under deterministic rules**  
  Prove that economy, wave generation, combat targeting, reward state, save snapshots, and config governance can coexist in one reproducible ruleset.

- **Close the gap between strong backend systems and incomplete player-facing surfaces**  
  Turn the current M1 state from “systems mostly exist, surfaces are partial” into a clearer playable slice where players can actually read, act, and understand outcomes in real time.

- **Establish a scalable PRD-to-task-to-wiring workflow**  
  Use this project not only to ship gameplay, but to prove that the repository’s governed workflow can carry a feature from locked PRD rules through implementation, UI wiring, audit evidence, and future expansion.

### Background and Rationale

`lastking` matters because it is trying to build a defense game around pressure, planning, and deterministic accountability rather than around raw action density or loosely tuned sandbox chaos. The PRD defines a very specific promise: a 15-day survival structure where daytime preparation and nighttime validation form the heart of the experience.

The current repository state shows that a large part of this promise already exists at the systems and governance level. Day/night state flow, wave channels, budget logic, economy rules, save handling, configuration safety, and validation infrastructure are substantially stronger than the current player-facing presentation. That makes this project important now: the next major value is no longer “invent the rules,” but “make the existing rules legible, playable, and coherent for an actual player.”

From a development perspective, this project also represents a disciplined alternative to ad hoc prototype growth. It combines game rules, config contracts, auditability, and UI wiring under one governed workflow. That is a meaningful differentiator for a solo or small-team strategy project, because it reduces drift while making future balancing and expansion more defensible.

---

## Unique Selling Points (USPs)

- **Daytime planning and nighttime proof under locked deterministic rules**  
  Many defense games create tension through volume and spectacle. `lastking` instead emphasizes whether your daytime economic, building, recruitment, and tech decisions actually hold up under a fixed nighttime validation loop.

- **System-governed difficulty, spawning, and auditability**  
  Core runtime behavior is not treated as loose tuning data. Difficulty, spawn composition, pressure scoring, migration, and config safety are all governed by explicit contracts and validation paths, giving the game a stronger “designed and reviewable system” identity.

- **Castle-defense loop with economy debt pressure instead of pure expansion fantasy**  
  The game’s rules around debt, refund, queue interruption, night pressure, and victory/failure produce a harsher planning space than a conventional relaxed builder. The player is not simply optimizing growth; they are making survivability tradeoffs under locked constraints.

- **M1 designed as a truthful vertical slice, not a fake feature-complete façade**  
  The current milestone is valuable because it exposes the real state of the game: strong systems, partial surfaces, and specific wiring gaps. That honesty supports faster iteration and better design correction than pretending the experience is already complete.

### Competitive Positioning

`lastking` is best positioned as a midcore single-player PC tower-defense/strategy hybrid for players who want stronger rule clarity, higher systemic accountability, and longer-form run pressure than casual tower defense games usually provide.

It does not primarily compete on spectacle, narrative character fantasy, or broad accessibility-first reach. It competes on the quality of its survival loop, the legibility of its planning consequences, and the credibility of its deterministic systems. If fully realized, it can stand out as a more governed, pressure-focused, systems-heavy alternative inside the PC defense niche.

---

## Core Gameplay

### Game Pillars

- **Prepared Survival Over Reactive Chaos**  
  The game is about whether the player’s daytime preparation can survive nighttime pressure, not about last-second twitch recovery or high-APM control.

- **Deterministic Pressure With Legible Consequences**  
  Waves, economy, targeting, rewards, and failure states should feel strict but understandable. Players should be able to see that outcomes follow rules, not arbitrary spikes.

- **Strategic Tradeoffs Across a 15-Day Arc**  
  Every build, upgrade, recruit, repair, and tech choice must compete for limited resources and future survivability. Short-term relief should often cost long-term resilience.

- **Readable Systems Before Spectacle**  
  UI, feedback, and flow should help players understand state, pressure, and consequences. If a system exists but the player cannot read it, the design is still incomplete.

**Pillar Prioritization:** When pillars conflict, prioritize in this order:  
1. Prepared Survival Over Reactive Chaos  
2. Deterministic Pressure With Legible Consequences  
3. Strategic Tradeoffs Across a 15-Day Arc  
4. Readable Systems Before Spectacle

### Core Gameplay Loop

The core loop of `lastking` is a repeating day-night survival cycle:

**DayStart -> BuildAndRecruit -> NightPrepare -> NightCombat -> Settlement -> RewardChoice -> NextDay**

During the day, the player stabilizes economy, spends resources, places or upgrades structures, manages training queues, and makes progression decisions. During night preparation, the game locks in the coming pressure phase. During night combat, the player watches the consequences of earlier planning play out under deterministic wave and targeting rules. Settlement then summarizes losses and gains, followed by a reward choice that slightly reshapes the next day’s options. The next day begins under tighter pressure, repeating the cycle until the run ends.

**Loop Diagram:**  
`Income/Autosave -> Build/Upgrade/Train -> Lock Night State -> Defend Against Wave -> Resolve Damage/Losses -> Pick Reward -> Start Next Day`

**Loop Timing:**  
- One macro loop: `Day 4 minutes + Night 2 minutes`  
- One full run target: `Day 1 ~ Day 15`  
- Intended run length: `60-90 minutes`

**Loop Variation:**  
Each iteration changes because of:
- escalating day-based pressure and budget scaling
- elite and boss night structure
- resource debt and recovery states
- build footprint and queue consequences
- upgrade, repair, and tech timing tradeoffs
- reward choices that alter the next day’s planning space
- partial M1 UI wiring, which currently changes how clearly players can read state even when the rules already exist

### Win/Loss Conditions

#### Victory Conditions

- Survive through the end of `Day 15`
- Maintain castle survival until the final night is fully resolved
- Complete the full Day/Night loop without triggering the only hard failure condition

#### Failure Conditions

- `Castle HP reaches 0`
- This is the only true run-ending failure condition in the locked PRD
- Debt, pressure, and lost tempo increase failure risk, but do not themselves create separate defeat states

#### Failure Recovery

- On defeat, the player is shown the battle report and then returns to the main menu
- Failure is intended to teach which planning decisions broke down:
  - underbuilt defenses
  - weak economy timing
  - poor queue or upgrade sequencing
  - inability to absorb late-night pressure
- In the current M1 state, this learning loop is partially limited by incomplete outcome, prompt, and summary surfaces

---

## Game Mechanics

### Primary Mechanics

#### 1. Build and Place Defenses
- **What the player does:** Place structures during daytime using footprint and pathing rules
- **When used:** Constantly during the preparation phase
- **What it tests:** Planning, spatial judgment, lane prioritization, future-risk management
- **How it feels:** Deliberate, grid-aware, consequence-heavy
- **How it progresses:** More meaningful as the player unlocks better structures, understands lane pressure, and learns where placement mistakes cascade into night failures
- **Pillars served:** Prepared Survival Over Reactive Chaos; Strategic Tradeoffs Across a 15-Day Arc

#### 2. Manage Economy and Debt
- **What the player does:** Spend, save, recover, and sequence actions under gold and iron constraints, including debt rules
- **When used:** Constantly during daytime and indirectly across the full run
- **What it tests:** Resource prioritization, discipline, sequencing, risk tolerance
- **How it feels:** Tight, restrictive, always slightly pressured
- **How it progresses:** The player gains mastery not by earning infinite resources, but by learning when to spend, pause, or accept short-term weakness
- **Pillars served:** Deterministic Pressure With Legible Consequences; Strategic Tradeoffs Across a 15-Day Arc

#### 3. Queue, Recruit, Upgrade, and Repair
- **What the player does:** Manage training queues, building upgrades, and repairs inside limited timing and cost windows
- **When used:** Frequently during daytime and between pressure spikes
- **What it tests:** Sequencing, anticipation, opportunity cost judgment
- **How it feels:** Structured, system-heavy, timing-sensitive
- **How it progresses:** Complexity rises as more concurrent needs compete for the same economy and preparation window
- **Pillars served:** Prepared Survival Over Reactive Chaos; Strategic Tradeoffs Across a 15-Day Arc

#### 4. Read and Respond to Night Pressure
- **What the player does:** Observe HUD state, wave pressure, lane escalation, damage intake, and defense performance during night combat
- **When used:** Constantly during nighttime
- **What it tests:** Situational reading, anticipation, interpretation of consequences rather than direct twitch execution
- **How it feels:** Tense, validating, constrained
- **How it progresses:** Nights become harder through budget scaling, elite/boss structure, and compounding earlier decisions
- **Pillars served:** Deterministic Pressure With Legible Consequences; Readable Systems Before Spectacle

#### 5. Choose Rewards and Progression Direction
- **What the player does:** Resolve nightly reward choices, guide build direction, and shape future planning through progression options
- **When used:** Once per night at a meaningful decision point
- **What it tests:** Long-term planning, adaptation, valuation under uncertainty
- **How it feels:** Relieving but strategic, never free of consequence
- **How it progresses:** Choices become more meaningful as the run matures and existing weaknesses or strengths compound
- **Pillars served:** Strategic Tradeoffs Across a 15-Day Arc; Prepared Survival Over Reactive Chaos

### Mechanic Interactions

These mechanics are not independent. They form a dependency chain:

- economy determines whether building, upgrading, training, and repairing are possible
- building placement changes lane pressure, target resolution, and survivability at night
- queue and upgrade timing compete with construction and repair for the same resource window
- nighttime outcomes feed settlement and reward choices
- reward choices influence the next day’s economy, defense priorities, and progression direction
- incomplete M1 surfaces currently weaken readability, but the underlying interaction chain already exists

In design terms, the game is strongest when the player can clearly see:  
`resource decision -> preparation outcome -> night pressure result -> reward/progression consequence`

### Mechanic Progression

Mechanics evolve less through action-combo complexity and more through compounded system pressure:

- early days teach basic building, recruitment, and survival rhythm
- mid-run introduces stronger pressure from scaling waves, economy stress, and higher-value tradeoffs
- late-run tests whether the player built a resilient system rather than a fragile short-term setup
- elite and boss nights act as validation spikes for earlier choices
- in the current M1 stage, progression understanding is partially limited by incomplete runtime presentation panels, not by missing systemic depth

---

## Controls and Input

### Control Scheme (Windows PC via Steam)

#### Core Input Model
- **Mouse**
  - navigate menus and panels
  - select structures, options, and reward choices
  - place buildings and confirm interaction targets
- **Keyboard**
  - camera movement
  - runtime time controls: `pause`, `1x`, `2x`
  - fast access to high-frequency gameplay actions
  - settings and utility shortcuts

#### Expected Input Mapping Style
- **Left Mouse Button:** select / confirm / place / interact
- **Right Mouse Button:** cancel / deselect / exit placement mode
- **Keyboard movement cluster:** camera pan or map navigation
- **Number row / hotkeys:** high-frequency build or control shortcuts
- **Space / dedicated key:** pause toggle
- **Dedicated speed keys:** switch between `1x` and `2x`
- **Esc:** open settings / pause context

This should remain rebindable where the PRD already expects keyboard rebinding support.

### Input Feel

Controls should feel:
- precise rather than flashy
- readable rather than overloaded
- low-friction for repeated daytime planning actions
- reliable for menu, panel, and placement-heavy interaction
- stable under long sessions, since one run is expected to last `60-90 minutes`

The game should not feel like an action RTS that demands constant micro. Inputs should support planning clarity first.

### Accessibility Controls

Planned accessibility and usability controls should align with existing PRD constraints:

- key rebinding
- mouse sensitivity adjustment
- UI scale presets: `80 / 100 / 125 / 150`
- in-match language switching: `zh-CN / en-US`
- persistent settings memory
- performance preset for lower-spec stability

Current limits that should remain explicit:
- no controller support at launch
- no high-contrast mode in v1
- no color-blind mode in v1

---

## Tower Defense Specific Design

### Tower Types and Upgrades

`lastking` 的塔防构成并不应被理解为传统“纯塔列表”，而应被理解为“建筑、防线、募兵与数值成长共同组成的防守体系”。在当前设计下，核心防守单元至少包含以下几类：

- **Castle-linked core defense structures**  
  直接服务于城堡生存的主防线建筑，是 run 成败的第一责任结构。
- **Blocking and path-shaping structures**  
  通过占地、堵路和通道塑形影响敌人路径与目标优先级。
- **Armed defense buildings**  
  对敌人造成主动火力压力，并参与路径失败后的敌方目标链。
- **Barracks-linked military production structures**  
  通过训练队列为防线提供单位支撑，不只是静态塔。
- **Progression-linked support structures or upgrades**  
  通过升级、维修、科技和数值强化延长防线寿命。

这意味着 `lastking` 的“塔”不是单一炮塔 catalogue，而是一个混合防守系统。

**Upgrade structure:**
- 升级路径以**线性强化**为主，而不是高度分叉 build tree
- 升级与修理共享资源压力，因此升级不是纯增益，而是对当日安全边际的押注
- 被摧毁升级的退款规则、取消规则、修理中断规则都已在 PRD 中锁定，强化了“升级即承诺”的手感
- 当前 M1 更偏系统已在、统一升级面板未完全收口，因此升级体验在逻辑上强于界面表达

### Enemy Wave Design

敌军设计是 `lastking` 最核心的塔防专属结构之一。

**Wave model:**
- 每晚 1 波
- Day `5` / `10` 为 `Elite Night`
- Day `15` 为 `Boss Night`
- 夜晚前 `80%` 时间按 `10s` 步长刷新
- 后 `20%` 不再刷新新敌人

**Wave composition principles:**
- `normal / elite / boss` 三通道独立
- 刷怪由 `budget -> spawn` 规则驱动，而不是脚本硬排
- 采样遵循 `greedy_fill + seeded_weighted_topk`
- 夜型权重只影响采样优先级，不替代通道预算
- 同一 run 在相同种子下必须可复现

**Enemy pressure identity:**
- 普通夜强调基础压迫与资源消耗
- 精英夜提升局部突破与防线验证强度
- 首领夜承担终局考试作用
- 路径失败时敌人必须攻击最近阻挡结构，不能空转，保证压力始终可落到玩家防线

在 M1 阶段，波次和生成逻辑的系统成熟度已经较高，但玩家对波次结构、压力来源和 lane 变化的读取面仍未完全成熟。

### Path and Placement Strategy

路径与占地策略不是附属系统，而是本作防守玩法的骨架。

**Path structure:**
- 核心是可被玩家防线主动塑形的路径压力结构
- 玩家允许通过建筑布局显著改变敌人推进路线
- 当路径完全不可达时，敌人进入阻挡建筑攻击回退链，而不是失效

**Placement restrictions:**
- 建造受 footprint 规则约束
- 建造决策既是空间选择，也是未来波次风险管理
- Placement 不是装饰层，而是“把白天判断转成夜晚结果”的主要手段

**Strategic consequences:**
- choke point 的价值极高
- 堵路不是漏洞，而是设计允许的高风险策略
- 建筑位置会影响火力效率、目标顺序、防线寿命与夜晚补救空间
- 当前 M1 在系统上已支撑这些逻辑，但 `BuildPanel`、路径可视反馈、相机/交互 overlay 还不足以把这套策略完整讲清楚

### Economy and Resources

本作经济系统不是传统轻量建塔金钱循环，而是强约束、可负债的长期生存经济。

**Core resources:**
- `gold`
- `iron_ore`

其他如人口上限、科技点更接近系统计数器，而非同级主货币。

**Economy characteristics:**
- 允许负债
- 金币为负时，大部分正成本行为被禁止
- 债务解除后立即恢复新消费能力
- 税收、维修、升级、训练、建造都与同一经济链相互牵制

**Spending identity:**
- 玩家并不是“有钱就升级”，而是在“当夜是否扛得住”和“长期是否会崩盘”之间做硬选择
- 建造、训练、升级、修理都属于真正有代价的承诺
- 队列取消、摧毁退款、修理暂停等规则共同保证经济后果可预期、可复盘

当前 M1 的问题不在于经济规则不够，而在于经济读面、成长读面和 build/progression 统一面板还不够完整。

### Abilities and Powers

`lastking` 当前不应被定义为重主动技能塔防。它更偏向“系统前置决策驱动”的防守游戏。

**Current active-power stance:**
- 以自动作战和前置准备为主
- 夜晚不是高频技能手操主导
- 玩家更像指挥与规划者，而不是持续施法者

**What currently fills the ‘powers’ role:**
- 奖励选择带来的 run 向修正
- 升级与科技带来的数值/结构强化
- 时间控制（`pause / 1x / 2x`）带来的节奏掌控
- 局部交互与防线管理，而非大招轰炸型主动技能体系

因此，在当前 GDD 阶段，能力系统应定义为**弱主动、强准备、强成长反馈**。如果未来加入更强主动能力，也必须服从“不破坏白天准备 / 夜晚验证”这一核心柱子。

### Difficulty and Replayability

本作的可重玩性主要来自确定性规则下的多难度、多决策路径和多 run 对比，而不是完全随机化。

**Difficulty structure:**
- 难度系统为 `config-driven`
- 至少 `10` 档
- 开局锁定，对局中不可切换
- 解锁链按配置逐级开放，不允许跨级跳过

**Replayability drivers:**
- 不同难度下预算倍率与资源倍率变化
- 不同建造、训练、升级、科技路线的长期后果
- elite / boss 夜作为不同 run 的关键验证点
- 奖励选择对中后期结构的重塑
- battle report、pressure score、config-governed auditability 带来的复盘价值

**What this is not:**
- 不是高度随机 roguelike
- 不是 endless-first score chase
- 不是靠频繁新技能轮换制造重复可玩性

它的 replayability 更接近：“在严格规则下继续优化一套你自认为正确、但每次都会在更高压力下被审判的防守方案”。

当前 M1 阶段的 replayability 潜力已经存在，但一部分复盘与结果展示面仍受限于 partial outcome/meta/config surfaces。

---

## Progression and Balance

### Player Progression

`lastking` 的 progression 不是以剧情推进或角色等级膨胀为主，而是以**单局内的系统成长**和**玩家对防守体系理解的提升**为主。它主要由三层组成：

1. **Run 内数值与结构成长**
   - 建筑升级
   - 防线修复与维持
   - 训练队列带来的兵力结构变化
   - 科技点与科技强化带来的长期收益

2. **Run 内策略成长**
   - 玩家逐步学会如何处理债务、税收、队列、升级与夜晚压力之间的关系
   - 玩家对 choke point、堵路代价、建筑排序、夜型差异形成更稳定判断

3. **Run 间挑战成长**
   - 难度解锁按配置逐级开放
   - 更高难度不是新机制分叉，而是同一规则集下更高强度的预算、资源与容错压力

#### Progression Types

- **Skill Progression**
  - 玩家更会做白天准备与夜晚结果的映射
  - 玩家更会识别何时扩张、何时保守、何时修理优先于升级

- **Power Progression**
  - 建筑、兵力、科技、奖励选择共同提高 run 内强度
  - 这种 power progression 受资源与 debt rules 严格约束，不是无限正反馈

- **Content Progression**
  - Day `1` 到 Day `15` 本身就是内容推进结构
  - `Elite Night` 与 `Boss Night` 构成阶段性验证节点

- **Collection / Meta-lite Progression**
  - 成就、战报、配置可审计性更偏向复盘与 mastery 记录
  - 当前并不强调重 meta 常驻养成

#### Progression Pacing

- 玩家应在前几天迅速理解基本循环：收入、建造、训练、守夜、奖励
- 中段开始感受到明显压力抬升，尤其是 elite night 前后
- 后段 progression 的重点从“获得更多东西”转向“证明现有结构是否足够稳”
- 真正的“成长感”不来自数值爆炸，而来自更稳定地撑过更高压的夜晚

### Difficulty Curve

`lastking` 的难度曲线更接近**分段抬升的 sawtooth + overall upward escalation**，而不是平滑线性增长。

#### Challenge Scaling

难度通过以下方式增长：

- 天数带来的 budget scaling
- `normal / elite / boss` 夜型切换
- 敌军构成从基础杂兵逐步走向更高压组合
- 经济与债务容错在后期变窄
- 玩家前期决策错误会在中后期被放大，而不是立刻结算

实际体验大致可分为：

- **Day 1-4**
  - 教学与理解期
  - 玩家建立基础防线和经济节奏

- **Day 5 / Day 10**
  - 精英夜 spike
  - 用于测试玩家是否真的建立了有效系统，而不是只是在普通夜勉强存活

- **Day 11-14**
  - 高压稳定期
  - 系统错误会开始持续暴露

- **Day 15**
  - 首领夜终局校验
  - 检验玩家是否真的完成了整局结构构筑

#### Difficulty Options

- 难度系统为 `config-driven`
- 至少 `10` 档
- 开局锁定，对局中不可切换
- 解锁链逐级开放，不允许跨级跳过
- 低难度负责可上手，高难度负责硬核验证
- 当前设计不依赖动态 rubber band 或临场自动救济

### Economy and Resources

本作存在明确且非常核心的 economy/resource system，因此这一节必须保留。

`lastking` 的经济不是辅助系统，而是核心 gameplay pressure source 之一。它决定玩家白天能做什么，也决定夜晚是否会因准备不足而崩盘。

#### Resources

- **Gold**
  - 核心通用支出资源
  - 参与建造、升级、训练、修理等关键行为
  - 允许为负，直接形成 debt gameplay

- **Iron Ore**
  - 核心建设/成长资源之一
  - 与 gold 一起构成主资源骨架

- **Population / Tech Points**
  - 更像系统计数器或成长约束资源
  - 服务于训练与科技 progression，而不是自由消费货币

#### Economy Flow

**Earn loop:**
- DayStart 结算税收
- 系统推进带来科技点或相应成长资源
- 夜晚结算与奖励选择为下一天提供新资源或方向

**Spend loop:**
- 建造
- 升级
- 训练
- 修理
- reroll 或其他受成本约束的 run 内选择

**Constraint identity:**
- gold 可进入负值，但负债会锁住大量新支出行为
- in-progress 行为与 new spend 行为有不同债务处理规则
- repair、queue、upgrade、building 并不是彼此独立的支出池，而是在争夺同一安全边际

**Economy balance philosophy:**
- 资源不是让玩家不断变强，而是逼迫玩家暴露优先级
- 允许负债让经济更像“延迟代价”，而不是简单的“没钱就停”
- refund、queue cancel、destroyed-state refund 等规则使系统后果可读、可控、可复盘

当前 M1 的真实问题不是 economy 设计缺失，而是这些经济和成长后果还没有被所有玩家面完整表达出来。

---

## Level Design Framework

### Structure Type

`lastking` 最合适的结构不是传统线性关卡列表，也不是开放世界，而是一个**single-run campaign structure**：

- 玩家进入一次完整 run
- run 由 `Day 1 -> Day 15` 的连续日夜循环组成
- 每一天既像一个微型 stage，又不是完全独立关卡
- 每一晚是该日的核心防守挑战节点
- 整体结构更接近“持续战役式 arena/campaign hybrid”

也就是说，它不是“选关式 tower defense”，而是“单张战场上的长程连续防守战役”。

### Level Types

虽然本作不采用传统分图关卡制，但仍然存在明确的内容段落类型：

#### 1. Opening / Onboarding Phase
- 对应 `Day 1-4`
- 负责让玩家理解白天准备、夜晚防守、基础经济与基本防线逻辑
- 重点不是低压拖时间，而是建立对主循环的正确认知

#### 2. Validation Spike Phases
- 对应 `Elite Night` 节点，如 `Day 5`、`Day 10`
- 这些不是单独地图，而是战役中的强校验段
- 作用是验证玩家是否真正建立了稳定结构

#### 3. Sustained Pressure Phase
- 对应 `Day 11-14`
- 这里不依赖新地图切换，而依赖敌军、经济和容错空间的持续收紧
- 内容差异主要来自系统压力，而不是环境美术切换

#### 4. Final Boss Resolution Phase
- 对应 `Day 15`
- 是整局 run 的终局关卡等价物
- 承担“最终考试”职责，而不是单纯更大波次

### Tutorial Integration

教程最适合以**战役内嵌式 onboarding**存在，而不是完全独立的训练关。

依据 PRD，当前最适合的结构是：
- `housing -> barracks -> survive first night` 的三步引导
- 引导应尽量嵌入前几天自然流程
- 教程目标是让玩家会完成一轮最小闭环，而不是穷举解释所有系统

因此，教程设计原则应是：
- 先教玩家如何开始 build
- 再教兵力/队列如何补足防线
- 最后让第一晚成为“低风险但真实”的验证，而不是纯脚本演示

### Special Levels

本作没有传统意义的 secret level 或独立 bonus level，但有明确的“特殊内容节点”：

- `Elite Night`
  - 中段 spike
  - 承担特殊波次验证作用

- `Boss Night`
  - 终局节点
  - 相当于最终关卡

- `DefeatReport / Outcome Resolution`
  - 虽然不是关卡，但在体验结构上承担 run 结算节点作用

如果未来扩展版本加入更多地图或模式，这些特殊节点仍应保留为战役结构骨架。

### Level Progression

本作的内容推进方式是**strict linear campaign progression inside a single run**。

玩家不会在多个地图之间自由选关，而是沿着固定的 `Day 1 -> Day 15` 顺序推进。其 progression 的核心不是地图解锁，而是：

- 日夜状态推进
- 夜型变化推进
- 敌军压力升级
- 自身防线与经济结构的累积后果

#### Unlock System

当前 run 内：
- 不存在自由选图解锁
- 内容推进由日数自然驱动
- `Elite Night` 和 `Boss Night` 是按日程锁定触发

当前 run 间：
- 主要 unlock 在 difficulty 层
- 更高难度通过逐级解锁获得
- 这类解锁不是新地图内容，而是同一内容结构下更苛刻的系统验证

#### Replayability

重玩价值主要来自：

- 更高难度挑战
- 不同建造/训练/升级/科技路线
- 不同奖励选择带来的 run 走向变化
- 对同一 15 天结构进行更优解验证
- 战报、压力评分、配置治理带来的复盘

本作的 replayability 不是“地图池很多”，而是“同一连续战役在不同策略和难度下会形成不同生存曲线”。

### Level Design Principles

建议为本作明确以下 level design 原则：

- **Teach the loop through real pressure**  
  教学应嵌在真实系统里，而不是用脱离主循环的假场景说明。

- **Each day should prepare a different question for the night**  
  每个白天不仅是经营窗口，还应让玩家面对不同的夜晚准备问题。

- **Pressure escalation should come from systems, not surprise gimmicks**  
  难度提升应主要来自预算、敌军构成、经济紧张和路径压力，而不是无预警的新 gimmick。

- **Special nights must validate prior decisions, not replace them**  
  精英夜和首领夜应该检验此前构筑，而不是突然改成另一种玩法。

- **Spatial readability matters as much as mechanical correctness**  
  如果玩家看不懂 choke point、路径后果、火力覆盖和损失来源，关卡设计就还没完成。

---

## Art and Audio Direction

### Art Style

`lastking` 最适合的视觉方向不是高写实，也不是超轻量抽象，而是**readable stylized strategy presentation**。它的首要目标不是炫技，而是支持玩家在长时段单局中快速读懂路径、防线、压力、损伤和层级关系。

建议的整体风格方向是：

- **Stylized top-down / strategy-readable presentation**
- 强调战场信息层级、建筑轮廓、敌军类别差异和压力可读性
- 美术服务于“白天规划 / 夜晚验证”的主循环，而不是喧宾夺主

这意味着视觉上应优先保证：

- 建筑类型一眼可区分
- 友军、敌军、防御建筑、城堡、城门、阻挡结构层级清晰
- 路径、 choke point、火力覆盖和损伤结果可被快速识别
- 夜晚压力升高时画面仍然不乱

#### Visual References

虽然当前仓内没有正式的美术方向文档定稿，但从玩法需求推导，适合参考的不是“纯叙事氛围型”作品，而是以下参考方向的组合：

- **They Are Billions / Kingdom-style pressure readability**
  - 用于参考持续防守压力和战场读面优先级

- **Northgard / RTS-lite functional clarity**
  - 用于参考建筑、单位、资源信息的清晰组织

- **Dark fantasy tower-defense framing**
  - 用于承接城堡防守、压力升级、Boss night 的整体气质

如果后续要补正式视觉 reference，建议明确到：
- UI reference
- battlefield readability reference
- mood/light reference
- enemy silhouette reference

#### Color Palette

推荐色彩方向采用**白天偏克制、夜晚偏压迫**的双态结构：

- **白天**
  - 中性、偏暖或偏灰土色调
  - 便于读建设状态、资源信息和地形关系
  - 强调“筹备期”的冷静判断感

- **夜晚**
  - 更冷、更暗、更具对比的压迫色调
  - 用局部高亮突出敌军、受击、城堡风险和关键事件
  - 让玩家在视觉上感到“白天决策现在开始被审判”

整体不建议使用过度饱和配色，因为这会削弱长时间策略阅读和夜晚压力分层。

#### Camera and Perspective

最适合本作的是：

- **Top-down or high-angle tactical perspective**
- 重点在于：
  - 看清路径和堵路关系
  - 看清火力覆盖和敌军推进
  - 支持白天建造与夜晚观察同一战场
- 相机应服务于策略可读性，而不是 cinematic 表演

当前 M1 审计也表明，相机与交互 overlay 仍有待补线，所以 GDD 应明确：相机体验是玩法清晰度的一部分，而不是次要 polish。

### Audio and Music

`lastking` 的音频方向应服务于两件事：

1. 强化白天与夜晚两种不同心理状态
2. 提供可靠的压力、损伤、结算与确认反馈

因此更适合的不是高密度旋律驱动，而是**atmospheric strategy tension + readable combat feedback**。

#### Music Style

建议音乐采用：

- **Ambient / orchestral-dark hybrid**
- 白天以较克制、低干扰、轻度紧张的背景为主
- 夜晚以更强的节奏压迫、持续张力和风险感提升为主
- 精英夜与首领夜需要有明显但不过度夸张的音乐层级抬升

目标不是把玩家推入动作兴奋，而是让玩家清楚感到：
- 白天在准备
- 夜晚在承压
- 特殊夜在接受更高强度审判

#### Sound Design

音效设计应优先保证“功能反馈可读”，其后才是氛围。

关键声音优先级应包括：

- 建造、取消、放置失败、升级、修理等白天操作反馈
- 资源不足、非法操作、队列受阻等错误/警示反馈
- 夜晚受击、城门受压、城堡受损、敌军突破等风险反馈
- 奖励选择、结算、战报与重要 milestone 的确认反馈
- 时间控制切换（pause / 1x / 2x）的清晰状态反馈

音效手感应偏：
- 清楚
- 稳定
- 可区分
- 不刺耳
- 长时间游玩不疲劳

#### Voice/Dialogue

当前版本不应依赖完整配音。

建议方向：

- **No full voice-over at this stage**
- 如需角色存在感，最多采用极轻量提示、简短 UI 语音或非语言化短反馈
- 核心信息传达仍应依赖清晰文本和系统反馈，而不是 VO

这更符合当前项目范围和 M1 的真实成熟度。

### Aesthetic Goals

本作的 art/audio 必须服务于此前已经定义的 pillars，而不是自成体系。

- **Prepared Survival Over Reactive Chaos**
  - 视觉和音频都应帮助玩家更好地做准备，而不是制造无意义噪音

- **Deterministic Pressure With Legible Consequences**
  - 画面与声音都应让玩家明白“发生了什么、为什么发生、哪里出问题”

- **Strategic Tradeoffs Across a 15-Day Arc**
  - 白天与夜晚的气质差异要足够明显，让 run 的结构性被感知到

- **Readable Systems Before Spectacle**
  - 美术和音频都必须优先支持清晰度
  - 如果特效、镜头、混音破坏读面，它们就违背了本作核心设计

---

## Technical Specifications

### Performance Requirements

`lastking` 的技术要求首先服从其平台锁定与长时段 run 结构：这是一个 `Windows only`、`Steam single-player`、单局 `60-90` 分钟的策略防守游戏。因此性能目标不能只看“能跑”，而必须看“在长时间局内压力和持续 UI 读面下是否稳定”。

#### Frame Rate Target

- Primary target: `60 FPS average`
- Lower-bound hard gate: `1% Low >= 45 FPS`
- Logic target:
  - `logic P95 <= 16.6ms`
  - `logic P99 <= 22.0ms`

这些目标已经在 PRD 中被锁为当前基线，不能在 GDD 层随意下调。

#### Resolution Support

当前优先支持方向应为：

- baseline validation target: `1024x768`
- standard player target: `1080p`
- 可接受更高分辨率运行，但当前 GDD 不把高分辨率表现作为首要卖点
- UI 必须支持缩放预设：`80 / 100 / 125 / 150`

重点不是追求超高分辨率展示，而是保证：
- HUD 不重叠
- 中英文不截断
- 路径、单位、防线、提示、结果面在常用 PC 分辨率下都可读

#### Load Times

- cold start target: `<= 3.0s`
- run 内切换应尽量避免破坏连续战役体验
- 由于本作是单张持续战场上的 campaign-style run，优先追求：
  - 启动快
  - 进入局内快
  - autosave / settlement / report 流转稳定
  - 不因配置校验、迁移或战报生成造成明显卡顿

### Platform-Specific Details

本作平台要求已经在 PRD 中锁定得比较完整，因此这里主要是汇总为 GDD 级约束，而不是发散设计。

#### Windows PC via Steam Requirements

- Platform: `Steam single-player, Windows only`
- Input: `keyboard/mouse only`
- No controller support at launch
- Steam-account scoped save ownership
- Steam Cloud support required
- Achievements support expected
- No mod/workshop support at launch
- No safe-mode startup path in launch scope

#### Runtime and UX Requirements

- fullscreen 为默认显示模式
- key rebinding 必须支持
- mouse sensitivity 必须支持
- in-match language switching 支持 `zh-CN / en-US`
- time controls 必须支持：`pause / 1x / 2x`
- pause 语义必须是真正的 full freeze，而不是只停表面动画
- low-spec stability 需要 performance preset 支撑

#### Save / Migration / Integrity Requirements

- save policy: `single autosave only`
- no manual overwrite path
- save migration: `forced migration with pre-backup`
- migration failure 时必须拒绝加载并建议删除存档
- unofficial / dirty save 与 cloud / achievement 行为必须遵守 PRD 锁定策略
- config mismatch / load failure 必须阻止开局，而不是 silent fallback into unsafe runtime

### Asset Requirements

本作的资产需求应按“支撑清晰战场读面和连续 run 体验”来定义，而不是按纯展示型高资产密度项目来定义。

#### Art Assets

关键美术资产类别包括：

- 战场地表与结构表现
- 建筑与防御结构视觉分类
- 敌军类型轮廓区分
- 友军 / 训练结果 / 队列相关表现
- HUD / Prompt / Outcome / Settings / Meta UI 资产
- Config / audit / report / migration 相关玩家面资产（如后续继续补齐）

质量重点：
- silhouette clarity 高于复杂细节
- 战场缩放下仍能区分关键对象
- 白天/夜晚状态变化可感知
- 中英文本地化布局可承受不同文本长度

#### Audio Assets

关键音频资产类别包括：

- 白天 BGM
- 夜晚 BGM
- Elite / Boss pressure variation tracks
- build / upgrade / repair / cancel / error / confirm 反馈音效
- damage / breach / loss / milestone / reward / report 反馈音效
- settings 和 time-control 反馈音效

质量重点：
- 长时游玩不疲劳
- 风险反馈优先级高
- 不依赖语音来替代系统可读性

#### External Assets

当前 GDD 可以接受使用外部资产，但前提是：

- 不破坏整体风格统一
- 不破坏 UI/战场读面优先级
- 不引入授权或维护风险
- 不让核心系统表现依赖无法持续控制的第三方包

如果使用第三方素材，更适合用于：
- 过渡性 UI 图元
- 通用环境元素
- 基础音效库
而不是用于决定整个项目核心识别度的关键战场资产。

### Technical Constraints

当前已知技术约束应明确写入：

- Engine / stack 已锁定为 `Godot 4.5.1 + C#`
- Windows-only 目标限制了输入与平台设计边界
- 长时间单局结构要求 autosave、report、config validation、migration 都必须稳定
- 本地化硬门要求 `zh-CN / en-US` HUD 无截断、无重叠
- performance preset 不是可有可无选项，而是低配稳定性必需项
- 当前 M1 的主要技术风险不只是性能，而是“系统能力存在，但玩家面表达不足”
- 因此后续技术工作不应只堆底层能力，还必须优先补齐：
  - HUD / outcome readability
  - combat interaction surfaces
  - economy/progression panels
  - meta/report/config audit surfaces

---

## Development Epics

### Epic Overview

| # | Epic Name | Scope | Dependencies | Est. Stories |
| --- | --- | --- | --- | --- |
| 1 | Foundation, Boot, and Runtime Baseline | 平台基线、启动流程、主菜单、基础运行时骨架、导出与环境锁定 | None | 6-8 |
| 2 | Core Survival Loop and HUD Readability | Day/Night 状态机、城堡 HP、胜负、基础 HUD、运行时结果与提示闭环 | 1 | 8-10 |
| 3 | Combat Pressure and Defensive Validation | 波次预算、刷怪、目标优先级、路径失败回退、战斗验证与夜晚压力表达 | 1, 2 | 8-10 |
| 4 | Economy, Building, and Progression Systems | 资源、建造、升级、修理、训练、科技、奖励与成长压力 | 2, 3 | 10-12 |
| 5 | Meta Systems, Save, and Platform Services | 存档、云存档、设置、本地化、音频、成就、性能门与平台整合 | 1, 2 | 8-10 |
| 6 | UI Wiring, Auditability, and Production Closure | Chapter 7 六大 UI slice 收口、配置审计、迁移状态、战报读面、最终体验闭环 | 2, 3, 4, 5 | 10-12 |

### Recommended Sequence

1. **Epic 1: Foundation, Boot, and Runtime Baseline**  
   先建立 Windows/Steam 单机、Godot 4.5.1 + C#、主菜单、主场景和运行时起点，否则后续所有可玩性都没有落脚点。

2. **Epic 2: Core Survival Loop and HUD Readability**  
   先让玩家能真正进入一局、看到昼夜与 HP、知道赢输与结果，否则无法验证主循环。

3. **Epic 3: Combat Pressure and Defensive Validation**  
   在 loop 能跑以后，补足夜晚真正的敌军压力、波次与路径逻辑，让“白天准备 / 夜晚验证”成立。

4. **Epic 4: Economy, Building, and Progression Systems**  
   把建造、资源、训练、升级、科技、奖励接入主循环，形成真正的策略防守而不是空壳夜战。

5. **Epic 5: Meta Systems, Save, and Platform Services**  
   当核心可玩后，再把存档、设置、本地化、音频、平台服务与性能门做成稳定产品层。

6. **Epic 6: UI Wiring, Auditability, and Production Closure**  
   最后把大量 partial/test-only/docs-only 的玩家面收口，并把配置治理、迁移、战报、审计面做成真实可读体验。

### Vertical Slice

**The first playable milestone:**  
玩家可以从主菜单进入游戏，完成 `housing -> barracks -> survive first night` 的最小 onboarding，看到基础 HUD、经历白天准备与第一晚防守，并在结果后继续进入下一天。这是最小真实 vertical slice，而不是假演示。

---

## Success Metrics

### Technical Metrics

本作的技术成功标准不只是“能运行”，而是“在长时间单局、持续读面、系统压力累积的前提下依然稳定、清晰、可验证”。

#### Key Technical KPIs

| Metric | Target | Measurement Method |
| --- | --- | --- |
| Average Frame Rate | `>= 60 FPS` | Performance gate runs on target baseline scenes and representative long-session captures |
| 1% Low Frame Rate | `>= 45 FPS` | Automated perf gate + repeatable scenario capture |
| Logic P95 | `<= 16.6ms` | Instrumented logic timing from representative runs |
| Logic P99 | `<= 22.0ms` | Instrumented logic timing from representative runs |
| Cold Start Time | `<= 3.0s` | Launch-to-menu timing on target baseline hardware |
| Save / Load Integrity | `0 blocker-level corruption cases in gated validation` | Save migration, autosave, and recovery test suite |
| Config Validation Safety | `0 unsafe match starts after config mismatch/load failure` | Config validation gate, audit logs, and negative-path tests |
| i18n HUD Hard Gate | `0 critical truncation / overlap in zh-CN and en-US` | Screenshot diff + manual spot check |
| Build Stability | `0 release-blocking bootstrap/export failures on governed baseline` | Windows export/startup validation pipeline |

#### Technical Success Interpretation

技术上成功意味着：
- 不只是 benchmark 好看
- 而是 boot、run、save、config、i18n、performance 全部在一个真实 vertical slice 内能同时成立
- 尤其不能出现“底层系统是对的，但玩家面读不懂”的伪成功

### Gameplay Metrics

本作的 gameplay metric 必须围绕此前定义的 pillars 和 goals，而不是泛泛看留存。

#### Key Gameplay KPIs

| Metric | Target | Measurement Method |
| --- | --- | --- |
| First-Night Survival Clarity | 新玩家在最小 onboarding 后能理解第一晚为何成功或失败 | Guided playtest observation + structured post-run interview |
| Day/Night Loop Legibility | 多数测试玩家能正确描述白天在准备什么、夜晚在验证什么 | Playtest interviews using pillar language |
| Day1-Day15 Run Completion at Intended Skill Bands | 低难可上手，高难可硬核；通关率符合 PRD 难度梯度预期 | Difficulty-band playtest cohorts + seeded run analysis |
| Difficulty Usage Distribution | 玩家不会异常集中在单一难度，难度梯度有真实区分度 | Save/report telemetry and playtest tracking |
| Economy Decision Meaningfulness | 玩家能感知债务、升级、训练、修理之间的真实取舍 | Post-session surveys + observation of repeated misplays |
| Combat Fairness Perception | 玩家普遍将失败归因为决策不足而非随机不公 | Defeat interviews + battle report review |
| Reward Choice Relevance | 奖励选择被视为能改变下一天 planning 的重要节点 | Reward selection logs + post-run recall |
| UI Readability Closure | 测试玩家能通过 surface 直接读懂状态，而不是靠外部解释 | UI-focused playtests on HUD / outcome / economy / meta surfaces |
| Battle Report Usefulness | 玩家或测试者能用战报解释 run 的关键失误或成功点 | Battle report review sessions |
| Replayability Through Strategy Variation | 不同 build / reward / difficulty 路径能产生不同 run 体验 | Comparative seeded-run playtests |

#### Gameplay Success Interpretation

玩法上成功意味着：
- 玩家真的感受到“白天规划 / 夜晚审判”
- 失败被理解为系统后果，而不是黑箱惩罚
- 经济、战斗、成长、奖励都在推动同一个生存主循环
- M1 不再只是系统存在，而是体验可读

### Qualitative Success Criteria

以下 qualitative signs 对本作同样关键：

- 玩家会主动用“准备、压力、撑住、扛夜、规划失误”这类词描述体验
- 玩家能够在不依赖开发者解释的情况下，说清自己为什么输
- 玩家认为高压来自系统，而不是来自混乱或信息缺失
- 玩家把精英夜和首领夜视为“验证点”，而不是无意义 spike
- 玩家会提到：
  - run 很长，但节奏是清楚的
  - 资源压力真实存在
  - 失败后知道下次要改什么
- 内部评审不再把 M1 评价为“功能很多但接不起来”，而是“已经形成真实可玩的防守闭环”

### Metric Review Cadence

建议将指标分三层回顾：

- **每次 gated validation / CI review**
  - 性能
  - 启动
  - 存档
  - config safety
  - i18n hard gate

- **每个 playable milestone**
  - first-night clarity
  - HUD / outcome readability
  - economy and reward understanding
  - battle report usefulness

- **每个 major vertical slice review**
  - Day1-Day15 完整体验质量
  - difficulty 梯度是否成立
  - replayability 是否来自真实策略差异
  - 当前 partial/test-only/docs-only surfaces 是否继续收口

最终，这些 metrics 的作用不是做形式化汇报，而是回答一个核心问题：

**`lastking` 是否已经从“规则正确”进化到“玩家可读、可玩、可复盘、可持续优化”的真实产品状态。**

---

## Out of Scope

以下内容明确不属于当前 `lastking` v1 / M1 交付范围：

- 多人联机、PVP、合作模式
- 主机版、移动版、Web 版、VR 版移植
- 手柄首发支持
- Mod / Workshop / 地图编辑器
- 开放世界、自由探索型地图结构
- 无限模式、纯分数追逐型 endless mode 作为首发主模式
- 大规模剧情演出、完整配音、角色驱动叙事扩展
- 高对比模式、色盲模式等更重 accessibility 扩展（当前 v1 不含）
- 大规模 post-launch 内容包、DLC 地图包、额外战役章节
- 任何会破坏 PRD 锁定规则的临时救济设计，例如对局中改难度、非审计热更核心配置

### Deferred to Post-Launch

可在后续版本评估，但当前不纳入首发承诺的方向包括：

- 更多语言支持：`ru-RU`、`es-419`、`pt-BR`、`de-DE`
- 更丰富的 meta 统计与长期成长层
- 更成熟的 achievements/readout surface
- 更强的 config audit / migration / report 可视面 polish
- 更复杂的主动能力系统
- 更多地图、模式或特殊事件结构

---

## Assumptions and Dependencies

### Key Assumptions

- 玩家平台为 `Windows PC + Steam`，并接受键鼠为唯一首发输入方式。
- 玩家愿意进行 `60-90` 分钟单局体验，而不是碎片化短会话玩法。
- 当前产品价值主要来自“白天规划 / 夜晚验证”的系统闭环，而不是重叙事或高动作密度。
- 配置治理、schema、migration、audit 这类底层治理能力会继续保留为正式产品约束，而不是开发期临时手段。
- 难度梯度通过配置和 seeded validation 可以持续校准，不需要在 v1 引入动态 rubber band 机制。
- 当前 M1 的主要短板在玩家面收口与可读性，而不是核心规则缺失。

### External Dependencies

- `Godot 4.5.1 + C#` 运行时与导出链稳定可用
- Steam 平台能力可用，包括 Cloud、achievement、账号范围存档语义
- 当前使用的测试、CI、配置审计和 review pipeline 继续可运行
- 所有第三方素材或工具链的授权保持有效
- Windows 导出与目标 baseline 机器环境可复现性能门结果

### Risk Factors

- 如果 UI wiring 继续落后于系统实现，项目会继续停留在“规则正确但体验不完整”的状态。
- 如果 battle report、migration、config audit 只停留在治理面而不转成玩家可读面，后续复盘价值会被削弱。
- 如果长局性能和中英文本地化读面不稳定，会直接损害目标用户体验。
- 如果 difficulty curve 与 reward/economy pressure 失衡，会破坏“公平但严苛”的设计身份。
- 如果保存、迁移或配置校验出现 blocker 级不稳定，长时段单局结构会失去可信度。

---

## Document Information

**Document:** lastking - Game Design Document  
**Version:** 1.0  
**Created:** 2026-04-27  
**Author:** skyo  
**Status:** Complete

### Change Log

| Version | Date | Changes |
| --- | --- | --- |
| 1.0 | 2026-04-27 | Initial GDD complete |
