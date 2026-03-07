extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ROOT_PROJECT_CONFIG := "res://../project.godot"
const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"
const BOOTSTRAP_ACTIONS := ["ui_accept", "ui_cancel", "ui_up", "ui_down", "ui_left", "ui_right"]

func _clear_bootstrap_actions() -> void:
    for action_name in BOOTSTRAP_ACTIONS:
        if InputMap.has_action(action_name):
            InputMap.erase_action(action_name)

func _collect_csharp_script_paths(root: Node) -> Array:
    var bindings: Array = []
    var stack: Array = [root]
    while not stack.is_empty():
        var current: Node = stack.pop_back() as Node
        var script_value: Variant = current.get_script()
        if script_value is Script:
            var script_path: String = String(script_value.resource_path)
            if script_path.ends_with(".cs"):
                bindings.append(script_path)
        for child in current.get_children():
            if child is Node:
                stack.append(child)
    return bindings

func _contains_bootstrap_binding(paths: Array) -> bool:
    for path_variant in paths:
        var path_text := String(path_variant)
        if path_text.find("/Scripts/Bootstrap/") >= 0 or path_text.find("/Autoloads/") >= 0:
            return true
    return false

# acceptance: ACC:T11.10
func test_project_exposes_a_loadable_main_scene_path() -> void:
    assert_bool(FileAccess.file_exists(ROOT_PROJECT_CONFIG)).is_true()
    var project_text := FileAccess.get_file_as_string(ROOT_PROJECT_CONFIG)

    assert_bool(project_text.find('run/main_scene="%s"' % MAIN_SCENE_PATH) >= 0).is_true()
    assert_bool(ResourceLoader.exists(MAIN_SCENE_PATH)).is_true()
    assert_bool(MAIN_SCENE_PATH.begins_with("res://")).is_true()
    assert_bool(MAIN_SCENE_PATH.ends_with(".tscn")).is_true()

# acceptance: ACC:T11.19
func test_main_scene_bootstrap_components_produce_runtime_initialization_effects() -> void:
    _clear_bootstrap_actions()

    var packed_scene := load(MAIN_SCENE_PATH) as PackedScene
    assert_object(packed_scene).is_not_null()

    var scene_root := packed_scene.instantiate()
    assert_object(scene_root).is_not_null()

    add_child(auto_free(scene_root))
    await get_tree().process_frame

    var csharp_paths := _collect_csharp_script_paths(scene_root)
    assert_bool(is_instance_valid(scene_root)).is_true()
    assert_int(csharp_paths.size()).is_greater(0)
    assert_bool(_contains_bootstrap_binding(csharp_paths)).is_true()
    assert_object(scene_root.get_node_or_null("InputMapper")).is_not_null()
    assert_object(scene_root.get_node_or_null("SettingsLoader")).is_not_null()
    assert_object(scene_root.get_node_or_null("ScreenNavigator")).is_not_null()

    for action_name in BOOTSTRAP_ACTIONS:
        assert_bool(InputMap.has_action(action_name)).is_true()

    _clear_bootstrap_actions()

func test_bootstrap_lifecycle_effect_disappears_when_input_mapper_binding_is_removed() -> void:
    _clear_bootstrap_actions()

    var packed_scene := load(MAIN_SCENE_PATH) as PackedScene
    assert_object(packed_scene).is_not_null()

    var scene_root := packed_scene.instantiate()
    var input_mapper := scene_root.get_node_or_null("InputMapper")
    assert_object(input_mapper).is_not_null()
    scene_root.remove_child(input_mapper)
    input_mapper.free()

    add_child(auto_free(scene_root))
    await get_tree().process_frame

    for action_name in BOOTSTRAP_ACTIONS:
        assert_bool(InputMap.has_action(action_name)).is_false()
