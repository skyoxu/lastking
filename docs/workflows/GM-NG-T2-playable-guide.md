# Lastking T2 Playable 场景落地指南

> 本文档是 `lastking` 当前仓库的可执行流程说明：如何从 triplet 任务出发，落地首个可玩 T2 闭环，并保持 PRD/Overlay/测试/日志回链一致。

## 1. 先锁定 PRD 与 Overlay

- 主 PRD 输入：`.taskmaster/docs/prd.txt`
- 纵切索引：`docs/architecture/overlays/PRD-lastking-T2/08/_index.md`
- 核心流程页：`docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md`
- 验收清单：`docs/architecture/overlays/PRD-lastking-T2/08/ACCEPTANCE_CHECKLIST.md`

要求：

- 先确认“昼夜循环 + 防守压力 + 建造/训练 + 胜负判定”在上述页面有明确描述。
- Overlay 只写特性切片，不复制 Base/ADR 阈值。

## 2. 任务推进最小闭环（建议顺序）

优先用 taskmaster 主任务编号推进，先基建后玩法：

1. 任务治理与回链完整（triplet、overlay refs、acceptance refs）
2. 运行时核心循环（Day/Night 状态机、波次/预算、失败条件）
3. 交互与可玩入口（镜头、HUD、速度切换、反馈）
4. 存档与可恢复（自动存档、迁移、拒绝策略）
5. 可观测与门禁（日志、perf、hard checks、review pipeline）

每一步都要保持：

- `tasks.json.master.tasks[].id`
- `tasks_back.json[].taskmaster_id`
- `tasks_gameplay.json[].taskmaster_id`

三者映射一致。

## 3. 任务字段最小要求

每个进入 `in-progress` 的任务至少补齐：

- `owner`
- `labels`
- `layer`
- `adr_refs`
- `chapter_refs`
- `overlay_refs`
- `test_refs`
- `acceptance`

执行校验：

```bash
py -3 scripts/python/task_links_validate.py
py -3 scripts/python/check_tasks_all_refs.py
py -3 scripts/python/validate_task_master_triplet.py
```

## 4. TDD 执行节奏（Core -> Scene -> Pipeline）

1) 先测试（红灯）  
2) 最小实现（绿灯）  
3) Refactor 门禁  
4) Review pipeline 固化证据

常用命令（Windows）：

```bash
py -3 scripts/sc/build.py tdd --task-id <task-id> --stage green
py -3 scripts/sc/build.py tdd --task-id <task-id> --stage refactor
py -3 scripts/sc/run_review_pipeline.py --task-id <task-id> --godot-bin "<godot-bin>"
```

## 5. 首个可玩闭环验收模板

可直接复用到任务 `acceptance`：

- 能进入主场景并开始一局；
- 完成至少 1 次白天 -> 夜晚 -> 次日切换；
- 夜晚波次按配置生成；
- 城堡血量归零触发失败；
- 自动存档按天触发；
- 至少 1 条 Core 测试 + 1 条 Godot 场景测试通过；
- 产物写入 `logs/ci/<YYYY-MM-DD>/` 或 `logs/e2e/<YYYY-MM-DD>/`。

## 6. 止损规则

- 不在同一次改动里同时重写 PRD、Overlay、Tasks、脚本门禁四个层面。
- 先修回链，再补语义；先保可执行，再做扩展。
- 任何路径/命名调整后，立即跑一次 triplet baseline 校验。

## 7. 结论

该指南只服务 `lastking` 当前仓库结构。  
若后续 PRD-ID 或 overlay 结构发生变化，先更新本文档中的路径，再继续任务推进。
