# Execution Plan - Task 70 Next Pass Residual Followup

- Title: Task 70 Next Pass Residual Followup
- Status: active
- Branch: task/T70
- Git Head: 5f058b97d918
- Goal: Close the remaining Task 70 reviewer semantic finding without reopening heavy repeated loops.
- Scope: Task 70 outcome-evidence semantics and designated integration-flow acceptance evidence only; no unrelated refactor.
- Current step: Recovery stop-loss handoff from repeated inspect/fork cycles.
- Last completed step: Updated overlay-task baseline by running `py -3 scripts/python/remind_overlay_task_drift.py --write`.
- Stop-loss: Enabled; avoid hard reruns while `chapter6-route` remains `inspect-first` and full rerun is forbidden.
- Next action: Run protocol recovery chain and apply minimal semantic/test assertion adjustment only if the latest finding maps to a real gap.
- Recovery command: `py -3 scripts/python/dev_cli.py inspect-run --kind pipeline --latest logs/ci/2026-05-14/sc-review-pipeline-task-70/latest.json`
- Open questions: Whether the latest `llm-security-auditor-needs-fix` finding still reproduces after narrow semantic alignment.
- Exit criteria: No P0/P1 Needs Fix for Task 70, or residual is recorded with explicit repeated-family stop-loss evidence.
- Related ADRs: n/a - no architecture threshold or policy change in this followup.
- Related decision logs: `decision-logs/2026-05-14-task-70-stop-loss-repeated-needs-fix.md`
- Related task id(s): 70
- Related run id: ff676470e2c846bf9ca0e4f7564bcc99
- Related latest.json: `logs/ci/2026-05-14/sc-review-pipeline-task-70/latest.json`
- Related pipeline artifacts: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/summary.json`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/repair-guide.json`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/run-events.jsonl`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/agent-review.json`

## Protocol Snapshot

```text
task_id=70
run_id=ff676470e2c846bf9ca0e4f7564bcc99
failure_code=review-needs-fix
recommended_action=inspect
forbidden_commands=py -3 scripts/sc/run_review_pipeline.py --task-id 70
chapter6_next_action=inspect
preferred_lane=inspect-first
```

## Planned Narrow Followup

1. Run `resume-task --recommendation-only`.
2. Run `chapter6-route --recommendation-only`.
3. Run `inspect-run --kind pipeline --recommendation-only` and inspect latest reviewer markdown.
4. Patch only the identified semantic/test gap if still valid.
5. Resume pipeline (`--resume`) and follow approval state machine when sidecar returns pending/approved/denied.
