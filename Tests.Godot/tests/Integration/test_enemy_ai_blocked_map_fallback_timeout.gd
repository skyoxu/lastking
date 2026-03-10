extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"
const DEFAULT_ENEMY_COUNT := 3
const DEFAULT_TIMEOUT_TICKS := 5

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

func _add_navigation_region(outline: PackedVector2Array) -> NavigationRegion2D:
	var region := NavigationRegion2D.new()
	var polygon := NavigationPolygon.new()
	polygon.add_outline(outline)
	polygon.make_polygons_from_outlines()
	region.navigation_polygon = polygon
	add_child(region)
	return region

func _count_diagnostics_for_enemy(diagnostics: Array, enemy_index: int, token: String) -> int:
	var count := 0
	var prefix := "enemy_%d:%s" % [enemy_index, token]
	for item in diagnostics:
		var message := String(item)
		if message.begins_with(prefix):
			count += 1
	return count

# acceptance: ACC:T6.19
func test_blocked_map_fallback_attack_completes_within_timeout_and_emits_diagnostics() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(8, 0), Vector2(8, 8), Vector2(0, 8)]))
	var result: Dictionary = probe.call(
		"SimulateBlockedMapFallbackWithNavigation",
		DEFAULT_ENEMY_COUNT,
		DEFAULT_TIMEOUT_TICKS,
		1,
		Vector2(1, 1),
		Vector2(6, 6)
	)
	var diagnostics: Array = result.get("diagnostics", [])

	assert_int(int(result.get("enemy_count", 0))).is_equal(DEFAULT_ENEMY_COUNT)
	assert_int(int(result.get("enemies_reached_fallback_attack", 0))).is_equal(DEFAULT_ENEMY_COUNT)
	assert_int(int(result.get("deadlock_count", 0))).is_equal(0)
	assert_bool(bool(result.get("navigation_api_used", false))).is_true()
	assert_int(diagnostics.size()).is_equal(DEFAULT_ENEMY_COUNT * 2)
	for enemy_index in range(DEFAULT_ENEMY_COUNT):
		assert_int(_count_diagnostics_for_enemy(diagnostics, enemy_index, "fallback_decision_0")).is_equal(1)
		assert_int(_count_diagnostics_for_enemy(diagnostics, enemy_index, "fallback_decision_1")).is_equal(1)

func test_blocked_map_fallback_reports_deadlock_when_timeout_prevents_attack() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(2, 0), Vector2(2, 8), Vector2(0, 8)]))
	var timeout_ticks := 2
	var result: Dictionary = probe.call(
		"SimulateBlockedMapFallbackWithNavigation",
		2,
		timeout_ticks,
		timeout_ticks + 5,
		Vector2(1, 1),
		Vector2(6, 6)
	)
	var diagnostics: Array = result.get("diagnostics", [])

	assert_int(int(result.get("enemies_reached_fallback_attack", 0))).is_equal(0)
	assert_int(int(result.get("deadlock_count", 0))).is_equal(2)
	assert_bool(bool(result.get("navigation_api_used", false))).is_true()
	assert_int(diagnostics.size()).is_equal(6)
	for enemy_index in range(2):
		assert_int(_count_diagnostics_for_enemy(diagnostics, enemy_index, "fallback_decision_0")).is_equal(1)
		assert_int(_count_diagnostics_for_enemy(diagnostics, enemy_index, "fallback_decision_1")).is_equal(1)
		assert_int(_count_diagnostics_for_enemy(diagnostics, enemy_index, "deadlock")).is_equal(1)

func test_blocked_map_fallback_simulation_is_deterministic_for_same_inputs() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(8, 0), Vector2(8, 8), Vector2(0, 8)]))
	var first: Dictionary = probe.call("SimulateBlockedMapFallbackWithNavigation", 2, 4, 1, Vector2(1, 1), Vector2(6, 6))
	var second: Dictionary = probe.call("SimulateBlockedMapFallbackWithNavigation", 2, 4, 1, Vector2(1, 1), Vector2(6, 6))

	assert_str(JSON.stringify(first)).is_equal(JSON.stringify(second))
