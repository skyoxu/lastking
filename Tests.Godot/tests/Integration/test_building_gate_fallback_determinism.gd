extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

# acceptance anchor: ACC:T13.4
# acceptance anchor: ACC:T13.5
func test_repeated_runs_with_same_seed_keep_results_identical() -> void:
	var runtime_a = _new_runtime()
	var runtime_b = _new_runtime()
	add_child(runtime_a)
	add_child(runtime_b)

	runtime_a.set_blocked(Vector2i(1, 1))
	runtime_b.set_blocked(Vector2i(1, 1))
	runtime_a.select_building_type("wall")
	runtime_b.select_building_type("wall")

	var confirm_ok_a = runtime_a.confirm_at(Vector2(8, 8))
	var confirm_ok_b = runtime_b.confirm_at(Vector2(8, 8))
	var confirm_reject_a = runtime_a.confirm_at(Vector2(4, 4))
	var confirm_reject_b = runtime_b.confirm_at(Vector2(4, 4))
	var gate_a = runtime_a.resolve_gate_fallback(1307)
	var gate_b = runtime_b.resolve_gate_fallback(1307)

	assert_that(confirm_ok_a).is_equal(confirm_ok_b)
	assert_that(confirm_reject_a).is_equal(confirm_reject_b)
	assert_that(runtime_a.resources).is_equal(runtime_b.resources)
	assert_that(runtime_a.placements).is_equal(runtime_b.placements)
	assert_that(gate_a).is_equal(gate_b)
	assert_that(bool(confirm_reject_a["accepted"])).is_false()

	runtime_a.queue_free()
	runtime_b.queue_free()

# acceptance anchor: ACC:T13.19
func test_blocked_path_fallback_tie_breaking_is_order_independent() -> void:
	var runtime_a = _new_runtime()
	var runtime_b = _new_runtime()
	add_child(runtime_a)
	add_child(runtime_b)

	var chosen_a = runtime_a.resolve_gate_fallback(77, ["GateNorth", "GateEast"])
	var chosen_b = runtime_b.resolve_gate_fallback(77, ["GateEast", "GateNorth"])

	assert_that(chosen_a).is_equal(chosen_b)

	runtime_a.queue_free()
	runtime_b.queue_free()

# acceptance anchor: ACC:T13.8
func test_rejected_placement_keeps_state_unchanged_and_reports_refusal() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.select_building_type("barracks")
	runtime.set_blocked(Vector2i(5, 5))
	var resources_before = runtime.resources
	var placements_before = runtime.placements.duplicate(true)

	var result = runtime.confirm_at(Vector2(20, 20))
	assert_that(bool(result["accepted"])).is_false()
	assert_that(runtime.resources).is_equal(resources_before)
	assert_that(runtime.placements).is_equal(placements_before)
	runtime.queue_free()
