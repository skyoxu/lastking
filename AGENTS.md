# Repository Guide

This file is the routing layer for `lastking`. Keep it short. Put durable detail in `docs/agents/**`, `docs/workflows/**`, `docs/adr/**`, and `docs/architecture/**`.

## Project Identity
- Repository: `lastking`
- Product: Windows-only single-player Godot 4.5.1 + C# game project
- Current default delivery posture: `fast-ship`
- Current default security posture: `host-safe`
- Upstream alignment baseline: `docs/workflows/business-repo-upgrade-guide.md`

## Non-Negotiables
- Communicate with users in Chinese.
- Default environment is Windows. All commands and paths must be Windows-compatible.
- Read and write documents with Python and UTF-8.
- Do not use emoji.
- Keep code, scripts, tests, comments, and printed messages in English.
- Put logs, evidence, and audit output under `logs/**`.
- Use small, explicit plans and update progress during non-trivial work.
- Do not keep dead compatibility layers. Remove obsolete paths instead of preserving them.

## Start Order After Context Reset
1. `README.md`
2. `docs/agents/00-index.md`
3. `docs/PROJECT_DOCUMENTATION_INDEX.md`
4. `docs/agents/13-rag-sources-and-session-ssot.md`
5. `DELIVERY_PROFILE.md`
6. `docs/testing-framework.md`
7. `docs/agents/16-directory-responsibilities.md`
8. `docs/workflows/prototype-lane.md`
9. Newest file in `execution-plans/`
10. Newest file in `decision-logs/`
11. If a local review already ran: `logs/ci/<date>/sc-review-pipeline-task-<task>/latest.json`

## Authoritative Sources
Use these sources first. Do not rebuild ad-hoc indexes unless the task explicitly requires it.
- Taskmaster triplet: `.taskmaster/tasks/tasks.json`, `.taskmaster/tasks/tasks_back.json`, `.taskmaster/tasks/tasks_gameplay.json`
- PRD material: `.taskmaster/docs/prd.txt`, `docs/prd/**`
- ADRs: `docs/adr/ADR-*.md`, `docs/architecture/ADR_INDEX_GODOT.md`
- Base architecture: `docs/architecture/base/**`
- Overlays: `docs/architecture/overlays/<PRD-ID>/08/**`
- Testing rules: `docs/testing-framework.md`
- Delivery and harness rules: `DELIVERY_PROFILE.md`, `docs/workflows/run-protocol.md`, `docs/workflows/local-hard-checks.md`

## Core Execution Entry Points
- Repo-scoped hard validation:
  - `py -3 scripts/python/dev_cli.py run-local-hard-checks --godot-bin <path>`
- Canonical task recovery:
  - `py -3 scripts/python/dev_cli.py resume-task --task-id <id>`
- Task-scoped review pipeline:
  - `py -3 scripts/sc/run_review_pipeline.py --task-id <id> --godot-bin <path>`
- Recovery doc validation:
  - `py -3 scripts/python/validate_recovery_docs.py --dir all`
- Gate bundle only:
  - `py -3 scripts/python/run_gate_bundle.py --mode hard --task-files .taskmaster/tasks/tasks_back.json .taskmaster/tasks/tasks_gameplay.json`

## Architecture And Contract Rules
- Contracts SSoT lives in `Game.Core/Contracts/**`.
- Contract code must stay BCL-only. Do not reference `Godot.*` in contracts.
- Core business logic belongs in `Game.Core/**`.
- Godot integration belongs in `Game.Godot/**` and adapter layers.
- Concrete feature slices belong only in `docs/architecture/overlays/<PRD-ID>/08/**`.
- If thresholds, contracts, security posture, or release policy change, add or supersede an ADR.

## Testing Rules
- Domain logic: xUnit in `Game.Core.Tests/**`.
- Engine and scene glue: GdUnit4 in `Tests.Godot/**`.
- Do not disable tests to get green. Fix them.
- Acceptance items must map to `Refs:` and those refs must stay aligned with task views and overlays.

## Task-View Rules
- Real task data is under `.taskmaster/tasks/**`; examples under `examples/taskmaster/**` are fallback/template-only.
- Cross-file mapping is fixed:
  - `tasks.json.master.tasks[].id`
  - `tasks_back.json[].taskmaster_id`
  - `tasks_gameplay.json[].taskmaster_id`
- `semantic_review_tier` belongs in the real view files, not only in examples.

## Documentation Rules
- Keep repo identity as `lastking`; remove stale upstream project names, PRD examples, and template-only examples when touching docs.
- Do not duplicate Base/ADR thresholds into overlays.
- Use paths and references, not pasted copies of contract fields.
