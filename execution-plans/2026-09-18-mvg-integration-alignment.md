# MVG integration alignment

- Title: Post-#108 MVG and Impact recommendation alignment
- Status: In progress
- Branch: sync-mvg-impact-evolution-20260918
- Goal: Align lastking with newrouge PR #180's reusable MVG testing capability while preserving target business semantics.
- Scope: MVG manifest/runner, evidence validation, revision-bound recommendation, BattleMap pilot, optional target mutation probe, CI wiring and documentation.
- Out of scope: source RewardOffer business implementation, Task115 data, source generated Knowledge state, wholesale Impact config replacement.
- Current step: Open PR and validate Windows MVG + protected repository checks.
- Stop-loss: Do not count missing/skipped reports, compile failure or timeout as acceptance or mutation detection; do not weaken existing quality gates.
- Recovery command: `py -3 scripts/python/dev_cli.py run-mvg-acceptance --mode plan`
- Exit criteria: MVG unit tests pass, committed BattleMap pilot is runtime_verified, disconnected-input challenge is detected by the designated assertion, and protected checks remain green.
