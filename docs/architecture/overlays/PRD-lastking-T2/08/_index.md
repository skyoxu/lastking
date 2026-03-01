---
PRD-ID: PRD-lastking-T2
Title: 08 章功能纵切索引（Lastking T2 Execution Overlay）
Arch-Refs: [CH01, CH02, CH03]
ADR-Refs: [ADR-0011, ADR-0018, ADR-0021, ADR-0022, ADR-0031, ADR-0032, ADR-0033]
Updated: true
---

本目录是 Lastking 当前唯一有效的 T2 功能纵切入口，目标是让任务回链、测试回链、工件回链三线闭环。

## Scope

- 只覆盖 Lastking T2 核心循环的功能纵切，不在此复制 Base 的跨切面阈值文本。
- 所有实现约束以引用方式连接到 Base/ADR/Contracts/Test-Refs。
- 所有执行证据统一落盘到 `logs/**`，用于 CI 与人工审计。

## Canonical Pages

- 功能纵切主文档：`docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md`
- 契约索引：`docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md`
- 测试索引：`docs/architecture/overlays/PRD-lastking-T2/08/08-Testing-T2.md`
- 可观测性索引：`docs/architecture/overlays/PRD-lastking-T2/08/08-Observability-T2.md`
- 验收清单：`docs/architecture/overlays/PRD-lastking-T2/08/ACCEPTANCE_CHECKLIST.md`

## Execution Invariants

- 任一 Taskmaster 任务必须能回链到本目录中的至少一个页面。
- `08-Feature-Slice-T2-Core-Loop.md`、`08-Contracts-T2.md`、`08-Testing-T2.md` 的 `Test-Refs` 至少要有 1 条现存测试文件路径。
- `08-Observability-T2.md` 的 `Test-Refs` 允许使用日志工件路径作为证据引用。
- 任一执行步骤必须产生可定位的日志工件路径。
- 任务映射粒度至少覆盖三段执行面：`11-20`（经济/建造/战斗）、`21-30`（运行时 UX/存档/平台）、`31-40`（配置契约治理）。
- 若任务语义不在上述三段内可定位到对应条目，视为 Overlay 回链语义不足，需先补文档再进入实现。

## Validation Commands (Windows)

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`
- `py -3 scripts/python/check_tasks_all_refs.py`
- `py -3 scripts/python/validate_task_master_triplet.py`
- `py -3 scripts/python/guard_archived_overlays.py --strict-git`
