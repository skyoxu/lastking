# MVG integration evidence alignment

- Title: Align lastking with post-reconciliation MVG integration and conservative impact recommendation
- Date: 2026-09-18
- Status: Implemented through newrouge #185; final target validation pending
- Supersedes: None
- Superseded by: None
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: e4281d6b0e3e13ddaaae32ff45fc54cd1c9a8195
- Why now: newrouge evolved through PR #180's bounded MVG integration acceptance, PR #182's runtime-snapshot/evidence-integrity hardening, PR #183's Project Health hard-gate follow-up, and PR #184's repository-neutral migration reconciliation protocol.
- Context: lastking needs the reusable MVG/Impact control-plane behavior plus the source reconciliation protocol while keeping a target-native BattleMap pilot, target-specific Project Health tests, and repository-derived Knowledge state.
- Decision: Port generic MVG/Impact and reconciliation control-plane behavior exactly where repository-neutral, adapt Reward-specific execution to a BattleMap pilot, upgrade historical reconciliation records to `repo.cross-repo-migration-reconciliation.v1`, classify source PRs #172-#185, and preserve target business defaults/generated-state boundaries.
- Consequences: lastking gains bounded domain/scene/input evidence, workspace MVG execution, cross-report duplicate identity rejection, manifest-bound Impact handoff evidence, authoritative source-PR verification support, and a mandatory offline reconciliation hard gate without claiming whole-MVG coverage.
- Recovery impact: Use matching MVG summaries plus source_revision/snapshot_digest and Impact sibling run-manifest evidence; planned/recommended output is never equivalent to runtime_verified evidence. Migration creation/review may use `--verify-source-github`; routine target hard gates remain offline and require manifests.
- Validation: The original implementation checkpoint 1b2a01d201f960d072ba9d9aa259bc11e1f430d5 passed MVG 35345323512, Quality 35345323570 and Smoke 35345323538. Audit checkpoint 77f96181580ad2d270ef28a29639208f551ea52a exposed three newly hard-gated regressions, all repaired before later checkpoints. newrouge #183 and #184 both passed source Windows Quality and Smoke before merge. Final lastking validation for the #183-#185 reconciliation checkpoint is pending.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related execution plans: `execution-plans/2026-09-18-mvg-integration-alignment.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; no Taskmaster status is changed by this decision.
- Related run id: Pending target checkpoint runs: MVG 35422124767; Quality 35422124746; Smoke 35422124730.
- Related latest.json: N/A because MVG acceptance is separate from the Chapter 6 latest.json authority.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Source evolution: skyoxu/newrouge PR #180 / merge 9f75081ae80e2b8eb1c222eda3ad9def93ef09b5; #182 / merge a8e2f39872c39ffe4ee38461076279353d0d5b0e; #183 / merge 84a7f242b48d7aec9bcfe8d248867e81e6b943f1; #184 / merge c6ec8f99138ae9b62ac95b6e22ec0f1cee2b4fcf; #185 / merge c758a5ec87cb1f2829c09566d0978770dd59a7b3.
- Reconciliation coverage: `docs/migration/reconciliation/` records newrouge PRs #172-#185; generated Knowledge publication PRs remain `derived_regenerate`.
- Target baseline: lastking main 0c5f642ff675c4119f2f46db39e5833d7ea2617a
- Preservation boundary: Do not copy source Task115/Reward business state, source generated Knowledge publication outputs, or source-specific runtime defaults.
- Impact boundary: KCP Impact core remains fail-closed; handoff requires a sibling run manifest bound to report bytes/path/revision/status.
- Migration boundary: `copy_exact` is blob-locked; target-native adaptations require validation evidence; source PR inventory is independently verifiable against GitHub without making ordinary hard gates network-dependent.
- Task authority: no Taskmaster status writes.
