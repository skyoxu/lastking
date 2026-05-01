extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

# acceptance: ACC:T6.18
# acceptance: ACC:T47.2
func test_enemy_switches_to_next_reachable_target_when_current_becomes_unreachable() -> void:
	var probe := _new_probe()
	var candidates := [
		{"id": "alpha", "priority": 100, "reachable": false, "valid": true},
		{"id": "beta", "priority": 80, "reachable": true, "valid": true},
		{"id": "gamma", "priority": 60, "reachable": true, "valid": true}
	]

	var next_target: Dictionary = probe.call("SelectNextReachableTarget", candidates, "alpha")

	assert_str(str(next_target.get("target_id", ""))).is_equal("beta")
	assert_bool(bool(next_target.get("is_reachable", false))).is_true()

# acceptance: ACC:T47.7
func test_returns_empty_when_no_reachable_valid_targets_exist_and_does_not_traverse_unreachable_space() -> void:
	var probe := _new_probe()
	var candidates := [
		{"id": "alpha", "priority": 100, "reachable": false, "valid": true},
		{"id": "beta", "priority": 80, "reachable": false, "valid": true},
		{"id": "gamma", "priority": 60, "reachable": true, "valid": false}
	]

	var nav_probe: Dictionary = probe.call("ProbeNavigationPath", Vector2(0, 0), Vector2(99, 99))
	assert_bool(bool(nav_probe.get("navigation_api_used", false))).is_true()
	assert_int(int(nav_probe.get("path_points", 0))).is_equal(0)

	var next_target: Dictionary = probe.call("SelectNextReachableTarget", candidates, "alpha")
	var replay_target: Dictionary = probe.call("SelectNextReachableTarget", candidates, "beta")

	assert_str(str(next_target.get("target_id", ""))).is_equal("")
	assert_bool(bool(next_target.get("is_reachable", false))).is_false()
	assert_str(str(replay_target.get("target_id", ""))).is_equal("")
	assert_bool(bool(replay_target.get("is_reachable", false))).is_false()
