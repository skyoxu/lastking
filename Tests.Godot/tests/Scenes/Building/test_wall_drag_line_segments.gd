extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

# acceptance: ACC:T13.11
func test_wall_drag_creates_continuous_segments_and_invalid_cells_remain_unchanged() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.set_blocked(Vector2i(9, 9))

	var firstPath: Array[Vector2i] = [Vector2i(0, 0), Vector2i(1, 0), Vector2i(2, 0)]
	var firstDrag = runtime.drag_place_wall(firstPath)
	var first_segments: Array = firstDrag["created_segments"]
	assert_int(first_segments.size()).is_equal(3)
	var are_continuous = true
	for i in range(first_segments.size() - 1):
		var delta: Vector2i = first_segments[i + 1] - first_segments[i]
		if abs(delta.x) + abs(delta.y) != 1:
			are_continuous = false
	assert_bool(are_continuous).is_true()

	var spent_after_valid_path = int(firstDrag["resources_spent"])
	var secondPath: Array[Vector2i] = [Vector2i(9, 9)]
	var secondDrag = runtime.drag_place_wall(secondPath)
	assert_int((secondDrag["created_segments"] as Array).size()).is_equal(0)
	assert_int(int(secondDrag["resources_spent"])).is_equal(0)
	assert_int(runtime.resources).is_equal(200 - spent_after_valid_path)
	runtime.queue_free()

# acceptance: ACC:T13.18
func test_wall_drag_cost_equals_successful_segment_count_excluding_skipped_cells() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	runtime.set_blocked(Vector2i(1, 0))
	runtime.set_blocked(Vector2i(3, 0))

	var path: Array[Vector2i] = [
		Vector2i(0, 0),
		Vector2i(1, 0),
		Vector2i(2, 0),
		Vector2i(3, 0),
		Vector2i(4, 0),
	]
	var result = runtime.drag_place_wall(path)

	var created_segments: Array = result["created_segments"]
	var resources_spent = int(result["resources_spent"])
	assert_int(created_segments.size()).is_equal(1)
	assert_int(resources_spent).is_equal(created_segments.size() * runtime.get_cost("wall"))
	runtime.queue_free()

# acceptance: ACC:T13.12
func test_wall_drag_rejects_non_adjacent_jump_cells_and_keeps_segments_continuous() -> void:
	var runtime = _new_runtime()
	add_child(runtime)

	var path: Array[Vector2i] = [
		Vector2i(0, 0),
		Vector2i(1, 0),
		Vector2i(3, 0),
		Vector2i(4, 0),
	]
	var result = runtime.drag_place_wall(path)
	var created_segments: Array = result["created_segments"]
	var skipped_cells: Array = result["skipped_cells"]

	assert_int(created_segments.size()).is_equal(2)
	assert_that(created_segments).is_equal([Vector2i(0, 0), Vector2i(1, 0)])
	assert_that(skipped_cells).contains(Vector2i(3, 0))
	assert_that(skipped_cells).contains(Vector2i(4, 0))
	runtime.queue_free()
