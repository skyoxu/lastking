extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")

# acceptance: ACC:T60.6
func test_overlay_wiring_emits_legality_changed_before_overlay_rendered() -> void:
	var controller := CONTROLLER.new()
	add_child(auto_free(controller))
	var events: Array[String] = []
	controller.legality_changed.connect(func(_cells: Array) -> void:
		events.append("changed")
	)
	controller.overlay_rendered.connect(func() -> void:
		events.append("rendered")
	)

	controller.apply_legality([Vector2i(1, 1), Vector2i(2, 2)])

	assert_array(events).is_equal(["changed", "rendered"])

# acceptance: ACC:T60.9
func test_battle_map_screen_registers_runtime_overlay_controller_path_via_real_scene_api() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	assert_object(screen.get_node_or_null("UI_PlacementOverlayController")).is_null()

	var path := NodePath("UI/PlacementOverlayController")
	var runtime_controller := CONTROLLER.new()
	screen.register_overlay_controller(path, runtime_controller)
	auto_free(runtime_controller)
	await get_tree().process_frame

	var overlay := screen.get_node_or_null("UI_PlacementOverlayController")
	assert_object(overlay).is_not_null()
	assert_str(String(runtime_controller.name)).is_equal("UI_PlacementOverlayController")
	assert_object(runtime_controller.get_parent()).is_equal(screen)

	runtime_controller.apply_legality_overlay({"slot_live": runtime_controller.LEGALITY_WALL})
	var slot_state := runtime_controller.read_slot_visual("slot_live")
	assert_that(slot_state["overlay_state"]).is_equal("overlay_illegal")

# acceptance: ACC:T60.9
func test_register_overlay_controller_replaces_existing_named_overlay_without_duplicate_nodes() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame

	var path := NodePath("UI/PlacementOverlayController")
	var first := CONTROLLER.new()
	var second := CONTROLLER.new()
	screen.register_overlay_controller(path, first)
	screen.register_overlay_controller(path, second)
	auto_free(first)
	auto_free(second)
	await get_tree().process_frame

	var target_name := "UI_PlacementOverlayController"
	var count := 0
	var found: Node = null
	for child_variant in screen.get_children():
		var child := child_variant as Node
		if child != null and String(child.name) == target_name:
			count += 1
			found = child

	assert_int(count).is_equal(1)
	assert_object(found).is_equal(second)
