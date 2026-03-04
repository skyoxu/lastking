extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _events: Array[String] = []

func before() -> void:
    var __bus: Node = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    __bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(__bus))

func _on_evt(type: String, _source: String, _data_json: String, _id: String, _spec: String, _ct: String, _ts: String) -> void:
    _events.append(type)

func test_score_panel_add10_emits_event_or_updates() -> void:
    var bus: Node = get_node_or_null("/root/EventBus")
    assert_object(bus).is_not_null()
    bus.connect("DomainEventEmitted", Callable(self, "_on_evt"))

    var packed := load("res://Game.Godot/Examples/UI/ScorePanel.tscn") as PackedScene
    if packed == null:
        push_warning("SKIP score_panel test: ScorePanel.tscn not found")
        return
    var panel: Node = packed.instantiate()
    add_child(auto_free(panel))
    var btn: Button = panel.get_node("VBox/Buttons/Add10")
    btn.emit_signal("pressed")
    await get_tree().process_frame
    assert_bool(_events.has("core.score.updated") or _events.has("score.changed")).is_true()
    panel.queue_free()
