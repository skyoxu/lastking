extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func before() -> void:
	var bus = get_node_or_null("/root/EventBus")
	if bus == null:
		bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
		bus.name = "EventBus"
		get_tree().get_root().add_child(auto_free(bus))

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func _publish(type_name: String, payload: Dictionary) -> void:
	var bus = get_node_or_null("/root/EventBus")
	assert_object(bus).is_not_null()
	bus.call("PublishSimple", type_name, "ut", JSON.stringify(payload))


func test_direct_battle_map_screen_should_activate_local_hud_top_and_bottom_bars() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var local_hud := screen.get_node_or_null("BattleHud") as Control
	var top_bar := screen.get_node_or_null("BattleHud/TopBar") as Control
	var bottom_bar := screen.get_node_or_null("BattleHud/CombatHud/BottomBar") as Control

	assert_object(local_hud).is_not_null()
	assert_object(top_bar).is_not_null()
	assert_object(bottom_bar).is_not_null()
	assert_bool(local_hud.visible).is_true()
	assert_bool(top_bar.visible).is_true()
	assert_bool(bottom_bar.visible).is_true()

func test_direct_battle_map_screen_should_keep_formal_bottom_bar_at_196px_height() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var combat_hud := screen.get_node_or_null("BattleHud/CombatHud") as Control
	var bottom_bar := screen.get_node_or_null("BattleHud/CombatHud/BottomBar") as Control

	assert_object(combat_hud).is_not_null()
	assert_object(bottom_bar).is_not_null()
	assert_float(bottom_bar.size.y).is_equal(196.0)
	assert_float(combat_hud.position.y).is_equal(704.0)
	assert_float(bottom_bar.position.y).is_equal(0.0)
	assert_float(bottom_bar.global_position.y).is_equal(704.0)


