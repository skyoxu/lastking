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


func _bridge_summary_metrics(bridge: Node) -> Dictionary:
	if not bridge.has_method("GetSummary"):
		return {}
	var payload: Variant = bridge.call("GetSummary")
	if payload is Dictionary:
		return payload
	return {}


func _hud_count(main: Node) -> int:
	var runtime_ui := main.get_node("RuntimeUi")
	var count := 0
	for child in runtime_ui.get_children():
		if str(child.name) == "HUD":
			count += 1
	return count


func _is_visible_inside_viewport(control: Control, viewport_size: Vector2) -> bool:
	var top_left := control.global_position
	var bottom_right := top_left + control.size
	return control.visible \
		and top_left.x >= 0.0 \
		and top_left.y >= 0.0 \
		and bottom_right.x <= viewport_size.x \
		and bottom_right.y <= viewport_size.y


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
func test_non_battlefield_layout_perturbation_should_not_shift_header_or_footer() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(540.0, 320.0)
	await _await_frames(2)

	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var controls: Control = screen.get_node("Margin/VBox/Controls")
	var title_before := title.global_position
	var metrics_before := metrics_help.global_position

	controls.position = controls.position + Vector2(80.0, 0.0)
	await _await_frames(1)

	assert_that(title.global_position).is_equal(title_before)
	assert_that(metrics_help.global_position).is_equal(metrics_before)


# ACC:T55.1
func test_1440x900_frame_keeps_three_player_visible_bands_simultaneously_visible() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)

	var viewport := Vector2(1440.0, 900.0)
	var background: Control = screen.get_node("Background")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var path: Line2D = screen.get_node("Background/Path")
	var background_top_before := background.global_position.y
	var background_height_before := background.size.y
	var title_top_before := title.global_position.y
	var title_height_before := title.size.y
	var metrics_top_before := metrics_help.global_position.y
	var metrics_height_before := metrics_help.size.y

	assert_bool(_is_visible_inside_viewport(title, viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(background, viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(metrics_help, viewport)).is_true()

	var title_mid := title.global_position.y + title.size.y * 0.5
	var midpoint_index := int(path.points.size() * 0.5)
	var battlefield_mid := background.global_position.y + path.points[midpoint_index].y
	var bottom_mid := metrics_help.global_position.y + metrics_help.size.y * 0.5
	assert_float(title_mid).is_less(battlefield_mid)
	assert_float(battlefield_mid).is_less(bottom_mid)

	# Resize stability: top/bottom bands stay anchored and keep their heights.
	screen.size = Vector2(1280.0, 720.0)
	await _await_frames(2)
	screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)
	assert_float(background.global_position.y).is_equal(background_top_before)
	assert_float(background.size.y).is_equal(background_height_before)
	assert_float(title.global_position.y).is_equal(title_top_before)
	assert_float(title.size.y).is_equal(title_height_before)
	assert_float(metrics_help.global_position.y).is_equal(metrics_top_before)
	assert_float(metrics_help.size.y).is_equal(metrics_height_before)


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
func test_battle_map_runtime_summary_should_distinguish_empty_progressed_and_completion_states() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var summary_label: Label = screen.get_node("Margin/VBox/Summary")

	var empty_status := String(status_label.text)
	var empty_summary := String(summary_label.text)
	var empty_metrics := _bridge_summary_metrics(bridge)
	assert_bool(empty_status.find("loaded") >= 0 or empty_status.find("加载") >= 0).is_true()
	assert_bool(empty_status.find("finished") < 0 and empty_status.find("结束") < 0).is_true()
	assert_bool(empty_status.length() > 0).is_true()
	assert_bool(empty_summary.length() > 0).is_true()
	assert_int(int(empty_metrics["friendly_units_deployed"])).is_equal(0)
	assert_int(int(empty_metrics["enemy_units_spawned"])).is_equal(0)
	assert_int(int(empty_metrics["combat_exchanges"])).is_equal(0)
	assert_int(int(empty_metrics["dead_units_retired"])).is_equal(0)
	await _await_frames(3)
	assert_str(status_label.text).is_equal(empty_status)
	assert_str(summary_label.text).is_equal(empty_summary)

	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var failure_status := String(status_label.text)
	assert_bool(failure_status.find("cleanup") >= 0 or failure_status.find("清理") >= 0).is_true()
	assert_bool(failure_status != empty_status).is_true()
	await _await_frames(2)
	assert_str(status_label.text).is_equal(failure_status)

	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	for _i in range(120):
		if bridge.has_method("AdvanceSimulation"):
			bridge.call("AdvanceSimulation", 0.1)
	await _await_frames(1)

	var progressed_status := String(status_label.text)
	var progressed_summary := String(summary_label.text)
	var progressed_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(progressed_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(progressed_metrics["castle_hp"])).is_less(int(empty_metrics["castle_hp"]))
	assert_int(int(progressed_metrics["friendly_units_deployed"])).is_equal(0)
	assert_bool(progressed_status != empty_status).is_true()
	assert_bool(progressed_status != failure_status).is_true()
	assert_bool(progressed_summary != empty_summary).is_true()
	await _await_frames(3)
	assert_str(status_label.text).is_equal(progressed_status)
	assert_str(summary_label.text).is_equal(progressed_summary)

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var completion_status := String(status_label.text)
	var completion_summary := String(summary_label.text)
	var completion_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(completion_metrics["friendly_units_deployed"])).is_greater_equal(1)
	assert_int(int(completion_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(completion_metrics["combat_exchanges"])).is_greater_equal(1)
	assert_bool(completion_status.find("finished") >= 0 or completion_status.find("结束") >= 0).is_true()
	assert_bool(completion_status.find("cleanup") < 0 and completion_status.find("清理") < 0).is_true()
	assert_bool(completion_status != progressed_status).is_true()
	assert_bool(completion_status != failure_status).is_true()
	assert_bool(completion_summary != progressed_summary).is_true()
	assert_bool(completion_summary.find("HP") >= 0 or completion_summary.find("生命") >= 0).is_true()
	assert_bool(completion_summary.find("Friendly") >= 0 or completion_summary.find("友军") >= 0).is_true()
	assert_bool(completion_summary.find("Enemy") >= 0 or completion_summary.find("敌军") >= 0).is_true()
	await _await_frames(3)
	assert_str(status_label.text).is_equal(completion_status)
	assert_str(summary_label.text).is_equal(completion_summary)

	assert_bool(progressed_metrics != completion_metrics).is_true()
	assert_bool(empty_metrics != progressed_metrics).is_true()


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

	var summary_before: Dictionary = {}
	if bridge.has_method("GetSummary"):
		summary_before = bridge.call("GetSummary")
	else:
		summary_before = bridge.call("RunCompleteCombatExperienceForTest")
	var before_hp := int(summary_before.get("castle_hp", -1))
	assert_int(before_hp).is_greater_equal(0)

	if bridge.has_method("AdvanceSimulation"):
		for _i in range(120):
			bridge.call("AdvanceSimulation", 0.1)

	if bridge.has_method("GetActorSnapshots"):
		var snapshots: Array = bridge.call("GetActorSnapshots")
		assert_int(snapshots.size()).is_greater_equal(1)

	var summary_after: Dictionary = {}
	if bridge.has_method("GetSummary"):
		summary_after = bridge.call("GetSummary")
	else:
		summary_after = bridge.call("RunCompleteCombatExperienceForTest")
	var after_hp := int(summary_after.get("castle_hp", -1))
	assert_int(after_hp).is_less_equal(before_hp)
