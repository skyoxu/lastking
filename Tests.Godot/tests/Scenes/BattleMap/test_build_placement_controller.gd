extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func _screen_runtime() -> Dictionary:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)
	return {
		"screen": screen,
		"hud": screen.get_node("BattleHud"),
		"battlefield": screen.get_node("Background"),
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
	}

func test_build_selection_should_project_region_legality_from_building_definition() -> void:
	var runtime := await _screen_runtime()
	var hud: Node = runtime["hud"]
	var battlefield: Node = runtime["battlefield"]

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)

	var tower_inner: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_00_00")
	var tower_outer: Dictionary = battlefield.call("read_slot_visual", "LeftOuterFieldSlot_00_00")
	assert_str(str(tower_inner["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(tower_inner["overlay_tint"])).is_equal("warm")
	assert_str(str(tower_outer["overlay_state"])).is_equal("overlay_illegal")

	hud.call("RequestBattleAction", "select_barracks")
	await _await_frames(2)

	var barracks_inner: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_00_00")
	var barracks_outer: Dictionary = battlefield.call("read_slot_visual", "LeftOuterFieldSlot_00_00")
	assert_str(str(barracks_inner["overlay_state"])).is_equal("overlay_illegal")
	assert_str(str(barracks_outer["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(barracks_outer["overlay_tint"])).is_equal("cool")

func test_drag_hover_should_refresh_preview_legality_for_current_slot() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(2)
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_02_00")
	await _await_frames(1)
	var inner_hover: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_02_00")
	assert_str(str(inner_hover["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(inner_hover["overlay_tint"])).is_equal("warm")

	controller.call("handle_battlefield_slot_hovered", "LeftOuterFieldSlot_02_00")
	await _await_frames(1)
	var outer_hover: Dictionary = battlefield.call("read_slot_visual", "LeftOuterFieldSlot_02_00")
	assert_str(str(outer_hover["overlay_state"])).is_equal("overlay_illegal")

func test_drag_release_should_place_only_after_release_on_legal_slot() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var bridge: Node = runtime["bridge"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(2)
	var before_release: Dictionary = bridge.call("GetSummary")
	assert_bool(before_release.get("mg_tower_built", false) == true).is_false()

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)

	var after_release: Dictionary = bridge.call("GetSummary")
	var placed_slot: Dictionary = battlefield.call("read_slot_visual", "InnerCastleRegionSlot_03_00")
	assert_bool(after_release.get("mg_tower_built", false) == true).is_true()
	assert_str(str(placed_slot["overlay_state"])).is_equal("overlay_hidden")

func test_drag_preview_should_switch_between_legal_and_illegal_visual_states() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var initial_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(initial_preview.get("visible", false) == true).is_true()
	assert_str(str(initial_preview.get("selection_id", ""))).is_equal("tower_alpha")

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_01_00")
	await _await_frames(1)
	var legal_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(legal_preview.get("visual_state", ""))).is_equal("legal")

	controller.call("handle_battlefield_slot_hovered", "LeftOuterFieldSlot_01_00")
	await _await_frames(1)
	var illegal_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(illegal_preview.get("visual_state", ""))).is_equal("illegal")

func test_drag_preview_should_follow_pointer_then_snap_to_hovered_slot() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "farm_alpha")
	controller.call("update_drag_pointer_position", Vector2(220, 180))
	await _await_frames(1)
	var free_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(free_preview.get("visual_state", ""))).is_equal("idle")
	assert_that(free_preview.get("position", Vector2.ZERO)).is_equal(Vector2(196, 156))

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_02_00")
	await _await_frames(1)
	var snapped_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(snapped_preview.get("slot_id", ""))).is_equal("InnerCastleRegionSlot_02_00")
	assert_str(str(snapped_preview.get("visual_state", ""))).is_equal("legal")
	assert_that(snapped_preview.get("position", Vector2.ZERO)).is_equal(Vector2(720, 0))

func test_drag_preview_should_expose_building_tooltip_and_status_text() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var presentation_controller: Node = screen.get_node("PresentationController")

	controller.call("begin_drag_building", "barracks_alpha")
	controller.call("update_drag_pointer_position", Vector2(320, 240))
	await _await_frames(1)
	var idle_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(idle_preview.get("tooltip_text", ""))).contains(str(presentation_controller.call("translate", "battlemap.building.barracks")))
	assert_str(str(idle_preview.get("tooltip_text", ""))).contains(str(presentation_controller.call("translate", "battlemap.drag_status.drag")))

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_01_00")
	await _await_frames(1)
	var invalid_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(invalid_preview.get("tooltip_text", ""))).contains(str(presentation_controller.call("translate", "battlemap.drag_status.invalid")))

func test_drag_preview_should_use_formal_building_sprite_state_instead_of_plain_color_rect_only() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var controller: Node = screen.get_node("BuildPlacementController")

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("update_drag_pointer_position", Vector2(240, 180))
	await _await_frames(1)
	var tower_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(tower_preview.get("selection_id", ""))).is_equal("tower_alpha")
	assert_str(str(tower_preview.get("preview_kind", ""))).is_equal("tower")
	assert_bool(tower_preview.get("has_texture", false) == true).is_true()

	controller.call("begin_drag_building", "barracks_alpha")
	controller.call("update_drag_pointer_position", Vector2(260, 180))
	await _await_frames(1)
	var barracks_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_str(str(barracks_preview.get("preview_kind", ""))).is_equal("barracks")
	assert_bool(barracks_preview.get("has_texture", false) == true).is_true()
	assert_str(str(barracks_preview.get("texture_path", ""))).contains("battlemap_build_preview_barracks")

func test_dragging_building_should_publish_bottom_bar_region_hint_and_selection_name() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var presentation_controller: Node = screen.get_node("PresentationController")
	var production_label: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/ProductionLabel")

	controller.call("begin_drag_building", "barracks_alpha")
	await _await_frames(2)
	var barracks_name := str(presentation_controller.call("translate", "battlemap.building.barracks"))
	var outer_hint := str(presentation_controller.call("translate", "battlemap.build_region.outer_field"))
	assert_str(production_label.text).contains(barracks_name)
	assert_str(production_label.text).contains(outer_hint)

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(2)
	var tower_name := str(presentation_controller.call("translate", "battlemap.building.tower"))
	var inner_hint := str(presentation_controller.call("translate", "battlemap.build_region.inner_castle"))
	assert_str(production_label.text).contains(tower_name)
	assert_str(production_label.text).contains(inner_hint)

func test_dragging_building_should_highlight_active_build_palette_button() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var hud: Control = runtime["hud"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var tower_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot")
	var barracks_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BarracksSlot")
	var residence_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot")

	controller.call("begin_drag_building", "barracks_alpha")
	await _await_frames(2)
	assert_float(barracks_slot.modulate.a).is_equal(1.0)
	assert_float(tower_slot.modulate.a).is_less(1.0)
	assert_float(residence_slot.modulate.a).is_less(1.0)

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(2)
	assert_float(tower_slot.modulate.a).is_equal(1.0)
	assert_float(barracks_slot.modulate.a).is_less(1.0)

	controller.call("cancel_active_placement")
	await _await_frames(1)
	assert_float(tower_slot.modulate.a).is_equal(1.0)
	assert_float(barracks_slot.modulate.a).is_equal(1.0)
	assert_float(residence_slot.modulate.a).is_equal(1.0)

func test_invalid_drag_release_should_report_specific_reason_for_region_and_occupied_slot() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var prompt_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")
	var presentation_controller: Node = screen.get_node("PresentationController")

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "LeftOuterFieldSlot_00_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "LeftOuterFieldSlot_00_00")
	await _await_frames(2)
	assert_str(prompt_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.wrong_region")))

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	assert_bool(bridge.call("GetSummary").get("mg_tower_built", false) == true).is_true()

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	assert_str(prompt_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.slot_occupied")))

func test_drag_hover_should_publish_slot_reason_overlay_for_invalid_cells() -> void:
	var runtime := await _screen_runtime()
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var controller: Node = screen.get_node("BuildPlacementController")
	var presentation_controller: Node = screen.get_node("PresentationController")
	var bubble: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/PlacementReasonBubble")
	var bubble_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/PlacementReasonBubble/BubbleLabel")

	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "LeftOuterFieldSlot_00_00")
	await _await_frames(1)
	var wrong_region_visual: Dictionary = battlefield.call("read_slot_visual", "LeftOuterFieldSlot_00_00")
	assert_str(str(wrong_region_visual.get("overlay_state", ""))).is_equal("overlay_illegal")
	assert_bool(bubble.visible).is_true()
	assert_str(bubble_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.wrong_region")))

	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	controller.call("handle_battlefield_slot_released", "InnerCastleRegionSlot_03_00")
	await _await_frames(2)
	controller.call("begin_drag_building", "tower_alpha")
	controller.call("handle_battlefield_slot_hovered", "InnerCastleRegionSlot_03_00")
	await _await_frames(1)
	assert_str(bubble_label.text).contains(str(presentation_controller.call("translate", "battlemap.build_error.slot_occupied")))

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

func test_clicking_invalid_or_occupied_slot_should_keep_building_unplaced() -> void:
	var runtime := await _screen_runtime()
	var hud: Node = runtime["hud"]
	var screen: Control = runtime["screen"]
	var battlefield: Node = runtime["battlefield"]
	var bridge: Node = runtime["bridge"]

	hud.call("RequestBattleAction", "select_tower")
	await _await_frames(2)
	battlefield.emit_signal("battlefield_slot_clicked", "LeftOuterFieldSlot_00_00")
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
