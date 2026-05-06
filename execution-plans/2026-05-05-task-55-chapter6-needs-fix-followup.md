# Task 55 Chapter 6 Needs-Fix Follow-Up

- Title: Task 55 chapter6 needs-fix follow-up
- Status: in-progress
- Branch: task/T55
- Git Head: 7be92f5e198c90b4c2a7003f416d30a52f5b5c1c
- Goal: Exit repeated-failure stop-loss and close reviewer gate under Chapter 6 protocol.
- Current step: blocked by repeated `recent_failure_summary` stop-loss.
- Last completed step: targeted assertions strengthened; deterministic targeted suites pass.
- Stop-loss: when inspect reports `rerun_forbidden=true` and route remains `inspect-first`, do not pay additional reruns.
- Next action: Run protocol-ordered recovery (`resume-task` -> `chapter6-route` -> optional `inspect-run`) and continue only with route-allowed command.
  1) `py -3 scripts/python/dev_cli.py resume-task --task-id 55 --recommendation-only`
  2) `py -3 scripts/python/dev_cli.py chapter6-route --task-id 55 --recommendation-only`
  3) if still inspect, `py -3 scripts/python/dev_cli.py inspect-run --kind pipeline --latest logs/ci/2026-05-05/sc-review-pipeline-task-55/latest.json --recommendation-only`
  4) continue only with protocol-allowed command when stop-loss no longer blocks.
- Exit criteria: route no longer returns repeated-failure stop-loss and reviewer gate closes without P0/P1.
- Related decision logs: decision-logs/2026-05-05-task-55-chapter6-needs-fix-residual.md
- Related task id(s): T55
- Related run ids: 90a978b210ad49e9a23307f229919c0b, 71f38c6ca5c2436490dda3b3157813ca, 01b8f97e1612485ebad9d3e7faf23754
- Scope: Task 55 Chapter 6 recovery/resume route stabilization and reviewer-gate closure planning.
- Recovery command: `py -3 scripts/python/dev_cli.py resume-task --task-id 55 --recommendation-only`; `py -3 scripts/python/dev_cli.py chapter6-route --task-id 55 --recommendation-only`; if still inspect: `py -3 scripts/python/dev_cli.py inspect-run --kind pipeline --latest logs/ci/2026-05-05/sc-review-pipeline-task-55/latest.json --recommendation-only`.
- Open questions: Whether a protocol-approved rerun lane can be reopened without violating repeated-failure stop-loss in the next cycle.
- Related ADRs: `ADR-0018`, `ADR-0021`, `ADR-0022`, `ADR-0032`.
- Related run id: `90a978b210ad49e9a23307f229919c0b`, `71f38c6ca5c2436490dda3b3157813ca`, `01b8f97e1612485ebad9d3e7faf23754`.
- Related latest.json: `logs/ci/2026-05-05/sc-review-pipeline-task-55/latest.json`.
- Related pipeline artifacts: `logs/ci/2026-05-05/sc-review-pipeline-task-55-01b8f97e1612485ebad9d3e7faf23754/`.
