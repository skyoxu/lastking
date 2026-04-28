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

# lastking - 游戏设计文档

**作者：** skyo
**游戏类型：** 塔防 / 策略混合
**目标平台：** Windows（Steam 单机）

---

## 执行摘要

### 游戏名称

lastking

### 核心概念

`lastking` 的原始 PRD 指向一个以 `Day 1 ~ Day 15` 为纵向战役结构的单机塔防/防守型策略游戏：玩家在白天进行资源分配、建造、维修、训练与科技决策，在夜晚承受按预算、通道和夜型规则驱动的敌军压力，目标是在城堡生命值归零前撑过第 15 天。其核心体验不是高速操作，而是“白天做准备、夜晚验证决策”的闭环压力管理。

但结合 `docs/gdd/t1-t46-m1-wiring-audit.md` 的当前证据，M1 版本还不能完全实现 PRD 预期的完整体验。当前更准确的产品定义应是：一个已具备核心状态机、波次、资源、部分 HUD、设置、存档和配置治理能力，但大量玩家面仍只完成部分接线的可玩垂直切片。因此本阶段 GDD 应优先描述“当前可被玩家实际串联游玩的最小闭环”，而不是默认把所有 PRD 能力都视为已完整上线。

在这个 M1 可玩闭环中，玩家能够从主菜单进入游戏，查看基础 HUD，经历白天/夜晚循环，承受敌军波次，使用一部分资源、建造、训练、奖励与设置相关能力，并在胜负条件、提示反馈、进度面板、战斗交互壳层、配置审计面等位置体验到不同程度的降级或缺口。换言之，M1 当前最真实的设计目标不是“完成版城堡防守体验”，而是“验证 Day/Night 核心循环是否已经有足够多的真实接线，可以支撑下一轮 UI 收口与体验闭环修正”。

### 目标用户

- 偏中核的 PC 塔防/策略玩家
- 能接受 `60-90` 分钟单局时长的 Steam 单机玩家
- 愿意处理资源、队列、建造、科技和夜晚压力协同的玩家
- 在当前 M1 阶段，也包括能够容忍 partial UI 接线并帮助验证系统闭环的内部体验者

### 独特卖点（USP）

- **在锁定的确定性规则下，以白天规划和夜晚验证为核心**  
  许多防守游戏通过怪物数量和视觉 spectacle 制造紧张感，`lastking` 更强调白天做出的经济、建造、募兵与科技选择，是否真能在夜晚压力下成立。

- **由系统治理的难度、刷怪与可审计性**  
  难度、刷怪、压力评分、迁移与配置安全并不是松散调参，而是通过显式契约、校验和审计链路治理，形成更可验证的系统身份。

- **以经济债务压力驱动的城堡防守循环，而非单纯扩张幻想**  
  债务、退款、队列中断、夜晚压力与唯一失败条件共同构成更严苛的生存决策空间，而不是轻量扩张幻想。

- **M1 作为真实垂直切片，而不是伪装成功能完整的门面**  
  当前里程碑的价值在于如实暴露“系统强、表面弱、接线缺口明确”的现状，便于后续按证据修正体验闭环。

### 游戏类型

**类型：** 塔防
**框架：** 本 GDD 使用 `tower-defense` 模板，并按类型补充波次压力、摆放策略、经济 / 资源、升级和难度缩放等章节。

### M1 当前可玩的玩家流程

1. **启动与入口阶段**：玩家从 `MainMenu` 进入，能够接触到基础启动入口与设置入口，但 `BootStatusPanel`、`ContinueGateDialog` 这类启动治理面仍未稳定落地。
2. **白天准备阶段**：玩家进入局内后，可通过现有系统进行资源管理、部分建造、升级、训练、科技相关操作；但这些能力更多是系统已存在，统一的 `ResourcePanel / BuildPanel / ProgressionPanel` 仍未完全成型。
3. **夜晚防守阶段**：夜晚波次、预算、敌军生成、目标优先级与基础战斗逻辑已有较强实现，HUD 可显示部分运行时状态；但 `CombatHud`、`PressurePanel`、`CameraControlOverlay` 等战斗壳层还不完整。
4. **夜间结算与奖励阶段**：奖励系统已具备核心逻辑和局部运行时面板，但奖励入口、确认、反馈与下一日衔接仍未完全收束到统一 outcome flow。
5. **局内辅助与元系统阶段**：设置、本地化、音频和部分存档能力相对最成熟；但云存档冲突、战报展示、成就可视化、配置审计与迁移状态更偏向测试/治理证据，而非成熟玩家面。
6. **一局结束阶段**：PRD 的真实目标是撑到 Day 15 或在城堡 HP 归零时失败；但 M1 当前更像是“可跑通核心循环并暴露缺口”的验证版，一局结束体验在 outcome 展示与收束方面仍未完全达标。

