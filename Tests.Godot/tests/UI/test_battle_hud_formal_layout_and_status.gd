extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
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

	return {
		"main": main,
		"screen": main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen"),
		"hud": main.get_node("RuntimeUi/HUD"),
	}


func test_battle_hud_top_bar_should_present_formal_metric_panels() -> void:
	var runtime := await _main_runtime()
	var hud: Node = runtime["hud"]
	var top_bar: Control = hud.get_node("TopBar")
	var hbox: HBoxContainer = hud.get_node("TopBar/HBox")
	var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
	var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")
	var speed_controls: HBoxContainer = hud.get_node("TopBar/HBox/SpeedControls")

	assert_bool(top_bar.get_theme_stylebox("panel") != null).is_true()
	assert_bool(day_label.get_theme_stylebox("normal") != null).is_true()
	assert_bool(cycle_label.get_theme_stylebox("normal") != null).is_true()
	assert_bool(hp_label.get_theme_stylebox("normal") != null).is_true()
	assert_int(hbox.get_child_count()).is_greater_equal(4)
	assert_int(speed_controls.get_child_count()).is_equal(3)


func test_battle_hud_action_cards_should_surface_runtime_phase_statuses() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = runtime["hud"]

	var build_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/BuildAction/Frame/Content/Status")
	var wave_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/WaveAction/Frame/Content/Status")
	var exchange_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/ExchangeAction/Frame/Content/Status")
	var cleanup_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/CleanupAction/Frame/Content/Status")
	var finish_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/FinishAction/Frame/Content/Status")

	assert_str(build_status.text).contains("Ready")
	assert_str(wave_status.text).contains("Ready")
	assert_str(exchange_status.text).contains("Locked")
	assert_str(cleanup_status.text).contains("Locked")
	assert_str(finish_status.text).contains("Locked")

	screen.get_node("Margin/VBox/Controls/WaveBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(wave_status.text).contains("Cooldown")
	assert_str(exchange_status.text).contains("Ready")
	assert_str(cleanup_status.text).contains("Ready")
	assert_str(finish_status.text).contains("Locked")

	screen.get_node("Margin/VBox/Controls/ExchangeBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(exchange_status.text).contains("Done")
	assert_str(finish_status.text).contains("Ready")

	screen.get_node("Margin/VBox/Controls/CleanupBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(cleanup_status.text).contains("Done")

	screen.get_node("Margin/VBox/Controls/FinishBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(finish_status.text).contains("Done")


func test_battle_hud_should_be_visibly_layered_above_battlemap_and_feedback_panels_enabled() -> void:
	var runtime := await _main_runtime()
	var main: Control = runtime["main"]
	var hud: Control = runtime["hud"]
	var screen_root: Control = main.get_node("RuntimeUi/ScreenRoot")
	var feedback_layer: Control = hud.get_node("FeedbackLayer")
	var bottom_bar: Control = hud.get_node("CombatHud/BottomBar")

	assert_bool(hud.visible).is_true()
	assert_bool(bottom_bar.visible).is_true()
	assert_bool(feedback_layer.visible).is_true()
	assert_int(hud.get_index()).is_greater(screen_root.get_index())


func test_battle_hud_action_cards_should_drive_battlemap_operations_without_legacy_buttons() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var status: Label = screen.get_node("Margin/VBox/Status")
	var legacy_controls: Control = screen.get_node("Margin/VBox/Controls")
	var operation_controller: Node = screen.get_node("OperationController")
	var finish_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/FinishAction/Frame/Content/Status")

	assert_bool(legacy_controls.visible).is_false()

	hud.call("RequestBattleAction", "build")
	await _await_frames(2)
	assert_bool(status.text.to_lower().find("build") >= 0).is_true()

	hud.call("RequestBattleAction", "wave")
	await _await_frames(2)
	assert_bool(operation_controller.call("is_wave_started")).is_true()

	hud.call("RequestBattleAction", "exchange")
	await _await_frames(2)
	assert_bool(operation_controller.call("is_combat_resolved")).is_true()

	hud.call("RequestBattleAction", "cleanup")
	await _await_frames(2)
	assert_bool(operation_controller.call("is_cleanup_completed")).is_true()

	hud.call("RequestBattleAction", "finish")
	await _await_frames(2)
	assert_str(finish_status.text).contains("Done")
