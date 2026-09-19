# Cross-Repository Migration Reconciliation

This directory records frozen source-PR inventories for reusable migrations from `skyoxu/newrouge` into `skyoxu/lastking`.

## Contract

- Every source changed file is classified exactly once.
- `copy_exact` means the target file must retain the frozen source Git blob identity.
- `adapt_target_native` and `already_present` require target-owned validation evidence.
- `derived_regenerate` means source generated state must never be copied; rebuild it from lastking inputs.
- `business_only_drop` means the source change is intentionally outside target migration scope.
- `protocol_name_retain` is reserved for compatible upstream protocol/schema identifiers that intentionally keep their upstream namespace.
- Manifests use `repo.cross-repo-migration-reconciliation.v1`.
- Run `py -3 scripts/python/check_cross_repo_migration.py --require-manifests`; it is part of the hard gate bundle.
- During migration creation/review, independently verify the frozen source PR with `--verify-source-github`; ordinary hard gates remain deterministic/offline.

## Reconciled Source Evolution

| Source PR | Source role | lastking disposition |
| --- | --- | --- |
| #172 | Knowledge publication freshness lifecycle | Migrated via lastking #103; shared exact files and target-local locator evolution are classified in `newrouge-172.json`. |
| #173 | Generated Knowledge publication | `derived_regenerate`; target publication is repository-derived, with lastking #104 representing the corresponding publication cycle. |
| #174 | Godot scene knowledge, Project Health and Impact evolution | Migrated via lastking #105; source implementation artifacts/generated state are excluded and the lightweight target renderer is explicit. |
| #175 | Generated Knowledge publication | `derived_regenerate`; lastking #106 is the target-owned publication cycle. |
| #176 | Runtime snapshot hardening + generated Knowledge update | Runtime hardening migrated through lastking #107; source generated state/spec evidence is not copied. |
| #177 | Generated Knowledge publication | `derived_regenerate`; no source publication payload is copied. |
| #178 | Scene-composition synchronization | Migrated via lastking #108 with target-agnostic browser fixtures; source implementation notes/generated catalog are excluded. |
| #179 | Generated Knowledge publication | `derived_regenerate`; no source publication payload is copied. |
| #180 | Bounded MVG integration acceptance | Migrated via lastking #109 with a BattleMap-native pilot and frozen 25/25 source-file classification. |
| #181 | Generated Knowledge publication after #180 | `derived_regenerate`; after #109 lands, lastking's publication workflow must rebuild generated state from target inputs. |
| #182 | Runtime snapshot and evidence-integrity hardening | Migrated through lastking #109: workspace BattleMap execution, GdUnit4 runtime-bin preservation, cross-report MVG identity deduplication, and Impact run-manifest rebinding. |
| #183 | Project Health hard-gate follow-up | Reconciled through lastking #109: event-route reachability and CI serve-test isolation were already repaired target-side; generic handoff guidance is exact and target-native Project Health suites stay protected. |
| #184 | Repository-neutral migration reconciliation protocol | Adopted through lastking #109: generic checker/tests/protocol are exact; target hard gate remains stricter with `--require-manifests`. |
| #185 | Generated Knowledge publication | `derived_regenerate`; no source publication payload is copied. |
| #186 | Generated Knowledge publication | `derived_regenerate`; rebuild from lastking inputs with the target publication command. |
| #187 | MVG coverage tiers and full-scope boundary | Reconciled on this alignment branch: generic coverage validation/tests are source-exact; BattleMap pilot declares its real Task 12 blocker; newrouge M1/Reward business inventories are not copied. |
| #190 | Generated Knowledge publication after #187 | `derived_regenerate`; no source publication payload is copied. |
| #191 | Knowledge-link full rebuild replacement | Reconciled on this alignment branch: restores the CLI-referenced generator, replaces stale catalog entries on full rebuild, and hard-gates the regression. |
| #192 | Canonical target PR uniqueness guard | Reconciled source-exact for the repository-neutral protocol, checker, and tests. |
| #193 | Project Health scene-graph browser isolation | Reconciled target-natively: delayed graph tests use synthetic fixtures while lastking's route-tree closure regression is retained. |
| #194 | Generated Knowledge publication after #192 | `derived_regenerate`; no source publication payload is copied. |

## Maintenance

For the next bounded upstream migration, freeze the source repository, PR, merge commit, and full changed-file list before implementation is declared complete. Verify that inventory against the merged source PR with `--verify-source-github`, add a new JSON manifest in this directory, and keep the offline `--require-manifests` hard gate green.