func test_direct_battle_map_screen_should_keep_optional_runtime_panels_hidden_until_runtime_data_arrives() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var pressure_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/PressurePanel") as Control
	var camera_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/CameraControlOverlay") as Control
	var config_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ConfigAuditPanel") as Control
	var migration_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/MigrationStatusDialog") as Control
	var metadata_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ReportMetadataPanel") as Control
	var outcome_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/OutcomePanel") as Control
	var prompt_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/RuntimePromptPanel") as Control
	var resource_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ResourcePanel") as Control
	var build_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/BuildPanel") as Control
	var progression_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ProgressionPanel") as Control

	assert_bool(pressure_panel.visible).is_false()
	assert_bool(camera_panel.visible).is_false()
	assert_bool(config_panel.visible).is_false()
	assert_bool(migration_panel.visible).is_false()
	assert_bool(metadata_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()
	assert_bool(resource_panel.visible).is_false()
	assert_bool(build_panel.visible).is_false()
	assert_bool(progression_panel.visible).is_false()

func test_direct_battle_map_screen_should_keep_legacy_feedback_panels_hidden_after_runtime_events() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var pressure_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/PressurePanel") as Control
	var camera_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/CameraControlOverlay") as Control
	var outcome_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/OutcomePanel") as Control
	var prompt_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/RuntimePromptPanel") as Control
	var resource_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ResourcePanel") as Control
	var build_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/BuildPanel") as Control
	var progression_panel := screen.get_node_or_null("BattleHud/FeedbackLayer/ProgressionPanel") as Control
	var pressure_label := screen.get_node_or_null("BattleHud/FeedbackLayer/PressurePanel/VBox/PressureLabel") as Label
	var camera_label := screen.get_node_or_null("BattleHud/FeedbackLayer/CameraControlOverlay/VBox/CameraStatusLabel") as Label
	var outcome_label := screen.get_node_or_null("BattleHud/FeedbackLayer/OutcomePanel/VBox/OutcomeLabel") as Label
	var prompt_label := screen.get_node_or_null("BattleHud/FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel") as Label
	var resource_label := screen.get_node_or_null("BattleHud/FeedbackLayer/ResourcePanel/VBox/ResourceSummaryLabel") as Label
	var build_label := screen.get_node_or_null("BattleHud/FeedbackLayer/BuildPanel/VBox/BuildSummaryLabel") as Label
	var progression_label := screen.get_node_or_null("BattleHud/FeedbackLayer/ProgressionPanel/VBox/ProgressionSummaryLabel") as Label

	_publish("core.lastking.castle.hp_changed", {"Day": 7, "PreviousHp": 100, "CurrentHp": 42})
	_publish("core.lastking.wave.spawned", {"day": 7, "count": 5})
	_publish("core.lastking.camera.scrolled", {"dx": 6, "dy": -2})
	_publish("core.run.state.transitioned", {"outcome": "win", "day": 15})
	_publish("core.lastking.ui_feedback.raised", {
		"Code": "build_invalid_tile",
		"MessageKey": "ui.build.invalid_tile",
		"Details": "occupied",
	})
	_publish("core.lastking.resources.changed", {"gold": 12, "iron": 8, "population_cap": 5})
	_publish("core.lastking.tax.collected", {"gold_delta": 3, "total_gold": 15, "residence_id": "house_1"})
	_publish("core.lastking.tech.applied", {"tech_id": "wall_1", "stat_key": "defense", "previous_value": 1, "current_value": 2})
	await _await_frames(3)

	assert_bool(pressure_panel.visible).is_false()
	assert_bool(camera_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()
	assert_bool(resource_panel.visible).is_false()
	assert_bool(build_panel.visible).is_false()
	assert_bool(progression_panel.visible).is_false()
	assert_bool(String(pressure_label.text).length() > 0).is_true()
	assert_bool(String(camera_label.text).length() > 0).is_true()
	assert_bool(String(outcome_label.text).length() > 0).is_true()
	assert_bool(String(prompt_label.text).length() > 0).is_true()
	assert_bool(String(resource_label.text).length() > 0).is_true()
	assert_bool(String(build_label.text).length() > 0).is_true()
	assert_bool(String(progression_label.text).length() > 0).is_true()


func test_direct_battle_map_screen_should_route_local_hud_actions_into_runtime_controller() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var local_hud := screen.get_node_or_null("BattleHud") as Control
	var operation_controller := screen.get_node("OperationController")

	local_hud.call("RequestBattleAction", "wave")
	await _await_frames(2)
	assert_bool(operation_controller.call("is_wave_started")).is_true()

	local_hud.call("RequestBattleAction", "exchange")
	await _await_frames(2)
	assert_bool(operation_controller.call("is_combat_resolved")).is_true()


func test_main_entry_should_mount_only_local_battle_hud_inside_screenroot() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)

	var global_hud := main.get_node_or_null("RuntimeUi/HUD") as Control
	var local_hud := main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen/BattleHud") as Control

	assert_object(local_hud).is_not_null()
	assert_object(global_hud).is_null()
	assert_bool(local_hud.visible).is_true()


func test_main_entry_should_keep_local_battle_feedback_panels_hidden_in_screenroot_instance() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)

	var base_path := "RuntimeUi/ScreenRoot/BattleMapScreen/BattleHud/FeedbackLayer"
	var local_hud := main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen/BattleHud") as Control
	var resource_panel := main.get_node_or_null("%s/ResourcePanel" % base_path) as Control
	var build_panel := main.get_node_or_null("%s/BuildPanel" % base_path) as Control
	var progression_panel := main.get_node_or_null("%s/ProgressionPanel" % base_path) as Control
	var outcome_panel := main.get_node_or_null("%s/OutcomePanel" % base_path) as Control
	var prompt_panel := main.get_node_or_null("%s/RuntimePromptPanel" % base_path) as Control

	assert_object(local_hud).is_not_null()
	assert_object(resource_panel).is_not_null()
	assert_object(build_panel).is_not_null()
	assert_object(progression_panel).is_not_null()
	assert_object(outcome_panel).is_not_null()
	assert_object(prompt_panel).is_not_null()
	assert_bool(local_hud.visible).is_true()
	assert_bool(resource_panel.visible).is_false()
	assert_bool(build_panel.visible).is_false()
	assert_bool(progression_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()
