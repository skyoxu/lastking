extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")

# acceptance: ACC:T60.1
func test_placement_mode_renders_single_valid_overlay_per_slot_region() -> void:
	var controller := CONTROLLER.new()

	var inner := controller.render_outcome("slot_inner", controller.LEGALITY_VALID_INNER)
	var outer := controller.render_outcome("slot_outer", controller.LEGALITY_VALID_OUTER)
	var outside := controller.render_outcome("slot_outside", controller.LEGALITY_OUTSIDE_VALID)

	assert_that(inner["valid_overlay"]).is_equal(true)
	assert_that(inner["overlay_tint"]).is_equal("warm")
	assert_that(inner["overlay_state"]).is_equal("overlay_legal")

	assert_that(outer["valid_overlay"]).is_equal(true)
	assert_that(outer["overlay_tint"]).is_equal("cool")
	assert_that(outer["overlay_state"]).is_equal("overlay_legal")

	assert_that(outside["valid_overlay"]).is_equal(false)
	assert_that(outside["overlay_tint"]).is_equal("none")
	assert_that(outside["overlay_state"]).is_equal("overlay_hidden")

# acceptance: ACC:T60.2
func test_placement_mode_invalid_and_wall_slots_keep_expected_visual_contract() -> void:
	var controller := CONTROLLER.new()

	var fixed_invalid := controller.render_outcome("slot_fixed", controller.LEGALITY_FIXED_INVALID)
	assert_that(fixed_invalid["overlay_tint"]).is_equal("grey")
	assert_that(fixed_invalid["marker"]).is_equal("lock")
	assert_that(fixed_invalid["reason_text"]).is_equal("")

	var temporary_invalid := controller.render_outcome("slot_temp", controller.LEGALITY_TEMP_INVALID)
	assert_that(temporary_invalid["frame"]).is_equal("red")
	assert_that(temporary_invalid["reason_text"]).is_equal("")

	var wall_first := controller.render_outcome("slot_wall", controller.LEGALITY_WALL)
	var wall_second := controller.render_outcome("slot_wall", controller.LEGALITY_WALL)
	assert_that(wall_first["overlay_tint"]).is_equal("red")
	assert_that(wall_second["overlay_tint"]).is_equal("red")
	assert_that(wall_first["marker"]).is_equal("blocker")
	assert_that(wall_second["marker"]).is_equal("blocker")
	assert_that(wall_second["frame"]).is_equal(wall_first["frame"])

# acceptance: ACC:T60.11
func test_unknown_legality_should_not_render_valid_overlay() -> void:
	var controller := CONTROLLER.new()
	var unknown := controller.render_outcome("slot_unknown", "unsupported_legality")

	assert_that(unknown["valid_overlay"]).is_equal(false)
	assert_that(unknown["overlay_state"]).is_equal("overlay_hidden")
	assert_that(unknown["reason_text"]).is_equal("")

# acceptance: ACC:T60.1
# acceptance: ACC:T60.2
func test_battlefield_slot_visual_feedback_is_applied_directly_on_runtime_slot_state() -> void:
	var controller := CONTROLLER.new()
	var slot_id := "runtime_slot_A1"

	controller.apply_legality_overlay({slot_id: controller.LEGALITY_VALID_INNER})
	var valid_state := controller.read_slot_visual(slot_id)
	assert_that(valid_state["valid_overlay"]).is_equal(true)
	assert_that(valid_state["overlay_state"]).is_equal("overlay_legal")

	controller.apply_legality_overlay({slot_id: controller.LEGALITY_TEMP_INVALID})
	var invalid_state := controller.read_slot_visual(slot_id)
	assert_that(invalid_state["valid_overlay"]).is_equal(false)
	assert_that(invalid_state["overlay_state"]).is_equal("overlay_illegal")

# acceptance: ACC:T60.7
func test_inactive_context_keeps_battlefield_slot_overlay_hidden_when_scene_state_changes() -> void:
	var controller := CONTROLLER.new()
	var slot_id := "runtime_slot_B1"

	controller.apply_legality_overlay({slot_id: controller.LEGALITY_VALID_INNER})
	var before := controller.read_slot_visual(slot_id)

	controller.set_placement_context_active(false)
	controller.apply_legality_overlay({slot_id: controller.LEGALITY_WALL})
	var after := controller.read_slot_visual(slot_id)

	assert_that(after).is_equal(before)

# acceptance: ACC:T60.8
func test_reactivating_context_restores_overlay_updates_without_reusing_stale_hidden_state() -> void:
	var controller := CONTROLLER.new()
	var slot_id := "runtime_slot_B2"

	controller.set_placement_context_active(false)
	controller.apply_legality_overlay({slot_id: controller.LEGALITY_WALL})
	var hidden := controller.read_slot_visual(slot_id)

	controller.set_placement_context_active(true)
	controller.apply_legality_overlay({slot_id: controller.LEGALITY_VALID_OUTER})
	var visible := controller.read_slot_visual(slot_id)

	assert_that(hidden["overlay_state"]).is_equal("overlay_hidden")
	assert_that(visible["overlay_state"]).is_equal("overlay_legal")
	assert_that(visible["overlay_tint"]).is_equal("cool")
