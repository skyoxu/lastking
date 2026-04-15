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
	camera.global_position = Vector2(80.0, 80.0)
	camera.limit_left = 0
	camera.limit_right = 100
	camera.limit_top = 0
	camera.limit_bottom = 100
	return {"controller": controller, "camera": camera}


func _manual_step(controller: Node, keyboard_axis: Vector2, mouse_pos: Vector2, delta: float) -> void:
	controller.call("SetManualKeyboardAxis", keyboard_axis)
	controller.call("SetManualMousePosition", mouse_pos)
	controller.call("ApplyManualStep", delta)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")


# acceptance: ACC:T22.11
func test_locked_state_refuses_keyboard_scroll_and_keeps_position_unchanged() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := Vector2(1920.0, 1080.0)

	controller.call("SetLocked", true)
	var camera_before := camera.global_position
	_manual_step(controller, Vector2.RIGHT, viewport * 0.5, 0.016)
	var camera_after := camera.global_position

	assert_that(camera_after).is_equal(camera_before)


func test_edge_scroll_clamps_position_to_configured_bounds() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := Vector2(1920.0, 1080.0)

	controller.set("EdgeMargin", 20.0)
	for _i in range(40):
		_manual_step(controller, Vector2.ZERO, Vector2(viewport.x - 2.0, viewport.y - 2.0), 0.016)

	assert_float(camera.global_position.x).is_less_equal(100.0)
	assert_float(camera.global_position.y).is_less_equal(100.0)


func test_edge_margin_threshold_triggers_only_at_or_beyond_margin() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := main.get_viewport().get_visible_rect().size

	controller.set("EdgeMargin", 10.0)
	var before_inside := camera.global_position
	_manual_step(controller, Vector2.ZERO, Vector2(10.0, viewport.y * 0.5), 0.016)
	var after_inside := camera.global_position
	assert_bool(after_inside.x < before_inside.x).is_true()

	controller.set("EdgeMargin", 10.0)
	controller.set_process(false)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")
	controller.call("SetManualKeyboardAxis", Vector2.ZERO)
	controller.call("SetManualMousePosition", Vector2(11.0, viewport.y * 0.5))
	var before_outside := camera.global_position
	controller.call("ApplyManualStep", 0.016)
	var after_outside := camera.global_position
	assert_that(after_outside).is_equal(before_outside)
