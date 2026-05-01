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

## Quantified Pass/Fail Criteria

- `validate_overlay_execution` 结果必须为 `status=ok` 且 `errors=0`。
- `validate_task_master_triplet` 必须无结构性错误与必填字段缺失。
- `check_tasks_all_refs` 与 `check_tasks_back_references` 必须无断链与坏引用。
- 任一 hard gate 失败即本清单验收失败，不允许以 warning 通过。

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


## Chapter 7 UI Wiring Acceptance (`T41-T46`)

- `T41-T46` must trace to `docs/gdd/ui-gdd-flow.md` section `## 5. UI Wiring Matrix` and `## 11. Next UI Wiring Task Candidates`.
- Each task must be generated from one `docs/gdd/ui-gdd-flow.candidates.json` candidate and preserve `ui_entry`, `player_action`, `system_response`, `empty_state`, `failure_state`, `completion_result`, and `suggested_standalone_surfaces`.
- Each acceptance item must keep concrete test refs and auditable artifact refs instead of relying on logs-only evidence.
- `T41-T46` must reuse presentation-safe existing contracts; only add a new domain event, DTO, or interface when `workflow.md` and `Game.Core/Contracts/DomainEvent.cs` plus `Game.Core/Contracts/EventTypes.cs` cannot represent the required signal.

## Combat Loop Closure Acceptance (`T47-T53`)

- `T47-T53` must trace to `docs/gdd/combat-loop-gdd.md` and `docs/gdd/combat-loop-task-candidates.md`, and the resulting task records must keep refs back to this overlay family.
- `T47-T53` 必须优先复用现有 `Game.Core/Contracts/Lastking/WaveSpawned.cs`, `Game.Core/Contracts/Lastking/CastleHpChanged.cs`, `Game.Core/Contracts/Lastking/ResourcesChanged.cs`, `Game.Core/Contracts/Lastking/TechApplied.cs`, `Game.Core/Contracts/Lastking/RewardOffered.cs`, `Game.Core/Contracts/Lastking/UiFeedbackRaised.cs`, `Game.Core/Contracts/Lastking/PerfSampled.cs`，以及 `Game.Core/Contracts/DomainEvent.cs` 和 `Game.Core/Contracts/EventTypes.cs`；只有现有 contracts 无法表达跨层共享语义时，才允许新增 event、DTO 或 interface skeleton。
- Battle loop evidence 不得仅靠 logs-only；至少要同时具备可执行测试引用和可机读工件引用，以证明索敌、出兵、命中、AoE、清理、战后总结真正接入 runtime。
- `T47-T49` 的验收必须证明敌方、塔、防御单位、兵营产出单位共享同一目标解释和战场登记口径，避免出现“看得见生成、打不到目标”或“只在 UI 扣资源不入场”。
- `T50-T51` 的验收必须证明 projectile/AoE 的速度、命中、范围和伤害来源可回链到 config/schema，而不是写死在场景或视觉特效里。
- `T52` 的验收必须证明死亡清理与对象回收在长局中保持稳定，不能以短时手玩无崩溃替代 churn/perf 证据。
- `T53` 的验收必须证明玩家可以在战后直接读到结果、损耗和下一步建议，避免只把失败原因留在开发日志或调试面板。
