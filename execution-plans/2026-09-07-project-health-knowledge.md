# Project Health Knowledge Integration

- Title: Project Health Knowledge Integration
- Status: active
- Branch: align-newrouge-features-20260912
- Git Head: bde6bc46678cd100865532236eebb535f5719cdf
- Goal: Align main-only Knowledge/Impact investigation and local Project Health service with upstream while binding runtime examples to lastking repository facts.
- Scope: CLI/API, GDD configuration, SSOT task pagination, static scene attachment evidence, and pre-merge target adaptation.
- Current step: Focused target-adaptation validation passed; repository PR checks are the remaining merge gate.
- Last completed step: Removed source-only CI repair history, replaced the invalid upstream Task 115/Reward binding with lastking Task 54/BattleMapScreen evidence, and passed focused regression validation.
- Stop-loss: Do not import newrouge gameplay fixtures or weaken repository recovery/Impact validation to make copied tooling pass.
- Next action: Require Windows Quality Gate and Windows Smoke to pass on the restored PR head; merge only if green.
- Recovery command: py -3 scripts/python/validate_recovery_docs.py --dir all
- Open questions: Native developer-machine browser interaction and local GODOT_BIN execution remain environment-dependent; repository CI is the merge gate.
- Exit criteria: Target-specific defaults are valid, focused regression checks pass, and PR checks are green.
- Related ADRs: docs/adr/ADR-0036-project-health-investigation.md
- Related decision logs: decision-logs/2026-09-07-project-health-impact-limits.md
- Related task id(s): T54 is used only as a static mapping example; this change is repository tooling, not a gameplay task implementation.
- Related run id: n/a - pre-merge adaptation
- Related latest.json: logs/ci/project-health-knowledge/latest.json
- Related pipeline artifacts: n/a - use PR #98 GitHub Actions checks

## Scope

Keep the imported Knowledge/Impact and loopback Project Health behavior aligned with upstream, while ensuring target defaults describe files and tasks that actually exist in lastking. Protocol schema identifiers remain unchanged for upstream compatibility.

## Target Adaptation

- Removed `.agents/skills/pr-ci-artifact-repair` because it encodes newrouge-specific PR/run history and Game.Core incident assumptions rather than reusable chapter workflow behavior.
- Rebound `task_scene_bindings` from upstream Task 115/Reward to lastking Task 54/BattleMapScreen.
- Replaced the source-specific `CardPoolSelection` review query hint with generic workflow/failure/analysis terms.
- Updated the Project Health workflow documentation to describe the lastking default mapping and aliases.

## Validation

- `py -3 -m unittest scripts.python.tests.test_impact_analysis_handoff scripts.python.tests.test_impact_analyzer scripts.sc.tests.test_project_health_knowledge scripts.sc.tests.test_project_health_navigation scripts.sc.tests.test_project_health_runtime_snapshot scripts.sc.tests.test_project_health_server`
- `node --check scripts/python/project_health_knowledge.js`
- `py -3 scripts/python/validate_recovery_docs.py --dir all`
- `git diff --check origin/main...HEAD`
- Verify configured Task 54 scene, root script assignment and witness exist in the repository.

## Observed Results

The target-adaptation workflow completed successfully on Windows: the focused Impact/Knowledge/Project Health regression set, JavaScript syntax check, recovery-doc validation, whitespace check, and Task 54 scene/script/witness assertions all passed. The upstream historical Task 115/Reward scan counts and PR 166 run ids are intentionally not carried forward as lastking evidence.