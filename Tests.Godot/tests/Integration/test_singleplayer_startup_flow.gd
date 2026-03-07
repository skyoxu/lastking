extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node
var _event_types: Array[String] = []

func before() -> void:
    _event_types.clear()
    _bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    _bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(_bus))
    _bus.connect("DomainEventEmitted", Callable(self, "_on_evt"))

func _on_evt(type, _source, _data_json, _id, _spec, _ct, _ts) -> void:
    _event_types.append(str(type))

func _instantiate_main() -> Node:
    var existing := get_tree().get_root().get_node_or_null("Main")
    if existing != null:
        existing.queue_free()
        await get_tree().process_frame
    var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    get_tree().get_root().add_child(auto_free(main))
    await get_tree().process_frame
    var navigator := main.get_node_or_null("ScreenNavigator")
    assert_object(navigator).is_not_null()
    navigator.UseFadeTransition = false
    return main

func _trigger_play(main: Node) -> void:
    var button := main.get_node("MainMenu/VBox/BtnPlay")
    button.emit_signal("pressed")

func _screen_root(main: Node) -> Node:
    return main.get_node("ScreenRoot")

func _screen_root_contains_start_screen(screen_root: Node) -> bool:
    for child in screen_root.get_children():
        if child is Node:
            var script_value: Variant = child.get_script()
            if script_value is Script and String(script_value.resource_path) == "res://Game.Godot/Scripts/Screens/StartScreen.cs":
                return true
            if String(child.name).findn("StartScreen") >= 0:
                return true
    return false

func _multiplayer_peer_class(multiplayer_api: MultiplayerAPI) -> String:
    if multiplayer_api.multiplayer_peer == null:
        return "<null>"
    return String(multiplayer_api.multiplayer_peer.get_class())

func _await_frames(count: int) -> void:
    for _i in range(count):
        await get_tree().process_frame

# ACC:T11.18
func test_real_main_scene_enters_start_screen_without_multiplayer_reconfiguration() -> void:
    var main := await _instantiate_main()
    var multiplayer_api := main.get_multiplayer()
    var screen_root := _screen_root(main)
    var peer_before := _multiplayer_peer_class(multiplayer_api)

    assert_object(multiplayer_api).is_not_null()
    assert_int(screen_root.get_child_count()).is_equal(0)

    _trigger_play(main)
    await _await_frames(5)

    assert_bool(_event_types.has("ui.menu.start")).is_true()
    assert_str(_multiplayer_peer_class(multiplayer_api)).is_equal(peer_before)
    assert_int(screen_root.get_child_count()).is_greater_equal(1)
    assert_bool(_screen_root_contains_start_screen(screen_root)).is_true()
    assert_bool(main.has_node("EngineDemo")).is_true()
    assert_bool(main.has_node("ScreenNavigator")).is_true()

func test_repeated_menu_start_keeps_singleplayer_target_and_multiplayer_state_stable() -> void:
    var main := await _instantiate_main()
    var multiplayer_api := main.get_multiplayer()
    var screen_root := _screen_root(main)
    var peer_before := _multiplayer_peer_class(multiplayer_api)

    _trigger_play(main)
    await _await_frames(5)
    _trigger_play(main)
    await _await_frames(5)

    assert_bool(_event_types.has("ui.menu.start")).is_true()
    assert_str(_multiplayer_peer_class(multiplayer_api)).is_equal(peer_before)
    assert_int(screen_root.get_child_count()).is_greater_equal(1)
    assert_bool(_screen_root_contains_start_screen(screen_root)).is_true()
