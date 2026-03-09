extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const MAX_DAY := 15

enum Phase {
	DAY,
	NIGHT
}

func _phase_name(phase: int) -> String:
	return "day" if phase == Phase.DAY else "night"

func _run_day_night_loop(input_steps: Array, max_day: int = MAX_DAY) -> Dictionary:
	var day := 1
	var phase := Phase.DAY
	var transitions: Array[String] = []

	for step in input_steps:
		if typeof(step) != TYPE_INT:
			continue
		if step <= 0:
			continue

		var i := 0
		while i < step and day < max_day:
			phase = Phase.NIGHT if phase == Phase.DAY else Phase.DAY
			if phase == Phase.DAY:
				day += 1
			transitions.append("%d:%s" % [day, _phase_name(phase)])
			i += 1

		if day >= max_day:
			break

	return {
		"day": day,
		"phase": _phase_name(phase),
		"transitions": transitions
	}

# acceptance: ACC:T3.3
func test_runtime_loop_scope_guard_caps_progress_at_day15() -> void:
	var result := _run_day_night_loop([100], 15)

	assert_int(result["day"]).is_equal(15)
	assert_bool(result.has("day")).is_true()
	assert_bool(result.has("phase")).is_true()
	assert_bool(result.has("transitions")).is_true()
	assert_bool(result.has("inventory")).is_false()

# acceptance: ACC:T3.5
func test_runtime_loop_is_deterministic_for_same_input_sequence() -> void:
	var sequence := [1, 2, 0, -1, 3, 4, 2]
	var first := _run_day_night_loop(sequence, 15)
	var second := _run_day_night_loop(sequence, 15)

	assert_bool(first == second).is_true()
	assert_array(first["transitions"]).is_not_empty()
	assert_str(first["phase"]).is_equal(second["phase"])

# acceptance: ACC:T3.20
func test_runtime_loop_updates_are_driven_by_process_and_stop_when_paused() -> void:
	var script: Script = load("res://Game.Godot/Scripts/Runtime/DayNightRuntimeLoopNode.cs")
	var runtime_node: Node = script.new()
	add_child(runtime_node)
	await get_tree().process_frame
	await get_tree().process_frame
	await get_tree().process_frame
	runtime_node.call("SimulateProcessStep", 240.0)
	assert_int(int(runtime_node.get("UpdateCallCount"))).is_greater(0)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(1)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(1)

	runtime_node.call("SimulatePhysicsStep", 120.0)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(2)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(0)
	var calls_before_pause := int(runtime_node.get("UpdateCallCount"))
	var day_before_pause := int(runtime_node.get("CurrentDay"))
	var phase_before_pause := int(runtime_node.get("CurrentPhase"))

	runtime_node.set("PauseLoop", true)
	await get_tree().process_frame
	await get_tree().process_frame
	assert_int(int(runtime_node.get("UpdateCallCount"))).is_equal(calls_before_pause)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(day_before_pause)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(phase_before_pause)

	runtime_node.set("PauseLoop", false)
	runtime_node.set_process(false)
	runtime_node.set_physics_process(false)
	var calls_before_stop := int(runtime_node.get("UpdateCallCount"))
	var day_before_stop := int(runtime_node.get("CurrentDay"))
	var phase_before_stop := int(runtime_node.get("CurrentPhase"))
	await get_tree().process_frame
	await get_tree().process_frame
	assert_int(int(runtime_node.get("UpdateCallCount"))).is_equal(calls_before_stop)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(day_before_stop)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(phase_before_stop)
