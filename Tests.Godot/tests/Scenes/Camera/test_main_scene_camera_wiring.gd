extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"
const CAMERA_CONTROLLER_SCRIPT_PATH := "res://Game.Godot/Scripts/Camera/CameraController.cs"


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


# acceptance: ACC:T22.9
func test_main_scene_wires_real_camera_controller_and_camera_nodes() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)

	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	assert_bool(camera.enabled).is_true()
	assert_bool(controller.has_method("HasActiveCamera")).is_true()
	assert_bool(bool(controller.call("HasActiveCamera"))).is_true()
	_stabilize_controller_for_test(controller)

	var script_value: Variant = controller.get_script()
	assert_bool(script_value is Script).is_true()
	assert_str(String((script_value as Script).resource_path)).is_equal(CAMERA_CONTROLLER_SCRIPT_PATH)


# acceptance: ACC:T22.1
func test_real_controller_manual_step_moves_active_camera_in_scene_runtime() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	_stabilize_controller_for_test(controller)

	var camera_before := camera.global_position
	controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
	controller.call("SetManualMousePosition", Vector2(960.0, 540.0))
	controller.call("ApplyManualStep", 0.016)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")

	assert_bool(camera.global_position.x > camera_before.x).is_true()
