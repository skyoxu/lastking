# 当前 `workflow.md` 与 2026-04-05 `main` 基线版的核心差异

## 基线说明

- 基线 commit：`e72eb193f7516369e1a102d3651cd8bd86838f95`
- 说明：Git 历史中 `workflow.md` 在 2026-04-05 当天没有更新，因此 4 月 5 日 GitHub `main` 可视为该 commit 对应版本。
- 当前版文件：`workflow.md`

## 总体结论

- 顶层阶段结构没有推翻重写，但 Chapter 6 的执行协议被明显加厚。
- 主要增量集中在：recovery order、artifact integrity、planned-only stop-loss、6.7/6.8 止损和 fast mode 模板化。
- 目标从“把流程说清楚”升级为“把无效重跑和误恢复控住”。

## 工作步骤差异

### 1. Recovery 顺序改了

- 旧：先看 `active-task`，再跑 `resume-task`。
- 新：先跑 `py -3 scripts/python/dev_cli.py resume-task --task-id <id>`，仅当 summary 不够时才看 `logs/ci/active-tasks/task-<id>.active.md`，最后才用 `py -3 scripts/python/inspect_run.py --kind pipeline --task-id <id>`。
- 解决问题：避免先读到 stale active-task pointer，减少 6.7/6.8 误判。

### 2. Recovery 判断字段变多了

- 旧：偏向人工看 `reason` 或 summary 直觉判断。
- 新：明确要先看 `Latest reason`、`Latest run type`、`Latest reuse mode`、`Latest artifact integrity`，再看 `Chapter6 next action`、`Chapter6 can skip 6.7`、`Chapter6 can go to 6.8`、`Chapter6 blocked by`。
- 解决问题：把 `resume-task` / `inspect_run` / `active-task` 三个入口的恢复语义统一了。

### 3. Planned-only 和 artifact integrity 被升级为硬止损

- 新增规则：若 `run_type = planned-only`、`reason = planned_only_incomplete` 或 `Chapter6 blocked by = artifact_integrity`，则当前 bundle 只能当 evidence，不能直接继续 6.7/6.8。
- 解决问题：避免把 dry-run 或 incomplete bundle 当成真实 producer run 使用。

### 4. 6.4 red-first 增加了成本控制

- 新版明确：新建 `.gd` 测试较多时，首轮 red 优先 `--verify unit`，不把 `--verify all` 当默认。
- 解决问题：避免 red stage 一上来就支付 Godot / GdUnit 重验证成本。

### 5. 6.7 变成带 stop-loss 的 pipeline

- 新版显式加入：
  - deterministic green + only LLM not clean -> 优先 narrow closure
  - repeat deterministic failure -> 先修根因，不再盲目 rerun
  - same-run `sc-test` retry stop-loss
  - profile lock / `--reselect-profile`
  - agent-level timeout memory / targeted timeout bump
- 解决问题：减少 `sc-test + acceptance_check + llm_review` 全套重付。

### 6. 6.8 变成 reviewer-anchor 驱动的窄路径

- 新版明确：只有当前修改命中上轮 reviewer anchors 时才值得再跑 6.8。
- 新增 `timeout_agents`、`failure_kind` 语义，并按 problem class 缩小 reviewer 集合。
- 解决问题：避免 6.8 成为重复支付 LLM 成本的循环。

### 7. 新增 6.10 / 6.11

- `6.10 PR#61 增量快用`：把 acceptance preflight / hard-check preflight / clean reuse 条件写进正式步骤。
- `6.11 Fast mode 最省时执行模板`：给出 fast-ship 下的推荐顺序、resume 条件和 6.8 快路径。
- 解决问题：降低每次任务都需要人工重新设计命令顺序的时间损耗。

## 新增功能

- canonical recovery summary：`resume-task` 成为主入口。
- recovery hints：`inspect_run.py --kind pipeline` 输出 `latest_summary_signals` + `chapter6_hints`。
- planned-only stop-loss：把 `planned_only_incomplete` / `artifact_integrity` 变成硬门。
- deterministic reuse：6.7 支持“复用 deterministic，只重跑 LLM”。
- fast mode template：新增 6.11 的模板化命令顺序。

## 新增脚本 / 参数 / 依赖 / 引用

### 脚本入口

- `scripts/python/dev_cli.py run-acceptance-preflight`
- `scripts/python/dev_cli.py run-local-hard-checks-preflight`
- `scripts/python/dev_cli.py resume-task`（从“可选摘要器”升级为“恢复主入口”）
- `scripts/python/inspect_run.py --kind pipeline`（从“辅助检查”升级为“第三层 recovery 入口”）
- `scripts/sc/run_review_pipeline.py`（在 workflow 中被明确定位为 task-level 统一主入口）
- `scripts/sc/llm_review_needs_fix_fast.py`（在 workflow 中被明确定位为 6.8 快路径）

### 参数

- `--allow-full-rerun`
- `--allow-repeat-deterministic-failures`
- `--reselect-profile`
- `--llm-base`（当前版明确默认基线为 `origin/main`）
- `--llm-agent-timeouts`

### 依赖的 artifact / sidecar 引用

- `summary.json`
- `latest.json`
- `execution-context.json`
- `repair-guide.json`
- `repair-guide.md`
- `agent-review.json`
- `run-events.jsonl`
- `child-artifacts/sc-test/summary.json`
- `child-artifacts/sc-acceptance-check/summary.json`
- `logs/ci/active-tasks/task-<id>.active.md`

## 这些差异具体解决了什么问题

| 差异点 | 解决的问题 |
|---|---|
| recovery 顺序调整 | 解决 stale active-task pointer 导致的误恢复 |
| `planned-only` / `artifact_integrity` 硬止损 | 解决 dry-run bundle 被误当真实 run 继续使用 |
| 6.7 deterministic reuse + stop-loss | 解决重复支付 `sc-test + acceptance_check + llm_review` 全成本 |
| 6.8 anchor-driven rerun | 解决 Needs Fix 阶段的 LLM 循环重跑 |
| acceptance / hard-check preflight | 解决一些可以更早 fail-fast 的问题要等到后面才暴露 |
| 6.11 fast mode template | 解决人工拼命令、步骤顺序不稳定、时间成本过高的问题 |

## 一句话总结

当前版 `workflow.md` 相比 2026-04-05 `main` 基线版，核心不是“多了几个命令”，而是把 Chapter 6 升级成了一个更严格的 recovery + stop-loss + narrow-closure + fast-mode 执行协议。
