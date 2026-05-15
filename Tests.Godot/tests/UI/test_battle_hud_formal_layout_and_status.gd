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