### 为什么归类为这一类型

本项目与 `Tower Defense` 最匹配，原因是其主信号集中在：

- `waves`：夜晚敌军按预算、通道和 schedule 生成；
- `placement`：白天建造与占地规则是核心准备手段；
- `strategy/resources/planning`：资源、债务、训练队列、升级和科技都服务于长期防守规划；
- `survival pressure`：玩家目标不是探索地图，而是在多日防守压力下维持城堡与经济体系。

它同时具有明显的 `strategy` 特征，但当前最强的玩法框架仍然是“建造准备 + 波次防守 + 夜晚压力验证”，所以在 BMAD 类型分类中应以 `tower-defense` 为主，而把策略深度视为其混合属性。

### M1 体验边界

当前 M1 不能被定义为“按 PRD 完整可交付的正式可玩版本”，而应被定义为：

- **可验证主循环**：Day/Night 状态机、波次、战斗、基础 HUD、设置、部分存档已可串联；
- **可验证系统正确性**：大量 contract、audit、schema、migration 与 gate 已经建立；
- **不可视为完整玩家体验**：Prompt/Outcome、Combat UI、Economy/Progression panel、Save/Meta summary、Config audit surfaces 仍大量处于 partial/test-only/docs-only 状态。

因此，后续 GDD 与玩家游玩动线设计都应以“如何在现有已接线能力上形成最小可玩闭环，并明确缺口处的降级体验”为主线。

---

## 目标平台

### 主要平台

Windows PC（通过 Steam，仅单机）

### 平台考虑

- 发布范围锁定为 `Windows only` 和 `Steam single-player`。
- 单局目标为 `60-90 minutes`，因此平台前提是桌面端、长时段会话，而不是移动端短局体验。
- 运行时交互围绕 `keyboard/mouse only` 构建；首发不计划支持手柄。
- PRD 预期的平台服务包括 `Steam Cloud`、成就、自动存档以及账号范围的存档归属。
- 性能预期受锁定基线约束：`Avg >= 60 FPS`、`1% Low >= 45 FPS`，并要求通过性能预设保障低配稳定性。
- 首发范围明确排除 `mod/workshop` 和 `safe-mode startup`。

### 控制方案

- 主要输入：`keyboard + mouse`
- 支持的交互方式：
  - 鼠标驱动的 UI 导航
  - 键鼠控制镜头与运行时操作
  - 局内时间控制：`pause, 1x, 2x`
  - 设置支持按键重绑定与鼠标灵敏度
- 首发不支持以手柄为主的交互方式。

---

## Target Audience

### 用户画像

- 核心受众：Steam 平台上的 PC 策略 / 塔防玩家
- 可能年龄段：从青少年后期到成年玩家，能够接受分层 UI 和多系统并行管理。
- 当前 M1 的实际受众：能容忍部分 UI 未接全、并可在表现层不完整前提下验证系统的玩家或内部评估者。

### 游戏经验要求

面向核心到中核玩家，同时保留低难度 onboarding 空间。

预期这些玩家能够处理：
- 昼夜节奏压力
- 资源债务与恢复规则
- 建造与队列规划
- 升级与科技取舍
- 跨多夜的生存适应

### 类型熟悉度

本作默认玩家至少对塔防和策略类约定有基础熟悉度。

玩家在最低难度下不需要是专家，但设计默认他们能够理解：
- 在敌军压力到来前先完成防守准备
- 路线 / 波次升级
- 经济与生存之间的取舍
- 会影响后续夜晚的成长选择

### 会话时长

`60-90 minutes` per run.

