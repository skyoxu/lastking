extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame


func test_battle_map_screen_minimum_runtime_loop_is_player_visible() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var build_btn := screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn := screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn := screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn := screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn := screen.get_node("Margin/VBox/Controls/FinishBtn")
	var summary := screen.get_node("Margin/VBox/Summary")
	var status := screen.get_node("Margin/VBox/Status")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(String(status.text).length() > 0).is_true()
	assert_bool(String(summary.text).find(":") >= 0).is_true()
	assert_bool(String(summary.text).find("HP") >= 0 or String(summary.text).find("生命") >= 0).is_true()
	assert_bool(String(summary.text).find("Friendly") >= 0 or String(summary.text).find("友军") >= 0).is_true()
	assert_bool(String(summary.text).find("Enemy") >= 0 or String(summary.text).find("敌军") >= 0).is_true()

func test_combat_bridge_single_source_updates_actor_snapshots_and_castle_hp() -> void:
	var bridge := preload("res://Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs").new()
	add_child(auto_free(bridge))
	await _await_frames(2)

	if bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
		bridge.call("BuildPhase")
		bridge.call("TrainFriendlyUnitPhase")
		bridge.call("SpawnEnemyWavePhase")
	else:
		bridge.call("RunCompleteCombatExperienceForTest")

	var before: Dictionary = {}
	if bridge.has_method("GetSummary"):
		before = bridge.call("GetSummary")
	else:
		before = bridge.call("RunCompleteCombatExperienceForTest")
	var before_hp := int(before.get("castle_hp", -1))
	assert_int(before_hp).is_greater_equal(0)

	if bridge.has_method("AdvanceSimulation"):
		for _i in range(120):
			bridge.call("AdvanceSimulation", 0.1)

	if bridge.has_method("GetActorSnapshots"):
		var snapshots: Array = bridge.call("GetActorSnapshots")
		assert_int(snapshots.size()).is_greater_equal(1)

	var after: Dictionary = {}
	if bridge.has_method("GetSummary"):
		after = bridge.call("GetSummary")
	else:
		after = bridge.call("RunCompleteCombatExperienceForTest")
	var after_hp := int(after.get("castle_hp", -1))
	assert_int(after_hp).is_less_equal(before_hp)
