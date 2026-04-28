---
Title: BMAD Epic To Task Alignment Audit
Status: Draft
Owner: codex
Last Updated: 2026-04-27
Encoding: UTF-8
Applies-To:
  - _bmad-output/gdd.md
  - _bmad-output/epics.md
  - .taskmaster/tasks/tasks.json
  - docs/gdd/ui-gdd-flow.md
  - docs/gdd/t1-t46-m1-wiring-audit.md
---

# BMAD Epic To Task Alignment Audit

## 1. Purpose

本文件用于回答一个具体问题：在当前 `lastking` 仓库现实下，基于 `_bmad-output/gdd.md` 与 `_bmad-output/epics.md`，下一步是否应该直接向 `.taskmaster/tasks/tasks.json` 追加新任务。

结论先行：

- 当前不建议直接追加一批新任务。
- 当前更合理的动作是先把 `BMAD epic -> 现有任务 -> 当前 UI/运行时接线状态` 的映射关系固定下来。
- 从当前证据看，现有 `T1-T46` 已经覆盖了 BMAD epics 的主骨架，真正的缺口主要集中在 `T41-T46` 的 UI wiring 收口与状态回写，而不是 backlog 骨架缺失。

## 2. Inputs

本审计基于以下仓内来源：

- BMAD 设计产物：`_bmad-output/gdd.md`, `_bmad-output/epics.md`
- 任务事实来源：`.taskmaster/tasks/tasks.json`
- UI 路由规划：`docs/gdd/ui-gdd-flow.md`
- M1 接线审计：`docs/gdd/t1-t46-m1-wiring-audit.md`

当前任务状态快照：

- `done = 40`
- `pending = 6`
- `pending` 全部为 `T41-T46`

## 3. Alignment Summary

| BMAD Epic | Epic Goal | Current Task Mapping | Coverage Judgment | Primary Gap |
| --- | --- | --- | --- | --- |
| Epic 1 | Foundation, Boot, and Runtime Baseline | `T01`, `T11`, `T21`, `T41` | Covered | Boot flow 已有基础 surface，但 `BootStatusPanel / ContinueGateDialog` 仍待稳定化 |
| Epic 2 | Core Survival Loop and HUD Readability | `T03`, `T07`, `T08`, `T09`, `T10`, `T18`, `T19`, `T23`, `T24`, `T42` | Covered | 主循环逻辑强于 outcome/prompt/speed UI 的最终 owned surface |
| Epic 3 | Combat Pressure and Defensive Validation | `T04`, `T05`, `T06`, `T20`, `T22`, `T43` | Covered | combat UI shell 与 camera/pressure surface 仍未完全固化 |
| Epic 4 | Economy, Building, and Progression Systems | `T12`, `T13`, `T14`, `T15`, `T16`, `T17`, `T44` | Covered | economy/build/progression 面板尚未统一为稳定玩家面 |
| Epic 5 | Meta Systems, Save, and Platform Services | `T25`, `T26`, `T27`, `T28`, `T29`, `T30`, `T45` | Covered | settings 较完整，但 save/cloud/achievement/perf surface 仍分散 |
| Epic 6 | UI Wiring, Auditability, and Production Closure | `T02`, `T31-T40`, `T41-T46` | Covered | config audit / migration / report metadata 更偏治理证据，玩家或操作者读面仍需收口 |

## 4. Interpretation

### 4.1 Why this is not a "missing backlog" problem

从结构上看，`_bmad-output/epics.md` 的 6 个 Epic 已经与当前仓库的任务分组高度对齐：

- Epic 1 对应启动、基线与入口；
- Epic 2 对应昼夜主循环、HUD、提示、结果；
- Epic 3 对应战斗压力、波次、AI、相机交互；
- Epic 4 对应资源、建造、训练、升级、科技；
- Epic 5 对应存档、云存档、本地化、音频、性能、平台；
- Epic 6 对应 UI wiring、config audit、migration、battle report metadata 和最终 M1 收口。

