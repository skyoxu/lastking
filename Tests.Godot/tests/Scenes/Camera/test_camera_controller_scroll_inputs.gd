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


func _stabilize_controller_for_test(controller: Node) -> void:
	controller.set_process(false)
	controller.call("SetPaused", false)
	controller.call("SetLocked", false)

func _prepare_camera_for_test(camera: Camera2D) -> void:
	camera.global_position = Vector2(120.0, 120.0)
	camera.limit_left = -10000
	camera.limit_right = 10000
	camera.limit_top = -10000
	camera.limit_bottom = 10000


func _step_with_manual_input(controller: Node, keyboard_axis: Vector2, mouse_pos: Vector2, delta: float) -> void:
	controller.call("SetManualKeyboardAxis", keyboard_axis)
	controller.call("SetManualMousePosition", mouse_pos)
	controller.call("ApplyManualStep", delta)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")


func _dispatch_key_event(keycode: Key, pressed: bool) -> void:
	var event := InputEventKey.new()
	event.keycode = keycode
	event.physical_keycode = keycode
	event.pressed = pressed
	Input.parse_input_event(event)
	Input.flush_buffered_events()


func _advance_frames(frames: int) -> void:
	for _i in range(frames):
		await get_tree().process_frame


# acceptance: ACC:T22.1
func test_continuous_navigation_with_edge_and_keyboard_scroll_inputs_uses_real_controller() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)
	_prepare_camera_for_test(camera)

	var viewport := Vector2(1920.0, 1080.0)
	var camera_before := camera.global_position
	for _i in range(6):
		_step_with_manual_input(controller, Vector2.RIGHT, Vector2(viewport.x - 2.0, viewport.y * 0.5), 0.016)
	var camera_after := camera.global_position

	assert_bool(camera_after.x > camera_before.x).is_true()


# acceptance: ACC:T22.4
func test_edge_margin_triggers_motion_and_idle_when_outside_margin_without_keyboard_on_real_controller() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)
	_prepare_camera_for_test(camera)

	controller.set("EdgeMargin", 20.0)
	var viewport := Vector2(1920.0, 1080.0)
	var camera_before_edge := camera.global_position
	_step_with_manual_input(controller, Vector2.ZERO, Vector2(2.0, viewport.y * 0.5), 0.016)
	var camera_after_edge := camera.global_position
	assert_bool(camera_after_edge.x < camera_before_edge.x).is_true()

	controller.set("EdgeMargin", -1000.0)
	var camera_before_idle := camera.global_position
	_step_with_manual_input(controller, Vector2.ZERO, viewport * 0.5, 0.016)
	var camera_after_idle := camera.global_position
	assert_that(camera_after_idle).is_equal(camera_before_idle)


# acceptance: ACC:T22.5
func test_keyboard_scroll_moves_through_real_input_path_and_stops_after_release_on_real_controller() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)
	_prepare_camera_for_test(camera)
	controller.set("EdgeMargin", -1000.0)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")
	controller.set_process(true)

	var camera_before_press := camera.global_position
	_dispatch_key_event(KEY_RIGHT, true)
	await _advance_frames(3)
	var camera_after_press := camera.global_position
	assert_bool(camera_after_press.x > camera_before_press.x).is_true()

	_dispatch_key_event(KEY_RIGHT, false)
	await _advance_frames(1)
	var camera_before_release := camera.global_position
	await _advance_frames(2)
	var camera_after_release := camera.global_position
	assert_that(camera_after_release).is_equal(camera_before_release)


# acceptance: ACC:T22.8
func test_same_frame_combines_edge_and_keyboard_contributions_on_real_controller() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)
	_prepare_camera_for_test(camera)

	var viewport := Vector2(1920.0, 1080.0)
	var camera_before := camera.global_position
	_step_with_manual_input(controller, Vector2.DOWN, Vector2(2.0, viewport.y * 0.5), 0.016)
	var camera_after := camera.global_position

	assert_bool(camera_after.x < camera_before.x).is_true()
	assert_bool(camera_after.y > camera_before.y).is_true()


# acceptance: ACC:T22.10
func test_default_margin_20_pixels_and_manual_keyboard_with_mouse_combined_read_on_real_controller() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)
	_prepare_camera_for_test(camera)

	assert_float(float(controller.get("EdgeMargin"))).is_equal(20.0)

	var viewport := Vector2(1920.0, 1080.0)
	var camera_before := camera.global_position
	_step_with_manual_input(controller, Vector2.DOWN, Vector2(20.0, viewport.y * 0.5), 0.016)
	var camera_after := camera.global_position
	assert_bool(camera_after.x < camera_before.x).is_true()
	assert_bool(camera_after.y > camera_before.y).is_true()
