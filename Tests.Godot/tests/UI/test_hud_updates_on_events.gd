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

func _remaining_seconds(label_text: String) -> float:
    var cleaned := label_text.replace("Cycle Remaining:", "").replace("s", "").strip_edges()
    return float(cleaned)

# ACC:T9.2
# ACC:T9.5
# ACC:T9.6
# ACC:T9.8
# ACC:T9.9
# ACC:T7.1
# ACC:T7.9
# ACC:T7.15
func test_hud_updates_day_cycle_and_castle_hp_when_runtime_publishes_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    _publish("core.lastking.day.started", {"day": 3, "from": "Night", "to": "Day", "tick": 11})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal("Day: 3")

    var cycle_before = _remaining_seconds(cycle_label.text)
    for i in range(15):
        await get_tree().process_frame
    var cycle_after = _remaining_seconds(cycle_label.text)
    assert_float(cycle_after).is_less(cycle_before)
    assert_float(cycle_after).is_greater_equal(0.0)
    assert_float(cycle_before).is_less(240.1)

    bridge.call("StartBattle", 50, "run-9", 3, "castle")
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(50)
    assert_str(hp_label.text).is_equal("HP: 50")

    bridge.call("ResolveCastleAttack", 8)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(42)
    assert_str(hp_label.text).is_equal("HP: 42")

# ACC:T9.5
# ACC:T9.6
func test_hud_acceptance_anchor_binding_for_t9_refs() -> void:
    var hud = await _hud()
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    _publish("core.lastking.day.started", {"day": 20, "from": "Night", "to": "Day", "tick": 1})
    _publish("core.lastking.castle.hp_changed", {"Day": 15, "PreviousHp": 100, "CurrentHp": 66})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal("Day: 15")
    assert_str(hp_label.text).is_equal("HP: 66")

    hud.call("SetCycleRemainingSeconds", 88.0)
    await get_tree().process_frame
    var day_before = day_label.text
    var cycle_before = cycle_label.text
    var hp_before = hp_label.text

    _publish("core.score.updated", {"value": 999})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal(day_before)
    assert_str(cycle_label.text).is_equal(cycle_before)
    assert_str(hp_label.text).is_equal(hp_before)

# ACC:T9.14
# ACC:T9.17
# ACC:T9.20
# ACC:T16.9
# ACC:T7.12
func test_hud_tracks_latest_castle_hp_across_runtime_published_events_and_ignores_non_castle_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    bridge.call("StartBattle", 50, "run-9", 2, "castle")
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

# ACC:T9.21
func test_cycle_remaining_is_monotonic_and_bounded_within_each_phase() -> void:
    var hud = await _hud()
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")

    _publish("core.lastking.day.started", {"day": 6, "from": "Night", "to": "Day", "tick": 31})
    await get_tree().process_frame
    var day_first = _remaining_seconds(cycle_label.text)
    for i in range(20):
        await get_tree().process_frame
    var day_second = _remaining_seconds(cycle_label.text)
    assert_float(day_first).is_less(240.1)
    assert_float(day_first).is_greater_equal(0.0)
    assert_float(day_second).is_less_equal(day_first)
    assert_float(day_second).is_greater_equal(0.0)

    _publish("core.lastking.night.started", {"day": 6, "from": "Day", "to": "Night", "tick": 32})
    await get_tree().process_frame
    var night_first = _remaining_seconds(cycle_label.text)
    for i in range(20):
        await get_tree().process_frame
    var night_second = _remaining_seconds(cycle_label.text)
    assert_float(night_first).is_less(120.1)
    assert_float(night_first).is_greater_equal(0.0)
    assert_float(night_second).is_less_equal(night_first)
    assert_float(night_second).is_greater_equal(0.0)
