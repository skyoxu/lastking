# MVG integration alignment

- Title: Post-#108 MVG and Impact recommendation alignment
- Status: Completed on sync branch; validated implementation checkpoint is green
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: 1b2a01d201f960d072ba9d9aa259bc11e1f430d5
- Goal: Align lastking with newrouge PR #180's reusable MVG testing capability while preserving target business semantics.
- Scope: MVG manifest/runner, evidence validation, revision-bound recommendation, BattleMap pilot, optional target mutation probe, CI wiring and documentation.
- Current step: No implementation repair remains. Recovery documents are being closed to the validated checkpoint so future resume does not report stale work.
- Last completed step: On checkpoint 1b2a01d201f960d072ba9d9aa259bc11e1f430d5, MVG Integration Pilot run 35345323512 passed with runtime_verified=true; Windows Quality Gate run 35345323570 and Windows Smoke run 35345323538 also passed.
- Stop-loss: Do not count missing/skipped reports, compile failure or timeout as acceptance or mutation detection; do not weaken existing quality gates.
- Next action: After documentation-only closure CI remains green, PR #109 is ready for normal review/merge. Full-MVG manifest expansion remains separate future scope.
- Recovery command: `py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan`
- Open questions: Full-MVG manifest expansion remains future scope; the current BattleMap pilot is intentionally bounded.
- Exit criteria: Met on validated implementation checkpoint: MVG unit tests pass, committed BattleMap pilot is runtime_verified, disconnected-input challenge is detected by the designated assertion, and automatic protected checks are green.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related decision logs: `decision-logs/2026-09-18-mvg-integration.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; this infrastructure does not write task status.
- Related run id: MVG 35345323512; Quality 35345323570; Smoke 35345323538.
- Related latest.json: N/A because this MVG infrastructure does not create or replace Chapter 6 latest.json.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Out of scope: source RewardOffer business implementation, Task115 data, source generated Knowledge state, wholesale Impact config replacement.
