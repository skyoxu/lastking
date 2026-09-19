# MVG integration alignment

- Title: Post-#108 MVG and Impact recommendation alignment
- Status: In validation after newrouge #183-#185 reconciliation
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: e4281d6b0e3e13ddaaae32ff45fc54cd1c9a8195
- Goal: Align lastking with newrouge #180/#182 MVG+Impact evolution and #183/#184 correctness/governance follow-ups while preserving target business semantics.
- Scope: MVG manifest/runner, evidence validation, revision-bound recommendation, BattleMap commit/workspace pilot, runtime snapshot integrity, Impact run-manifest handoff binding, repository-neutral migration reconciliation, target hard-gate enforcement, CI wiring and documentation.
- Current step: Wait for the protected checks on the #183-#185-aligned checkpoint, then close recovery state only if MVG, Windows Quality Gate and Windows Smoke are all green.
- Last completed step: Reconciled newrouge #183/#184 and generated publication #185. Generic checker/tests/protocol are now source-exact; historical manifests #172-#182 use the shared `repo.cross-repo-migration-reconciliation.v1` schema; lastking retains a stricter offline `--require-manifests` hard gate and target-native Project Health test organization.
- Stop-loss: Do not count missing/skipped reports, compile failure or timeout as acceptance or mutation detection; do not weaken existing quality gates; do not copy source generated Knowledge state; do not treat embedded manifest inventories as authoritative without source PR verification during migration creation/review.
- Next action: Inspect target runs MVG 35422124767, Quality 35422124746 and Smoke 35422124730. If any fail, repair the root cause on this branch; if all pass, record the validated checkpoint and prepare PR #109 for normal review/merge.
- Recovery command: `py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan`
- Open questions: Full-MVG manifest expansion remains future scope. The current BattleMap pilot is intentionally bounded. Remote source verification is an explicit migration-creation/review step, while ordinary hard gates remain deterministic and offline.
- Exit criteria: MVG failure-path tests pass; committed and workspace BattleMap pilots execute with valid evidence; disconnected-input challenge is detected; repository-neutral reconciliation passes with mandatory manifests; Impact handoff requires valid sibling run manifest; protected Quality and Smoke checks are green.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related decision logs: `decision-logs/2026-09-18-mvg-integration.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; this infrastructure does not write task status.
- Related run id: Pending target checkpoint runs: MVG 35422124767; Quality 35422124746; Smoke 35422124730.
- Related latest.json: N/A because this MVG infrastructure does not create or replace Chapter 6 latest.json.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Out of scope: source RewardOffer business implementation, Task115 data, source generated Knowledge state, wholesale Impact config replacement.
