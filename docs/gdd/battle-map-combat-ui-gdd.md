---
GDD-ID: GDD-LASTKING-BATTLEMAP-COMBAT-UI-V1
Title: Lastking Battle Map, Combat Feedback, and Outcome UI GDD
Status: Draft
Owner: codex
Last Updated: 2026-05-03
Encoding: UTF-8
Applies-To:
  - Game.Godot/Scenes/Screens/BattleMapScreen.tscn
  - Game.Godot/Scripts/Screens/BattleMapScreen.gd
  - Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs
  - Game.Godot/Localization/en-US.json
  - Game.Godot/Localization/zh-CN.json
  - Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd
  - Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd
---

# Lastking Battle Map, Combat Feedback, and Outcome UI GDD

## 1. 目标

把 `BattleMapScreen` 从“可运行的战斗切片”收敛成三层清晰 UI：

1. Battle Map UI
2. Combat Feedback UI
3. Outcome UI

目标不是增加新战斗规则，而是让现有运行时状态可读、可操作、可复盘。

## 2. 当前现状

### 2.1 已有能力

- 场景文件已存在：`Game.Godot/Scenes/Screens/BattleMapScreen.tscn`
- 场景脚本已存在：`Game.Godot/Scripts/Screens/BattleMapScreen.gd`
- 战斗桥接已存在：`Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`
- 中英文本地化已存在：`Game.Godot/Localization/en-US.json` 和 `Game.Godot/Localization/zh-CN.json`
- 有运行时测试已覆盖主流程和桥接层

### 2.2 当前呈现方式

- 地图底图是 `Background` + `Line2D Path`
- 阵营与地形骨架已静态摆放：
  - `PlayerCastle`
  - `EnemySpawnB`
- 按钮已存在并能触发流程：
  - `BuildBtn`
  - `WaveBtn`
  - `AutoWaveBtn`
  - `ExchangeBtn`
  - `CleanupBtn`
  - `FinishBtn`
  - `BackBtn`
- 文本反馈主要集中在：
  - `Status`
  - `Summary`
  - `Legend`
  - `MetricsHelp`
- 敌军 token 已在 `_process()` 中按 `GetActorSnapshots()` 生成并沿路径移动
- `WaveTimer` 已支持自动刷波

### 2.3 现有反馈问题

- Battle Map UI 有骨架，但还不是“可读地图层”
- Combat Feedback UI 现在主要只是 `Summary` 文本，没有独立信息层
- Outcome UI 目前只是在 `Status` 和 `Summary` 中收束结果，没有明确胜负、奖励、日志证据与后续行动入口
- 当前 `PublishOutcomePhase()` 只发布事件，不提供专门的 outcome surface

## 3. 三层 UI 定义

### 3.1 Battle Map UI

职责：

- 展示地图布局
- 展示阵营图例
- 展示建筑槽位
- 展示敌军刷新点
- 展示单位路径与移动方向
- 展示按钮状态与即时反馈

现有实现：

- 路径线已显示
- 建筑槽位与出生点已显示
- 传奇说明已显示
- 按钮可触发流程

缺口：

- 缺少明确的建筑/路径/刷点视觉层级
- 缺少按钮反馈的独立状态区
- 缺少“单位路径阅读”的辅助标注

### 3.2 Combat Feedback UI

职责：

- 展示友军数量
- 展示敌军数量
- 展示活跃节点
- 展示交火次数
- 展示死亡退场数
- 展示城堡生命

现有实现：

- `Summary` 已经输出：
  - `Castle HP`
  - `Friendly Units`
  - `Enemy Units Spawned`
  - `Combat Exchanges`
  - `Dead Units Retired`
  - `Active Combat Nodes`
- `MetricsHelp` 已解释两个关键指标
- `GetSummary()` 已提供这些字段

缺口：

- 缺少单独的压力面板
- 缺少对“当前战斗是否继续/是否受阻”的即时可读提示
- 缺少对数值变化的短促反馈，如击杀、掉血、退场

### 3.3 Outcome UI

职责：

- 展示胜负
- 展示奖励
- 展示继续/返回
- 展示日志证据

现有实现：

- `PublishOutcomePhase()` 已发布 outcome 相关事件
- `BackBtn` 已提供返回菜单入口
- `Status` 会显示 “Battle finished. Outcome published.”

缺口：

- 没有明确的胜/负分离面板
- 没有奖励展示层
- 没有日志证据摘要
- 没有“继续 / 返回”双路径的结果动作面板

## 4. 设计目标

1. 地图层只负责“看懂战场”
2. 反馈层只负责“看懂战斗状态”
3. 结果层只负责“看懂结算”
4. 所有文本必须可中英切换
5. 结果必须能回链到测试与事件证据

## 5. 页面结构建议

### 5.1 Battle Map UI

- `MapCanvas`
- `LegendPanel`
- `PathOverlay`
- `SpawnMarkerLayer`
- `BuildSlotLayer`
- `ControlBar`

### 5.2 Combat Feedback UI

- `PressurePanel`
- `CombatMetricRow`
- `ActionToastLayer`
- `RuntimeStatusLine`

### 5.3 Outcome UI

- `OutcomeHeader`
- `OutcomeStatePanel`
- `RewardPanel`
- `EvidencePanel`
- `OutcomeActionBar`

## 6. 交互规则

- `BuildBtn` 只能表达准备态，不直接伪装成结算态
- `WaveBtn` 只负责刷新敌军
- `ExchangeBtn` 只负责交火反馈
- `CleanupBtn` 只负责退场回收
- `FinishBtn` 只负责结算输出
- `BackBtn` 只负责离开战斗页

## 7. 文案规则

- 地图层文案短
- 反馈层文案清楚
- 结果层文案必须包含胜负、原因、奖励、下一步
- 错误与阻断文案必须告诉玩家下一步怎么做

## 8. 当前实现映射

| 需求 | 当前实现 | 状态 |
| --- | --- | --- |
| 地图布局 | `Background` + `Path` + 固定锚点 | 已有 |
| 阵营图例 | `Legend` 文本 | 已有 |
| 建筑槽位 | 严格 `48x48` 网格槽位 | 已有 |
| 敌军刷新点 | `EnemySpawnB` 右侧整列出生带 | 已有 |
| 单位路径 | `Line2D` + `GetActorSnapshots()` token | 部分已有 |
| 按钮反馈 | `Status` + 按钮文本 | 部分已有 |
| 友军/敌军数量 | `Summary` 文本 | 已有 |
| 活跃节点 | `Summary` 文本 | 已有 |
| 交火次数 | `Summary` 文本 | 已有 |
| 死亡退场 | `Summary` 文本 | 已有 |
| 城堡生命 | `Summary` 文本 | 已有 |
| 胜负 | `PublishOutcomePhase()` 事件 | 部分已有 |
| 奖励 | 未单独渲染 | 缺失 |
| 继续/返回 | `BackBtn` 仅返回 | 部分已有 |
| 日志证据 | 事件已发布，未展示 | 缺失 |

## 9. 验收标准

- 玩家能一眼看懂地图结构和刷怪方向
- 玩家能在战斗中实时看到关键压力指标
- 玩家能在结算时看懂胜负、奖励和证据
- UI 文案支持 `zh-CN` / `en-US`
- `BattleMapScreen` 的测试仍能通过

## 10. 下一步建议

1. 把 `Summary` 拆成独立 `PressurePanel`
2. 新增 `OutcomePanel`
3. 把日志证据从事件层提炼成可见文本
4. 再补一轮 GdUnit4 场景测试

