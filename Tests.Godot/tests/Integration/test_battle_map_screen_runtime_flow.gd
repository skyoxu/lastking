extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame


func _status_and_summary(screen: Node) -> Dictionary:
	var status: Label = screen.get_node("Margin/VBox/Status")
	var summary: Label = screen.get_node("Margin/VBox/Summary")
	return {
		"status": String(status.text),
		"summary": String(summary.text),
	}


func _hud_count(main: Node) -> int:
	var runtime_ui := main.get_node("RuntimeUi")
	var count := 0
	for child in runtime_ui.get_children():
		if str(child.name) == "HUD":
			count += 1
	return count


# ACC:T55.1
func test_narrow_layout_keeps_header_footer_fixed_when_only_battlefield_moves() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(540.0, 320.0)
	await _await_frames(2)

	var background: Control = screen.get_node("Background")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var title_before := title.global_position
	var metrics_before := metrics_help.global_position
	var background_before := background.global_position

	background.position = background.position + Vector2(-120.0, 0.0)
	await _await_frames(1)

	assert_float(background.global_position.x).is_equal(background_before.x - 120.0)
	assert_that(title.global_position).is_equal(title_before)
	assert_that(metrics_help.global_position).is_equal(metrics_before)


# ACC:T55.1
# ACC:T55.3
# ACC:T55.4
# ACC:T55.6
# ACC:T55.8
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


# ACC:T55.3
# ACC:T55.7
func test_battle_map_terminal_summary_should_stay_stable_without_state_change() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var snapshot_before := _status_and_summary(screen)
	assert_bool(String(snapshot_before["status"]).length() > 0).is_true()
	assert_bool(String(snapshot_before["summary"]).length() > 0).is_true()

	await _await_frames(5)
	var snapshot_after := _status_and_summary(screen)
	assert_that(snapshot_after).is_equal(snapshot_before)


# ACC:T55.2
# ACC:T55.4
func test_battle_map_cycle_should_keep_hud_singleton_and_navigator_ownership() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	assert_int(_hud_count(main)).is_equal(1)

	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(1)
	assert_object(main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen")).is_not_null()
	assert_int(_hud_count(main)).is_equal(1)

	nav.call("ClearCurrentScreen")
	await _await_frames(1)
	assert_object(main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen")).is_null()
	assert_int(_hud_count(main)).is_equal(1)
	assert_object(main.get_node_or_null("ScreenNavigator")).is_not_null()


# ACC:T55.7
# ACC:T55.9
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