这是一种坐下来持续游玩的 PC 会话结构，而不是短局制格式。预期模式是一局聚焦式 run，并在其中反复经历昼夜决策循环。

### 玩家动机s

这类玩家通常会被以下因素吸引：
- 在越来越 hostile 的 15 天防守弧线上存活下来
- 把白天规划转化为夜晚回报
- 在压力下平衡经济、防守与招募
- 看到确定性系统产出公平但严苛的结果
- 从较易上手的难度一路爬升到更强调优化的高难 run

---

## 目标与上下文

### 项目目标

- **交付一个可玩的 Day1-Day15 防守循环**  
  交付一条完整的 run 结构，使其能够从 `DayStart` 走到 `DefeatReport / Day15 victory`，且核心运行时流程中不存在阻断级断点。

- **在确定性规则下验证系统优先的塔防设计**  
  证明经济、波次生成、战斗索敌、奖励状态、存档快照与配置治理，能够在同一套可复现规则集中共存。

- **缩小强后端系统与不完整玩家面的落差**  
  把当前 M1 从“系统大多存在、表现面仍不完整”的状态，推进成一个更清晰的可玩切片，让玩家能够实时读懂状态、采取行动并理解结果。

- **建立可扩展的 PRD 到任务再到接线的工作流**  
  让这个项目不仅能交付玩法，也能证明仓库中的受治理工作流，能够把一个功能从锁定的 PRD 规则一路带到实现、UI 接线、审计证据以及后续扩展。

### 背景与设计理由

`lastking` 之所以重要，是因为它试图围绕压力、规划和确定性问责来构建一款防守游戏，而不是围绕原始动作密度或松散调参的沙盒混乱来组织体验。PRD 给出了一个非常明确的承诺：以 15 天生存结构为骨架，让白天准备与夜晚验证成为体验核心。

当前仓库状态表明，这个承诺中相当大的一部分已经在系统和治理层面存在。昼夜状态流转、波次通道、预算逻辑、经济规则、存档处理、配置安全与校验基础设施，都明显强于当前面向玩家的表现层。因此这个项目当前真正有价值的下一步，已经不再是“发明规则”，而是“让现有规则对真实玩家变得可读、可玩且连贯”。

从开发视角看，这个项目也代表了一种相对于临时原型膨胀更有纪律的路线。它把游戏规则、配置契约、可审计性和 UI 接线放进同一条受治理的工作流中。对单人或小团队策略项目而言，这是一个有意义的差异化点，因为它能减少漂移，并让后续平衡与扩展更有依据。

---

## Unique Selling Points (USPs)

- **在锁定的确定性规则下，以白天规划和夜晚验证为核心**  
  许多防守游戏依靠数量堆叠和场面奇观制造紧张感；`lastking` 更强调的是：你在白天做出的经济、建造、招募与科技决策，是否真的能在固定的夜间验证循环中站得住脚。

- **由系统治理的难度、刷怪与可审计性**  
  核心运行时行为并不被视为松散的调参数据。难度、刷怪组成、压力评分、迁移与配置安全都受明确契约和校验路径治理，因此本作更像一个“被设计过、可被审查”的系统。

- **以经济债务压力驱动的城堡防守循环，而非单纯扩张幻想**  
  围绕债务、返还、队列中断、夜间压力与胜负条件的规则，共同构成了比传统宽松建造类游戏更严苛的规划空间。玩家并不只是优化增长，而是在锁定约束下做出生存性取舍。

- **M1 被设计成真实垂直切片，而不是伪装成功能完整的门面**  
  当前里程碑的价值在于，它真实暴露了游戏现状：系统很强、表现面不完整、接线缺口明确。相比假装体验已经完整，这种诚实更有利于更快迭代和更准确的设计修正。

### 竞品定位

`lastking` 最适合被定位为一款面向中核玩家的 PC 单机塔防 / 策略混合游戏，服务于那些希望获得比休闲塔防更清晰规则、更高系统约束性以及更长程 run 压力的玩家。

它的主要竞争点不是视觉奇观、角色叙事幻想，或以广泛可达性为先的覆盖面；它真正竞争的是生存循环质量、规划后果的可读性，以及确定性系统的可信度。如果最终完整落地，它会成为 PC 防守细分品类中一个更受治理、更强调压力、也更偏系统型的替代方案。

