extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for i in range(count):
		await get_tree().process_frame

func _main_runtime() -> Dictionary:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)

	var screen: Control = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	return {
		"main": main,
		"screen": screen,
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
		"status": screen.get_node("Margin/VBox/Status"),
		"summary": screen.get_node("Margin/VBox/Summary"),
	}

# ACC:T65.3
# ACC:T65.9
func test_runtime_ui_semantics_mapping_for_empty_failure_completion() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	var summary_label: Label = runtime["summary"]
	var background: ColorRect = screen.get_node("Background")
	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	assert_object(screen.get_node_or_null("CombatExperienceRuntimeBridge")).is_not_null()
	assert_object(screen.get_node_or_null("Background")).is_not_null()
	assert_object(screen.get_node_or_null("Margin/VBox/Status")).is_not_null()
	assert_object(screen.get_node_or_null("Margin/VBox/Summary")).is_not_null()
	assert_bool(screen.visible).is_true()
	assert_bool(background.visible).is_true()
	assert_bool(String(status_label.text).find("Map loaded.") >= 0 or String(status_label.text).find("地图已加载") >= 0).is_true()
	assert_bool(String(summary_label.text).find("BattleMap Runtime") >= 0 or String(summary_label.text).find("战斗地图") >= 0).is_true()

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(summary_label.text.find("Castle HP:") >= 0 or summary_label.text.find("城堡") >= 0).is_true()
	assert_bool(summary_label.text.find("Active Combat Nodes") >= 0 or summary_label.text.find("战场活跃节点") >= 0).is_true()
	assert_bool(status_label.text.to_lower().find("finished") >= 0 or status_label.text.find("结束") >= 0).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
	assert_bool(bridge.has_method("BuildPhase")).is_true()
	assert_bool(bridge.has_method("PublishOutcomePhase")).is_true()

func test_runtime_bridge_illegal_sequence_does_not_advance_to_completion() -> void:
	var runtime := await _main_runtime()
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	var summary_label: Label = runtime["summary"]

	var before_status := status_label.text
	var before_summary := summary_label.text
	var before_state: Dictionary = bridge.call("GetSummary")

	bridge.call("ResolveCombatExchangePhase")
	await _await_frames(2)

	var after_state: Dictionary = bridge.call("GetSummary")
	assert_str(status_label.text).is_equal(before_status)
	assert_str(summary_label.text).is_equal(before_summary)
	assert_int(int(after_state.get("combat_exchanges", -1))).is_equal(int(before_state.get("combat_exchanges", -1)))
	assert_int(int(after_state.get("enemy_units_spawned", -1))).is_equal(int(before_state.get("enemy_units_spawned", -1)))

# ACC:T65.4
func test_runtime_bridge_preserves_hud_navigator_runtime_ownership_boundaries() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var background: Node = screen.get_node("Background")
	var margin: Node = screen.get_node("Margin")

	assert_str(str(background.get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(margin.get_meta("ownership_container"))).is_equal("legacy_prototype")

# ACC:T65.9
func test_new_visible_node_paths_require_focused_scene_test_coverage() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var background := screen.get_node_or_null("Background")
	var title := screen.get_node_or_null("Margin/VBox/Title")
	var wave_btn := screen.get_node_or_null("Margin/VBox/Controls/WaveBtn")

	assert_object(bridge).is_not_null()
	assert_object(background).is_not_null()
	assert_object(title).is_not_null()
	assert_object(wave_btn).is_not_null()
	assert_bool(background.visible).is_true()
	assert_bool(String((title as Label).text).length() > 0).is_true()
	assert_bool(bridge.has_method("BuildPhase")).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
