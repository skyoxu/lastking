---
PRD-ID: PRD-lastking-T2
Title: PRD-lastking-T2 功能纵切验收清单（执行版）
Status: Accepted
ADR-Refs:
  - ADR-0001
  - ADR-0005
  - ADR-0011
Test-Refs:
  - Game.Core.Tests/Tasks/Task1BaselineVerificationGateTests.cs
  - Game.Core.Tests/Tasks/Task1ProjectRootUniquenessTests.cs
  - Tests.Godot/tests/Integration/test_project_bootstrap_editor_compile_run.gd
  - Tests.Godot/tests/Integration/test_windows_export_startup_flow.gd
---

# PRD-lastking-T2 功能纵切验收清单（执行版）

## 一、文档完整性验收

| Check ID | Pass Criterion | Fail Condition | Evidence |
| --- | --- | --- | --- |
| AC-001 | `tasks.json` all tasks have valid `overlay` path | missing or invalid overlay path | `.taskmaster/tasks/tasks.json` |
| AC-002 | `tasks_back.json` and `tasks_gameplay.json` all tasks pass link checks | any missing ADR/CH/overlay refs | `py -3 scripts/python/check_tasks_all_refs.py` output |
| AC-003 | `validate_task_master_triplet.py` ends with `Overall result: OK` | mapping/dependency/layer errors | `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json` |
| AC-004 | `validate_overlay_execution.py` returns exit code 0 | missing sections/front-matter/paths | `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json` |
| AC-005 | archived overlay guard passes under CI strict mode | archived path leakage or retired active references | `py -3 scripts/python/guard_archived_overlays.py --strict-git` |

## 二、架构设计验收

- Overlay 页面必须回链到已接受 ADR，并与 `tasks.json` / `tasks_back.json` / `tasks_gameplay.json` 的引用一致。
- Taskmaster 主任务与视图任务必须保持 `taskmaster_id` 映射闭环，不允许孤立条目。
- Task 级实现上下文必须包含 `adrRefs` 与 `archRefs`，防止实现脱离架构基线。

## 三、代码实现验收

- Task 1 仅允许在单一 canonical root 下完成编辑器启动、C# 编译、主场景运行与 Windows 导出基线验证。
- 任一验收检查失败必须 fail fast，不允许降级为 warning。
- 证据工件命名与字段结构必须可复现、可审计。

## 四、测试框架验收

### Required Commands (Windows)

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`
- `py -3 scripts/python/check_tasks_back_references.py`
- `py -3 scripts/python/check_tasks_all_refs.py`
- `py -3 scripts/python/validate_task_master_triplet.py`
- `py -3 scripts/python/guard_archived_overlays.py --strict-git`

### DoD Anchors

- 文档层：Overlay 页面结构、Front-Matter、引用路径齐全。
- 任务层：Taskmaster 三件套回链一致、无漂移。
- 测试层：Test-Refs 指向现存测试，且有日志工件可追溯。
- 门禁层：CI hard gate 覆盖 archived guard + overlay lint + task link checks。