---

## 核心玩法

### 设计支柱

- **以准备驱动的生存，高于临场混乱反应**  
  这款游戏的核心，在于玩家白天的准备能否扛住夜晚压力，而不是依赖最后一秒的手速挽救或高 APM 操作。

- **具有可读后果的确定性压力**  
  波次、经济、索敌、奖励和失败状态都应当严苛但可理解。玩家应该能看出结果来自规则，而不是任意的数值尖刺。

- **贯穿 15 天弧线的策略取舍**  
  每一次建造、升级、招募、维修和科技选择，都必须争夺有限资源与未来生存空间。短期缓解通常应该以长期韧性为代价。

- **系统可读性优先于场面奇观**  
  UI、反馈与流程应帮助玩家理解状态、压力和后果。如果一个系统已经存在，但玩家无法读懂它，那么设计依然是不完整的。

**支柱优先级：** 当支柱之间发生冲突时，按以下顺序优先：  
1. 以准备驱动的生存，高于临场混乱反应  
2. 具有可读后果的确定性压力  
3. 贯穿 15 天弧线的策略取舍  
4. 系统可读性优先于场面奇观

### 核心玩法 Loop

`lastking` 的核心循环，是一个不断重复的昼夜生存循环：

**DayStart -> BuildAndRecruit -> NightPrepare -> NightCombat -> Settlement -> RewardChoice -> NextDay**

在白天，玩家需要稳定经济、消耗资源、放置或升级建筑、管理训练队列并做出成长决策。到了夜间准备阶段，系统会锁定即将到来的压力局面。夜战阶段中，玩家将看到先前规划在确定性波次与索敌规则下被验证的结果。随后进入结算，对损失与收益进行总结，再通过一次奖励选择稍微重塑下一天的规划空间。接下来压力更高的一天开始，循环往复直到 run 结束。

**循环示意：**  
`收入/自动存档 -> 建造/升级/训练 -> 锁定夜间状态 -> 抵御波次 -> 结算伤害/损失 -> 选择奖励 -> 开始下一天`

**循环时长：**  
- 一个宏循环：`白天 4 分钟 + 夜晚 2 分钟`  
- 一次完整 run 的目标：`Day 1 ~ Day 15`  
- 预期单局长度：`60-90 minutes`

**循环变化来源：**  
每一轮变化主要来自：
- 按天递增的压力与预算缩放
- 精英夜与 Boss 夜结构
- 资源债务与恢复状态
- 建造占地与队列后果
- 升级、维修与科技时机取舍
- 会改变次日规划空间的奖励选择
- M1 当前仍不完整的 UI 接线，这会影响玩家在规则已存在时能否清晰读懂状态

### 胜负条件

#### 胜利条件

- 存活到 `Day 15` 结束
- 让城堡一直存活到最终夜晚完全结算
- 在不触发唯一硬失败条件的前提下完成完整的昼夜循环

#### 失败条件

- `Castle HP` 归零
- 这是锁定版 PRD 中唯一真正会终结 run 的失败条件
- 债务、压力和节奏损失会提高失败风险，但它们本身不会单独构成额外失败状态

#### 失败后的复盘

- 失败后，玩家会看到战报，然后返回主菜单
- 失败的目的之一，是让玩家理解究竟是哪类规划决策出了问题：
  - 防线建得不够
  - 经济节奏过弱
  - 队列或升级顺序不合理
  - 无法承受后夜段压力
- 在当前 M1 状态下，这条学习闭环仍会受到 outcome、prompt 和 summary 界面不完整的限制

---

## Game Mechanics

### 主要机制

#### 1. 建造并摆放防御设施
- **玩家行为：** 在白天依据占地规则与路径规则摆放建筑。
- **使用时机：** 准备阶段中持续发生。
- **考验内容：** 规划能力、空间判断、路线优先级取舍、未来风险管理。
- **体验感受：** 审慎、强调格位感、后果沉重。
- **成长方式：** 随着玩家解锁更好的建筑、理解路线压力，并学会识别哪些摆放错误会连锁放大为夜间失败，这一机制会变得更有分量。
- **服务支柱：** 以准备驱动的生存，高于临场混乱反应；贯穿 15 天弧线的策略取舍。

