extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


# acceptance: ACC:T23.25
func test_runtime_speed_timeline_evidence_contains_before_after_tick_and_source() -> void:
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	add_child(auto_free(manager))
	await get_tree().process_frame
	manager.call("ResetRuntimeForTest")

	manager.call("SetTwoX")
	manager.call("SetPause")
	var timeline: Array = manager.call("GetRuntimeSpeedTimeline")

	assert_int(timeline.size()).is_equal(2)
	var first: Dictionary = timeline[0]
	assert_int(int(first["before_scale_percent"])).is_equal(100)
	assert_int(int(first["after_scale_percent"])).is_equal(200)
	assert_int(int(first["effective_tick"])).is_equal(1)
	assert_str(str(first["source"])).is_equal("ui.2x")
