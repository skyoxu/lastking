extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

func test_non_gate_wall_tile_blocks_traversal() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	assert_bool(runtime.can_traverse("wall", Vector2i.RIGHT, Vector2i.RIGHT)).is_false()
	runtime.queue_free()

# acceptance: ACC:T13.12
func test_gate_allows_only_configured_direction_and_blocks_opposite_direction() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var created = runtime.create_gate(Vector2i(2, 4), Vector2i(4, 4), 3)
	assert_that(created["accepted"]).is_equal(true)
	var allowed = runtime.can_traverse_gate(Vector2i(3, 4), Vector2i.RIGHT)
	var blocked = runtime.can_traverse_gate(Vector2i(3, 4), Vector2i.LEFT)
	assert_bool(allowed).is_true()
	assert_bool(blocked).is_false()
	runtime.queue_free()
