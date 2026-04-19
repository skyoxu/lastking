extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CANONICAL_MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"
const ROOT_PROJECT_CONFIG := "res://../project.godot"

var _bus: Node
var _etype := ""
var _got := false


func _read_root_project_setting(key: String) -> String:
    var text: String = FileAccess.get_file_as_string(ROOT_PROJECT_CONFIG)
    var prefix: String = key + "=\""
    for line in text.split("\n"):
        var trimmed: String = line.strip_edges()
        if not trimmed.begins_with(prefix):
            continue
        var value_start: int = prefix.length()
        var value_end: int = trimmed.find("\"", value_start)
        if value_end > value_start:
            return trimmed.substr(value_start, value_end - value_start)
    return ""

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
# ACC:T11.17 ACC:T11.26
# ACC:T31.4
# ACC:T31.14
func test_main_scene_glue_publishes_on_menu_start() -> void:
    var configured_main_scene: String = _read_root_project_setting("run/main_scene")
    assert_bool(configured_main_scene == CANONICAL_MAIN_SCENE_PATH).is_true()
    assert_bool(ResourceLoader.exists(CANONICAL_MAIN_SCENE_PATH)).is_true()
    assert_bool(ResourceLoader.exists("res://Scenes/Main.tscn")).is_false()

    var main = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    add_child(auto_free(main))
    await get_tree().process_frame
    assert_bool(String(main.scene_file_path) == configured_main_scene).is_true()
    assert_object(main.get_node_or_null("ScreenRoot")).is_not_null()
    var menu := main.get_node_or_null("MainMenu")
    assert_object(menu).is_not_null()
    assert_bool(String(menu.get_script().resource_path) == "res://Game.Godot/Scripts/UI/MainMenu.cs").is_true()
    _trigger_play(main)
    await get_tree().process_frame
    assert_bool(_got).is_true()
    assert_str(_etype).is_equal("ui.menu.start")

# ACC:T1.10
# ACC:T11.5
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
