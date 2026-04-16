extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _task24_bus: Node

func after() -> void:
    if is_instance_valid(_task24_bus):
        if _task24_bus.get_parent() != null:
            _task24_bus.get_parent().remove_child(_task24_bus)
        _task24_bus.queue_free()
    _task24_bus = null

func _spawn_hud_with_event_bus() -> Dictionary:
    var bus: Node = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    bus.name = "EventBus"
    get_tree().get_root().add_child(bus)
    _task24_bus = bus

    var hud_scene = load("res://Game.Godot/Scenes/UI/HUD.tscn")
    if hud_scene == null:
        return {}
    var hud = hud_scene.instantiate()
    add_child(auto_free(hud))
    await get_tree().process_frame
    return {"bus": bus, "hud": hud}

# ACC:T24.1
# ACC:T24.2
# ACC:T24.5
# ACC:T24.6
# ACC:T24.7
# ACC:T24.9
# ACC:T24.10
# ACC:T24.11
# ACC:T24.12
# ACC:T24.16
func test_hud_real_scene_should_handle_invalid_blocked_and_error_feedback_paths() -> void:
    var setup = await _spawn_hud_with_event_bus()
    if setup.is_empty():
        push_warning("SKIP: HUD/event bus setup unavailable")
        return

    var bus: Node = setup["bus"]
    var hud: Node = setup["hud"]

    var feedback_label: Label = hud.get_node("FeedbackLayer/FeedbackLabel")
    var error_dialog: PanelContainer = hud.get_node("FeedbackLayer/ErrorDialog")
    var error_label: Label = hud.get_node("FeedbackLayer/ErrorDialog/VBox/ErrorMessageLabel")
    var dismiss_btn: Button = hud.get_node("FeedbackLayer/ErrorDialog/VBox/DismissButton")

    bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
        "Code": "tile_occupied",
        "MessageKey": "ui.invalid_action.tile_occupied",
        "Severity": "warning",
        "Details": "tile=(3,4)"
    }))
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_bool(error_dialog.visible).is_false()
    assert_bool(feedback_label.text.strip_edges().length() > 0).is_true()
    assert_bool(feedback_label.text.find("tile_occupied") == -1).is_true()

    bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Severity": "warning",
        "Details": "chapter_locked"
    }))
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Action blocked") >= 0).is_true()
    assert_bool(feedback_label.text.find("chapter_locked") >= 0).is_true()

    bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
        "Code": "missing_required_field",
        "MessageKey": "ui.migration_failure.missing_required_field",
        "Severity": "error",
        "Details": "slot=slot_a"
    }))
    await get_tree().process_frame

    assert_bool(error_dialog.visible).is_true()
    assert_bool(feedback_label.visible).is_false()
    assert_bool(error_label.text.find("Migration failed") >= 0).is_true()
    assert_bool(error_label.text.find("slot=slot_a") >= 0).is_true()

    await get_tree().process_frame
    assert_bool(error_dialog.visible).is_true()

    dismiss_btn.emit_signal("pressed")
    await get_tree().process_frame
    assert_bool(error_dialog.visible).is_false()

# ACC:T24.4
# ACC:T24.13
func test_hud_real_scene_should_auto_hide_temporary_feedback_after_timeout() -> void:
    var setup = await _spawn_hud_with_event_bus()
    if setup.is_empty():
        push_warning("SKIP: HUD/event bus setup unavailable")
        return

    var bus: Node = setup["bus"]
    var hud: Node = setup["hud"]
    var feedback_label: Label = hud.get_node("FeedbackLayer/FeedbackLabel")

    bus.PublishSimple("core.lastking.ui_feedback.raised", "ut", JSON.stringify({
        "Code": "tile_occupied",
        "MessageKey": "ui.invalid_action.tile_occupied",
        "Severity": "warning",
        "Details": "tile=(6,6)"
    }))
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()

    await get_tree().create_timer(1.8).timeout
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_false()
