extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _open_battlemap_screen() -> Dictionary:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	return {
		"main": main,
		"screen": screen,
		"hud": screen.get_node("BattleHud"),
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
	}


func test_local_hud_should_open_settings_and_resume_from_top_bar_action() -> void:
	var runtime := await _open_battlemap_screen()
	var screen: Node = runtime["screen"]
	var hud: Node = runtime["hud"]
	var settings_menu: Control = screen.get_node("BattleSettingsMenu")
	var return_btn: Button = screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn")
	var manager := get_node_or_null("/root/GameManager")
	assert_object(manager).is_not_null()

	manager.call("SetTwoX")
	await get_tree().process_frame
	assert_bool(settings_menu.visible).is_false()
	assert_bool(get_tree().paused).is_false()

	hud.call("RequestBattleAction", "open_settings")
	await get_tree().process_frame
	assert_bool(settings_menu.visible).is_true()
	assert_bool(get_tree().paused).is_true()

	return_btn.emit_signal("pressed")
	await get_tree().process_frame
	var speed_state: Dictionary = manager.call("GetSpeedState")
	assert_bool(settings_menu.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_int(int(speed_state.get("scale_percent", 0))).is_equal(200)


func test_defeat_modal_buttons_should_stay_clickable_in_local_scene() -> void:
	var runtime := await _open_battlemap_screen()
	var screen: Node = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var defeat_modal: PanelContainer = screen.get_node("DefeatOutcomeModal")
	var restart_btn: Button = screen.get_node("DefeatOutcomeModal/VBox/Actions/RestartBtn")
	var return_btn: Button = screen.get_node("DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn")

	bridge.call("ForceDefeatStateForTest", "wall_breached", 42, 0)
	screen.get_node("BattleHud").call("RequestBattleAction", "wave")
	await get_tree().process_frame

	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(restart_btn.disabled).is_false()
	assert_bool(return_btn.disabled).is_false()

	restart_btn.emit_signal("pressed")
	await get_tree().process_frame
	var reset_summary: Dictionary = bridge.call("GetSummary")
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_int(int(reset_summary.get("wall_hp", -1))).is_equal(100)
	assert_int(int(reset_summary.get("castle_hp", -1))).is_equal(100)
