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
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    var bus: Node = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(bus))
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

