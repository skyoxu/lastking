extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _ensure_event_bus() -> void:
	var bus := get_node_or_null("/root/EventBus")
	if bus == null:
		bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
		bus.name = "EventBus"
		get_tree().root.add_child(auto_free(bus))

func _ensure_game_manager() -> Node:
	var existing := get_node_or_null("/root/GameManager")
	if existing != null:
		return existing
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	manager.name = "GameManager"
	get_tree().root.add_child(auto_free(manager))
	return manager

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func _remaining_seconds(label_text: String) -> float:
	var parts := label_text.split(":")
	var tail := parts[parts.size() - 1] if parts.size() > 0 else label_text
	var cleaned := tail.replace("s", "").strip_edges()
	return float(cleaned)

func _main_runtime() -> Dictionary:
	_ensure_event_bus()
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(3)

	return {
		"main": main,
		"screen": main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen"),
		"hud": main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen/BattleHud"),
		"manager": manager,
	}

func test_battle_hud_top_bar_should_present_formal_metric_panels() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var top_bar: Control = hud.get_node("TopBar")
	var hbox: HBoxContainer = hud.get_node("TopBar/HBox")
	var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
	var phase_label: Label = hud.get_node("TopBar/HBox/PhaseLabel")
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
	var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")
	var speed_controls: HBoxContainer = hud.get_node("TopBar/HBox/SpeedControls")

	assert_bool(hud.visible).is_true()
	assert_bool(top_bar.visible).is_true()
	assert_bool(top_bar.get_theme_stylebox("panel") != null).is_true()
	assert_int(hbox.get_child_count()).is_greater_equal(6)
	assert_int(speed_controls.get_child_count()).is_greater_equal(3)
	assert_bool(day_label.text.find("1") >= 0).is_true()
	assert_bool(phase_label.text.find("Day") >= 0 or phase_label.text.find("白天") >= 0).is_true()
	assert_bool(cycle_label.text.find("Remaining") >= 0 or cycle_label.text.find("剩余") >= 0).is_true()
	assert_str(hp_label.text).contains("100/100")
	assert_int(int(bridge.call("GetSummary").get("castle_hp", -1))).is_equal(100)

func test_battle_hud_top_bar_should_track_battlemap_castle_hp_instead_of_default_zero() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

	assert_str(hp_label.text).contains("100/100")
	bridge.call("ConfigureDurabilityForTest", 42, 18)
	await _await_frames(2)
	assert_str(hp_label.text).contains("42/100")

func test_battle_hud_top_bar_should_track_enemy_count_in_formal_status_bar() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var enemies_label: Label = hud.get_node("TopBar/HBox/EnemiesLabel")

	assert_bool(enemies_label.text.find("0") >= 0).is_true()
	bridge.call("SpawnEnemyWavePhase")
	await _await_frames(2)
	assert_bool(enemies_label.text.find("2") >= 0).is_true()

func test_battle_hud_top_bar_should_switch_between_day_and_night_remaining_labels() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
	var phase_label: Label = hud.get_node("TopBar/HBox/PhaseLabel")
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")

	assert_bool(day_label.text.find("1") >= 0).is_true()
	assert_bool(phase_label.text.find("Day") >= 0 or phase_label.text.find("白天") >= 0).is_true()
	var initial_cycle_seconds := _remaining_seconds(cycle_label.text)
	assert_float(initial_cycle_seconds).is_less_equal(240.0)

	hud.call("SetDayPhaseForTest", 3, false, 45.0)
	await _await_frames(1)

	assert_bool(day_label.text.find("3") >= 0).is_true()
	assert_bool(phase_label.text.find("Night") >= 0 or phase_label.text.find("黑夜") >= 0).is_true()
	assert_bool(cycle_label.text.find("Remaining") >= 0 or cycle_label.text.find("剩余") >= 0).is_true()
	var night_cycle_seconds := _remaining_seconds(cycle_label.text)
	assert_float(night_cycle_seconds).is_less_equal(45.0)
	assert_float(night_cycle_seconds).is_greater(44.0)

func test_battle_hud_should_use_local_hud_and_hide_global_hud_when_entered_from_main() -> void:
	var runtime := await _main_runtime()
	var main: Control = runtime["main"]
	var local_hud: Control = runtime["hud"]
	var global_hud := main.get_node_or_null("RuntimeUi/HUD") as Control

	assert_object(global_hud).is_not_null()
	assert_bool(local_hud.visible).is_true()
	assert_bool(global_hud.visible).is_false()

func test_battle_hud_top_bar_speed_controls_should_apply_pause_and_resume() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var manager: Node = runtime["manager"]
	var pause_button: Button = hud.get_node("TopBar/HBox/SpeedControls/PauseButton")
	var one_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/OneXButton")
	var two_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/TwoXButton")

	pause_button.emit_signal("pressed")
	await _await_frames(1)
	var paused: Dictionary = manager.call("GetSpeedState")
	assert_bool(paused["is_paused"] == true).is_true()

	two_x_button.emit_signal("pressed")
	await _await_frames(1)
	var two_x: Dictionary = manager.call("GetSpeedState")
	assert_bool(two_x["is_paused"] == true).is_false()
	assert_int(int(two_x["scale_percent"])).is_equal(200)

	one_x_button.emit_signal("pressed")
	await _await_frames(1)
	var one_x: Dictionary = manager.call("GetSpeedState")
	assert_int(int(one_x["scale_percent"])).is_equal(100)

