extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

# acceptance: ACC:T13.3
func test_build_mode_exposes_required_building_types_and_preview_snaps_to_grid() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var expected: Array[String] = [
		"castle", "residence", "mine", "barracks", "mg tower", "wall", "mine trap"
	]
	expected.sort()

	assert_that(runtime.list_building_types()).is_equal(expected)
	runtime.select_building_type("castle")
	var preview = runtime.preview_at(Vector2(5, 7))
	assert_that(bool(preview["is_valid"])).is_true()
	assert_that(preview["snapped_cell"]).is_equal(Vector2i(1, 2))
	runtime.queue_free()

# acceptance: ACC:T13.7
func test_footprint_rules_match_task_contract_for_core_buildings() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	assert_that(runtime.get_footprint_size("castle")).is_equal(4)
	assert_that(runtime.get_footprint_size("barracks")).is_equal(2)
	assert_that(runtime.get_footprint_size("mg tower")).is_equal(1)
	assert_that(runtime.get_footprint_size("wall")).is_equal(1)
	runtime.queue_free()

# acceptance: ACC:T13.6
func test_confirm_valid_placement_snaps_to_grid_and_deducts_cost_once() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.select_building_type("wall")
	var result = runtime.confirm_at(Vector2(8, 12))
	assert_that(bool(result["accepted"])).is_true()
	assert_that(result["placed"]["origin"]).is_equal(Vector2i(2, 3))
	assert_that(int(result["resources_before"]) - int(result["resources_after"])).is_equal(runtime.get_cost("wall"))
	runtime.queue_free()

# acceptance: ACC:T13.7
func test_confirm_rejects_off_grid_position_and_leaves_state_unchanged() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.select_building_type("mine")
	var resources_before = runtime.resources
	var result = runtime.confirm_at(Vector2(10, 12))
	assert_that(bool(result["accepted"])).is_false()
	assert_that(runtime.resources).is_equal(resources_before)
	runtime.queue_free()

# acceptance: ACC:T13.10
func test_each_selectable_building_can_be_confirmed_once_with_sufficient_resources() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.resources = 2000
	var anchors: Array[Vector2] = [
		Vector2(0, 0),
		Vector2(8, 0),
		Vector2(16, 0),
		Vector2(24, 0),
		Vector2(0, 16),
		Vector2(8, 16),
		Vector2(16, 16),
	]
	var index = 0
	for building_type in runtime.list_building_types():
		runtime.select_building_type(building_type)
		var result = runtime.confirm_at(anchors[index])
		assert_that(bool(result["accepted"])).is_true()
		index += 1
	runtime.queue_free()

# acceptance: ACC:T13.14
func test_preview_and_confirm_are_deterministic_for_same_input() -> void:
	var runtime_a = _new_runtime()
	var runtime_b = _new_runtime()
	add_child(runtime_a)
	add_child(runtime_b)
	runtime_a.select_building_type("barracks")
	runtime_b.select_building_type("barracks")
	var world = Vector2(16, 8)
	var preview_a = runtime_a.preview_at(world)
	var preview_b = runtime_b.preview_at(world)
	var confirm_a = runtime_a.confirm_at(world)
	var confirm_b = runtime_b.confirm_at(world)
	assert_that(preview_a).is_equal(preview_b)
	assert_that(confirm_a).is_equal(confirm_b)
	runtime_a.queue_free()
	runtime_b.queue_free()

# acceptance: ACC:T13.16
func test_confirm_without_selected_type_is_refused_and_no_cost_is_deducted() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var resources_before = runtime.resources
	var result = runtime.confirm_at(Vector2(0, 0))
	assert_that(bool(result["accepted"])).is_false()
	assert_that(runtime.resources).is_equal(resources_before)
	runtime.queue_free()
