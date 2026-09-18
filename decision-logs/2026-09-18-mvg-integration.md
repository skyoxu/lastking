# MVG integration evidence alignment

- Title: Align lastking with post-reconciliation MVG integration and conservative impact recommendation
- Date: 2026-09-18
- Status: Implemented through newrouge #182; final target validation pending
- Supersedes: None
- Superseded by: None
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: 10b988acea91b0a38e3d0c4d67a1955c1c50eb0a
- Why now: newrouge evolved through PR #180's bounded MVG integration acceptance and PR #182's runtime-snapshot/evidence-integrity hardening.
- Context: lastking needed the reusable MVG execution/evidence layer plus the #182 workspace snapshot, cross-report deduplication and Impact run-manifest rebinding fixes, while keeping a target-native BattleMap pilot.
- Decision: Port generic MVG/Impact control-plane behavior exactly where repository-neutral, adapt Reward-specific execution to a BattleMap pilot, classify source PRs #172-#182 with migration reconciliation manifests, and preserve target business defaults/generated-state boundaries.
- Consequences: lastking gains bounded domain/scene/input evidence, workspace MVG execution, cross-report duplicate identity rejection, manifest-bound Impact handoff evidence, and a fail-closed source-to-target reconciliation gate without claiming whole-MVG coverage.
- Recovery impact: Use matching MVG summaries plus source_revision/snapshot_digest and Impact sibling run-manifest evidence; planned/recommended output is never equivalent to runtime_verified evidence.
- Validation: The original implementation checkpoint 1b2a01d201f960d072ba9d9aa259bc11e1f430d5 passed MVG 35345323512, Quality 35345323570 and Smoke 35345323538. Later audit checkpoint 77f96181580ad2d270ef28a29639208f551ea52a passed MVG and Smoke but Quality exposed three newly hard-gated Project Health/reconciliation regressions; those root causes were repaired before checkpoint 10b988acea91b0a38e3d0c4d67a1955c1c50eb0a. newrouge #182 source head 535e3d227964f32f60d8d3c010ab4f963ad32b35 passed source MVG, Quality and Smoke. Final lastking validation for the #182-aligned checkpoint is pending.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related execution plans: `execution-plans/2026-09-18-mvg-integration-alignment.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; no Taskmaster status is changed by this decision.
- Related run id: Pending target checkpoint runs: MVG 35367820494; Quality 35367820237; Smoke 35367820478.
- Related latest.json: N/A because MVG acceptance is separate from the Chapter 6 latest.json authority.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Source evolution: skyoxu/newrouge PR #180 / merge 9f75081ae80e2b8eb1c222eda3ad9def93ef09b5 plus PR #182 / merge a8e2f39872c39ffe4ee38461076279353d0d5b0e
- Reconciliation coverage: `docs/migration/reconciliation/` records newrouge PRs #172-#182; generated Knowledge publication PRs remain derived-regenerate.
- Target baseline: lastking main 0c5f642ff675c4119f2f46db39e5833d7ea2617a
- Preservation boundary: Do not copy source Task115/Reward business state, source generated Knowledge publication outputs, or source-specific runtime defaults.
- Impact boundary: KCP Impact core remains fail-closed; handoff now additionally requires a sibling run manifest bound to report bytes/path/revision/status.
- Task authority: no Taskmaster status writes.
