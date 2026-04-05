extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

func _assert_rejected_without_side_effects(result: Dictionary, runtime: Node, resources_before: int, placements_before: int) -> void:
	assert_that(bool(result["accepted"])).is_false()
	assert_that(runtime.resources).is_equal(resources_before)
	assert_that(runtime.placements.size()).is_equal(placements_before)

# acceptance: ACC:T13.8
func test_confirm_refuses_out_of_bounds_blocked_or_occupied_and_keeps_state_unchanged() -> void:
	var out_of_bounds = _new_runtime()
	add_child(out_of_bounds)
	out_of_bounds.select_building_type("wall")
	var out_resources_before = out_of_bounds.resources
	var out_placements_before = out_of_bounds.placements.size()
	var out_result = out_of_bounds.confirm_at(Vector2(40, 0))
	_assert_rejected_without_side_effects(out_result, out_of_bounds, out_resources_before, out_placements_before)
	out_of_bounds.queue_free()

	var blocked = _new_runtime()
	add_child(blocked)
	blocked.select_building_type("wall")
	blocked.set_blocked(Vector2i(2, 2))
	var blocked_resources_before = blocked.resources
	var blocked_placements_before = blocked.placements.size()
	var blocked_result = blocked.confirm_at(Vector2(8, 8))
	_assert_rejected_without_side_effects(blocked_result, blocked, blocked_resources_before, blocked_placements_before)
	blocked.queue_free()

	var occupied = _new_runtime()
	add_child(occupied)
	occupied.select_building_type("wall")
	occupied.set_occupied(Vector2i(3, 3))
	var occupied_resources_before = occupied.resources
	var occupied_placements_before = occupied.placements.size()
	var occupied_result = occupied.confirm_at(Vector2(12, 12))
	_assert_rejected_without_side_effects(occupied_result, occupied, occupied_resources_before, occupied_placements_before)
	occupied.queue_free()

# acceptance: ACC:T13.9
func test_confirm_refuses_when_resources_are_insufficient_and_keeps_state_unchanged() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.resources = 10
	runtime.select_building_type("barracks")
	var resources_before = runtime.resources
	var placements_before = runtime.placements.size()
	var result = runtime.confirm_at(Vector2(0, 0))
	_assert_rejected_without_side_effects(result, runtime, resources_before, placements_before)
	runtime.queue_free()

# acceptance: ACC:T13.14
func test_preview_snaps_to_discrete_grid_and_confirm_rejects_off_grid_submission() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.select_building_type("wall")
	var preview = runtime.preview_at(Vector2(5, 7))
	assert_that(preview["snapped_cell"]).is_equal(Vector2i(1, 2))
	var resources_before = runtime.resources
	var placements_before = runtime.placements.size()
	var result = runtime.confirm_at(Vector2(5, 7))
	_assert_rejected_without_side_effects(result, runtime, resources_before, placements_before)
	runtime.queue_free()

# acceptance: ACC:T13.17
func test_multi_cell_preview_covers_all_cells_and_refuses_partial_when_any_cell_is_invalid() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.select_building_type("castle")
	var preview = runtime.preview_at(Vector2(16, 12))
	assert_that((preview["covered_cells"] as Array).size()).is_equal(4)
	runtime.set_blocked(Vector2i(5, 4))
	var resources_before = runtime.resources
	var placements_before = runtime.placements.size()
	var result = runtime.confirm_at(Vector2(16, 12))
	_assert_rejected_without_side_effects(result, runtime, resources_before, placements_before)
	runtime.queue_free()
