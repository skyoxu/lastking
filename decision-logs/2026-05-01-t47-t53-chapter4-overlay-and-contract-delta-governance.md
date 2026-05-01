# Decision Log

- Title: T47-T53 Chapter4 overlay and contract-delta governance
- Date: 2026-05-01
- Status: accepted
- Supersedes: n/a because this is the first governance decision log for T47-T53 Chapter4 in this repository
- Superseded by: n/a because no newer governance decision has replaced this log yet
- Branch: task/T46
- Git Head: c5445e3
- Why now: T47-T53 entered Chapter5 execution and needed explicit Chapter4 governance to avoid overlay drift and premature contract proliferation
- Context: Chapter4 requires deterministic overlay and contract governance before implementation continuation for T47-T53
- Decision: keep PRD-lastking-T2 overlay family, apply incremental updates only in existing 08 pages, and enforce contracts reuse-first with deferred promotion of candidates
- Consequences: alignment across task views, overlays, and contracts is preserved while reducing architecture drift risk
- Recovery impact: recovery and rerun routing can resolve one authoritative overlay family and avoid ambiguous contract creation paths
- Validation: validate_overlay_execution, check_tasks_all_refs, and validate_task_master_triplet all passed on 2026-05-01
- Related ADRs: `docs/adr/ADR-0010-delivery-profile-security-baseline.md`, `docs/adr/ADR-0011-overlay-execution-governance-and-task-triplet-linkage.md`, `docs/adr/ADR-0019-run-protocol-recovery-order-and-stop-loss.md`, `docs/adr/ADR-0025-recovery-docs-and-run-evidence-governance.md`
- Related execution plans: `execution-plans/2026-05-01-task-47-combat-entity-registry-and-shared-target-query-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-48-activate-mgtower-auto-attack-runtime-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-49-spawn-barracks-units-into-battlefield-runtime-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-50-projectile-runtime-for-towers-and-ranged-enemies-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-51-area-damage-resolver-and-elite-pressure-slice-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-52-combat-lifecycle-and-pooling-hardening-acceptance-test-generation-plan.md`, `execution-plans/2026-05-01-task-53-battle-after-action-summary-and-guidance-acceptance-test-generation-plan.md`
- Related task id(s): 47,48,49,50,51,52,53
- Related run id: 25201721244
- Related latest.json: `logs/ci/2026-05-01/single-task-light-lane-t47-t53-rerun2/latest.json`
- Related pipeline artifacts: `logs/ci/2026-05-01/single-task-light-lane-t47-t53-rerun2/`, `logs/ci/2026-05-01/single-task-light-lane-t51-t52-rerun3/`

## Notes

- Overlay family remains `docs/architecture/overlays/PRD-lastking-T2/08/**`.
- Contract code creation stays deferred until implementation evidence proves reuse is insufficient.
