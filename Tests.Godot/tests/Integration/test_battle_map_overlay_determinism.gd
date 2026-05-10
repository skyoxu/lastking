extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")

# acceptance: ACC:T60.3
# Determinism contract: same context update must only touch affected slots.
func test_overlay_update_changes_only_affected_slots_for_same_context() -> void:
	var controller := CONTROLLER.new()
	controller.apply_legality_overlay({
		"A1": controller.LEGALITY_VALID_INNER,
		"A2": controller.LEGALITY_TEMP_INVALID,
		"A3": controller.LEGALITY_OUTSIDE_VALID
	})
	var a2_before := controller.read_slot_visual("A2")
	var a3_before := controller.read_slot_visual("A3")

	controller.apply_legality_overlay({"A1": controller.LEGALITY_WALL})
	var a2_after := controller.read_slot_visual("A2")
	var a3_after := controller.read_slot_visual("A3")

	assert_that(controller.read_slot_visual("A1")["overlay_state"]).is_equal("overlay_illegal")
	assert_that(a2_after).is_equal(a2_before)
	assert_that(a3_after).is_equal(a3_before)

# acceptance: ACC:T60.3
func test_inactive_placement_context_renders_no_legality_overlay() -> void:
	var controller := CONTROLLER.new()
	controller.apply_legality_overlay({"B1": controller.LEGALITY_VALID_INNER})
	var before := controller.read_slot_visual("B1")

	controller.set_placement_context_active(false)
	controller.apply_legality_overlay({"B1": controller.LEGALITY_WALL})
	var after := controller.read_slot_visual("B1")

	assert_that(before["overlay_state"]).is_equal("overlay_legal")
	assert_that(after["overlay_state"]).is_equal("overlay_hidden")
	assert_that(after["overlay_tint"]).is_equal("none")
