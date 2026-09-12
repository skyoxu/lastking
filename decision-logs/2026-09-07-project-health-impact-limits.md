# Project Health Impact Limits

- Title: Project Health Impact Limits and Target Adaptation
- Date: 2026-09-12
- Status: accepted
- Supersedes: n/a - imported upstream decision adapted for lastking
- Superseded by: n/a - active decision
- Branch: align-newrouge-features-20260912
- Git Head: f36ce53b36d09048fe7579f2406b8daee9954cd9
- Why now: Pre-merge review found source-repository incident history and an invalid Task 115/Reward default binding in the copied tooling.
- Context: Core Project Health/Impact behavior is reusable, but repository-specific examples must resolve against lastking rather than newrouge.
- Decision: Preserve formal Impact/KCP boundaries and upstream schema identifiers, remove source-only CI repair history, and bind Project Health defaults only to verified lastking files.
- Consequences: Project Health starts with a real lastking static mapping; future CI repair guidance is not contaminated by newrouge PR history; protocol compatibility is preserved.
- Recovery impact: Validate recovery docs and PR CI before merge; do not import source gameplay fixtures solely to satisfy upstream assumptions.
- Validation: Run py -3 scripts/python/validate_recovery_docs.py --dir all plus focused Impact/Project Health tests and PR checks.
- Related ADRs: docs/adr/ADR-0036-project-health-investigation.md
- Related execution plans: execution-plans/2026-09-07-project-health-knowledge.md
- Related task id(s): T54 is static evidence only; no gameplay behavior is changed by this decision.
- Related run id: n/a - pre-merge adaptation
- Related latest.json: logs/ci/project-health-knowledge/latest.json
- Related pipeline artifacts: n/a - use PR #98 GitHub Actions checks

## Decision

The local investigation page must remain exploratory and must not create authoritative chapter handoffs. Formal Impact behavior remains strict. Repository-specific defaults must point to real target files and cannot be copied blindly from the source repository.

## Target Evidence

Task 54 is present in lastking's task SSOT. `Game.Godot/Scenes/Screens/BattleMapScreen.tscn` assigns `Game.Godot/Scripts/Screens/BattleMapScreen.gd` on the root node, and that script contains `func _initialize_battle_screen() -> void:`. This is sufficient for the existing conservative `static_attached` declaration model and does not claim runtime verification.

## Limits

Runtime Godot verification still requires an actual `GODOT_BIN` execution path. Static attachment does not prove gameplay correctness. Imported `newrouge.*` schema identifiers are retained as protocol identifiers for compatibility; they are not evidence that lastking gameplay equals newrouge.
