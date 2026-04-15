extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"


func _load_main() -> Node:
	var main: Node = preload(MAIN_SCENE_PATH).instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	return main


func _controller(main: Node) -> Node:
	return main.get_node_or_null("CameraController")


func _camera(main: Node) -> Camera2D:
	return main.get_node_or_null("WorldRoot/Camera2D") as Camera2D


func _prepare(main: Node) -> Dictionary:
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	controller.set_process(false)
	controller.call("SetPaused", false)
	controller.call("SetLocked", false)
	controller.set("EdgeMargin", 20.0)
	camera.global_position = Vector2(120.0, 120.0)
	return {"controller": controller, "camera": camera}


func _manual_step(controller: Node, keyboard_axis: Vector2, mouse_pos: Vector2, delta: float) -> void:
	controller.call("SetManualKeyboardAxis", keyboard_axis)
	controller.call("SetManualMousePosition", mouse_pos)
	controller.call("ApplyManualStep", delta)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")


# acceptance: ACC:T22.13
func test_mode_diagnostics_report_stable_idle_edge_keyboard_combined_locked_transitions() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var viewport := Vector2(1920.0, 1080.0)

	controller.set("EdgeMargin", -1000.0)
	_manual_step(controller, Vector2.ZERO, viewport * 0.5, 0.016)
	assert_str(String(controller.call("CurrentMode"))).is_equal("idle")

	controller.set("EdgeMargin", 20.0)
	_manual_step(controller, Vector2.ZERO, Vector2(2.0, viewport.y * 0.5), 0.016)
	assert_str(String(controller.call("CurrentMode"))).is_equal("edge")

	controller.set("EdgeMargin", -1000.0)
	_manual_step(controller, Vector2.UP, viewport * 0.5, 0.016)
	assert_str(String(controller.call("CurrentMode"))).is_equal("keyboard")

	controller.set("EdgeMargin", 20.0)
	_manual_step(controller, Vector2.UP, Vector2(2.0, viewport.y * 0.5), 0.016)
	assert_str(String(controller.call("CurrentMode"))).is_equal("combined")

	controller.call("SetLocked", true)
	_manual_step(controller, Vector2.LEFT, Vector2(2.0, viewport.y * 0.5), 0.016)
	assert_str(String(controller.call("CurrentMode"))).is_equal("locked")


func test_repeated_input_sequence_does_not_escape_bounds_and_locked_keeps_position_unchanged() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := Vector2(1920.0, 1080.0)

	camera.limit_left = -1
	camera.limit_right = 1
	camera.limit_top = -1
	camera.limit_bottom = 1
	camera.global_position = Vector2(0.0, 0.0)

	for _i in range(20):
		_manual_step(controller, Vector2.DOWN, Vector2(viewport.x - 2.0, viewport.y - 2.0), 0.016)

	var before_locked := camera.global_position
	controller.call("SetLocked", true)
	for _j in range(10):
		_manual_step(controller, Vector2.LEFT, Vector2(2.0, 2.0), 0.016)
	var after_locked := camera.global_position

	assert_float(after_locked.x).is_between(-1.0, 1.0)
	assert_float(after_locked.y).is_between(-1.0, 1.0)
	assert_that(after_locked).is_equal(before_locked)
