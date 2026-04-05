extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

# acceptance: ACC:T13.2
func test_wall_gate_invalid_input_is_refused_without_side_effects_or_resource_change() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var resources_before = runtime.resources
	var gates_before = runtime.gates.size()
	var result = runtime.create_gate(Vector2i(1, 1), Vector2i(2, 2), 3)
	assert_that(result["accepted"]).is_equal(false)
	assert_that(result["reason"]).is_equal("invalid_input")
	assert_that(runtime.resources).is_equal(resources_before)
	assert_that(runtime.gates.size()).is_equal(gates_before)
	runtime.queue_free()
