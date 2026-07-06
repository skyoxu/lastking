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

func test_clicking_legal_slot_should_place_building_and_clear_active_placement_overlay() -> void:
	var runtime := await _screen_runtime()
	var hud: Node = runtime["hud"]
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var bridge: Node = runtime["bridge"]

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)

	battlefield.emit_signal("battlefield_slot_clicked", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)

	var summary: Dictionary = bridge.call("GetSummary")
	var placed_slot: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	var another_slot: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_04_00")
	var prompt_panel: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel")
	assert_bool(summary.get("mg_tower_built", false) == true).is_true()
	assert_bool(bridge.has_node("Battlefield/MgTower")).is_true()
	assert_str(str(placed_slot["overlay_state"])).is_equal("overlay_hidden")
	assert_str(str(another_slot["overlay_state"])).is_equal("overlay_hidden")
	assert_bool(prompt_panel.visible).is_false()