func test_battle_hud_should_auto_advance_day_night_cycle_without_player_input() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
	var before_seconds := _remaining_seconds(cycle_label.text)
	await _await_frames(10)
	var after_seconds := _remaining_seconds(cycle_label.text)
	assert_float(after_seconds).is_less(before_seconds)

func test_battle_hud_bottom_bar_should_present_formal_three_column_layout() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var bottom_bar: Control = hud.get_node("CombatHud/BottomBar")
	var root: HBoxContainer = hud.get_node("CombatHud/BottomBar/Root")
	var left_panel: Control = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel")
	var center_panel: Control = hud.get_node("CombatHud/BottomBar/Root/BattlePanel")
	var right_panel: Control = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel")
	var production_label: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/ProductionLabel")
	var combat_counts_label: Label = hud.get_node("CombatHud/BottomBar/Root/BattlePanel/VBox/CountsRow/CombatCountsLabel")
	var summary_label: Label = hud.get_node("CombatHud/BottomBar/Root/BattlePanel/VBox/SummaryLabel")
	var reserved_label: Label = hud.get_node("CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel")
	var hint_label: Label = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/HintLabel")
	var tower_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot")
	var residence_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot")

	assert_bool(bottom_bar.visible).is_true()
	assert_float(bottom_bar.size.y).is_equal(196.0)
	assert_object(root).is_not_null()
	assert_object(left_panel).is_not_null()
	assert_object(center_panel).is_not_null()
	assert_object(right_panel).is_not_null()
	assert_bool(production_label.text.find("scripted battle buildings") < 0).is_true()
	assert_bool(summary_label.text.find("Summary") < 0 or summary_label.text.find("摘要") < 0).is_true()
	assert_bool(reserved_label.text.find("reserved") < 0 and reserved_label.text.find("预留") < 0).is_true()
	assert_bool(hint_label.text.find("placeholder") < 0 and hint_label.text.find("占位") < 0).is_true()
	assert_bool(combat_counts_label.text.find("/99") < 0).is_true()
	assert_bool(tower_slot.visible).is_false()
	assert_bool(residence_slot.visible).is_false()

func test_battle_hud_should_open_battle_settings_menu_from_top_bar() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var manager: Node = runtime["manager"]
	var hud: Control = runtime["hud"]
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")
	var settings_menu: Control = screen.get_node("BattleSettingsMenu")
	var return_button: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn")
	var close_button: Button = screen.get_node("BattleSettingsMenu/VBox/Panel/SettingsPanel/VBox/Buttons/CloseBtn")

	assert_bool(settings_menu.visible).is_false()
	settings_button.emit_signal("pressed")
	await _await_frames(1)
	assert_bool(settings_menu.visible).is_true()
	assert_bool(close_button.visible).is_false()
	var paused: Dictionary = manager.call("GetSpeedState")
	assert_bool(paused["is_paused"] == true).is_true()

	return_button.emit_signal("pressed")
	await _await_frames(1)
	assert_bool(settings_menu.visible).is_false()

func test_battle_hud_settings_menu_should_resume_previous_speed_state_when_returning() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var manager: Node = runtime["manager"]
	var hud: Control = runtime["hud"]
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")
	var return_button: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn")
	var two_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/TwoXButton")

	two_x_button.emit_signal("pressed")
	await _await_frames(1)
	var before_menu: Dictionary = manager.call("GetSpeedState")
	assert_int(int(before_menu["scale_percent"])).is_equal(200)
	assert_bool(before_menu["is_paused"] == true).is_false()

	settings_button.emit_signal("pressed")
	await _await_frames(1)
	var paused: Dictionary = manager.call("GetSpeedState")
	assert_bool(paused["is_paused"] == true).is_true()

	return_button.emit_signal("pressed")
	await _await_frames(1)
	var resumed: Dictionary = manager.call("GetSpeedState")
	assert_bool(resumed["is_paused"] == true).is_false()
	assert_int(int(resumed["scale_percent"])).is_equal(200)

func test_battle_screen_should_isolate_legacy_prototype_root_from_formal_hud_layers() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var legacy_root: Control = screen.get_node("LegacyPrototypeRoot")
	var formal_hud: Control = screen.get_node("BattleHud")

	assert_object(legacy_root).is_not_null()
	assert_bool(legacy_root.visible).is_true()
	assert_bool(legacy_root.has_meta("ownership_container")).is_true()
	assert_str(str(legacy_root.get_meta("ownership_container"))).is_equal("legacy_prototype")
	assert_object(formal_hud).is_not_null()
