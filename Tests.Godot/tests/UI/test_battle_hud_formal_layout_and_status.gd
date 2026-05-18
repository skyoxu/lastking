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

func test_battle_hud_top_bar_should_show_runtime_resources_in_formal_status_bar() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var resources_label: Label = hud.get_node("TopBar/HBox/ResourcesLabel")

	assert_str(resources_label.text).contains("120")
	assert_str(resources_label.text).contains("44")
	assert_str(resources_label.text).contains("26")

	bridge.call("ConfigureResourcesForTest", 70, 12, 19)
	hud.call("RefreshBottomBarFromRuntimeForTest")
	await _await_frames(2)

	assert_str(resources_label.text).contains("70")
	assert_str(resources_label.text).contains("12")
	assert_str(resources_label.text).contains("19")

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
	var local_hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var global_hud := screen.get_node_or_null("RuntimeUi/HUD") as Control

	assert_bool(local_hud.visible).is_true()
	assert_object(global_hud).is_null()

func test_battle_hud_should_remove_main_menu_and_fullscreen_combat_input_blockers() -> void:
	var runtime := await _main_runtime()
	var main: Control = runtime["main"]
	var hud: Control = runtime["hud"]
	var main_menu := main.get_node_or_null("RuntimeUi/MainMenu") as Control
	var combat_hud := hud.get_node("CombatHud") as Control
	var pause_button: Button = hud.get_node("TopBar/HBox/SpeedControls/PauseButton")
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")

	assert_object(main_menu).is_not_null()
	assert_bool(main_menu.visible).is_false()
	assert_int(int(main_menu.mouse_filter)).is_equal(Control.MOUSE_FILTER_IGNORE)
	assert_int(int(combat_hud.mouse_filter)).is_equal(Control.MOUSE_FILTER_PASS)
	assert_float(combat_hud.position.y).is_greater_equal(700.0)
	assert_float(combat_hud.size.y).is_less_equal(196.0)
	assert_bool(pause_button.disabled).is_false()
	assert_bool(settings_button.disabled).is_false()

func test_battle_screen_modal_layers_should_stack_above_hud_and_legacy_root_should_ignore_pointer() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var legacy_root: Control = screen.get_node("LegacyPrototypeRoot")
	var settlement_modal: Control = screen.get_node("DailySettlementModal")
	var victory_modal: Control = screen.get_node("VictoryOutcomeModal")
	var defeat_modal: Control = screen.get_node("DefeatOutcomeModal")

	assert_int(int(legacy_root.mouse_filter)).is_equal(Control.MOUSE_FILTER_IGNORE)
	assert_int(settlement_modal.z_index).is_greater(int(hud.z_index))
	assert_int(victory_modal.z_index).is_greater(int(hud.z_index))
	assert_int(defeat_modal.z_index).is_greater(int(hud.z_index))

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

