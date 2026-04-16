extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


# acceptance: ACC:T23.27
func test_runtime_speed_timeline_gate_accepts_valid_sequence() -> void:
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	add_child(auto_free(manager))
	await get_tree().process_frame
	manager.call("ResetRuntimeForTest")

	manager.call("SetTwoX")
	manager.call("SetPause")
	manager.call("SetOneX")

	assert_bool(bool(manager.call("ValidateRuntimeSpeedTimelineGate"))).is_true()


# acceptance: ACC:T23.27
func test_runtime_speed_timeline_gate_rejects_missing_evidence() -> void:
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	add_child(auto_free(manager))
	await get_tree().process_frame
	manager.call("ResetRuntimeForTest")

	assert_bool(bool(manager.call("ValidateRuntimeSpeedTimelineGate"))).is_false()
