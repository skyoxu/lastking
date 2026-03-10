extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const FIXED_SEED: int = 1337
const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

func _select_target_with_probe(probe: Node, candidates: Array) -> Dictionary:
	return probe.call("SelectTarget", candidates)

func _build_determinism_trace(probe: Node, candidates: Array, fixed_seed: int, steps: int) -> Array:
	return probe.call("BuildDeterminismTrace", candidates, fixed_seed, steps)

func _decision_trace(probe: Node, candidates: Array, fixed_seed: int, steps: int) -> Array:
	return _build_determinism_trace(probe, candidates, fixed_seed, steps)

# acceptance: ACC:T6.17
# acceptance: ACC:T6.3
func test_target_selection_is_deterministic_for_equal_cost_ties_with_fixed_seed() -> void:
	var probe := _new_probe()
	var candidates: Array = [
		{"id": "alpha", "priority": 10, "blocked": false},
		{"id": "bravo", "priority": 10, "blocked": false},
		{"id": "charlie", "priority": 8, "blocked": false}
	]

	var first_trace: Array = _decision_trace(probe, candidates, FIXED_SEED, 8)
	var second_trace: Array = _decision_trace(probe, candidates, FIXED_SEED, 8)
	assert_str(JSON.stringify(second_trace)).is_equal(JSON.stringify(first_trace))
	assert_bool(String(first_trace[0]) == "alpha" or String(first_trace[0]) == "bravo").is_true()

	var changed_seed_trace: Array = _decision_trace(probe, candidates, FIXED_SEED + 1, 8)
	assert_str(JSON.stringify(changed_seed_trace)).is_not_equal(JSON.stringify(first_trace))

func test_target_selection_prefers_lower_path_cost_before_tie_break() -> void:
	var probe := _new_probe()
	var candidates: Array = [
		{"id": "near", "class": "unit", "reachable": true, "blocked": false, "path_points": 5, "distance": 2, "blocks_route_to_higher_priority": false},
		{"id": "far", "class": "unit", "reachable": true, "blocked": false, "path_points": 5, "distance": 5, "blocks_route_to_higher_priority": false},
		{"id": "blocked", "class": "unit", "reachable": false, "blocked": true, "path_points": 0, "distance": 1, "blocks_route_to_higher_priority": false}
	]

	var decision: Dictionary = _select_target_with_probe(probe, candidates)
	assert_str(String(decision.get("target_id", ""))).is_equal("near")
	assert_bool(bool(decision.get("is_fallback_attack", false))).is_false()

func test_blocked_targets_emit_deterministic_fallback_trace() -> void:
	var probe := _new_probe()
	var candidates: Array = [
		{"id": "alpha", "priority": 10, "blocked": true},
		{"id": "bravo", "priority": 10, "blocked": true}
	]

	var first_trace: Array = _decision_trace(probe, candidates, FIXED_SEED, 6)
	var second_trace: Array = _decision_trace(probe, candidates, FIXED_SEED, 6)
	assert_str(JSON.stringify(second_trace)).is_equal(JSON.stringify(first_trace))
	assert_bool(String(first_trace[0]).begins_with("fallback:")).is_true()
