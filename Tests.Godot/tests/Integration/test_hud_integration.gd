extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func before() -> void:
    var __bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    __bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(__bus))

func _load_main() -> Node:
    var main = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    get_tree().get_root().add_child(auto_free(main))
    await get_tree().process_frame
    return main

func _remaining_seconds(label_text: String) -> float:
    var cleaned := label_text.replace("Cycle Remaining:", "").replace("s", "").strip_edges()
    return float(cleaned)

# ACC:T9.7
# ACC:T9.11
# ACC:T9.15
# ACC:T9.19
func test_hud_updates_on_core_events() -> void:
    var main = await _load_main()
    var bus = get_node_or_null("/root/EventBus")
    assert_object(bus).is_not_null()

    var hud = main.get_node("HUD")
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    bus.PublishSimple("core.lastking.day.started", "ut", '{"day":4,"from":"Night","to":"Day","tick":20}')
    for i in range(20):
        await get_tree().process_frame
    assert_str(day_label.text).is_equal("Day: 4")
    var day_start_remaining = _remaining_seconds(cycle_label.text)
    assert_float(day_start_remaining).is_greater(200.0)

    var cycle_before = cycle_label.text
    for i in range(30):
        await get_tree().process_frame
    var cycle_after = cycle_label.text
    assert_str(cycle_after).is_not_equal(cycle_before)
    var cycle_after_seconds = _remaining_seconds(cycle_after)
    assert_float(cycle_after_seconds).is_less(day_start_remaining)

    bus.PublishSimple("core.lastking.castle.hp_changed", "ut", '{"Day":4,"PreviousHp":100,"CurrentHp":77}')
    for i in range(20):
        await get_tree().process_frame
    assert_str(hp_label.text).is_equal("HP: 77")

    bus.PublishSimple("core.lastking.night.started", "ut", '{"day":4,"from":"Day","to":"Night","tick":21}')
    for i in range(20):
        await get_tree().process_frame
    var night_cycle = cycle_label.text
    assert_str(night_cycle).is_not_equal(cycle_after)
    var night_remaining = _remaining_seconds(night_cycle)
    assert_float(night_remaining).is_less(120.1)
    assert_float(night_remaining).is_greater(90.0)
