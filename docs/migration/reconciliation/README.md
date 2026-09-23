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

| #195 | Semantic delivery topology structural layer | Reconciled target-natively: semantic topology schemas/core/HTTP/UI are aligned while lastking BattleMap Project Health defaults remain authoritative. |
| #196 | Generated Knowledge publication | `derived_regenerate`; no source publication payload is copied. |
| #197 | ADR-0038 semantic topology acceptance | Reconciled with the target ADR and semantic topology regression coverage. |
| #199 | Semantic topology structural closure | Reconciled into topology/catalog/navigation core while preserving lastking browser and Project Health target regressions. |
| #200 | Generated Knowledge publication | `derived_regenerate`; no source publication payload is copied. |
| #201 | Chapter 3 semantic conservation and Knowledge refresh | Reconciled into Source Block Ledger, Semantic Projection A, task generation, guarded refresh, and hard regressions. |
| #203 | Chapter 3 reconciliation hardening | Reconciled into triplet attestation, semantic conservation, refresh and workflow skill generation. |
| #206 | Final Chapter 3 reconciliation gaps | Reconciled into exact JSON provenance, guarded lifecycle and semantic regressions. |
| #208 | Downstream Chapter 4/5/6/7 + Review reconciliation | Reconciled into Chapter 5 semantic authority, Chapter 6 local Knowledge boundary, Chapter 7 lineage and Review evidence gates. |
| #211 | Final Chapter 5 authority/freshness closure | Reconciled into explicit semantic verdicts, freshness fingerprinting, authority review and guarded Chapter 5 execution. |
| #213 | Chapter 3 semantic task grouping quality | Reconciled into CJK-aware grouping/title generation and advisory multi-Capability evidence. |
| #215 | Unicode-aware Chapter 3 quality audit | Reconciled into task-intent quality audit and task-generation regressions. |
| #216 | Generated Knowledge publication | `derived_regenerate`; final source reconciliation boundary for this migration. |
| #217 | Compatibility and semantic-proof closure after #216 | Reconciled into Chapter 3/5/6 and Review guards; target business data remains unchanged. |
| #218 | Regression closure for #217 | Reconciled into hard regressions and Chapter 6 orchestration coverage. |
| #219 | Workflow optimization v2 | Reconciled target-natively: task-scoped context, single reviewer + lenses, P1 fix-through floor, debt-only residuals, verification surfaces, causal RED, and compact recovery. |
| #221 | Workflow-v2 audit gap closure | Reconciled structured review contracts, target-bound RED/human evidence, stable finding IDs, and stricter review recovery semantics. |
| #222 | Evidence-validation hardening | Reconciled fresh direct-RED evidence, execution-evidence validation, debt synchronization guards, and review timeout isolation. |
| #223 | Final workflow-v2 P1 closure | Reconciled single-reviewer legacy compatibility and MVG critical/full journey evidence binding while preserving lastking's BattleMap-native MVG scope. |
| #224 | Source workflow-v2 closure evidence | `business_only_drop` for the source execution-plan evidence; newrouge run IDs/revisions are not copied as lastking validation. |
| #227 | Passed task-local Acceptance evidence | Reconciled pass-only TRX/JUnit evidence semantics and classified-obligation execution gates. |

Unmerged generated Knowledge PRs #220, #225, and #226 are outside the migration inventory; no source publication payload is copied.

## Maintenance

For the next bounded upstream migration, freeze the source repository, PR, merge commit, and full changed-file list before implementation is declared complete. Verify that inventory against the merged source PR with `--verify-source-github`, add a new JSON manifest in this directory, and keep the offline `--require-manifests` hard gate green.
