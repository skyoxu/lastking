extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await await_idle_frame()

func _screen_runtime() -> Dictionary:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(screen)
	await _await_frames(3)
	return {
		"screen": screen,
		"hud": screen.get_node("BattleHud"),
		"battlefield": screen.get_node("Background"),
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
	}

func test_clicking_invalid_or_occupied_slot_should_keep_building_unplaced() -> void:
	var runtime := await _screen_runtime()
	var hud: Node = runtime["hud"]
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var bridge: Node = runtime["bridge"]

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)
	battlefield.emit_signal("battlefield_slot_clicked", "RightOuterFieldSlot_00_00")
	await _await_frames(2)

	var prompt_panel: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel")
	var prompt_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")
	var after_invalid: Dictionary = bridge.call("GetSummary")
	assert_bool(after_invalid.get("mg_tower_built", false) == true).is_false()
	assert_bool(prompt_panel.visible).is_true()
	assert_str(prompt_label.text.strip_edges()).is_not_empty()

	battlefield.emit_signal("battlefield_slot_clicked", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	var after_legal: Dictionary = bridge.call("GetSummary")
	assert_bool(after_legal.get("mg_tower_built", false) == true).is_true()

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)
	var occupied_visual: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	assert_str(str(occupied_visual["overlay_state"])).is_equal("overlay_illegal")

	battlefield.emit_signal("battlefield_slot_clicked", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	var after_occupied: Dictionary = bridge.call("GetSummary")
	assert_bool(after_occupied.get("mg_tower_built", false) == true).is_true()
	assert_bool(prompt_panel.visible).is_true()
	assert_str(prompt_label.text.strip_edges()).is_not_empty()
