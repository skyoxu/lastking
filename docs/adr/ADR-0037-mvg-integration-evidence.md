# ADR-0037: Bounded MVG Integration Evidence

- Status: Proposed
- Date: 2026-09-18
- Context: Per-task commits and existing gates do not alone prove that an integrated MVG behaves correctly across task boundaries.
- Decision:
  - Add an opt-in manifest connecting existing Taskmaster IDs, handoff ownership, contract references and integration tests; Taskmaster remains task-state authority.
  - Execute declared tests against an isolated, identified commit or workspace snapshot.
  - Require positive nonempty reports, exact class/suite identity, consistent counts, process success and no skipped tests for runtime verification.
  - Distinguish domain-integration, scene-method and engine-input evidence.
  - Use explicit path mappings only for conservative regression recommendations. Unknown/unmapped impact never authorizes excluding required tests; formal KCP Impact remains authoritative for its own scope.
  - Keep seeded mutation optional and target-native.
- Consequences: Integration gaps gain explicit ownership and executable evidence with bounded runtime cost. Manifest scope and behavioral assertions still require review; this does not change branch protection or task status.
- Target pilot: `docs/testing/mvg/battlemap-pilot.json`.
- References: `docs/adr/ADR-0025-godot-test-strategy.md`, `docs/adr/ADR-0035-repository-knowledge-control-plane.md`, `docs/workflows/mvg-integration-acceptance.md`.
