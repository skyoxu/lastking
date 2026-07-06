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

func test_formal_hud_should_expose_sniper_tower_build_slot_and_actions() -> void:
	var runtime := await _screen_runtime()
	var hud: Control = runtime["hud"]
	var screen: Control = runtime["screen"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var sniper_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/SniperTowerSlot")
	var sniper_title: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/SniperTowerSlot/Card/Title")

	assert_object(sniper_slot).is_not_null()
	assert_bool(sniper_slot.visible).is_true()
	assert_bool(sniper_title.text.strip_edges().is_empty()).is_false()

	hud.call("RequestBattleAction", "select_tower_beta")
	await _await_frames(2)
	var selection_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(selection_preview.get("selection_id", ""))).is_equal("tower_beta")

	controller.call("cancel_active_placement")
	await _await_frames(1)
	controller.call("begin_drag_building", "tower_beta")
	await _await_frames(2)
	var drag_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(drag_preview.get("selection_id", ""))).is_equal("tower_beta")
	assert_bool(drag_preview.get("visible", false) == true).is_true()
	controller.call("cancel_active_placement")
	await _await_frames(1)

func test_invalid_drag_release_should_report_specific_reason_for_region_and_occupied_slot() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var bridge: Node = runtime["bridge"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var prompt_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")
	var presentation_controller: Node = screen.get_node("PresentationController")

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "RightOuterFieldSlot_00_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "RightOuterFieldSlot_00_00")
	await _await_frames(2)
	assert_str(prompt_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.wrong_region")))

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	assert_bool(bridge.call("GetSummary").get("mg_tower_built", false) == true).is_true()
	assert_int(int(bridge.call("GetSummary").get("resource_gold", -1))).is_equal(60)

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	assert_str(prompt_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.slot_occupied")))

	bridge.call("ConfigureResourcesForTest", 20, 44, 26)
	hud.call("RefreshBottomBarFromRuntimeForTest")
	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_04_00")
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_04_00")
	assert_str(prompt_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.insufficient_resources")))

func test_drag_hover_should_keep_invalid_cells_as_red_overlay_without_reason_bubble() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var bubble: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/PlacementReasonBubble")
	var bubble_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/PlacementReasonBubble/BubbleLabel")

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "RightOuterFieldSlot_00_00")
	await _await_frames(1)
	var wrong_region_visual: Dictionary = battlefield.call("read_slot_visual", "RightOuterFieldSlot_00_00")
	assert_str(str(wrong_region_visual.get("overlay_state", ""))).is_equal("overlay_illegal")
	assert_bool(bubble.visible).is_false()
	assert_str(bubble_label.text).is_empty()

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	var occupied_visual: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	assert_str(str(occupied_visual.get("overlay_state", ""))).is_equal("overlay_illegal")
	assert_bool(bubble.visible).is_false()
	assert_str(bubble_label.text).is_empty()

func test_build_drag_mode_should_switch_build_button_to_cancel_and_lock_other_actions() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var build_action: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction")
	var wave_action: Button = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var exchange_action: Button = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/ExchangeAction")
	var cleanup_action: Button = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/CleanupAction")
	var finish_action: Button = hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(2)

	assert_str(build_action.text.strip_edges()).is_not_empty()
	assert_bool(wave_action.disabled).is_true()
	assert_bool(exchange_action.disabled).is_true()
	assert_bool(cleanup_action.disabled).is_true()
	assert_bool(finish_action.disabled).is_true()

	controller.call("cancel_active_placement")
	await _await_frames(2)

	assert_str(build_action.text.strip_edges()).is_not_empty()
	assert_bool(wave_action.disabled).is_false()

func test_drag_release_outside_slot_should_cancel_drag_without_placing() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	assert_bool(controller.call("has_active_placement") == true).is_true()

	controller.call("handle_pointer_release_without_slot")
	await _await_frames(1)

	var summary: Dictionary = bridge.call("GetSummary")
	var preview: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(controller.call("has_active_placement") == false).is_true()
	assert_bool(summary.get("mg_tower_built", false) == true).is_false()
	assert_bool(preview.get("visible", true) == false).is_true()
