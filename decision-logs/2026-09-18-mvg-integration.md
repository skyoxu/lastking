# MVG integration evidence alignment

- Title: Align lastking with post-reconciliation MVG integration and conservative impact recommendation
- Date: 2026-09-18
- Status: Implemented; validated implementation checkpoint passed all automatic PR checks
- Supersedes: None
- Superseded by: None
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: 1b2a01d201f960d072ba9d9aa259bc11e1f430d5
- Why now: newrouge evolved after the prior reconciliation with PR #180's bounded MVG integration acceptance and conservative regression recommendation.
- Context: The formal KCP Impact core was already aligned, but lastking did not yet have the new MVG manifest/execution/evidence layer or target-native pilot.
- Decision: Port the generic MVG manifest/execution/recommendation infrastructure, replace the source RewardOffer pilot with a target-native BattleMap/resource pilot, and preserve all target-specific Impact configuration and business defaults.
- Consequences: lastking gains bounded domain/scene/input evidence and conservative related-first recommendations without introducing a second task-state authority or claiming whole-MVG coverage.
- Recovery impact: Use the matching MVG summary and source_revision/snapshot_digest as evidence; planned/recommended output is never equivalent to runtime_verified evidence.
- Validation: On implementation checkpoint 1b2a01d201f960d072ba9d9aa259bc11e1f430d5, MVG Integration Pilot run 35345323512 passed with runtime_verified=true; domain 3/3, scene-method 2/2 and engine-input 1/1 passed, and the disconnected-input challenge was caught by the designated assertion. Windows Quality Gate run 35345323570 and Windows Smoke run 35345323538 also passed.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related execution plans: `execution-plans/2026-09-18-mvg-integration-alignment.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; no Taskmaster status is changed by this decision.
- Related run id: MVG 35345323512; Quality 35345323570; Smoke 35345323538.
- Related latest.json: N/A because MVG acceptance is separate from the Chapter 6 latest.json authority.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Source evolution: skyoxu/newrouge PR #180 / merge commit 9f75081ae80e2b8eb1c222eda3ad9def93ef09b5
- Target baseline: lastking main 0c5f642ff675c4119f2f46db39e5833d7ea2617a
- Preservation boundary: Do not copy source Task115/Reward business state, source generated Knowledge publication outputs, or source-specific runtime defaults.
- Impact boundary: KCP Impact core is already aligned; this change adds revision-bound conservative MVG regression recommendation without treating it as a call graph or as test-exclusion authority.
- Task authority: no Taskmaster status writes.
