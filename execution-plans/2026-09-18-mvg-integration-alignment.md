# MVG integration alignment

- Title: Post-#108 MVG and Impact recommendation alignment
- Status: In validation after newrouge #182 incremental hardening
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: 10b988acea91b0a38e3d0c4d67a1955c1c50eb0a
- Goal: Align lastking with newrouge #180 and #182 reusable MVG/Impact testing capability while preserving target business semantics.
- Scope: MVG manifest/runner, evidence validation, revision-bound recommendation, BattleMap commit/workspace pilot, runtime snapshot integrity, Impact run-manifest handoff binding, migration reconciliation, CI wiring and documentation.
- Current step: Wait for the protected checks on the #182-aligned checkpoint, then close recovery state only if MVG, Windows Quality Gate and Windows Smoke are all green.
- Last completed step: Migrated newrouge #182: GdUnit4 runtime-bin workspace preservation, cross-report MVG identity deduplication, Impact report/run-manifest rebinding, shared regression tests, and a BattleMap-native workspace MVG CI run. Also repaired three regressions exposed when Project Health/reconciliation tests were promoted into the hard gate.
- Stop-loss: Do not count missing/skipped reports, compile failure or timeout as acceptance or mutation detection; do not weaken existing quality gates or copy source generated Knowledge state.
- Next action: Inspect target runs MVG 35367820494, Quality 35367820237 and Smoke 35367820478. If any fail, repair the root cause on this branch; if all pass, record the validated checkpoint and prepare PR #109 for normal review/merge.
- Recovery command: `py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan`
- Open questions: Full-MVG manifest expansion remains future scope. The current BattleMap pilot is intentionally bounded. The reconciliation checker validates committed inventories and exact-copy drift but does not independently refetch the remote source PR on every hard-gate run.
- Exit criteria: MVG failure-path tests pass; committed and workspace BattleMap pilots execute with valid evidence; disconnected-input challenge is detected; migration reconciliation passes; Impact handoff requires valid sibling run manifest; protected Quality and Smoke checks are green.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related decision logs: `decision-logs/2026-09-18-mvg-integration.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; this infrastructure does not write task status.
- Related run id: Pending target checkpoint runs: MVG 35367820494; Quality 35367820237; Smoke 35367820478.
- Related latest.json: N/A because this MVG infrastructure does not create or replace Chapter 6 latest.json.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Out of scope: source RewardOffer business implementation, Task115 data, source generated Knowledge state, wholesale Impact config replacement.
