extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONTROLLER := preload("res://Game.Godot/Scripts/Screens/PlacementOverlayController.gd")

# acceptance: ACC:T60.6
# acceptance: ACC:T61.6
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
# acceptance: ACC:T61.9
func test_battle_map_screen_registers_runtime_overlay_controller_path_via_real_scene_api() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	var selection_controller := screen.get_node_or_null("SelectionController")
	assert_object(selection_controller).is_not_null()
	assert_bool((selection_controller as Node).has_method("register_overlay_controller")).is_true()

	var default_overlay := screen.get_node_or_null("UI_PlacementOverlayController")
	assert_object(default_overlay).is_not_null()
	assert_str(String((default_overlay as Node).name)).is_equal("UI_PlacementOverlayController")

	var path := NodePath("UI/PlacementOverlayController")
	var runtime_controller := CONTROLLER.new()
	(selection_controller as Node).call("register_overlay_controller", path, runtime_controller)
	auto_free(runtime_controller)
	await get_tree().process_frame

	var overlay := screen.get_node_or_null("UI_PlacementOverlayController")
	assert_object(overlay).is_not_null()
	assert_str(String(runtime_controller.name)).is_equal("UI_PlacementOverlayController")
	assert_object(runtime_controller.get_parent()).is_equal(screen)

	selection_controller.call("apply_legality_overlay", {"slot_live": runtime_controller.LEGALITY_WALL})
	var slot_state := selection_controller.call("read_slot_visual", "slot_live") as Dictionary
	assert_that(slot_state["overlay_state"]).is_equal("overlay_illegal")

# acceptance: ACC:T60.9
func test_register_overlay_controller_replaces_existing_named_overlay_without_duplicate_nodes() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	var selection_controller: Node = screen.get_node("SelectionController")

	var path := NodePath("UI/PlacementOverlayController")
	var first := CONTROLLER.new()
	var second := CONTROLLER.new()
	selection_controller.call("register_overlay_controller", path, first)
	selection_controller.call("register_overlay_controller", path, second)
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

# acceptance: ACC:T61.6
# acceptance: ACC:T61.9
func test_register_overlay_controller_reparents_foreign_parent_controller_without_duplicate_nodes() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	var selection_controller: Node = screen.get_node("SelectionController")

	var foreign_parent := Node.new()
	foreign_parent.name = "ForeignParent"
	add_child(auto_free(foreign_parent))

	var controller := CONTROLLER.new()
	foreign_parent.add_child(controller)
	auto_free(controller)
	await get_tree().process_frame

	var path := NodePath("UI/PlacementOverlayController")
	selection_controller.call("register_overlay_controller", path, controller)
	await get_tree().process_frame

	assert_object(controller.get_parent()).is_equal(screen)
	assert_str(String(controller.name)).is_equal("UI_PlacementOverlayController")
	assert_object(foreign_parent.get_node_or_null("UI_PlacementOverlayController")).is_null()

	var count := 0
	for child_variant in screen.get_children():
		var child := child_variant as Node
		if child != null and String(child.name) == "UI_PlacementOverlayController":
			count += 1
	assert_int(count).is_equal(1)

# acceptance: ACC:T61.6
# acceptance: ACC:T61.14
# acceptance: ACC:T61.15
func test_runtime_registered_overlay_controller_produces_scene_scoped_feedback_contracts() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	var selection_controller: Node = screen.get_node("SelectionController")

	var path := NodePath("UI/PlacementOverlayController")
	var runtime_controller := CONTROLLER.new()
	selection_controller.call("register_overlay_controller", path, runtime_controller)
	auto_free(runtime_controller)
	await get_tree().process_frame

	var overlay := screen.get_node_or_null("UI_PlacementOverlayController")
	assert_object(overlay).is_not_null()
	assert_object(overlay).is_equal(runtime_controller)

	selection_controller.call("apply_legality_overlay", {
		"linked_unit_slot": runtime_controller.LEGALITY_VALID_OUTER,
		"non_linked_unit_slot": runtime_controller.LEGALITY_OUTSIDE_VALID,
		"blocked_segment": runtime_controller.LEGALITY_WALL,
		"unblocked_segment": runtime_controller.LEGALITY_VALID_OUTER,
	})

	var linked := selection_controller.call("read_slot_visual", "linked_unit_slot") as Dictionary
	var non_linked := selection_controller.call("read_slot_visual", "non_linked_unit_slot") as Dictionary
	var blocked := selection_controller.call("read_slot_visual", "blocked_segment") as Dictionary
	var unblocked := selection_controller.call("read_slot_visual", "unblocked_segment") as Dictionary

	assert_that(linked["overlay_state"]).is_equal("overlay_legal")
	assert_that(linked["overlay_tint"]).is_equal("cool")
	assert_that(non_linked["overlay_state"]).is_equal("overlay_hidden")
	assert_that(blocked["overlay_state"]).is_equal("overlay_illegal")
	assert_that(blocked["marker"]).is_equal("blocker")
	assert_that(unblocked["overlay_state"]).is_equal("overlay_legal")
	assert_that(unblocked["marker"]).is_equal("none")