#### 2. 管理经济与债务
- **玩家行为：** 在金币、铁以及债务规则约束下进行支出、保留、恢复与行动排序。
- **使用时机：** 白天持续发生，并间接影响整个 run。
- **考验内容：** 资源优先级、纪律性、行动顺序安排、风险承受度。
- **体验感受：** 紧绷、受限，并始终带有轻微压力。
- **成长方式：** 玩家的精通并不来自获得无限资源，而是来自学会何时花费、何时按住不动、何时接受短期虚弱。
- **服务支柱：** 具有可读后果的确定性压力；贯穿 15 天弧线的策略取舍。

#### 3. 排队、招募、升级与维修
- **玩家行为：** 在受限的时间窗口和成本窗口内管理训练队列、建筑升级与维修。
- **使用时机：** 白天高频发生，并穿插在压力上升节点之间。
- **考验内容：** 顺序安排、预判能力、机会成本判断。
- **体验感受：** 结构化、系统密集、对时机敏感。
- **成长方式：** 当更多并发需求争夺同一份经济与准备窗口时，复杂度会持续上升。
- **服务支柱：** 以准备驱动的生存，高于临场混乱反应；贯穿 15 天弧线的策略取舍。

#### 4. 读取并响应夜间压力
- **玩家行为：** 在夜间战斗中观察 HUD 状态、波次压力、路线升级、受伤情况与防线表现。
- **使用时机：** 夜晚全程持续发生。
- **考验内容：** 局势读取、预判能力，以及对后果的理解，而不是直接依赖高频手速操作。
- **体验感受：** 紧张、带有验证意味、受约束。
- **成长方式：** 夜晚会随着预算缩放、精英 / Boss 结构以及早先决策的叠加效应而变得更难。
- **服务支柱：** 具有可读后果的确定性压力；系统可读性优先于场面奇观。

#### 5. 选择奖励与成长方向
- **玩家行为：** 处理夜间奖励选择、引导 build 方向，并通过成长选项塑造后续规划。
- **使用时机：** 每晚一次，发生在有分量的决策节点。
- **考验内容：** 长线规划、适应能力、不确定性下的价值判断。
- **体验感受：** 有一定缓解感，但仍然是策略性的，而且从不脱离后果。
- **成长方式：** 随着 run 进入中后段，既有弱点或优势持续叠加，选择会越来越有分量。
- **服务支柱：** 贯穿 15 天弧线的策略取舍；以准备驱动的生存，高于临场混乱反应。

### 机制交互

这些机制并非彼此独立，而是构成了一条依赖链：

- 经济决定了建造、升级、训练和维修是否可行
- 建筑摆放会改变路线压力、目标结算和夜晚生存能力
- 队列与升级时机要和建造、维修争夺同一份资源窗口
- 夜晚结果会反馈到结算与奖励选择
- 奖励选择会影响次日的经济、防守优先级与成长方向
- 当前不完整的 M1 表现面确实削弱了可读性，但底层交互链条已经存在

从设计上说，当玩家能够清楚看见以下因果链时，这款游戏的优势才会真正成立：  
`资源决策 -> 准备结果 -> 夜间压力结果 -> 奖励/成长后果`

### 机制成长

机制的演化，更多来自系统压力的层层叠加，而不是动作连招复杂度：

- 前几天负责教会玩家基本的建造、招募与生存节律
- 中盘通过波次缩放、经济压力和更高价值的取舍，建立更强的压迫感
- 后盘检验玩家搭建的是一套稳健系统，还是脆弱的短期方案
- 精英夜和 Boss 夜会成为对早先选择的关键验证点
- 在当前 M1 阶段，成长理解受限更多来自运行时表现面不完整，而不是系统深度缺失

---

## 控制与输入

### 控制方案 (Windows PC via Steam)

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
- **鼠标右键：** 取消 / 取消选择 / 退出摆放模式
- **键盘移动键组：** 镜头平移或地图导航
- **数字行 / 快捷键：** 高频建造或控制快捷操作
- **Space / dedicated key:** pause toggle
- **专用速度键：** 在 `1x` 与 `2x` 之间切换
- **Esc:** open settings / pause context

