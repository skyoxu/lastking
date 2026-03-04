extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node
var _etype := ""
var _got := false

func before() -> void:
    _bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    _bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(_bus))
    _bus.connect("DomainEventEmitted", Callable(self, "_on_evt"))

func _on_evt(type, _source, _data_json, _id, _spec, _ct, _ts) -> void:
    _etype = str(type)
    _got = true

func _trigger_play(main: Node) -> void:
    var menu := main.get_node_or_null("MainMenu")
    assert_object(menu).is_not_null()
    var btn := menu.get_node("VBox/BtnPlay")
    btn.emit_signal("pressed")

# ACC:T1.10
func test_main_scene_glue_publishes_on_menu_start() -> void:
    var main = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    add_child(auto_free(main))
    await get_tree().process_frame
    var menu := main.get_node_or_null("MainMenu")
    assert_object(menu).is_not_null()
    assert_str(String(menu.get_script().resource_path)).is_equal("res://Game.Godot/Scripts/UI/MainMenu.cs")
    _trigger_play(main)
    await get_tree().process_frame
    assert_bool(_got).is_true()
    assert_str(_etype).is_equal("ui.menu.start")

# ACC:T1.10
func test_main_scene_glue_persists_across_reinstantiation() -> void:
    _got = false
    _etype = ""
    var first = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    add_child(auto_free(first))
    await get_tree().process_frame
    _trigger_play(first)
    await get_tree().process_frame
    assert_bool(_got).is_true()
    assert_str(_etype).is_equal("ui.menu.start")

    _got = false
    _etype = ""
    var second = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    add_child(auto_free(second))
    await get_tree().process_frame
    _trigger_play(second)
    await get_tree().process_frame
    assert_bool(_got).is_true()
    assert_str(_etype).is_equal("ui.menu.start")

# ACC:T1.10
func test_main_scene_glue_fails_when_menu_script_binding_is_removed() -> void:
    _got = false
    _etype = ""
    var broken = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    var menu := broken.get_node_or_null("MainMenu")
    assert_object(menu).is_not_null()
    menu.set_script(null)
    add_child(auto_free(broken))
    await get_tree().process_frame
    _trigger_play(broken)
    await get_tree().process_frame
    assert_bool(_got).is_false()

