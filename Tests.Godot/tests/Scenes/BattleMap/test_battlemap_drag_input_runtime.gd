extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func test_screen_level_pointer_bridge_should_drive_drag_preview_and_release_cancel() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var motion := InputEventMouseMotion.new()
	motion.position = Vector2(300, 760)
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var moving_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(moving_preview.get("visible", false) == true).is_true()
	assert_that(moving_preview.get("position", Vector2.ZERO)).is_equal(Vector2(276, 736))

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = Vector2(300, 760)
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(1)

	var after_release: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(controller.call("has_active_placement") == false).is_true()
	assert_bool(after_release.get("visible", true) == false).is_true()

func test_screen_level_pointer_bridge_should_release_on_hovered_slot_when_pointer_is_over_tile() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_03_00")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)

	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	var summary: Dictionary = bridge.call("GetSummary")
	assert_bool(summary.get("mg_tower_built", false) == true).is_true()
	assert_bool(controller.call("has_active_placement") == false).is_true()