这说明当前问题不是“BMAD 设计完了，但 tasks.json 还没有对应 backlog”，而是“backlog 主骨架已经在，但玩家面/操作者面还没有完全闭环”。

### 4.2 Why this is primarily a Chapter 7 closure problem

根据 `docs/gdd/t1-t46-m1-wiring-audit.md`：

- 很多能力已经在 `Game.Core`、`Game.Godot`、`Contracts`、`Validators`、`Audit Readers` 中落地；
- 但玩家真正看得到、能操作、能理解的 `surface owner` 还大量停留在 partial 状态；
- `T41-T46` 正是为六个 UI slice 设立的收口任务，但它们当前仍然是 `pending`。

因此，当前最接近真实价值交付的下一步，不是再建一轮抽象新任务，而是先把：

- 哪些 UI surface 已经是 runtime；
- 哪些仍是 partial / docs-only / test-only；
- `T41-T46` 应以什么证据条件回写为 `done`；

这些判断固定住，然后让 Chapter 7 顶层编排器去消费这些事实。

## 5. Task Addition Decision

### Decision

`No immediate bulk task addition.`

### Rationale

1. `T1-T46` 已经覆盖了 BMAD epics 的核心骨架。
2. 当前最明显未收口区域，已经被 `T41-T46` 直接命名和承接。
3. 大量缺口属于 UI wiring、owned surface、evidence visibility，而不是功能主题缺失。
4. 如果此时再新增任务，极易和 `T41-T46` 或 `T02`、`T31-T40` 发生重复定义。

## 6. What may still justify new tasks later

后续仍有可能新增少量任务，但前提应非常严格。只有在出现以下情形时，才建议追加新任务：

1. `T41-T46` 全部完成后，仍有一整个 BMAD story 没有任何任务承接。
2. 某项能力既不属于现有 slice，也无法通过 `T41-T46` 的 surface 收口解决。
3. 缺口不是“面板未接线”，而是“底层功能本体根本不存在”。
4. 缺口不能通过 acceptance、overlay、evidence path、state write-back 修复，只能通过新增 backlog ownership 解决。

在当前仓库证据下，还没有看到必须立即新增大批任务的信号。

## 7. Recommended Next Step In This Repository

按当前仓库现实，建议顺序如下：

1. 继续把 Chapter 7 视为主战场，而不是回到 BMAD 再扩 backlog。
2. 让 Chapter 7 顶层编排器显式消费：
   - `docs/gdd/ui-gdd-flow.md`
   - `docs/gdd/t1-t46-m1-wiring-audit.md`
   - `T41-T46` 当前状态
3. 为每个 UI slice 给出清晰的 `done write-back condition`。
4. 先判断 `T41-T46` 是否应回写为 `done`、`partial closure` 或继续保留 `pending`。
5. 只有在 `T41-T46` 收口后仍存在明确 story orphan，再考虑追加 `T47+`。

## 8. Practical Recommendation

如果现在就要做一个仓内动作，推荐优先级是：

1. 让 Chapter 7 顶层编排器输出每个 slice 的 `surface status`、`evidence status`、`write-back recommendation`。
2. 基于该结果更新 `T41-T46` 的状态，而不是先扩 `tasks.json`。
3. 如仍有 orphan story，再最小化新增 `0-3` 个 gap tasks，而不是整批扩展。

## 9. Final Judgment

基于 `_bmad-output/gdd.md`、`_bmad-output/epics.md`、`.taskmaster/tasks/tasks.json`、`docs/gdd/ui-gdd-flow.md` 和 `docs/gdd/t1-t46-m1-wiring-audit.md` 的当前一致性判断：

- 现有任务体系已经足以承载 BMAD GDD 的 M1 主体。
- 当前瓶颈不在 backlog 数量，而在 Chapter 7 对六个 UI slices 的最终接线与证据回写。
- 因此下一步更推荐：`先收口 T41-T46，再决定是否需要 T47+`。
