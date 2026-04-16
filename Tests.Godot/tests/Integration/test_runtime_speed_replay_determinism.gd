extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


# acceptance: ACC:T23.26
func test_runtime_speed_replay_sequence_is_deterministic() -> void:
	var manager := _ensure_game_manager()
	await get_tree().process_frame

	manager.call("ResetRuntimeForTest")
	var first_replay: Dictionary = _run_replay_sequence(manager)
	var first_timeline: Array = manager.call("GetRuntimeSpeedTimeline")
	var first_state: Dictionary = manager.call("GetSpeedState")

	manager.call("ResetRuntimeForTest")
	var second_replay: Dictionary = _run_replay_sequence(manager)
	var second_timeline: Array = manager.call("GetRuntimeSpeedTimeline")
	var second_state: Dictionary = manager.call("GetSpeedState")

	assert_that(first_timeline).is_equal(second_timeline)
	assert_that(first_state).is_equal(second_state)
	assert_that(first_replay["completed"]).is_equal(second_replay["completed"])
	assert_that(first_replay["progress"]).is_equal(second_replay["progress"])
	assert_that(first_replay["completed"]).is_equal(["alpha", "beta"])
	assert_that(first_replay["progress"]).is_equal(["10:2", "20:2", "30:3", "40:5"])


func _ensure_game_manager() -> Node:
	var existing := get_node_or_null("/root/GameManager")
	if existing != null:
		return existing
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	manager.name = "GameManager"
	get_tree().root.add_child(auto_free(manager))
	return manager


func _run_replay_sequence(manager: Node) -> Dictionary:
	manager.call("EnqueueCountdown", "alpha", 1.5)
	manager.call("EnqueueCountdown", "beta", 3.5)
	var completed: Array = []
	var progress: Array = []
	manager.call("SetTwoX")
	var step_one: Dictionary = manager.call("SimulateRuntimeStep", 1.0)
	progress.append("10:%s" % String.num(float(step_one["progress"]), 0))
	completed.append_array(step_one["completed"])
	manager.call("SetPause")
	var step_two: Dictionary = manager.call("SimulateRuntimeStep", 1.0)
	progress.append("20:%s" % String.num(float(step_two["progress"]), 0))
	completed.append_array(step_two["completed"])
	manager.call("SetOneX")
	var step_three: Dictionary = manager.call("SimulateRuntimeStep", 1.0)
	progress.append("30:%s" % String.num(float(step_three["progress"]), 0))
	completed.append_array(step_three["completed"])
	manager.call("SetTwoX")
	var step_four: Dictionary = manager.call("SimulateRuntimeStep", 1.0)
	progress.append("40:%s" % String.num(float(step_four["progress"]), 0))
	completed.append_array(step_four["completed"])
	return {
		"completed": completed,
		"progress": progress,
	}
