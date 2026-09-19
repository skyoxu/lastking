# MVG coverage-tier and upstream reconciliation alignment

- Title: Align lastking with newrouge #187-#194 reusable evolution
- Date: 2026-09-19
- Status: Implemented on alignment branch; target protected validation pending
- Supersedes: None
- Superseded by: None
- Branch: sync-newrouge-187-194-20260919
- Git Head: 0e2b24e018e0a7713bd460b5a976639652aee7e4
- Why now: newrouge added explicit MVG coverage tiers, fixed full Knowledge-link rebuild replacement, added canonical target-PR uniqueness verification, and isolated Project Health browser graph tests after lastking's previous reconciliation checkpoint at source #185.
- Context: lastking already carries a target-native BattleMap MVG pilot, repository-neutral reconciliation tooling, Project Health, and repository-derived Knowledge publication. The new source behavior must be adopted without copying newrouge Reward/M1 task identities or generated Knowledge outputs.
- Decision: Adopt the repository-neutral MVG coverage contract and its regression tests, preserve the BattleMap input challenge and target paths, declare the existing pilot's real Taskmaster blocker, restore the missing Knowledge-link generator and full-rebuild regression, adopt canonical migration-PR verification, isolate delayed browser graph tests with synthetic data, and reconcile generated source publications as derived state.
- Consequences: MVG evidence now states pilot/critical/full scope explicitly; BattleMap pilot remains executable but cannot be misrepresented as critical/full while Task 12 is pending. Full Knowledge-link rebuilds replace stale catalog entries. Migration tooling can detect duplicate canonical target PR ownership. Project Health delayed-response browser tests no longer depend on live repository graph contents.
- Recovery impact: Resume from the matching execution plan. Do not copy source M1/Reward manifests or generated Knowledge publication bytes. Keep Taskmaster as status authority and rerun target-owned Knowledge publication from lastking inputs.
- Validation: Generic source-exact files are blob-aligned where classified copy_exact; target-native adaptations are covered by MVG, Knowledge-link, migration, and Project Health regression paths. Protected target workflow results are pending for this branch.
- Related ADRs: `docs/adr/ADR-0037-mvg-integration-evidence.md`
- Related execution plans: `execution-plans/2026-09-19-newrouge-187-194-alignment.md`
- Related task id(s): Tasks 12 and 54 remain the BattleMap pilot task scope; Task 12 is pending and Task 54 is done. No Taskmaster status is changed.
- Related run id: Pending target PR checks.
- Related latest.json: N/A; this reconciliation does not replace Chapter 6 latest.json authority.
- Related pipeline artifacts: `logs/ci/mvg-acceptance/**`, `logs/ci/**/gate-bundle/**`

- Source evolution: newrouge #186/#190/#194 are generated Knowledge publications; #187 adds MVG coverage tiers; #191 fixes Knowledge-link full rebuilds; #192 adds canonical target-PR uniqueness verification; #193 isolates Project Health scene-graph browser coverage.
- Preservation boundary: keep lastking BattleMap business paths and challenge marker `MVG_BATTLEMAP_INPUT_DID_NOT_SPAWN_WAVE`; do not copy newrouge M1/Reward task IDs, scenes, or generated Knowledge output.
- Task authority: no Taskmaster status writes.
