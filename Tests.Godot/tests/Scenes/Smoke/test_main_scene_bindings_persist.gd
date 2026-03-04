extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _startup_scene_path() -> String:
    var configured_path: String = String(ProjectSettings.get_setting("application/run/main_scene", ""))
    if configured_path != "":
        return configured_path
    return "res://Game.Godot/Scenes/Main.tscn"

func _startup_scene_sha256(scene_path: String) -> String:
    if scene_path == "" or not FileAccess.file_exists(scene_path):
        return ""
    return FileAccess.get_sha256(ProjectSettings.globalize_path(scene_path))

func _startup_scene_mtime(scene_path: String) -> int:
    if scene_path == "" or not FileAccess.file_exists(scene_path):
        return -1
    return int(FileAccess.get_modified_time(ProjectSettings.globalize_path(scene_path)))


func _instantiate_startup_scene() -> Node:
    var scene_path: String = _startup_scene_path()
    if scene_path == "" or not FileAccess.file_exists(scene_path):
        return null

    var packed: PackedScene = load(scene_path) as PackedScene
    if packed == null:
        return null

    var instance: Node = packed.instantiate()
    add_child(auto_free(instance))
    return instance


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

    bindings.sort()
    return bindings


# ACC:T1.17
func test_startup_scene_has_attached_csharp_bootstrap_script() -> void:
    var startup_scene: Node = _instantiate_startup_scene()
    assert_object(startup_scene).is_not_null()
    if startup_scene == null:
        return
    await get_tree().process_frame

    var bindings: Array = _collect_csharp_script_paths(startup_scene)
    assert_int(bindings.size()).is_greater(0)


func test_startup_scene_csharp_bindings_persist_after_reload() -> void:
    var first_instance: Node = _instantiate_startup_scene()
    assert_object(first_instance).is_not_null()
    if first_instance == null:
        return
    await get_tree().process_frame
    var first_bindings: Array = _collect_csharp_script_paths(first_instance)

    var second_instance: Node = _instantiate_startup_scene()
    assert_object(second_instance).is_not_null()
    if second_instance == null:
        return
    await get_tree().process_frame
    var second_bindings: Array = _collect_csharp_script_paths(second_instance)

    assert_array(first_bindings).is_equal(second_bindings)

# acceptance: ACC:T1.26
func test_startup_scene_file_hash_and_mtime_stay_stable_across_reloads() -> void:
    var scene_path: String = _startup_scene_path()
    assert_str(scene_path).is_not_empty()
    assert_bool(FileAccess.file_exists(scene_path)).is_true()

    var hash_before: String = _startup_scene_sha256(scene_path)
    var mtime_before: int = _startup_scene_mtime(scene_path)
    assert_str(hash_before).is_not_empty()
    assert_int(mtime_before).is_greater_equal(0)

    var first_instance: Node = _instantiate_startup_scene()
    assert_object(first_instance).is_not_null()
    await get_tree().process_frame

    var second_instance: Node = _instantiate_startup_scene()
    assert_object(second_instance).is_not_null()
    await get_tree().process_frame

    var hash_after: String = _startup_scene_sha256(scene_path)
    var mtime_after: int = _startup_scene_mtime(scene_path)
    assert_str(hash_after).is_equal(hash_before)
    assert_int(mtime_after).is_equal(mtime_before)
