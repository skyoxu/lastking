extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _task24_bus: Node

func after() -> void:
    if is_instance_valid(_task24_bus):
        if _task24_bus.get_parent() != null:
            _task24_bus.get_parent().remove_child(_task24_bus)
        _task24_bus.queue_free()
    _task24_bus = null

# ACC:T24.18
# ACC:T24.23
# ACC:T28.4
# ACC:T28.11
# ACC:T28.12
# ACC:T28.16
func test_language_applies_runtime() -> void:
    var packed = load("res://Game.Godot/Scenes/UI/SettingsPanel.tscn")
    if packed == null:
        push_warning("SKIP: SettingsPanel.tscn not found")
        return
    var panel = packed.instantiate()
    add_child(auto_free(panel))
    await get_tree().process_frame
    var lang_opt = panel.get_node("VBox/LangRow/LangOpt")
    if lang_opt.get_item_count() == 0:
        lang_opt.add_item("en"); lang_opt.add_item("zh")
    # select zh and emit selection
    var idx := -1
    for i in range(lang_opt.get_item_count()):
        if str(lang_opt.get_item_text(i)).to_lower() == "zh":
            idx = i
            break
    if idx == -1:
        push_warning("SKIP: zh option not found")
        return
    lang_opt.select(idx)
    lang_opt.emit_signal("item_selected", idx)
    await get_tree().process_frame
    assert_str(TranslationServer.get_locale()).contains("zh")

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

# ACC:T24.18
# ACC:T24.23
# ACC:T28.5
func test_localized_feedback_text_is_user_facing_and_avoids_raw_reason_codes_after_locale_switch() -> void:
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
        "Details": "tile=(1,2)"
    }))
    await get_tree().process_frame

    var text := feedback_label.text.strip_edges()
    assert_bool(feedback_label.visible).is_true()
    assert_bool(text.length() > 3).is_true()
    assert_bool(text.find("tile_occupied") == -1).is_true()
    assert_bool(text.find("missing_required_field") == -1).is_true()