在 PRD 已明确要求支持键位重绑定的前提下，这些输入应保持可重绑定。

### Input Feel

控制手感应当具备以下特征：
- 精确，而不是花哨
- 可读，而不是过载
- 对重复的白天规划操作保持低摩擦
- 对菜单、面板和高频摆放操作保持可靠
- 在长时段会话中保持稳定，因为一局 run 预期会持续 `60-90 minutes`

游戏不应表现得像一款要求持续高频微操的动作 RTS。输入设计应优先服务于规划清晰度。

### 无障碍控制

规划中的无障碍与易用性控制，应与现有 PRD 约束保持一致：

- 按键重绑定
- 鼠标灵敏度调节
- UI 缩放预设：`80 / 100 / 125 / 150`
- 对局内语言切换：`zh-CN / en-US`
- 持久化设置记忆
- 面向低配稳定性的性能预设

当前仍需明确保留的限制：
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

### 经济与资源

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

### 能力与主动技能

`lastking` 当前不应被定义为重主动技能塔防。它更偏向“系统前置决策驱动”的防守游戏。

**当前主动技能立场：**
- 以自动作战和前置准备为主
- 夜晚不是高频技能手操主导
- 玩家更像指挥与规划者，而不是持续施法者

**当前承担“powers”角色的内容：**
- 奖励选择带来的 run 向修正
- 升级与科技带来的数值/结构强化
- 时间控制（`pause / 1x / 2x`）带来的节奏掌控
- 局部交互与防线管理，而非大招轰炸型主动技能体系

因此，在当前 GDD 阶段，能力系统应定义为**弱主动、强准备、强成长反馈**。如果未来加入更强主动能力，也必须服从“不破坏白天准备 / 夜晚验证”这一核心柱子。

### 难度与重玩性

本作的可重玩性主要来自确定性规则下的多难度、多决策路径和多 run 对比，而不是完全随机化。

**Difficulty structure:**
- 难度系统为 `config-driven`
- 至少 `10` 档
- 开局锁定，对局中不可切换
- 解锁链按配置逐级开放，不允许跨级跳过

**重玩驱动因素：**
- 不同难度下预算倍率与资源倍率变化
- 不同建造、训练、升级、科技路线的长期后果
- 精英 / Boss 夜作为不同 run 的关键验证点
- 奖励选择对中后期结构的重塑
- `battle report`、压力评分以及配置治理可审计性带来的复盘价值

**这不是什么：**
- 不是高度随机 roguelike
- 不是以 endless-first 为导向的纯分数追逐
- 不是靠频繁新技能轮换制造重复可玩性

它的 replayability 更接近于：在严格规则下，持续优化一套你自认为正确、但每次都会在更高压力下接受审判的防守方案。

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

### 难度曲线

`lastking` 的难度曲线更接近**分段锯齿式抬升并整体持续上扬**，而不是平滑的线性增长。

#### 挑战缩放

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

### 经济与资源

本作存在明确且非常核心的 economy/resource system，因此这一节必须保留。

`lastking` 的经济不是辅助系统，而是核心 gameplay pressure source 之一。它决定玩家白天能做什么，也决定夜晚是否会因准备不足而崩盘。

#### 资源项

- **金币（Gold）**
  - 核心通用支出资源
  - 参与建造、升级、训练、修理等关键行为
  - 允许为负，直接形成 debt gameplay

- **Iron Ore**
  - 核心建设/成长资源之一
  - 与 gold 一起构成主资源骨架

- **人口 / 科技点（Population / Tech Points）**
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

### 结构类型

`lastking` 最适合的结构不是传统线性关卡列表，也不是开放世界，而是一个**单局战役式结构**：

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

### 关卡推进

本作的内容推进方式是**单局内部的严格线性战役推进**。

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

### 关卡设计原则

建议为本作明确以下关卡设计原则：

- **通过真实压力教会玩家循环**  
  教学应嵌在真实系统里，而不是用脱离主循环的假场景说明。

- **每个白天都应为夜晚准备一个不同的问题**  
  每个白天不仅是经营窗口，还应让玩家面对不同的夜晚准备问题。

