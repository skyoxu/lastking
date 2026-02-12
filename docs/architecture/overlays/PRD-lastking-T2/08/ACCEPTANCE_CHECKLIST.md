# PRD-lastking-T2 功能纵切验收清单（执行版）

## Quantified Pass/Fail Criteria

| Check ID | Pass Criterion | Fail Condition | Evidence |
| --- | --- | --- | --- |
| AC-001 | `tasks.json` 40/40 tasks have valid `overlay` path | missing or invalid overlay path | `.taskmaster/tasks/tasks.json` |
| AC-002 | `tasks_back.json` and `tasks_gameplay.json` 40/40 pass link checks | any missing ADR/CH/overlay refs | `py -3 scripts/python/check_tasks_all_refs.py` output |
| AC-003 | `validate_task_master_triplet.py` ends with `Overall result: OK` | mapping/dependency/layer errors | `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json` |
| AC-004 | `validate_overlay_execution.py` returns exit code 0 | missing sections/front-matter/paths | `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json` |
| AC-005 | archived overlay guard passes under CI strict mode | archived path leakage or retired active references | `py -3 scripts/python/guard_archived_overlays.py --strict-git` |

## Required Commands (Windows)

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`
- `py -3 scripts/python/check_tasks_back_references.py`
- `py -3 scripts/python/check_tasks_all_refs.py`
- `py -3 scripts/python/validate_task_master_triplet.py`
- `py -3 scripts/python/guard_archived_overlays.py --strict-git`

## DoD Anchors

- 文档层：Overlay 页面结构、Front-Matter、引用路径齐全。
- 任务层：Taskmaster 三件套回链一致、无漂移。
- 测试层：Test-Refs 指向现存测试，且有日志工件可追溯。
- 门禁层：CI hard gate 覆盖 archived guard + overlay lint + task link checks。
