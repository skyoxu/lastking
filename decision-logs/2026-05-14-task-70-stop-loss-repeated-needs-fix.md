# Decision Log - Task 70 Stop Loss Repeated Needs Fix

- Title: Task 70 Stop Loss on Repeated Needs Fix Family
- Date: 2026-05-14
- Status: accepted
- Supersedes: n/a - no prior stop-loss decision log for this Task 70 loop.
- Superseded by: n/a - active decision at this time.
- Branch: task/T70
- Git Head: 5f058b97d918
- Why now: Task 70 recovery loop repeated the same `review-needs-fix` family across multiple fork/resume turns while route fields stayed on `inspect-first` and blocked full rerun.
- Context: Latest protocol fields showed `latest_reason=pipeline_clean`, `chapter6_next_action=inspect`, `preferred_lane=inspect-first`, `six_eight_worthwhile=no`, and forbidden full rerun command `py -3 scripts/sc/run_review_pipeline.py --task-id 70`.
- Decision: Trigger stop-loss for this loop and switch to residual recording plus narrow follow-up instead of repeated heavy reruns.
- Consequences: Prevents repeated compute waste and preserves auditable artifacts for targeted next-pass remediation.
- Recovery impact: Recovery path must start from recommendation-only chain and inspect-first evidence before any further rerun decisions.
- Validation: Deterministic integration checks for Task 70 were green locally; remaining blocker was reviewer semantic finding family rather than deterministic test failure.
- Related ADRs: n/a - no ADR-level policy change required.
- Related execution plans: `execution-plans/2026-05-14-task-70-next-pass-residual-followup.md`
- Related task id(s): 70
- Related run id: ff676470e2c846bf9ca0e4f7564bcc99
- Related latest.json: `logs/ci/2026-05-14/sc-review-pipeline-task-70/latest.json`
- Related pipeline artifacts: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/summary.json`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/repair-guide.json`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/run-events.jsonl`, `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/agent-review.json`

## Evidence

- Summary: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/summary.json`
- Repair guide: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/repair-guide.json`
- Run events: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/run-events.jsonl`
- Agent review: `logs/ci/2026-05-14/sc-review-pipeline-task-70-ff676470e2c846bf9ca0e4f7564bcc99/agent-review.json`
