extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
    _bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    _bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(_bus))

func _hud() -> Node:
    var hud = preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(hud))
    await get_tree().process_frame
    return hud

func _bridge() -> Node:
    var bridge = preload("res://Game.Godot/Scripts/Combat/CastleBattleEventBridge.cs").new()
    add_child(auto_free(bridge))
    await get_tree().process_frame
    return bridge

func _publish(type_name: String, payload: Dictionary) -> void:
    _bus.PublishSimple(type_name, "ut", JSON.stringify(payload))

# ACC:T7.1
# ACC:T7.9
# ACC:T7.15
func test_hud_updates_current_castle_hp_when_runtime_publishes_castle_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    bridge.call("StartBattle", 50, "run-7", 1, "castle")
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(50)
    assert_str(hp_label.text).is_equal("HP: 50")

    bridge.call("ResolveCastleAttack", 8)
    await get_tree().process_frame

    assert_int(int(bridge.GetCurrentHp())).is_equal(42)
    assert_str(hp_label.text).is_equal("HP: 42")

# ACC:T7.12
func test_hud_tracks_latest_castle_hp_across_runtime_published_events_and_ignores_non_castle_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    bridge.call("StartBattle", 50, "run-7", 1, "castle")
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(50)
    assert_str(hp_label.text).is_equal("HP: 50")

    bridge.call("ResolveCastleAttack", 3)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(47)
    assert_str(hp_label.text).is_equal("HP: 47")

    bridge.call("ResolveCastleAttack", 8)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(39)
    assert_str(hp_label.text).is_equal("HP: 39")

    _publish("core.score.updated", {"value": 99})
    await get_tree().process_frame
    assert_str(hp_label.text).is_equal("HP: 39")

