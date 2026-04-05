extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

# acceptance: ACC:T13.1
func test_wall_drag_line_creates_gate_segments_with_count_position_and_direction() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var segments = runtime.create_gate_segments(Vector2i(2, 4), Vector2i(4, 4))

	assert_that(segments.size()).is_equal(3)
	assert_that(segments[0]["position"]).is_equal(Vector2(32.0, 64.0))
	assert_that(segments[1]["position"]).is_equal(Vector2(48.0, 64.0))
	assert_that(segments[2]["position"]).is_equal(Vector2(64.0, 64.0))
	assert_that(segments[0]["direction"]).is_equal(Vector2.RIGHT)
	assert_that(segments[1]["direction"]).is_equal(Vector2.RIGHT)
	assert_that(segments[2]["direction"]).is_equal(Vector2.RIGHT)
	runtime.queue_free()

func test_wall_drag_line_refuses_diagonal_gate_segments() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var segments = runtime.create_gate_segments(Vector2i(1, 1), Vector2i(3, 3))
	assert_that(segments).is_empty()
	runtime.queue_free()
