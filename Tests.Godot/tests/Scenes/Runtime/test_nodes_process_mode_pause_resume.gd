extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


class CountingNode:
	extends Node

	var ticks := 0

	func _process(_delta: float) -> void:
		ticks += 1


func _ensure_game_manager() -> Node:
	var existing := get_node_or_null("/root/GameManager")
	if existing != null:
		return existing
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	manager.name = "GameManager"
	get_tree().root.add_child(auto_free(manager))
	return manager


func _ensure_runtime_loop_node() -> Node:
	var existing := get_node_or_null("/root/DayNightRuntimeLoopNode")
	if existing != null:
		return existing
	var loop_node := preload("res://Game.Godot/Scripts/Runtime/DayNightRuntimeLoopNode.cs").new()
	loop_node.name = "DayNightRuntimeLoopNode"
	get_tree().root.add_child(auto_free(loop_node))
	return loop_node


# acceptance: ACC:T23.10
# acceptance: ACC:T23.24
# acceptance: ACC:T42.7
func test_nodes_marked_for_gameplay_pause_stop_processing_and_resume() -> void:
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var node := CountingNode.new()
	add_child(auto_free(node))
	node.process_mode = Node.PROCESS_MODE_PAUSABLE
	await get_tree().process_frame
	var ticks_before_pause := node.ticks

	manager.call("SetPause")
	await get_tree().process_frame
	await get_tree().process_frame
	var ticks_during_pause := node.ticks

	assert_bool(get_tree().paused).is_true()
	assert_int(int(node.process_mode)).is_equal(Node.PROCESS_MODE_PAUSABLE)
	assert_int(ticks_before_pause).is_greater(0)
	assert_int(ticks_during_pause).is_equal(ticks_before_pause)

	manager.call("SetOneX")
	await get_tree().process_frame

	assert_bool(get_tree().paused).is_false()
	assert_int(node.ticks).is_greater(ticks_during_pause)


# acceptance: ACC:T42.5
func test_runtime_day_night_loop_state_should_not_advance_while_paused() -> void:
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var runtime_node := _ensure_runtime_loop_node()
	await get_tree().process_frame

	for i in range(10):
		await get_tree().process_frame

	var calls_before_pause := int(runtime_node.get("UpdateCallCount"))
	var day_before_pause := int(runtime_node.get("CurrentDay"))
	var phase_before_pause := int(runtime_node.get("CurrentPhase"))
	var elapsed_before_pause := float(runtime_node.get("CurrentPhaseElapsedSeconds"))

	manager.call("SetPause")
	await get_tree().process_frame
	await get_tree().process_frame

	assert_int(int(runtime_node.get("UpdateCallCount"))).is_equal(calls_before_pause)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(day_before_pause)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(phase_before_pause)
	assert_float(float(runtime_node.get("CurrentPhaseElapsedSeconds"))).is_equal(elapsed_before_pause)

	manager.call("SetOneX")
	await get_tree().process_frame
	assert_bool(get_tree().paused).is_false()

# acceptance: ACC:T42.7
func test_runtime_day_night_loop_state_should_not_advance_when_loop_is_stopped() -> void:
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var runtime_node := _ensure_runtime_loop_node()
	await get_tree().process_frame

	for i in range(10):
		await get_tree().process_frame

	var calls_before_stop := int(runtime_node.get("UpdateCallCount"))
	var day_before_stop := int(runtime_node.get("CurrentDay"))
	var phase_before_stop := int(runtime_node.get("CurrentPhase"))
	var elapsed_before_stop := float(runtime_node.get("CurrentPhaseElapsedSeconds"))

	runtime_node.set("PauseLoop", true)
	await get_tree().process_frame
	await get_tree().process_frame

	assert_int(int(runtime_node.get("UpdateCallCount"))).is_equal(calls_before_stop)
	assert_int(int(runtime_node.get("CurrentDay"))).is_equal(day_before_stop)
	assert_int(int(runtime_node.get("CurrentPhase"))).is_equal(phase_before_stop)
	assert_float(float(runtime_node.get("CurrentPhaseElapsedSeconds"))).is_equal(elapsed_before_stop)
