# newrouge #187-#194 alignment

- Title: Align post-#185 reusable source evolution into lastking
- Status: In validation
- Branch: sync-newrouge-187-194-20260919
- Git Head: 0e2b24e018e0a7713bd460b5a976639652aee7e4
- Goal: Reconcile newrouge #186-#194 while preserving lastking business semantics and generated-state ownership.
- Scope: MVG coverage contract, BattleMap pilot adaptation, Knowledge-link generation/full-rebuild regression, migration canonical-PR guard, Project Health browser isolation, reconciliation manifests, and generated Knowledge publication classifications.
- Current step: Run repository-level validation and inspect target PR checks after the reconciliation manifests are complete.
- Last completed step: Implemented generic control-plane changes and target-native BattleMap/Project Health adaptations on the alignment branch.
- Stop-loss: Do not mark source-specific M1/Reward business files as target coverage; do not copy generated Knowledge publication bytes; do not weaken target hard gates; do not treat pilot evidence as critical/full coverage.
- Next action: Validate Python unit coverage, reconciliation manifests, MVG plan semantics, and protected CI workflows; repair only target-native regressions.
- Recovery command: `py -3 scripts/python/check_cross_repo_migration.py --require-manifests`
- Open questions: A target-native critical/full MVG manifest requires separately confirmed multi-flow business scope; this migration intentionally adds the executable coverage capability without inventing such a claim.
- Exit criteria: Reconciliation manifests pass; MVG coverage tests pass; BattleMap pilot validates with blocker [12]; Knowledge full-rebuild regression is hard-gated; canonical migration tests pass; Project Health browser tests are isolated; protected PR checks are green.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related decision logs: `decision-logs/2026-09-19-mvg-coverage-tiers-lastking.md`
- Related task id(s): 12, 54
- Related run id: Pending target PR checks.
- Related latest.json: N/A.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/**/gate-bundle/**`

- Out of scope: copying newrouge M1/Reward business implementation, source generated Knowledge state, or asserting full product coverage from the BattleMap pilot.