func test_battle_hud_controls_should_keep_pause_safe_process_modes() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var pause_button: Button = hud.get_node("TopBar/HBox/SpeedControls/PauseButton")
	var one_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/OneXButton")
	var two_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/TwoXButton")
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")
	var build_action: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction")
	var wave_action: Button = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var settings_menu: Control = screen.get_node("BattleSettingsMenu")
	var return_button: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn")
	var main_menu_button: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToMainMenuBtn")

	assert_int(int(hud.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(pause_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(one_x_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(two_x_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(settings_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(build_action.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(wave_action.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(settings_menu.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(return_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_int(int(main_menu_button.process_mode)).is_equal(Node.PROCESS_MODE_ALWAYS)
	assert_bool(settings_menu.visible).is_false()
	assert_bool(return_button.disabled).is_false()
	assert_bool(main_menu_button.disabled).is_false()

func test_battle_hud_should_auto_advance_day_night_cycle_without_player_input() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
	var before_seconds := _remaining_seconds(cycle_label.text)
	await _await_frames(30)
	var after_seconds := _remaining_seconds(cycle_label.text)
	assert_float(after_seconds).is_less(before_seconds)

func test_battle_hud_should_force_runtime_back_to_active_when_entering_battle_map() -> void:
	_ensure_event_bus()
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	manager.call("SetPause")
	var paused_before: Dictionary = manager.call("GetSpeedState")
	assert_bool(paused_before["is_paused"] == true).is_true()

	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(5)

	var screen: Control = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var hud: Control = screen.get_node("BattleHud")
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
	var state_after_enter: Dictionary = manager.call("GetSpeedState")
	assert_bool(state_after_enter["is_paused"] == true).is_false()
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
	assert_float(bottom_bar.size.y).is_greater_equal(196.0)
	assert_object(root).is_not_null()
	assert_object(left_panel).is_not_null()
	assert_object(center_panel).is_not_null()
	assert_object(right_panel).is_not_null()
	assert_bool(production_label.text.find("scripted battle buildings") < 0).is_true()
	assert_bool(summary_label.text.find("Summary") < 0 or summary_label.text.find("摘要") < 0).is_true()
	assert_bool(reserved_label.text.find("reserved") < 0 and reserved_label.text.find("预留") < 0).is_true()
	assert_bool(hint_label.text.find("placeholder") < 0 and hint_label.text.find("占位") < 0).is_true()
	assert_bool(combat_counts_label.text.find("/99") < 0).is_true()
	assert_bool(tower_slot.visible).is_true()
	assert_bool(residence_slot.visible).is_true()
	assert_bool(tower_slot.disabled).is_false()
	assert_bool(residence_slot.disabled).is_false()

func test_battle_hud_building_palette_buttons_should_drive_formal_selection_feedback() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var battlefield_view: Node = screen.get_node("Background")

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)

	var tower_building: Dictionary = battlefield_view.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	var tower_range: Dictionary = battlefield_view.call("read_slot_visual", "InnerCastleRegionSlot_04_00")
	var tower_blocked_range: Dictionary = battlefield_view.call("read_slot_visual", "InnerCastleRegionSlot_05_00")
	assert_bool(str(tower_building["selection_owner"]) == "tower_alpha").is_true()
	assert_bool(str(tower_building["selection_category"]) == "defense").is_true()
	assert_bool(str(tower_building["feedback_channel"]) == "building_outline").is_true()
	assert_bool(str(tower_range["feedback_channel"]) == "defense_range").is_true()
	assert_bool(str(tower_blocked_range["selection_owner"]) == "tower_alpha").is_true()
	assert_bool(tower_blocked_range["range_clipped"] == true).is_true()

	hud.call("RequestBattleAction", "select_residence")
	await _await_frames(2)

	var residence_building: Dictionary = battlefield_view.call("read_slot_visual", "InnerCastleRegionSlot_06_00")
	var cleared_tower_building: Dictionary = battlefield_view.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	assert_bool(str(residence_building["selection_owner"]) == "farm_alpha").is_true()
	assert_bool(str(residence_building["selection_category"]) == "economy").is_true()
	assert_bool(str(residence_building["feedback_channel"]) == "economy_glow").is_true()
	assert_bool(str(cleared_tower_building["selection_owner"]) == "").is_true()

func test_battle_hud_building_palette_should_use_card_buttons_with_preview_icons() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var tower_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot")
	var barracks_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BarracksSlot")
	var residence_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot")

	assert_object(tower_slot.get_node_or_null("Card")).is_not_null()
	assert_object(tower_slot.get_node_or_null("Card/PreviewIcon")).is_not_null()
	assert_object(tower_slot.get_node_or_null("Card/Title")).is_not_null()
	assert_object(barracks_slot.get_node_or_null("Card/PreviewIcon")).is_not_null()
	assert_object(residence_slot.get_node_or_null("Card/PreviewIcon")).is_not_null()
	assert_int(tower_slot.text.length()).is_equal(0)
	assert_int(barracks_slot.text.length()).is_equal(0)
	assert_int(residence_slot.text.length()).is_equal(0)

func test_battle_hud_should_not_expose_raw_localization_keys_in_visible_labels() -> void:
	var runtime := await _main_runtime()
	var hud: Control = runtime["hud"]
	var visible_paths := [
		"TopBar/HBox/DayLabel",
		"TopBar/HBox/PhaseLabel",
		"TopBar/HBox/CycleRemainingLabel",
		"TopBar/HBox/HealthLabel",
		"TopBar/HBox/EnemiesLabel",
		"TopBar/HBox/SpeedStateLabel",
		"TopBar/HBox/SpeedControls/PauseButton",
		"TopBar/HBox/SpeedControls/OneXButton",
		"TopBar/HBox/SpeedControls/TwoXButton",
		"TopBar/HBox/SettingsButton",
		"CombatHud/BottomBar/Root/BuildingsPanel/VBox/TitleLabel",
		"CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction",
		"CombatHud/BottomBar/Root/BuildingsPanel/VBox/ProductionLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/TitleLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/CountsRow/CombatCountsLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/CountsRow/MoraleLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/CountsRow/BattlePressureLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/SummaryLabel",
		"CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/TitleLabel",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/ExchangeAction",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/CleanupAction",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction",
		"CombatHud/BottomBar/Root/SkillsPanel/VBox/HintLabel",
	]

	for path in visible_paths:
		var node := hud.get_node(path)
		var text := ""
		if node is Label:
			text = String((node as Label).text)
		elif node is Button:
			text = String((node as Button).text)
		assert_bool(text.find("hud.") < 0).is_true()

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
	await _await_frames(3)
	assert_bool(settings_menu.visible).is_true()
	assert_bool(close_button.visible).is_false()
	var paused: Dictionary = manager.call("GetSpeedState")
	assert_bool(paused["is_paused"] == true).is_true()

	return_button.emit_signal("pressed")
	await _await_frames(1)
	assert_bool(settings_menu.visible).is_false()

func test_battle_settings_menu_should_keep_embedded_settings_panel_inside_panel_bounds() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")
	var panel: Control = screen.get_node("BattleSettingsMenu/VBox/Panel")
	var settings_panel: Control = screen.get_node("BattleSettingsMenu/VBox/Panel/SettingsPanel")
	var settings_vbox: Control = screen.get_node("BattleSettingsMenu/VBox/Panel/SettingsPanel/VBox")
	var return_button: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn")

	settings_button.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(settings_vbox.global_position.x >= panel.global_position.x).is_true()
	assert_bool(settings_vbox.global_position.y >= panel.global_position.y).is_true()
	assert_bool(return_button.global_position.y >= panel.global_position.y + panel.size.y).is_true()
	assert_bool(settings_panel.global_position.y + settings_panel.size.y <= return_button.global_position.y).is_true()
	assert_int(int(settings_panel.mouse_filter)).is_equal(Control.MOUSE_FILTER_PASS)

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