- **压力升级应来自系统，而不是突发 gimmick**  
  难度提升应主要来自预算、敌军构成、经济紧张和路径压力，而不是无预警的新 gimmick。

- **特殊夜晚必须验证先前决策，而不是取代它们**  
  精英夜和首领夜应该检验此前构筑，而不是突然改成另一种玩法。

- **空间可读性与机制正确性同样重要**  
  如果玩家看不懂 choke point、路径后果、火力覆盖和损失来源，关卡设计就还没完成。

---

## 美术与音频方向

### 美术风格

`lastking` 最适合的视觉方向不是高写实，也不是超轻量抽象，而是**强调可读性的风格化策略表现**。它的首要目标不是炫技，而是帮助玩家在长时段单局中快速读懂路径、防线、压力、损伤以及层级关系。

建议的整体风格方向是：

- **Stylized top-down / strategy-readable presentation**
- 强调战场信息层级、建筑轮廓、敌军类别差异和压力可读性
- 美术服务于“白天规划 / 夜晚验证”的主循环，而不是喧宾夺主

这意味着视觉上应优先保证：

- 建筑类型一眼可区分
- 友军、敌军、防御建筑、城堡、城门、阻挡结构层级清晰
- 路径、 choke point、火力覆盖和损伤结果可被快速识别
- 夜晚压力升高时画面仍然不乱

#### 视觉参考

虽然当前仓内没有正式的美术方向文档定稿，但从玩法需求推导，适合参考的不是“纯叙事氛围型”作品，而是以下参考方向的组合：

- **They Are Billions / Kingdom 风格的压力可读性**
  - 用于参考持续防守压力和战场读面优先级

- **Northgard / RTS-lite 功能清晰度**
  - 用于参考建筑、单位、资源信息的清晰组织

- **黑暗奇幻塔防框架**
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

因此更适合的并不是高密度旋律驱动，而是**氛围化的策略紧张感 + 可读的战斗反馈**。

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

- **当前阶段不做完整配音**
- 如需角色存在感，最多采用极轻量提示、简短 UI 语音或非语言化短反馈
- 核心信息传达仍应依赖清晰文本和系统反馈，而不是 VO

这更符合当前项目范围和 M1 的真实成熟度。

### Aesthetic Goals

本作的 art/audio 必须服务于此前已经定义的 pillars，而不是自成体系。

- **以准备驱动的生存，高于临场混乱反应**
  - 视觉和音频都应帮助玩家更好地做准备，而不是制造无意义噪音

- **具有可读后果的确定性压力**
  - 画面与声音都应让玩家明白“发生了什么、为什么发生、哪里出问题”

- **贯穿 15 天弧线的策略取舍**
  - 白天与夜晚的气质差异要足够明显，让 run 的结构性被感知到

- **系统可读性优先于场面奇观**
  - 美术和音频都必须优先支持清晰度
  - 如果特效、镜头、混音破坏读面，它们就违背了本作核心设计

---

## 技术规格

### 性能要求

`lastking` 的技术要求首先服从其平台锁定和长时段 run 结构：它是一款 `Windows only`、`Steam single-player`、单局 `60-90` 分钟的策略防守游戏。因此性能目标不能只看“能不能跑”，而必须看“在长时间局内压力和持续 UI 读面下是否稳定”。

#### 帧率目标

- 主要目标：`平均 60 FPS`
- 下限硬门：`1% Low >= 45 FPS`
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

#### 运行时与 UX 要求

- fullscreen 为默认显示模式
- key rebinding 必须支持
- mouse sensitivity 必须支持
- in-match language switching 支持 `zh-CN / en-US`
- time controls 必须支持：`pause / 1x / 2x`
- pause 语义必须是真正的 full freeze，而不是只停表面动画
- low-spec stability 需要 performance preset 支撑

#### Save / Migration / Integrity Requirements

