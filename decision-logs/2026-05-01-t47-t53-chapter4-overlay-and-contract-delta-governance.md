# Decision Log

- Date: 2026-05-01
- Status: accepted
- Task ID: 47
- Related Tasks: T47-T53
- Title: T47-T53 Chapter4 overlay and contract-delta governance

## Why Now

T47-T53 entered workflow Chapter 5 light-lane execution and repeatedly hit deterministic acceptance/obligation gate failures before stable semantic convergence. Chapter 4 governance needed to be made explicit so implementation can continue without architecture drift or contract duplication.

## Context

- Repository: `lastking`
- Governing workflow section: `workflow.md` Chapter 4 (`4.1` to `4.4`)
- Current overlay family: `docs/architecture/overlays/PRD-lastking-T2/08/**`
- Existing contract SSoT: `Game.Core/Contracts/**`
- Recent artifacts confirmed:
  - `logs/ci/2026-05-01/single-task-light-lane-t47-t53-rerun2/summary.json`
  - `logs/ci/2026-05-01/single-task-light-lane-t51-t52-rerun3/summary.json`
  - `logs/ci/2026-05-01/overlay-lint/report.json`

## Decision

1. Keep using the existing overlay family `PRD-lastking-T2/08` for T47-T53. Do not create a new overlay family.
2. Apply limited Chapter 4 updates by extending existing pages only:
   - `08-Feature-Slice-T2-Core-Loop.md`
   - `08-Contracts-T2.md`
   - `08-Testing-T2.md`
   - `08-Observability-T2.md`
   - `ACCEPTANCE_CHECKLIST.md`
3. For contracts, enforce reuse-first policy. Do not create new `Game.Core/Contracts/**` files unless implementation proves existing contracts cannot represent required cross-layer semantics.
4. Keep candidate contract deltas as documented overlay intent (interface/DTO/event candidates), not immediate code artifacts.

## Consequences

- Positive:
  - Prevents architecture drift between Taskmaster views, GDD intent, and runtime contracts.
  - Keeps contract surface minimal and auditable.
  - Enables narrow reruns in Chapter 5 by removing deterministic semantic gaps first.
- Tradeoff:
  - Some task-level implementation may require an additional explicit contract-creation step later.

## Recovery Impact

- Recovery tools can rely on a single overlay family and stable refs for T47-T53.
- When Chapter 6 or Chapter 7 resumes these tasks, state inspection no longer needs to guess which overlay family is authoritative.
- If Needs Fix persists, follow overlay-declared contract candidates first, then promote to real contract files only with evidence.

## Validation

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2` -> pass
- `py -3 scripts/python/check_tasks_all_refs.py` -> pass
- `py -3 scripts/python/validate_task_master_triplet.py` -> pass
- `T47-T53` Chapter 5 convergence evidence exists in:
  - `logs/ci/2026-05-01/single-task-light-lane-t47-t53-rerun2/summary.json`
  - `logs/ci/2026-05-01/single-task-light-lane-t51-t52-rerun3/summary.json`

## Links

- workflow: `workflow.md` (Chapter 4)
- overlay root: `docs/architecture/overlays/PRD-lastking-T2/08/_index.md`
- contract inventory: `docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md`

