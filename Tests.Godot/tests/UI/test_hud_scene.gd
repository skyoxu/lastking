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
    var phase_label: Label = scene.get_node("TopBar/HBox/PhaseLabel")
    var cycle_label: Label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = scene.get_node("TopBar/HBox/HealthLabel")
    var feedback_layer: Control = scene.get_node("FeedbackLayer")
    var feedback_label: Label = scene.get_node("FeedbackLayer/FeedbackLabel")
    var error_dialog: PanelContainer = scene.get_node("FeedbackLayer/ErrorDialog")
    var dismiss_button: Button = scene.get_node("FeedbackLayer/ErrorDialog/VBox/DismissButton")
    assert_str(day_label.text).is_not_empty()
    assert_str(phase_label.text).is_not_empty()
    assert_str(cycle_label.text).is_not_empty()
    assert_bool(day_label.text.find("1") >= 0).is_true()
    assert_bool(phase_label.text.find("Day") >= 0 or phase_label.text.find("昼") >= 0 or phase_label.text.find("白天") >= 0).is_true()
    assert_bool(cycle_label.text.find("Remaining") >= 0 or cycle_label.text.find("剩余") >= 0).is_true()
    assert_bool(cycle_label.text.find("240.0s") >= 0).is_true()
    assert_bool(hp_label.text.find("100/100") >= 0).is_true()
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
    var phase_label = scene.get_node("TopBar/HBox/PhaseLabel")
    var cycle_label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label = scene.get_node("TopBar/HBox/HealthLabel")
    var speed_state_label = scene.get_node("TopBar/HBox/SpeedStateLabel")
    assert_object(day_label).is_not_null()
    assert_object(phase_label).is_not_null()
    assert_object(cycle_label).is_not_null()
    assert_object(hp_label).is_not_null()
    assert_object(speed_state_label).is_not_null()
    assert_bool(scene.has_node("TopBar/HBox/ScoreLabel")).is_false()
    var hbox: HBoxContainer = scene.get_node("TopBar/HBox")
    var label_count := 0
    for child in hbox.get_children():
        if child is Label:
            label_count += 1
    assert_int(label_count).is_equal(6)



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
    assert_bool(pressure_label.text.find("n/a") >= 0).is_true()
    assert_bool(camera_label.text.find("idle") >= 0 or camera_label.text.find("空闲") >= 0).is_true()

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
    assert_bool(outcome_label.text.find("n/a") >= 0).is_true()
    assert_bool(prompt_label.text.find("n/a") >= 0).is_true()

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

    assert_bool(resource_label.text.find("gold=n/a") >= 0).is_true()
    assert_bool(resource_label.text.find("iron=n/a") >= 0).is_true()
    assert_bool(resource_label.text.find("pop=n/a") >= 0).is_true()
    assert_bool(build_label.text.find("tax=n/a") >= 0).is_true()
    assert_bool(build_label.text.find("total_gold=n/a") >= 0).is_true()
    assert_bool(progression_label.text.find("tech=n/a") >= 0).is_true()
    assert_bool(progression_label.text.find("reward=n/a") >= 0).is_true()


