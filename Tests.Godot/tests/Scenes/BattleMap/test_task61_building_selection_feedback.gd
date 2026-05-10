extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")

# acceptance: ACC:T61.1
func test_selected_category_feedback_stays_exclusive_and_switch_clears_previous_state() -> void:
	var controller := CONTROLLER.new()
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

	var path := NodePath("UI/PlacementOverlayController")
	var controller := CONTROLLER.new()
	screen.register_overlay_controller(path, controller)
	auto_free(controller)
	await get_tree().process_frame

	controller.apply_legality_overlay({"slot_runtime": controller.LEGALITY_TEMP_INVALID})
	var state := controller.read_slot_visual("slot_runtime")
	assert_that(state["overlay_state"]).is_equal("overlay_illegal")
	assert_that(state["frame"]).is_equal("red")
	assert_that(state["marker"]).is_equal("none")

	controller.apply_legality_overlay({"slot_runtime_b": controller.LEGALITY_VALID_OUTER})
	var switched_new := controller.read_slot_visual("slot_runtime_b")
	assert_that(switched_new["overlay_state"]).is_equal("overlay_legal")
	assert_that(switched_new["overlay_tint"]).is_equal("cool")

	controller.set_placement_context_active(false)
	controller.set_placement_context_active(true)
	var cleared_old := controller.read_slot_visual("slot_runtime")
	var cleared_new := controller.read_slot_visual("slot_runtime_b")
	assert_that(cleared_old["overlay_state"]).is_equal("overlay_hidden")
	assert_that(cleared_new["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T61.1
func test_category_switch_maintains_expected_shared_outline_color_contract() -> void:
	var controller := CONTROLLER.new()
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
