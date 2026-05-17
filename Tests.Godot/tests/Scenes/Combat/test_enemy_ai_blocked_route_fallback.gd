extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"

func _normalize_target_id(raw_value: Variant) -> String:
	var text := str(raw_value)
	var sanitized := ""
	var skipping_ansi := false
	for index in range(text.length()):
		var code := text.unicode_at(index)
		if not skipping_ansi and code == 27:
			skipping_ansi = true
			continue
		if skipping_ansi:
			if code == 109:
				skipping_ansi = false
			continue
		sanitized += String.chr(code)
	return sanitized.strip_edges()

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

# acceptance: ACC:T6.1
# acceptance: ACC:T47.2
# acceptance: ACC:T47.5
# acceptance: ACC:T51.5
# acceptance: ACC:T51.10
# acceptance: ACC:T51.11
func test_blocked_route_uses_fallback_even_with_higher_priority_targets() -> void:
	var probe := _new_probe()
	var candidates := [
		{
			"id": "hero",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": false
		},
		{
			"id": "wall_far",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 10,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "wall_near",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 2,
			"blocks_route_to_higher_priority": true
		}
	]
	var action: Dictionary = probe.call("SelectTarget", candidates)

	assert_bool(action.get("is_fallback_attack", false) == true).is_true()
	assert_str(str(action.get("target_id", ""))).is_equal("wall_near")

# acceptance: ACC:T6.11
# acceptance: ACC:T51.10
func test_blocked_high_priority_does_not_idle_or_switch_to_unrelated_targets() -> void:
	var probe := _new_probe()
	var candidates := [
		{
			"id": "hero",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": false
		},
		{
			"id": "gate_blocker",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 3,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "neutral_crate",
			"class": "decoration",
			"reachable": true,
			"blocked": false,
			"path_points": 3,
			"distance": 1,
			"blocks_route_to_higher_priority": false
		}
	]
	var action: Dictionary = probe.call("SelectTarget", candidates)

	assert_bool(action.get("is_fallback_attack", false) == true).is_true()
	assert_str(str(action.get("target_id", ""))).is_equal("gate_blocker")
	assert_str(str(action.get("target_id", ""))).is_not_equal("neutral_crate")

# acceptance: ACC:T6.6
# acceptance: ACC:T51.5
# acceptance: ACC:T51.6
# acceptance: ACC:T51.11
func test_fallback_selects_nearest_blocker_and_emits_attack_event_to_that_target() -> void:
	var probe := _new_probe()
	var candidates := [
		{
			"id": "hero_blocked",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": false
		},
		{
			"id": "barricade_5",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 5,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "barricade_1",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "barricade_3",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 3,
			"blocks_route_to_higher_priority": true
		}
	]
	var action: Dictionary = probe.call("SelectTarget", candidates)

	var selected_target := _normalize_target_id(action.get("target_id", ""))
	var emitted_target := _normalize_target_id(action.get("attack_event_target_id", ""))
	assert_str(selected_target).is_equal("barricade_1")
	assert_str(emitted_target).is_equal("barricade_1")

# acceptance: ACC:T51.11
func test_blocked_route_selection_is_deterministic_across_repeated_runs_with_identical_inputs() -> void:
	var probe := _new_probe()
	var candidates := [
		{
			"id": "hero_blocked",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": false
		},
		{
			"id": "barricade_1",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 1,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "barricade_5",
			"class": "blocking_structure",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 5,
			"blocks_route_to_higher_priority": true
		}
	]
	var first_target := ""
	var first_emitted := ""

	for index in range(5):
		var action: Dictionary = probe.call("SelectTarget", candidates)
		var selected_target := _normalize_target_id(action.get("target_id", ""))
		var emitted_target := _normalize_target_id(action.get("attack_event_target_id", ""))
		if index == 0:
			first_target = selected_target
			first_emitted = emitted_target
		assert_str(selected_target).is_equal(first_target)
		assert_str(emitted_target).is_equal(first_emitted)

# acceptance: ACC:T51.5
# acceptance: ACC:T51.10
# acceptance: ACC:T51.11
func test_clustered_targets_show_observable_single_target_vs_aoe_distribution_delta() -> void:
	var probe := _new_probe()
	var targets := [
		{"id": "enemy_near", "team_id": 2, "distance": 0.0, "damageable": true},
		{"id": "enemy_mid", "team_id": 2, "distance": 2.0, "damageable": true},
		{"id": "enemy_far", "team_id": 2, "distance": 4.0, "damageable": true},
		{"id": "friendly", "team_id": 1, "distance": 1.0, "damageable": true}
	]

	var single_target: Dictionary = probe.call("SimulateAreaDamageRuntime", targets, false, 1, 20, 3.0, 0.25, 0.4, 5, 12)
	var aoe: Dictionary = probe.call("SimulateAreaDamageRuntime", targets, true, 1, 20, 3.0, 0.25, 0.4, 5, 12)
	var aoe_repeat: Dictionary = probe.call("SimulateAreaDamageRuntime", targets, true, 1, 20, 3.0, 0.25, 0.4, 5, 12)

	assert_int(int(single_target.get("committed_count", 0))).is_equal(1)
	assert_int(int(single_target.get("total_damage", -1))).is_equal(20)

	assert_int(int(aoe.get("committed_count", 0))).is_equal(2)
	assert_int(int(aoe.get("total_damage", -1))).is_equal(22)
	assert_int(int(aoe.get("out_of_radius_count", -1))).is_equal(1)

	assert_dict(aoe_repeat).is_equal(aoe)
