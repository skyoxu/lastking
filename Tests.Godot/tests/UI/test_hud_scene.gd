extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _instantiate_hud() -> Control:
	var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate() as Control
	add_child(auto_free(scene))
	await get_tree().process_frame
	return scene

# ACC:T9.1
# ACC:T9.4
# ACC:T9.10
# ACC:T9.12
# ACC:T9.13
func test_hud_scene_instantiates_with_formal_battle_hud_bands() -> void:
	var scene := await _instantiate_hud()
	var top_bar := scene.get_node("TopBar") as Control
	var bottom_bar := scene.get_node("CombatHud/BottomBar") as Control
	var day_label := scene.get_node("TopBar/HBox/DayLabel") as Label
	var phase_label := scene.get_node("TopBar/HBox/PhaseLabel") as Label
	var cycle_label := scene.get_node("TopBar/HBox/CycleRemainingLabel") as Label
	var hp_label := scene.get_node("TopBar/HBox/HealthLabel") as Label
	var resources_label := scene.get_node("TopBar/HBox/ResourcesLabel") as Label
	var enemies_label := scene.get_node("TopBar/HBox/EnemiesLabel") as Label
	var speed_state_label := scene.get_node("TopBar/HBox/SpeedStateLabel") as Label

	assert_bool(scene.visible).is_true()
	assert_bool(top_bar.visible).is_true()
	assert_bool(bottom_bar.visible).is_true()
	assert_bool(day_label.text.find("1") >= 0).is_true()
	assert_bool(phase_label.text.find("Day") >= 0 or phase_label.text.find("白天") >= 0 or phase_label.text.find("昼") >= 0).is_true()
	assert_bool(cycle_label.text.find("240.0s") >= 0).is_true()
	assert_bool(hp_label.text.find("100/100") >= 0).is_true()
	assert_bool(resources_label.text.find(":") >= 0).is_true()
	assert_bool(enemies_label.text.find("0") >= 0).is_true()
	assert_bool(speed_state_label.text.length() > 0).is_true()
	assert_bool(speed_state_label.text.find(":") >= 0).is_true()

# ACC:T9.16
# ACC:T9.18
func test_hud_scene_exposes_formal_top_bar_controls_only() -> void:
	var scene := await _instantiate_hud()
	var hbox := scene.get_node("TopBar/HBox") as HBoxContainer
	var settings_button := scene.get_node("TopBar/HBox/SettingsButton") as Button
	var pause_button := scene.get_node("TopBar/HBox/SpeedControls/PauseButton") as Button
	var one_x_button := scene.get_node("TopBar/HBox/SpeedControls/OneXButton") as Button
	var two_x_button := scene.get_node("TopBar/HBox/SpeedControls/TwoXButton") as Button

	assert_bool(scene.has_node("TopBar/HBox/ScoreLabel")).is_false()
	assert_object(settings_button).is_not_null()
	assert_object(pause_button).is_not_null()
	assert_object(one_x_button).is_not_null()
	assert_object(two_x_button).is_not_null()
	assert_int(hbox.get_child_count()).is_greater_equal(8)

# ACC:T24.15
# ACC:T42.2
# ACC:T42.6
# ACC:T43.2
# ACC:T44.1
func test_hud_scene_keeps_legacy_feedback_panels_hidden_by_default() -> void:
	var scene := await _instantiate_hud()
	var feedback_layer := scene.get_node("FeedbackLayer") as Control
	var feedback_label := scene.get_node("FeedbackLayer/FeedbackLabel") as Label
	var error_dialog := scene.get_node("FeedbackLayer/ErrorDialog") as PanelContainer
	var dismiss_button := scene.get_node("FeedbackLayer/ErrorDialog/VBox/DismissButton") as Button
	var legacy_paths := [
		"FeedbackLayer/PressurePanel",
		"FeedbackLayer/CameraControlOverlay",
		"FeedbackLayer/ConfigAuditPanel",
		"FeedbackLayer/MigrationStatusDialog",
		"FeedbackLayer/ReportMetadataPanel",
		"FeedbackLayer/OutcomePanel",
		"FeedbackLayer/RuntimePromptPanel",
		"FeedbackLayer/ResourcePanel",
		"FeedbackLayer/BuildPanel",
		"FeedbackLayer/ProgressionPanel",
	]

	assert_object(feedback_layer).is_not_null()
	assert_object(feedback_label).is_not_null()
	assert_object(error_dialog).is_not_null()
	assert_object(dismiss_button).is_not_null()
	assert_bool(feedback_layer.visible).is_true()
	assert_bool(feedback_label.visible).is_false()
	assert_bool(error_dialog.visible).is_false()
	for path in legacy_paths:
		var panel := scene.get_node(path) as Control
		assert_object(panel).is_not_null()
		assert_bool(panel.visible).is_false()

# ACC:T45.3
func test_hud_renders_runtime_feedback_on_declared_transient_surface() -> void:
	var bus: Node = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(bus))

	var scene := await _instantiate_hud()
	var feedback_label := scene.get_node("FeedbackLayer/FeedbackLabel") as Label
	assert_bool(feedback_label.visible).is_false()

	bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
		"Code": "perf_gate_warn",
		"MessageKey": "ui.blocked_action.perf_gate_warn",
		"Details": "p95=41.7 platform=windows"
	}))
	await get_tree().process_frame

	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("platform=windows") >= 0).is_true()
