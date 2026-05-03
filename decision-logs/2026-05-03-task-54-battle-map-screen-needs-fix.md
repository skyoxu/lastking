# Task 54 Residual Needs Fix

- Title: Task 54 residual needs-fix after battle map closure
- Date: 2026-05-03
- Status: accepted
- Supersedes: n/a: no earlier decision log captured this specific residual gap.
- Superseded by: n/a: no later decision has replaced this residual assessment yet.
- Branch: testplay
- Git Head: 914073a607f5cd1585ff2749ae5c936a82df2886
- Why now: Task 54 was accepted for its scoped route and playable-loop outcome, but one runtime consistency gap remained and needed an explicit recovery entry.
- Context: The user required a visible player-facing battle map experience instead of an abstract combat summary. Task 54 delivered the route to the battle map screen, interactive loop controls, and integration evidence, but map-side moving tokens and bridge-side combat entities still lived on parallel runtime surfaces.
- Decision: Keep Task 54 marked `done` for the scoped acceptance, while recording the remaining runtime unification work as a separate needs-fix item rather than silently expanding Task 54.
- Consequences: The current build remains demonstrable and playable for the accepted Task 54 scope, but a future task must unify map movement, combat entities, and synchronization assertions before the runtime can be treated as fully coherent.
- Recovery impact: Any future work that addresses this gap should start from the recorded residual here, create a dedicated execution plan, and attach deterministic synchronization evidence.
- Validation: Evidence for the scoped acceptance exists in `Game.Godot/Scenes/Screens/BattleMapScreen.tscn`, `Game.Godot/Scripts/Screens/BattleMapScreen.gd`, `Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs`, and `Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd`.
- Related ADRs: `ADR-0018`, `ADR-0021`, `ADR-0022`
- Related execution plans: `execution-plans/2026-05-03-task-54-battle-map-screen-followup.md`
- Related task id(s): `T54`
- Related run id: n/a: this decision was recorded as a recovery artifact, not from a dedicated CI run.
- Related latest.json: n/a: no task-specific review pipeline artifact was generated when this decision was first logged.
- Related pipeline artifacts: n/a: no dedicated pipeline artifact bundle was created for this residual decision.

