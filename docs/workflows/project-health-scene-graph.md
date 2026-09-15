# Project Health Godot scene evidence

This repository aligns the post-reconciliation Godot scene-knowledge evolution from the template while preserving lastking business defaults.

## Local scene graph

Run the normal Project Health scan, then open `/knowledge/scenes` on the loopback Project Health server. The graph starts from `project.godot` main scene and distinguishes:

- `effective`: a statically proven PackedScene, explicit scene switch/instantiation, event route, or controller route.
- `possible`: a scene-path literal or other static reference without a proven trigger.
- `unreachable-candidate`: a scanned scene not proven reachable from the configured main scene. This is evidence uncertainty, not a deletion recommendation.

`/knowledge/scenes/unreachable` lists unconfirmed scenes separately. Image previews remain revision-bound to the scanned main snapshot.

## Chapter 6 capture

`chapter6_knowledge.py` now emits `docs/knowledge/generated/chapter6-task-<id>-elements.json` plus a non-blocking documentation-gap report after the normal scan/link/catalog stages. Changed Godot resources without a task binding are retained as unmapped evidence instead of being silently dropped.

The lastking defaults remain Task 54 / BattleMap and the local query aliases configured in `project_health_knowledge.py`; source-repository Task 115 / Reward defaults are intentionally not imported.

## Generated state

Do not copy `docs/knowledge/catalog/godot-elements.json` or Knowledge publication indexes from another repository. They are derived from this repository and must be generated from lastking's own main revision.
