extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DEFAULT_SPEED := 600.0
const EPSILON := 0.0001
const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"


func _integrate_position(position: Vector2, direction: Vector2, delta: float, speed: float) -> Vector2:
	if direction == Vector2.ZERO:
		return position
	return position + direction.normalized() * speed * delta


func _clamp_to_world(position: Vector2, world_min: Vector2, world_max: Vector2) -> Vector2:
	return Vector2(
		clampf(position.x, world_min.x, world_max.x),
		clampf(position.y, world_min.y, world_max.y)
	)


func _p95(values: Array[float]) -> float:
	var sorted := values.duplicate()
	sorted.sort()
	var index := int(ceil(float(sorted.size()) * 0.95)) - 1
	index = maxi(0, mini(index, sorted.size() - 1))
	return sorted[index]


func _load_main() -> Node:
	var main: Node = preload(MAIN_SCENE_PATH).instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	return main


func _controller(main: Node) -> Node:
	return main.get_node_or_null("CameraController")


func _camera(main: Node) -> Camera2D:
	return main.get_node_or_null("WorldRoot/Camera2D") as Camera2D


func _prepare_controller(main: Node) -> Dictionary:
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()
	controller.set_process(false)
	controller.call("SetPaused", false)
	controller.call("SetLocked", false)
	return {"controller": controller, "camera": camera}


# acceptance: ACC:T22.2
func test_position_remains_unchanged_when_no_scroll_input_across_frames() -> void:
	var main := await _load_main()
	var prepared := _prepare_controller(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	camera.global_position = Vector2(40.0, -25.0)
	var initial := camera.global_position
	var viewport := Vector2(1920.0, 1080.0)

	controller.set("EdgeMargin", -1000.0)
	for _i in range(60):
		controller.call("SetManualKeyboardAxis", Vector2.ZERO)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")

	assert_that(camera.global_position).is_equal(initial)


# acceptance: ACC:T22.2
func test_position_changes_continuously_without_jumps_when_scroll_input_persists() -> void:
	var main := await _load_main()
	var prepared := _prepare_controller(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	camera.global_position = Vector2.ZERO
	var frame_steps: Array[float] = []
	var viewport := Vector2(1920.0, 1080.0)

	controller.set("EdgeMargin", -1000.0)
	for _i in range(30):
		var previous := camera.global_position
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
		frame_steps.append(camera.global_position.x - previous.x)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")

	assert_that(camera.global_position.x > 0.0).is_true()
	for step in frame_steps:
		assert_that(abs(step - frame_steps[0]) <= EPSILON).is_true()


# acceptance: ACC:T22.3
func test_delta_scaled_motion_is_frame_rate_independent_and_speed_configurable() -> void:
	var main := await _load_main()
	var prepared := _prepare_controller(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	controller.set("EdgeMargin", -1000.0)
	controller.set("ScrollSpeed", 600.0)
	var viewport := Vector2(1920.0, 1080.0)

	camera.global_position = Vector2.ZERO
	for _i in range(60):
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
	var at_60 := camera.global_position

	camera.global_position = Vector2.ZERO
	for _j in range(30):
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 30.0)
	var at_30 := camera.global_position

	assert_that(abs(at_60.x - at_30.x) <= EPSILON).is_true()

	controller.set("ScrollSpeed", 300.0)
	camera.global_position = Vector2.ZERO
	for _k in range(60):
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
	var slow := camera.global_position

	controller.set("ScrollSpeed", 900.0)
	camera.global_position = Vector2.ZERO
	for _m in range(60):
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
	var fast := camera.global_position

	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")
	assert_that(abs((fast.x / slow.x) - 3.0) <= EPSILON).is_true()


# acceptance: ACC:T22.6
func test_world_bounds_clamp_blocks_all_out_of_bounds_attempts() -> void:
	var main := await _load_main()
	var prepared := _prepare_controller(main)
	var controller: Node = prepared["controller"]
	var camera: Camera2D = prepared["camera"]
	controller.set("EdgeMargin", -1000.0)
	var viewport := Vector2(1920.0, 1080.0)

	camera.limit_left = -100
	camera.limit_right = 100
	camera.limit_top = -50
	camera.limit_bottom = 50
	camera.global_position = Vector2(100.0, 0.0)

	controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
	controller.call("SetManualMousePosition", viewport * 0.5)
	controller.call("ApplyManualStep", 1.0 / 60.0)
	assert_float(camera.global_position.x).is_equal(100.0)

	controller.call("SetManualKeyboardAxis", Vector2.DOWN)
	for _i in range(120):
		controller.call("SetManualMousePosition", viewport * 0.5)
		controller.call("ApplyManualStep", 1.0 / 60.0)
	assert_float(camera.global_position.y).is_less_equal(50.0)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")


# acceptance: ACC:T22.7
func test_performance_gate_p95_frame_time_is_within_threshold_under_continuous_scrolling() -> void:
	var main := await _load_main()
	var controller := _controller(main)
	var camera := _camera(main)
	assert_object(controller).is_not_null()
	assert_object(camera).is_not_null()

	controller.set_process(false)
	controller.call("SetPaused", false)
	controller.call("SetLocked", false)
	controller.set("EdgeMargin", -1000.0)

	var frame_times_ms: Array[float] = []
	var viewport := Vector2(1920.0, 1080.0)
	for _i in range(30):
		controller.call("SetManualKeyboardAxis", Vector2.RIGHT)
		controller.call("SetManualMousePosition", viewport * 0.5)
		var t0 := Time.get_ticks_usec()
		controller.call("ApplyManualStep", 0.016)
		var elapsed_ms := float(Time.get_ticks_usec() - t0) / 1000.0
		frame_times_ms.append(elapsed_ms)
	controller.call("ClearManualKeyboardAxis")
	controller.call("ClearManualMousePosition")

	var threshold_ms := 20.0
	var p95 := _p95(frame_times_ms)

	assert_that(p95 <= threshold_ms).is_true()
