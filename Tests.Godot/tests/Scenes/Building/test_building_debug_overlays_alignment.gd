extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

func _bounds_from_cells(cells: Array[Vector2i]) -> Rect2i:
	if cells.is_empty():
		return Rect2i()
	var min_x = cells[0].x
	var max_x = cells[0].x
	var min_y = cells[0].y
	var max_y = cells[0].y
	for cell in cells:
		min_x = min(min_x, cell.x)
		max_x = max(max_x, cell.x)
		min_y = min(min_y, cell.y)
		max_y = max(max_y, cell.y)
	return Rect2i(Vector2i(min_x, min_y), Vector2i(max_x - min_x + 1, max_y - min_y + 1))

# acceptance: ACC:T13.13
func test_debug_overlay_cells_and_boundary_match_committed_cells_after_successful_placement() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var origin = Vector2i(4, 7)
	var footprint: Array[Vector2i] = [Vector2i.ZERO, Vector2i.RIGHT, Vector2i.DOWN]
	var overlay = runtime.preview_overlay_for(origin, footprint)
	var committed = runtime.commit_cells(origin, footprint)
	assert_that(overlay["occupied_cells"]).is_equal(committed)
	assert_that(overlay["footprint_boundary"]).is_equal(_bounds_from_cells(committed))
	runtime.queue_free()

# acceptance: ACC:T13.20
func test_debug_overlay_grid_alignment_matches_final_committed_cells_for_offset_origin() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var origin = Vector2i(11, 3)
	var footprint: Array[Vector2i] = [Vector2i.ZERO, Vector2i.LEFT, Vector2i.UP, Vector2i(-1, -1)]
	var overlay = runtime.preview_overlay_for(origin, footprint)
	var committed = runtime.commit_cells(origin, footprint)
	assert_that(overlay["occupied_cells"]).is_equal(committed)
	assert_that(overlay["footprint_boundary"]).is_equal(_bounds_from_cells(committed))
	runtime.queue_free()
