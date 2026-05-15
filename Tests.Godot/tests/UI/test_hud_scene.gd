extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T9.1
# ACC:T9.4
# ACC:T9.10
# ACC:T9.12
# ACC:T9.13
# ACC:T24.15
func test_hud_scene_instantiates() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    assert_bool(scene.visible).is_true()
    var day_label: Label = scene.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = scene.get_node("TopBar/HBox/HealthLabel")
    var feedback_layer: Control = scene.get_node("FeedbackLayer")
    var feedback_label: Label = scene.get_node("FeedbackLayer/FeedbackLabel")
    var error_dialog: PanelContainer = scene.get_node("FeedbackLayer/ErrorDialog")
    var dismiss_button: Button = scene.get_node("FeedbackLayer/ErrorDialog/VBox/DismissButton")
    assert_str(day_label.text).is_not_empty()
    assert_str(cycle_label.text).is_not_empty()
    assert_str(hp_label.text).is_equal("HP: 0")
    assert_object(feedback_layer).is_not_null()
    assert_object(feedback_label).is_not_null()
    assert_object(error_dialog).is_not_null()
    assert_object(dismiss_button).is_not_null()

# ACC:T9.16
# ACC:T9.18
func test_hud_scene_exposes_expected_labels() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    var day_label = scene.get_node("TopBar/HBox/DayLabel")
    var cycle_label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label = scene.get_node("TopBar/HBox/HealthLabel")
    assert_object(day_label).is_not_null()
    assert_object(cycle_label).is_not_null()
    assert_object(hp_label).is_not_null()
    assert_bool(scene.has_node("TopBar/HBox/ScoreLabel")).is_false()
    var hbox: HBoxContainer = scene.get_node("TopBar/HBox")
    var label_count := 0
    for child in hbox.get_children():
        if child is Label:
            label_count += 1
    assert_int(label_count).is_equal(3)



# ACC:T45.3
func test_hud_renders_perf_and_platform_status_feedback_from_runtime_events() -> void:
    var bus: Node = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(bus))

    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    var feedback_label: Label = scene.get_node("FeedbackLayer/FeedbackLabel")
    assert_bool(feedback_label.visible).is_false()

    bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
        "Code": "perf_gate_warn",
        "MessageKey": "ui.blocked_action.perf_gate_warn",
        "Details": "p95=41.7 platform=windows"
    }))
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Action blocked") >= 0).is_true()
    assert_bool(feedback_label.text.find("platform=windows") >= 0).is_true()

# ACC:T43.2
# ACC:T53.6
func test_hud_scene_exposes_task43_owned_surfaces_with_player_visible_defaults() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    assert_bool(scene.has_node("CombatHud")).is_true()
    assert_bool(scene.has_node("FeedbackLayer/PressurePanel")).is_true()
    assert_bool(scene.has_node("FeedbackLayer/CameraControlOverlay")).is_true()

    var pressure_label: Label = scene.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")
    var camera_label: Label = scene.get_node("FeedbackLayer/CameraControlOverlay/VBox/CameraStatusLabel")
    assert_str(pressure_label.text).is_equal("Pressure: n/a")
    assert_str(camera_label.text).is_equal("Camera: idle")

# ACC:T42.2
# ACC:T42.6
func test_hud_scene_exposes_task42_owned_surfaces_with_runtime_defaults() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    assert_bool(scene.has_node("FeedbackLayer/OutcomePanel")).is_true()
    assert_bool(scene.has_node("FeedbackLayer/RuntimePromptPanel")).is_true()
    var outcome_label: Label = scene.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
    var prompt_label: Label = scene.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")
    assert_str(outcome_label.text).is_equal("Outcome: n/a")
    assert_str(prompt_label.text).is_equal("Prompt: n/a")

# ACC:T44.1
# ACC:T44.2
# ACC:T44.3
func test_hud_scene_exposes_task44_owned_surfaces_with_runtime_defaults() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    assert_bool(scene.has_node("FeedbackLayer/ResourcePanel")).is_true()
    assert_bool(scene.has_node("FeedbackLayer/BuildPanel")).is_true()
    assert_bool(scene.has_node("FeedbackLayer/ProgressionPanel")).is_true()

    var resource_label: Label = scene.get_node("FeedbackLayer/ResourcePanel/VBox/ResourceSummaryLabel")
    var build_label: Label = scene.get_node("FeedbackLayer/BuildPanel/VBox/BuildSummaryLabel")
    var progression_label: Label = scene.get_node("FeedbackLayer/ProgressionPanel/VBox/ProgressionSummaryLabel")

    assert_str(resource_label.text).is_equal("Resources: gold=n/a iron=n/a pop=n/a")
    assert_str(build_label.text).is_equal("Build: tax=n/a total_gold=n/a")
    assert_str(progression_label.text).is_equal("Progression: tech=n/a reward=n/a")

