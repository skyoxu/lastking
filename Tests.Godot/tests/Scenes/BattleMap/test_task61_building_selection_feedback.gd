extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")
const BATTLE_MAP_SCREEN := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")

func _runtime_slot_snapshot(screen: Node, slot_id: String) -> Dictionary:
	var slot := screen.find_child(slot_id, true, false) as ColorRect
	assert_object(slot).is_not_null()
	return {
		"overlay_state": str(slot.get_meta("overlay_state", "overlay_hidden")),
		"overlay_tint": str(slot.get_meta("overlay_tint", "none")),
		"marker": str(slot.get_meta("marker", "none")),
		"frame": str(slot.get_meta("frame", "none")),
		"reason_text": str(slot.get_meta("reason_text", "")),
		"feedback_channel": str(slot.get_meta("feedback_channel", "none")),
		"selection_owner": str(slot.get_meta("selection_owner", "")),
		"selection_category": str(slot.get_meta("selection_category", "")),
		"outline_tint": str(slot.get_meta("outline_tint", "none")),
		"range_clipped": bool(slot.get_meta("range_clipped", false)),
	}

# acceptance: ACC:T61.1
func test_selected_category_feedback_stays_exclusive_and_switch_clears_previous_state() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"economy_slot": controller.LEGALITY_VALID_INNER})
	var economy_selected := controller.read_slot_visual("economy_slot")
	var defense_initial := controller.read_slot_visual("defense_slot")
	var linked_initial := controller.read_slot_visual("linked_unit_slot")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({"defense_slot": controller.LEGALITY_WALL})
	var economy_after_switch := controller.read_slot_visual("economy_slot")
	var defense_selected := controller.read_slot_visual("defense_slot")
	var linked_after_switch := controller.read_slot_visual("linked_unit_slot")

	assert_that(economy_selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(economy_selected["overlay_tint"]).is_equal("warm")
	assert_that(defense_initial["overlay_state"]).is_equal("overlay_hidden")
	assert_that(linked_initial["overlay_state"]).is_equal("overlay_hidden")
	assert_that(defense_selected["overlay_state"]).is_equal("overlay_illegal")
	assert_that(defense_selected["overlay_tint"]).is_equal("red")
	assert_that(defense_selected["marker"]).is_equal("blocker")
	assert_that(defense_selected["frame"]).is_equal("red")
	assert_that(economy_after_switch["overlay_state"]).is_equal("overlay_hidden")
	assert_that(economy_after_switch["overlay_tint"]).is_equal("none")
	assert_that(linked_after_switch["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.2
func test_non_linked_units_stay_unhighlighted_when_linked_unit_feedback_changes() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"linked_unit_slot": controller.LEGALITY_VALID_OUTER})

	var linked := controller.read_slot_visual("linked_unit_slot")
	var non_linked := controller.read_slot_visual("non_linked_unit_slot")
	var unrelated := controller.read_slot_visual("unrelated_slot")

	assert_that(linked["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked["overlay_tint"]).is_equal("cool")
	assert_that(linked["marker"]).is_equal("none")
	assert_that(linked["frame"]).is_equal("none")
	assert_that(non_linked["overlay_state"]).is_equal("overlay_hidden")
	assert_that(non_linked["overlay_tint"]).is_equal("none")
	assert_that(unrelated["overlay_state"]).is_equal("overlay_hidden")
	assert_that(unrelated["overlay_tint"]).is_equal("none")

# acceptance: ACC:T61.3
func test_entering_placement_mode_clears_selection_feedback_until_new_selection() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({
		"selected_slot": controller.LEGALITY_VALID_INNER,
		"linked_unit_slot": controller.LEGALITY_VALID_OUTER,
	})
	var selected_before := controller.read_slot_visual("selected_slot")
	var linked_before := controller.read_slot_visual("linked_unit_slot")

	controller.set_placement_context_active(false)
	controller.apply_legality_overlay({
		"selected_slot": controller.LEGALITY_WALL,
		"linked_unit_slot": controller.LEGALITY_WALL,
	})
	var hidden_during_inactive := controller.read_slot_visual("selected_slot")
	var linked_hidden_during_inactive := controller.read_slot_visual("linked_unit_slot")

	controller.set_placement_context_active(true)
	var still_hidden_before_new_selection := controller.read_slot_visual("new_slot")
	var linked_still_hidden_before_new_selection := controller.read_slot_visual("linked_unit_slot")
	controller.apply_legality_overlay({"new_slot": controller.LEGALITY_VALID_OUTER})
	var new_selected := controller.read_slot_visual("new_slot")
	var linked_after_new_selection := controller.read_slot_visual("linked_unit_slot")

	assert_that(selected_before["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked_before["overlay_state"]).is_equal("overlay_legal")
	assert_that(hidden_during_inactive["overlay_state"]).is_equal("overlay_hidden")
	assert_that(linked_hidden_during_inactive["overlay_state"]).is_equal("overlay_hidden")
	assert_that(hidden_during_inactive["overlay_tint"]).is_equal("none")
	assert_that(still_hidden_before_new_selection["overlay_state"]).is_equal("overlay_hidden")
	assert_that(linked_still_hidden_before_new_selection["overlay_state"]).is_equal("overlay_hidden")
	assert_that(new_selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked_after_new_selection["overlay_state"]).is_equal("overlay_hidden")
	assert_that(new_selected["overlay_tint"]).is_equal("cool")

# acceptance: ACC:T61.3
func test_clearing_selection_should_not_leave_stale_outline_or_range_feedback() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"selected_slot": controller.LEGALITY_WALL})
	var selected := controller.read_slot_visual("selected_slot")
	assert_that(selected["overlay_state"]).is_equal("overlay_illegal")
	assert_that(selected["overlay_tint"]).is_equal("red")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	var cleared_slot := controller.read_slot_visual("cleared_slot")
	var selected_after_clear := controller.read_slot_visual("selected_slot")

	assert_that(cleared_slot["overlay_state"]).is_equal("overlay_hidden")
	assert_that(cleared_slot["overlay_tint"]).is_equal("none")
	assert_that(selected_after_clear["overlay_state"]).is_equal("overlay_hidden")
	assert_that(selected_after_clear["overlay_tint"]).is_equal("none")

# acceptance: ACC:T61.4
func test_selection_feedback_transitions_are_deterministic_across_repeat_sequences() -> void:
	var first := CONTROLLER.new()
	var second := CONTROLLER.new()
	add_child(auto_free(first))
	add_child(auto_free(second))
	var sequence := [
		{"slot_a": first.LEGALITY_VALID_INNER},
		{"slot_b": first.LEGALITY_WALL},
		{"slot_a": first.LEGALITY_TEMP_INVALID},
		{"slot_b": first.LEGALITY_VALID_OUTER},
	]

	for item in sequence:
		first.apply_legality_overlay(item)
	for item in sequence:
		second.apply_legality_overlay(item)

	assert_that(first.read_slot_visual("slot_a")).is_equal(second.read_slot_visual("slot_a"))
	assert_that(first.read_slot_visual("slot_b")).is_equal(second.read_slot_visual("slot_b"))
	assert_that(first.read_slot_visual("slot_b")["overlay_state"]).is_equal("overlay_legal")

# acceptance: ACC:T61.7
func test_inactive_context_prevents_stale_overlay_reuse_until_context_reactivated() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.set_placement_context_active(false)
	controller.apply_legality_overlay({"stale_slot": controller.LEGALITY_WALL})
	var hidden := controller.read_slot_visual("stale_slot")

	controller.set_placement_context_active(true)
	var before_refresh := controller.read_slot_visual("stale_slot")
	controller.apply_legality_overlay({"stale_slot": controller.LEGALITY_VALID_INNER})
	var refreshed := controller.read_slot_visual("stale_slot")

	assert_that(hidden["overlay_state"]).is_equal("overlay_hidden")
	assert_that(before_refresh["overlay_state"]).is_equal("overlay_hidden")
	assert_that(refreshed["overlay_state"]).is_equal("overlay_legal")
	assert_that(refreshed["overlay_tint"]).is_equal("warm")

# acceptance: ACC:T61.8
# acceptance: ACC:T61.9
func test_task_scoped_scene_checks_cover_runtime_registration_and_selection_feedback_paths() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var selection_controller: Node = screen.get_node("SelectionController")
	var path := NodePath("UI/PlacementOverlayController")
	var controller := CONTROLLER.new()
	selection_controller.call("register_overlay_controller", path, controller)
	auto_free(controller)
	await get_tree().process_frame

	selection_controller.call("apply_legality_overlay", {"slot_runtime": controller.LEGALITY_TEMP_INVALID})
	var state := selection_controller.call("read_slot_visual", "slot_runtime") as Dictionary
	assert_that(state["overlay_state"]).is_equal("overlay_illegal")
	assert_that(state["frame"]).is_equal("red")
	assert_that(state["marker"]).is_equal("none")

	selection_controller.call("apply_legality_overlay", {"slot_runtime_b": controller.LEGALITY_VALID_OUTER})
	var switched_new := selection_controller.call("read_slot_visual", "slot_runtime_b") as Dictionary
	assert_that(switched_new["overlay_state"]).is_equal("overlay_legal")
	assert_that(switched_new["overlay_tint"]).is_equal("cool")

	selection_controller.call("set_placement_context_active", false)
	selection_controller.call("set_placement_context_active", true)
	var cleared_old := selection_controller.call("read_slot_visual", "slot_runtime") as Dictionary
	var cleared_new := selection_controller.call("read_slot_visual", "slot_runtime_b") as Dictionary
	assert_that(cleared_old["overlay_state"]).is_equal("overlay_hidden")
	assert_that(cleared_new["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.1
func test_category_switch_maintains_expected_shared_outline_color_contract() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"economy": controller.LEGALITY_VALID_INNER})
	controller.apply_legality_overlay({"economy": controller.LEGALITY_VALID_OUTER})
	var economy := controller.read_slot_visual("economy")

	controller.apply_legality_overlay({"defense": controller.LEGALITY_WALL})
	var defense := controller.read_slot_visual("defense")

	assert_that(economy["overlay_tint"]).is_equal("cool")
	assert_that(economy["overlay_state"]).is_equal("overlay_legal")
	assert_that(defense["overlay_tint"]).is_equal("red")
	assert_that(defense["overlay_state"]).is_equal("overlay_illegal")

# acceptance: ACC:T61.8
func test_linked_unit_highlight_should_clear_after_switch_and_clear_selection() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))

	controller.apply_legality_overlay({
		"unit_building_linked_unit": controller.LEGALITY_VALID_OUTER,
		"unit_building_non_linked_unit": controller.LEGALITY_OUTSIDE_VALID,
	})
	var linked_selected := controller.read_slot_visual("unit_building_linked_unit")
	var non_linked_selected := controller.read_slot_visual("unit_building_non_linked_unit")

	controller.apply_legality_overlay({
		"economy_selected_slot": controller.LEGALITY_VALID_INNER,
	})
	var linked_after_switch := controller.read_slot_visual("unit_building_linked_unit")
	var economy_selected := controller.read_slot_visual("economy_selected_slot")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	var linked_after_clear := controller.read_slot_visual("unit_building_linked_unit")
	var economy_after_clear := controller.read_slot_visual("economy_selected_slot")

	assert_that(linked_selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked_selected["overlay_tint"]).is_equal("cool")
	assert_that(non_linked_selected["overlay_state"]).is_equal("overlay_hidden")
	assert_that(linked_after_switch["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked_after_switch["overlay_tint"]).is_equal("cool")
	assert_that(economy_selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked_after_clear["overlay_state"]).is_equal("overlay_hidden")
	assert_that(economy_after_clear["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.5
func test_economy_selection_glow_only_for_selected_economy_building() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"economy_building": controller.LEGALITY_VALID_INNER})
	var selected := controller.read_slot_visual("economy_building")
	var non_selected := controller.read_slot_visual("other_economy_building")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	var cleared := controller.read_slot_visual("economy_building")

	assert_that(selected["overlay_tint"]).is_equal("warm")
	assert_that(selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(non_selected["overlay_state"]).is_equal("overlay_hidden")
	assert_that(cleared["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.6
func test_defense_selection_shows_only_defense_range_overlay() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"defense_building": controller.LEGALITY_WALL})
	var defense := controller.read_slot_visual("defense_building")
	var economy := controller.read_slot_visual("economy_building")

	assert_that(defense["overlay_state"]).is_equal("overlay_illegal")
	assert_that(defense["overlay_tint"]).is_equal("red")
	assert_that(economy["overlay_state"]).is_equal("overlay_hidden")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({"economy_building": controller.LEGALITY_VALID_INNER})
	var defense_after_non_defense_selection := controller.read_slot_visual("defense_building")
	var economy_after_non_defense_selection := controller.read_slot_visual("economy_building")
	assert_that(defense_after_non_defense_selection["overlay_state"]).is_equal("overlay_hidden")
	assert_that(economy_after_non_defense_selection["overlay_state"]).is_equal("overlay_legal")

# acceptance: ACC:T61.7
func test_unit_building_range_overlay_switch_clears_previous_before_new_selection() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"unit_building_a": controller.LEGALITY_VALID_OUTER})
	var first := controller.read_slot_visual("unit_building_a")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({"unit_building_b": controller.LEGALITY_VALID_OUTER})
	var first_after_switch := controller.read_slot_visual("unit_building_a")
	var second := controller.read_slot_visual("unit_building_b")

	assert_that(first["overlay_state"]).is_equal("overlay_legal")
	assert_that(first_after_switch["overlay_state"]).is_equal("overlay_hidden")
	assert_that(second["overlay_state"]).is_equal("overlay_legal")

# acceptance: ACC:T61.8
func test_range_overlay_wall_clipped_contract_uses_blocked_vs_unblocked_states() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({
		"blocked_segment": controller.LEGALITY_WALL,
		"unblocked_segment": controller.LEGALITY_VALID_OUTER,
	})
	var blocked := controller.read_slot_visual("blocked_segment")
	var unblocked := controller.read_slot_visual("unblocked_segment")

	assert_that(blocked["overlay_state"]).is_equal("overlay_illegal")
	assert_that(blocked["marker"]).is_equal("blocker")
	assert_that(unblocked["overlay_state"]).is_equal("overlay_legal")
	assert_that(unblocked["marker"]).is_equal("none")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({
		"segment_a": controller.LEGALITY_VALID_OUTER,
		"segment_b": controller.LEGALITY_VALID_OUTER,
	})
	var no_wall_a := controller.read_slot_visual("segment_a")
	var no_wall_b := controller.read_slot_visual("segment_b")
	assert_that(no_wall_a["overlay_state"]).is_equal("overlay_legal")
	assert_that(no_wall_b["overlay_state"]).is_equal("overlay_legal")
	assert_that(no_wall_a["marker"]).is_equal("none")
	assert_that(no_wall_b["marker"]).is_equal("none")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({
		"blocked_segment": controller.LEGALITY_WALL,
		"unblocked_segment": controller.LEGALITY_VALID_OUTER,
	})
	var blocked_before_switch := controller.read_slot_visual("blocked_segment")
	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({
		"new_selected_segment": controller.LEGALITY_VALID_OUTER,
	})
	var blocked_after_switch := controller.read_slot_visual("blocked_segment")
	var new_selected_segment := controller.read_slot_visual("new_selected_segment")
	assert_that(blocked_before_switch["overlay_state"]).is_equal("overlay_illegal")
	assert_that(blocked_after_switch["overlay_state"]).is_equal("overlay_hidden")
	assert_that(new_selected_segment["overlay_state"]).is_equal("overlay_legal")

# acceptance: ACC:T61.9
func test_selected_building_ownership_feedback_uses_shared_outline_color_contract() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({"economy_slot": controller.LEGALITY_VALID_OUTER})
	controller.apply_legality_overlay({"unit_slot": controller.LEGALITY_VALID_OUTER})
	var economy := controller.read_slot_visual("economy_slot")
	var unit := controller.read_slot_visual("unit_slot")
	var non_selected := controller.read_slot_visual("defense_slot")

	assert_that(economy["overlay_state"]).is_equal("overlay_legal")
	assert_that(unit["overlay_state"]).is_equal("overlay_legal")
	assert_that(economy["overlay_tint"]).is_equal(unit["overlay_tint"])
	assert_that(economy["overlay_tint"]).is_equal("cool")
	assert_that(non_selected["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.10
func test_empty_prompt_state_clears_all_building_selection_feedback_channels() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({
		"economy_slot": controller.LEGALITY_VALID_INNER,
		"defense_slot": controller.LEGALITY_WALL,
		"unit_slot": controller.LEGALITY_VALID_OUTER,
		"linked_unit_slot": controller.LEGALITY_VALID_OUTER,
	})

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)

	var economy := controller.read_slot_visual("economy_slot")
	var defense := controller.read_slot_visual("defense_slot")
	var unit := controller.read_slot_visual("unit_slot")
	var linked := controller.read_slot_visual("linked_unit_slot")

	assert_that(economy["overlay_state"]).is_equal("overlay_hidden")
	assert_that(defense["overlay_state"]).is_equal("overlay_hidden")
	assert_that(unit["overlay_state"]).is_equal("overlay_hidden")
	assert_that(linked["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.11
func test_switching_between_multiple_producers_updates_linked_unit_set_without_overlap() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	controller.apply_legality_overlay({
		"producer_a_linked_unit": controller.LEGALITY_VALID_OUTER,
		"producer_b_linked_unit": controller.LEGALITY_OUTSIDE_VALID,
	})
	var a_linked_when_a_selected := controller.read_slot_visual("producer_a_linked_unit")
	var b_linked_when_a_selected := controller.read_slot_visual("producer_b_linked_unit")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({
		"producer_a_linked_unit": controller.LEGALITY_OUTSIDE_VALID,
		"producer_b_linked_unit": controller.LEGALITY_VALID_OUTER,
	})
	var a_linked_when_b_selected := controller.read_slot_visual("producer_a_linked_unit")
	var b_linked_when_b_selected := controller.read_slot_visual("producer_b_linked_unit")

	assert_that(a_linked_when_a_selected["overlay_state"]).is_equal("overlay_legal")
	assert_that(b_linked_when_a_selected["overlay_state"]).is_equal("overlay_hidden")
	assert_that(a_linked_when_b_selected["overlay_state"]).is_equal("overlay_hidden")
	assert_that(b_linked_when_b_selected["overlay_state"]).is_equal("overlay_legal")

# acceptance: ACC:T61.8
# acceptance: ACC:T61.11
func test_scene_selection_controller_should_mount_formal_selection_feedback_into_runtime_slots() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var selection_controller: Node = screen.get_node("SelectionController")
	assert_bool(selection_controller.has_method("apply_building_selection")).is_true()
	selection_controller.call("apply_building_selection", {
		"selection_id": "barracks_alpha",
		"category": "unit",
		"building_slots": ["InnerCastleRegionSlot_00_00"],
		"range_slots": ["InnerCastleRegionSlot_01_00"],
		"blocked_range_slots": ["InnerCastleRegionSlot_02_00"],
		"linked_unit_slots": ["LeftOuterFieldSlot_00_00"],
	})

	var building := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_00_00")
	var visible_range := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_01_00")
	var blocked_range := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_02_00")
	var linked_unit := _runtime_slot_snapshot(screen, "LeftOuterFieldSlot_00_00")
	var unrelated := _runtime_slot_snapshot(screen, "LeftOuterFieldSlot_00_01")

	assert_that(building["feedback_channel"]).is_equal("building_outline")
	assert_that(building["selection_owner"]).is_equal("barracks_alpha")
	assert_that(building["selection_category"]).is_equal("unit")
	assert_that(building["outline_tint"]).is_equal("cool")
	assert_that(visible_range["feedback_channel"]).is_equal("unit_range")
	assert_that(visible_range["overlay_state"]).is_equal("overlay_legal")
	assert_that(blocked_range["feedback_channel"]).is_equal("unit_range")
	assert_that(blocked_range["overlay_state"]).is_equal("overlay_illegal")
	assert_that(blocked_range["marker"]).is_equal("blocker")
	assert_that(bool(blocked_range["range_clipped"])).is_true()
	assert_that(linked_unit["feedback_channel"]).is_equal("linked_unit")
	assert_that(linked_unit["overlay_state"]).is_equal("overlay_legal")
	assert_that(unrelated["feedback_channel"]).is_equal("none")
	assert_that(unrelated["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.1
# acceptance: ACC:T61.3
# acceptance: ACC:T61.5
# acceptance: ACC:T61.6
# acceptance: ACC:T61.7
# acceptance: ACC:T61.9
# acceptance: ACC:T61.10
func test_scene_selection_controller_should_switch_and_clear_formal_selection_feedback_without_stale_runtime_state() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var selection_controller: Node = screen.get_node("SelectionController")
	assert_bool(selection_controller.has_method("apply_building_selection")).is_true()
	assert_bool(selection_controller.has_method("clear_building_selection")).is_true()
	selection_controller.call("apply_building_selection", {
		"selection_id": "tower_alpha",
		"category": "defense",
		"building_slots": ["InnerCastleRegionSlot_03_00"],
		"range_slots": ["InnerCastleRegionSlot_04_00"],
		"blocked_range_slots": ["InnerCastleRegionSlot_05_00"],
		"linked_unit_slots": ["LeftOuterFieldSlot_01_00"],
	})

	selection_controller.call("apply_building_selection", {
		"selection_id": "farm_alpha",
		"category": "economy",
		"building_slots": ["InnerCastleRegionSlot_06_00"],
	})

	var old_building := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_03_00")
	var old_range := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_04_00")
	var old_blocked := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_05_00")
	var old_linked := _runtime_slot_snapshot(screen, "LeftOuterFieldSlot_01_00")
	var new_economy := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_06_00")

	assert_that(old_building["overlay_state"]).is_equal("overlay_hidden")
	assert_that(old_range["overlay_state"]).is_equal("overlay_hidden")
	assert_that(old_blocked["overlay_state"]).is_equal("overlay_hidden")
	assert_that(old_linked["overlay_state"]).is_equal("overlay_hidden")
	assert_that(new_economy["feedback_channel"]).is_equal("economy_glow")
	assert_that(new_economy["selection_owner"]).is_equal("farm_alpha")
	assert_that(new_economy["selection_category"]).is_equal("economy")
	assert_that(new_economy["overlay_tint"]).is_equal("warm")
	assert_that(new_economy["outline_tint"]).is_equal("cool")

	selection_controller.call("clear_building_selection")
	var cleared_economy := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_06_00")
	assert_that(cleared_economy["overlay_state"]).is_equal("overlay_hidden")
	assert_that(cleared_economy["feedback_channel"]).is_equal("none")
	assert_that(cleared_economy["selection_owner"]).is_equal("")

# acceptance: ACC:T61.1
# acceptance: ACC:T61.5
# acceptance: ACC:T61.8
# acceptance: ACC:T61.10
func test_scene_should_mount_independent_formal_selection_snapshot_provider() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var battlefield_view := screen.get_node("Background")
	var selection_data_provider := screen.get_node("SelectionDataProvider")
	assert_object(battlefield_view).is_not_null()
	assert_object(selection_data_provider).is_not_null()
	assert_bool(selection_data_provider.has_method("get_formal_selection_snapshot")).is_true()
	assert_bool(selection_data_provider.has_method("get_building_definition")).is_true()
	assert_bool(battlefield_view.has_method("get_formal_selection_snapshot")).is_false()

	var barracks_definition := selection_data_provider.call("get_building_definition", "barracks_alpha") as Dictionary
	assert_that(barracks_definition["building_type"]).is_equal("barracks")
	assert_that(barracks_definition["category"]).is_equal("unit")
	assert_that(barracks_definition["placement_zone"]).is_equal("inner_castle")

	var initial_runtime_snapshot := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_00_00") as Dictionary
	assert_that(initial_runtime_snapshot.is_empty()).is_true()

	var initial_economy_snapshot := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_06_00") as Dictionary
	assert_that(initial_economy_snapshot.is_empty()).is_true()

	var missing_snapshot := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_01_12") as Dictionary
	assert_that(missing_snapshot.is_empty()).is_true()

# acceptance: ACC:T61.1
# acceptance: ACC:T61.3
func test_runtime_formal_selection_snapshot_should_follow_bridge_build_occupancy() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var selection_data_provider := screen.get_node("SelectionDataProvider")
	var bridge := screen.get_node("CombatExperienceRuntimeBridge")
	assert_object(selection_data_provider).is_not_null()
	assert_object(bridge).is_not_null()

	var before_build := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_00_00") as Dictionary
	assert_that(before_build.is_empty()).is_true()

	bridge.call("BuildPhase")
	await get_tree().process_frame

	var after_build := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_00_00") as Dictionary
	assert_that(after_build["selection_id"]).is_equal("barracks_alpha")
	assert_that(after_build["building_type"]).is_equal("barracks")

	var economy_after_build := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_06_00") as Dictionary
	assert_that(economy_after_build["selection_id"]).is_equal("farm_alpha")
	assert_that(economy_after_build["building_type"]).is_equal("residence")

	bridge.call("ResetForInteractiveRun")
	await get_tree().process_frame

	var after_reset := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_00_00") as Dictionary
	assert_that(after_reset.is_empty()).is_true()
	var economy_after_reset := selection_data_provider.call("get_formal_selection_snapshot", "InnerCastleRegionSlot_06_00") as Dictionary
	assert_that(economy_after_reset.is_empty()).is_true()

# acceptance: ACC:T61.3
# acceptance: ACC:T61.10
func test_scene_clicking_runtime_slot_without_formal_snapshot_should_clear_previous_selection_feedback() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var bridge := screen.get_node("CombatExperienceRuntimeBridge")
	bridge.call("BuildPhase")
	await get_tree().process_frame

	var selected_slot := screen.get_node(
		"Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer/InnerCastleSlots/InnerCastleRegionSlot_00_00"
	) as ColorRect
	var empty_slot := screen.get_node(
		"Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer/InnerCastleSlots/InnerCastleRegionSlot_01_12"
	) as ColorRect
	assert_object(selected_slot).is_not_null()
	assert_object(empty_slot).is_not_null()

	var click_selected := InputEventMouseButton.new()
	click_selected.button_index = MOUSE_BUTTON_LEFT
	click_selected.pressed = true
	click_selected.position = selected_slot.size * 0.5
	click_selected.global_position = selected_slot.global_position + (selected_slot.size * 0.5)
	selected_slot.emit_signal("gui_input", click_selected)
	await get_tree().process_frame

	var building_before_clear := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_00_00")
	assert_that(building_before_clear["feedback_channel"]).is_equal("building_outline")

	var click_empty := InputEventMouseButton.new()
	click_empty.button_index = MOUSE_BUTTON_LEFT
	click_empty.pressed = true
	click_empty.position = empty_slot.size * 0.5
	click_empty.global_position = empty_slot.global_position + (empty_slot.size * 0.5)
	empty_slot.emit_signal("gui_input", click_empty)
	await get_tree().process_frame

	var building_after_clear := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_00_00")
	var linked_after_clear := _runtime_slot_snapshot(screen, "LeftOuterFieldSlot_00_00")
	assert_that(building_after_clear["overlay_state"]).is_equal("overlay_hidden")
	assert_that(building_after_clear["feedback_channel"]).is_equal("none")
	assert_that(linked_after_clear["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.1
# acceptance: ACC:T61.5
# acceptance: ACC:T61.8
# acceptance: ACC:T61.10
func test_scene_clicking_runtime_battlefield_slot_should_drive_formal_building_selection_feedback() -> void:
	var screen := BATTLE_MAP_SCREEN.instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var bridge := screen.get_node("CombatExperienceRuntimeBridge")
	bridge.call("BuildPhase")
	await get_tree().process_frame

	var clicked_slot := screen.get_node(
		"Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer/InnerCastleSlots/InnerCastleRegionSlot_00_00"
	) as ColorRect
	assert_object(clicked_slot).is_not_null()

	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	click.position = clicked_slot.size * 0.5
	click.global_position = clicked_slot.global_position + (clicked_slot.size * 0.5)
	clicked_slot.emit_signal("gui_input", click)
	await get_tree().process_frame

	var building := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_00_00")
	var visible_range := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_01_00")
	var blocked_range := _runtime_slot_snapshot(screen, "InnerCastleRegionSlot_02_00")
	var linked_unit := _runtime_slot_snapshot(screen, "LeftOuterFieldSlot_00_00")

	assert_that(building["selection_owner"]).is_equal("barracks_alpha")
	assert_that(building["selection_category"]).is_equal("unit")
	assert_that(building["feedback_channel"]).is_equal("building_outline")
	assert_that(visible_range["feedback_channel"]).is_equal("unit_range")
	assert_that(visible_range["overlay_state"]).is_equal("overlay_legal")
	assert_that(blocked_range["overlay_state"]).is_equal("overlay_illegal")
	assert_that(bool(blocked_range["range_clipped"])).is_true()
	assert_that(linked_unit["feedback_channel"]).is_equal("linked_unit")
