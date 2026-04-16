extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _new_manager() -> Node:
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	add_child(auto_free(manager))
	await get_tree().process_frame
	manager.call("ResetRuntimeForTest")
	return manager


# acceptance: ACC:T23.2 ACC:T23.3 ACC:T23.4 ACC:T23.6 ACC:T23.7 ACC:T23.12 ACC:T23.14 ACC:T23.15 ACC:T23.19 ACC:T23.20
func test_runtime_speed_progression_order_and_timer_freeze() -> void:
	var manager := await _new_manager()
	manager.call("EnqueueCountdown", "wave", 10.0)

	manager.call("SetPause")
	manager.call("SimulateRuntimeStep", 10.0)
	var paused_progress := float(manager.call("GetGameplayProgress"))
	var paused_remaining := float(manager.call("GetCountdownRemaining", "wave"))

	manager.call("SetOneX")
	manager.call("SimulateRuntimeStep", 2.0)
	var one_x_progress := float(manager.call("GetGameplayProgress"))
	var one_x_remaining := float(manager.call("GetCountdownRemaining", "wave"))

	manager.call("SetTwoX")
	manager.call("SimulateRuntimeStep", 2.0)
	var two_x_progress := float(manager.call("GetGameplayProgress"))
	var two_x_remaining := float(manager.call("GetCountdownRemaining", "wave"))

	assert_float(paused_progress).is_equal(0.0)
	assert_float(paused_remaining).is_equal(10.0)
	assert_float(one_x_progress).is_greater(paused_progress)
	assert_float(two_x_progress).is_greater(one_x_progress)
	assert_float(two_x_remaining).is_less(one_x_remaining)


# acceptance: ACC:T23.7 ACC:T23.12
func test_pause_keeps_existing_and_new_countdowns_frozen_until_resume() -> void:
	var manager := await _new_manager()
	manager.call("EnqueueCountdown", "before-pause", 5.0)

	manager.call("SetPause")
	manager.call("EnqueueCountdown", "during-pause", 2.0)
	manager.call("SimulateRuntimeStep", 10.0)

	assert_float(float(manager.call("GetCountdownRemaining", "before-pause"))).is_equal(5.0)
	assert_float(float(manager.call("GetCountdownRemaining", "during-pause"))).is_equal(2.0)

	manager.call("SetOneX")
	manager.call("SimulateRuntimeStep", 1.0)

	assert_float(float(manager.call("GetCountdownRemaining", "before-pause"))).is_equal(4.0)
	assert_float(float(manager.call("GetCountdownRemaining", "during-pause"))).is_equal(1.0)


# acceptance: ACC:T23.20
func test_resume_preserves_saved_queue_order_for_countdowns() -> void:
	var manager := await _new_manager()
	manager.call("EnqueueCountdown", "before-pause", 3.0)
	manager.call("SetPause")
	manager.call("EnqueueCountdown", "during-pause", 2.0)

	manager.call("SimulateRuntimeStep", 10.0)
	manager.call("SetOneX")
	var first_step: Dictionary = manager.call("SimulateRuntimeStep", 2.0)
	var second_step: Dictionary = manager.call("SimulateRuntimeStep", 1.0)

	assert_that(first_step["completed"]).contains_exactly(["during-pause"])
	assert_that(second_step["completed"]).contains_exactly(["before-pause"])
