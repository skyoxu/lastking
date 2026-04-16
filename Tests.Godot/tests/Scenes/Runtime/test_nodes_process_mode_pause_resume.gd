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


# acceptance: ACC:T23.10
# acceptance: ACC:T23.24
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
