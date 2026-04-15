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
	controller.set("EdgeMargin", -1000.0)
	camera.global_position = Vector2(120.0, 120.0)
	return {"controller": controller, "camera": camera}


func _manual_step(controller: Node, keyboard_axis: Vector2, mouse_pos: Vector2, delta: float) -> void:
	controller.call("SetManualKeyboardAxis", keyboard_axis)
	controller.call("SetManualMousePosition", mouse_pos)
	controller.call("ApplyManualStep", delta)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")


func test_active_state_allows_continuous_edge_and_keyboard_scrolling() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := Vector2(1920.0, 1080.0)

	var first_before := camera.global_position
	_manual_step(controller, Vector2.RIGHT, Vector2(viewport.x - 2.0, viewport.y * 0.5), 0.016)
	var first_after := camera.global_position
	_manual_step(controller, Vector2.RIGHT, Vector2(viewport.x - 2.0, viewport.y * 0.5), 0.016)
	var second_after := camera.global_position

	assert_bool(first_after.x > first_before.x).is_true()
	assert_bool(second_after.x > first_after.x).is_true()


# acceptance: ACC:T22.12
func test_paused_state_refuses_same_input_and_keeps_position_unchanged() -> void:
	var main := await _load_main()
	var prepared := _prepare(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	var viewport := Vector2(1920.0, 1080.0)

	_manual_step(controller, Vector2.RIGHT, viewport * 0.5, 0.016)
	var before_paused := camera.global_position
	controller.call("SetPaused", true)
	_manual_step(controller, Vector2.RIGHT, viewport * 0.5, 0.016)
	var after_paused := camera.global_position

	assert_that(after_paused).is_equal(before_paused)