- 存档策略：仅保留 `single autosave`
- no manual overwrite path
- 存档迁移：`forced migration with pre-backup`
- migration failure 时必须拒绝加载并建议删除存档
- unofficial / dirty save 与 cloud / achievement 行为必须遵守 PRD 锁定策略
- 配置不匹配 / 读取失败必须阻止开局，而不是静默回退到不安全运行时

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
| 2 | 核心生存循环与 HUD 可读性 | Day/Night 状态机、城堡 HP、胜负、基础 HUD、运行时结果与提示闭环 | 1 | 8-10 |
| 3 | Combat Pressure and Defensive Validation | 波次预算、刷怪、目标优先级、路径失败回退、战斗验证与夜晚压力表达 | 1, 2 | 8-10 |
| 4 | Economy, Building, and Progression Systems | 资源、建造、升级、修理、训练、科技、奖励与成长压力 | 2, 3 | 10-12 |
| 5 | Meta Systems, Save, and Platform Services | 存档、云存档、设置、本地化、音频、成就、性能门与平台整合 | 1, 2 | 8-10 |
| 6 | UI 接线、可审计性与生产收口 | Chapter 7 六大 UI slice 收口、配置审计、迁移状态、战报读面、最终体验闭环 | 2, 3, 4, 5 | 10-12 |

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

### 垂直切片

**第一个可玩里程碑：**  
玩家可以从主菜单进入游戏，完成 `housing -> barracks -> survive first night` 的最小 onboarding，看到基础 HUD、经历白天准备与第一晚防守，并在结果后继续进入下一天。这是最小但真实的 vertical slice，而不是假演示。

---

## 成功指标

### 技术指标

本作的技术成功标准不只是“能运行”，而是“在长时间单局、持续读面、系统压力累积的前提下依然稳定、清晰、可验证”。

#### 关键技术 KPI

| Metric | Target | Measurement Method |
| --- | --- | --- |
| 平均帧率 | `>= 60 FPS` | 在目标基线场景和代表性长局采样上运行性能门禁 |
| 1% Low 帧率 | `>= 45 FPS` | 自动化性能门禁 + 可复现场景采样 |
| 逻辑 P95 | `<= 16.6ms` | 来自代表性 run 的逻辑埋点计时 |
| 逻辑 P99 | `<= 22.0ms` | 来自代表性 run 的逻辑埋点计时 |
| 冷启动时间 | `<= 3.0s` | 在目标基线硬件上测量启动到菜单耗时 |
| 存读档完整性 | `0 blocker-level corruption cases in gated validation` | 存档迁移、自动存档和恢复测试套件 |
| 配置校验安全性 | `0 unsafe match starts after config mismatch/load failure` | 配置校验门禁、审计日志和负路径测试 |
| i18n HUD 硬门 | `0 critical truncation / overlap in zh-CN and en-US` | 截图差异对比 + 人工抽查 |
| 构建稳定性 | `0 release-blocking bootstrap/export failures on governed baseline` | Windows 导出 / 启动验证流水线 |

#### Technical Success Interpretation

技术上成功意味着：
- 不只是 benchmark 好看
- 而是启动、run、存档、配置、i18n、性能全部在一个真实 vertical slice 内能同时成立
- 尤其不能出现“底层系统是对的，但玩家面读不懂”的伪成功

### 玩法指标

本作的 gameplay metric 必须围绕此前定义的 pillars 和 goals，而不是泛泛看留存。

#### 关键玩法 KPI

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

### 指标复盘节奏

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

## 范围外内容

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

## 假设与依赖

### 关键假设

- 玩家平台为 `Windows PC + Steam`，并接受键鼠为唯一首发输入方式。
- 玩家愿意进行 `60-90` 分钟单局体验，而不是碎片化短会话玩法。
- 当前产品价值主要来自“白天规划 / 夜晚验证”的系统闭环，而不是重叙事或高动作密度。
- 配置治理、schema、migration、audit 这类底层治理能力会继续保留为正式产品约束，而不是开发期临时手段。
- 难度梯度通过配置和 seeded validation 可以持续校准，不需要在 v1 引入动态 rubber band 机制。
- 当前 M1 的主要短板在玩家面收口与可读性，而不是核心规则缺失。

### 外部依赖

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

## 文档信息rmation

**Document:** lastking - Game Design Document  
**Version:** 1.0  
**Created:** 2026-04-27  
**作者：** skyo  
**Status:** Complete

### 变更记录

| Version | Date | Changes |
| --- | --- | --- |
| 1.0 | 2026-04-27 | Initial GDD complete |
