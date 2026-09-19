# MVG 集成验收

本入口验证同一版本中多个功能提交组合后的行为。它补充现有 Chapter 3–7、Chapter 6 review、delivery profile 和正式 KCP Impact，不引入第二套任务状态，也不自动修改 Taskmaster。

## 接入原则

- Chapter 3：为跨任务链路指定真实 task IDs 与整合责任；已有任务能承担就复用。
- Chapter 4：记录 producer / consumer / owner、现有契约引用和可观察行为。
- Chapter 5：planned 测试必须有明确行为；继续使用现有 Refs/overlay 校验。
- Chapter 6：开发期可以按影响建议先跑相关测试，但 MVG 收尾仍执行 manifest 中全部 required tests。
- Chapter 7：玩家输入证据必须来自真实场景、真实 Control 焦点和引擎输入；scene-method 不得冒充 engine-input。
- MVG 运行结果只证明 manifest 范围内、指定输入版本的运行证据，不授权把 Taskmaster 状态改为 done。

## Manifest 与证据

默认 target pilot 是 `docs/testing/mvg/battlemap-pilot.json`。schema ID 保持 `newrouge.mvg-integration.v1`，用于与上游工具协议兼容；其中 task IDs、路径、契约和测试全部指向 lastking 自身内容。

每个 flow 必须有 outcome、真实 task_ids、source_paths、handoffs、test_ids。每个 handoff 必须声明 producer_task / consumer_task / owner_task、contract_ref、behavior 与 test_ids。每个测试必须声明 kind、state、path、evidence_level 和 min_tests；dotnet 还要求精确类 selector。

### Coverage contract

与 `newrouge` #187 对齐后，每个 manifest 还必须声明 `coverage`：`mode` 取 `pilot` / `critical` / `full`，`scope_id` 标识覆盖边界，`required_flow_ids` 必须与 flow 顺序完全一致，`blocking_task_ids` 必须精确等于范围内 Taskmaster 非 `done` 任务，`excluded_claims` 明确本 manifest 不证明什么。

`critical` 至少包含两个 flow，`full` 至少包含三个 flow；二者在存在非 `done` scoped task 时禁止执行。`pilot` 允许保留 blocker 并继续运行，用于证明有限链路，但不能据此升级成 critical/full 覆盖声明。当前 lastking 的默认 `battlemap-pilot` 因 Task 12 仍为 `pending`，明确记录 `blocking_task_ids: [12]`；Task 54 已为 `done`。目标仓尚未定义经过业务确认的 critical/full manifest，因此不会从 newrouge 复制其 M1/Reward 任务 ID、场景路径或覆盖结论。

运行器对 TRX / GdUnit JUnit 做正向证据校验：报告必须存在、测试非空、类/套件身份精确匹配、计数一致、无重复结果、零失败、零跳过，并且子进程成功。plan/recommend 成功不等于 runtime_verified。

## lastking BattleMap pilot

当前 pilot 是一个有界样例，不代表整个 MVG：

- `battlemap-core`：真实 C# ResourceManager / BuildingPlacementService 组合，验证资源变更事件和 placement 边界。
- `battlemap-scene`：复用真实 BattleMap 场景控制测试，属于 scene-method 证据。
- `battlemap-input`：实例化真实 BattleMapScreen，让 `WaveAction` 获得焦点后向 Viewport 推送 `InputEventAction("ui_accept")`，验证敌军波次实际增加，属于 engine-input 证据。

`--challenge-input` 会在隔离运行中断开 WaveAction 的 pressed 接线。只有测试以 `MVG_BATTLEMAP_INPUT_DID_NOT_SPAWN_WAVE` 指定断言失败，才算证明旅程能发现接线断裂；编译失败、缺运行时、空报告或超时都不算发现缺陷。

## Impact 建议边界

`recommend` 比较 Git 改动与 manifest 的 source_paths、contract_ref 和测试路径，并输出 matched flows、证据路径与 unmapped changes。

- commit 模式的 manifest 与比较终点都绑定 `--revision`。
- workspace 模式明确包含工作区改动。
- 未映射文件、没有命中链路、没有可用比较范围时回退为 `full-mvg`。
- `required_tests` 始终保留完整列表，`authorizes_test_exclusion=false`。
- `related-first` 只是执行优先级建议，当前 run 仍执行全部必测项。
- 这不是完整调用图，也不替代正式 KCP Impact；“没有映射到”绝不能解释为“没有影响”。

## Windows 命令

```powershell
py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan
py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode recommend --base origin/main
py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode run --snapshot commit --revision HEAD --godot-bin $env:GODOT_BIN --challenge-input
py -3 scripts/python/run_mvg_mutation_probe.py --snapshot commit --revision HEAD
```

正式收尾优先 commit snapshot；开发中的未提交改动可使用 workspace snapshot。输出保存在 `logs/ci/mvg-acceptance/<run-id>/` 与 `logs/ci/mvg-mutation/<run-id>/`。

## 可选变异实验

target-native mutation probe 只在隔离快照里变异 `BuildingPlacementService` 的两个明确边界：exact-cost 接受条件与 placement 扣费方向。它先要求基线通过，再要求冻结测试真正 kill mutant；survived 或 unverified 都需要调查。该实验不是全程序 mutation score，也不是默认门禁。

## CI 与人工验收

`.github/workflows/mvg-integration.yml` 在 MVG 工具、manifest 和 BattleMap pilot 相关代码变化时执行 Windows pilot；mutation 仅在手动 dispatch 明确开启。CI 不替代现有 quality/smoke/build-test-export，也不替代视觉、手感、平衡、鼠标命中、OS 级输入或试玩判断。
