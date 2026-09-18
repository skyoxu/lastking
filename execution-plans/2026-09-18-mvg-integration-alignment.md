# MVG integration alignment

- Title: Post-#108 MVG and Impact recommendation alignment
- Status: In progress
- Branch: sync-mvg-impact-evolution-20260918
- Git Head: 806d3355119a97b835609eabb815dec42d33e0ab
- Goal: Align lastking with newrouge PR #180's reusable MVG testing capability while preserving target business semantics.
- Scope: MVG manifest/runner, evidence validation, revision-bound recommendation, BattleMap pilot, optional target mutation probe, CI wiring and documentation.
- Current step: Repair recovery-document schema fields and re-run PR checks on the resulting head.
- Last completed step: MVG Integration Pilot run 35344431894 passed on the first PR implementation checkpoint, including the disconnected-input challenge.
- Stop-loss: Do not count missing/skipped reports, compile failure or timeout as acceptance or mutation detection; do not weaken existing quality gates.
- Next action: Require the new PR head to pass MVG Integration Pilot, Windows Quality Gate and Windows Smoke; keep build-test-export as the repository's existing manual workflow.
- Recovery command: `py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan`
- Open questions: Full-MVG manifest expansion remains future scope; the current BattleMap pilot is intentionally bounded.
- Exit criteria: MVG unit tests pass, committed BattleMap pilot is runtime_verified, disconnected-input challenge is detected by the designated assertion, and automatic protected checks remain green.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related decision logs: `decision-logs/2026-09-18-mvg-integration.md`
- Related task id(s): Tasks 12 and 54 are referenced by the pilot; this infrastructure does not write task status.
- Related run id: GitHub Actions MVG run 35344431894 is the first implementation-checkpoint evidence; the latest PR-head run remains authoritative.
- Related latest.json: N/A because this MVG infrastructure does not create or replace Chapter 6 latest.json.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/mvg-mutation/**`

- Out of scope: source RewardOffer business implementation, Task115 data, source generated Knowledge state, wholesale Impact config replacement.
