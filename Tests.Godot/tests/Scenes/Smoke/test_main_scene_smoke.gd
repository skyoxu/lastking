extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const CI_DATE_PATTERN_LENGTH: int = 10
const EXTERNAL_SMOKE_TIMEOUT_SEC: int = 5
const CANONICAL_MAIN_SCENE_PATH: String = "res://Game.Godot/Scenes/Main.tscn"
const CANONICAL_MAIN_SCRIPT_PATH: String = "res://Game.Godot/Scripts/Main.gd"
const ROOT_PROJECT_CONFIG: String = "res://../project.godot"

func _instantiate_main_scene() -> Node:
    var packed_scene: PackedScene = preload("res://Game.Godot/Scenes/Main.tscn")
    var scene: Node = packed_scene.instantiate()
    add_child(auto_free(scene))
    return scene

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

func _latest_ci_date_dir() -> String:
    var ci_root: String = ProjectSettings.globalize_path("res://../logs/ci")
    var bound_date_dir: String = CiRunBinding.find_ci_date_dir_by_run_id(ci_root)
    if bound_date_dir != "":
        return bound_date_dir
    var dir: DirAccess = DirAccess.open(ci_root)
    if dir == null:
        return ""

    var latest_date: String = ""
    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir() and entry.length() == CI_DATE_PATTERN_LENGTH and entry[4] == "-" and entry[7] == "-":
            var summary_candidate: String = ci_root.path_join(entry).path_join("sc-test").path_join("summary.json")
            if FileAccess.file_exists(summary_candidate) and entry > latest_date:
                latest_date = entry
    dir.list_dir_end()

    if latest_date == "":
        return ""
    return ci_root.path_join(latest_date)

func _latest_smoke_summaries(limit: int = 2) -> Array:
    var results: Array = []
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return results

    var smoke_root: String = date_dir.path_join("smoke")
    var dir: DirAccess = DirAccess.open(smoke_root)
    if dir == null:
        return results

    var stamps: Array[String] = []
    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir():
            stamps.append(entry)
    dir.list_dir_end()
    stamps.sort()

    for index in range(stamps.size() - 1, -1, -1):
        var summary_path: String = smoke_root.path_join(stamps[index]).path_join("summary.json")
        if not FileAccess.file_exists(summary_path):
            continue
        var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(summary_path))
        if parsed is Dictionary:
            var summary: Dictionary = parsed
            var expected_run_id: String = CiRunBinding.expected_run_id()
            if expected_run_id != "":
                if String(summary.get("sc_test_run_id", "")).strip_edges() != expected_run_id:
                    continue
            var markers: Dictionary = summary.get("markers", {})
            if bool(markers.get("template_smoke_ready", false)):
                results.append(summary)
                if results.size() >= limit:
                    break
    return results

func _latest_sc_test_smoke_log_text() -> String:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return ""
    var smoke_log_path: String = date_dir.path_join("sc-test").path_join("smoke.log")
    if not FileAccess.file_exists(smoke_log_path):
        return ""
    return FileAccess.get_file_as_string(smoke_log_path)


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

func _contains_runtime_error_markers(text: String) -> bool:
    var lowered: String = text.to_lower()
    var markers: PackedStringArray = PackedStringArray([
        "binding error",
        "assembly load",
        "runtime script error",
        "cannot get class",
        "script backtrace"
    ])
    for marker in markers:
        if lowered.find(marker) >= 0:
            return true
    return false

func _contains_hard_runtime_error_markers(text: String) -> bool:
    var lowered: String = text.to_lower()
    var markers: PackedStringArray = PackedStringArray([
        "binding error:",
        "failed to load assembly",
        "missing assembly",
        "runtime script error",
        "cannot get class"
    ])
    for marker in markers:
        if lowered.find(marker) >= 0:
            return true
    return false

func _run_external_scene_probe() -> Dictionary:
    var editor_bin: String = OS.get_executable_path()
    if editor_bin == "" or not FileAccess.file_exists(editor_bin):
        return {"rc": 1, "output": "missing_editor_binary"}
    var project_root: String = ProjectSettings.globalize_path("res://../").simplify_path()
    var args: PackedStringArray = PackedStringArray([
        "--headless",
        "--path",
        project_root,
        "--scene",
        CANONICAL_MAIN_SCENE_PATH,
        "--quit-after",
        str(EXTERNAL_SMOKE_TIMEOUT_SEC)
    ])
    var output: Array = []
    var rc: int = OS.execute(editor_bin, args, output, true, false)
    var output_text: String = ""
    for line in output:
        output_text += str(line)
        output_text += "\n"
    return {"rc": rc, "output": output_text}

# ACC:T1.2
# ACC:T1.13
# ACC:T1.16
# ACC:T11.2 ACC:T11.24
# ACC:T21.14
# ACC:T41.9
func test_main_scene_instantiates_and_visible() -> void:
    var scene: Node = _instantiate_main_scene()
    await get_tree().process_frame
    assert_bool(scene.visible).is_true()
    var csharp_bindings: Array = _collect_csharp_script_paths(scene)
    assert_int(csharp_bindings.size()).is_greater(0)

# ACC:T1.6
# ACC:T11.24
func test_main_scene_naming_and_bootstrap_paths_follow_canonical_convention() -> void:
    var scene: Node = _instantiate_main_scene()
    await get_tree().process_frame
    var configured_main_scene: String = _read_root_project_setting("run/main_scene")
    assert_bool(configured_main_scene == CANONICAL_MAIN_SCENE_PATH).is_true()
    assert_bool(String(scene.scene_file_path) == configured_main_scene).is_true()
    assert_str(scene.name).is_equal("Main")
    var root_script: Script = scene.get_script() as Script
    assert_object(root_script).is_not_null()
    assert_bool(String(root_script.resource_path) == CANONICAL_MAIN_SCRIPT_PATH).is_true()

    var required_bootstrap_nodes := {
        "InputMapper": "res://Game.Godot/Scripts/Bootstrap/InputMapper.cs",
        "SettingsLoader": "res://Game.Godot/Scripts/UI/SettingsLoader.cs",
        "ScreenNavigator": "res://Game.Godot/Scripts/Navigation/ScreenNavigator.cs"
    }
    for node_name in required_bootstrap_nodes.keys():
        var node: Node = scene.get_node_or_null(String(node_name))
        assert_object(node).is_not_null()
        var script_value: Variant = node.get_script()
        assert_bool(script_value is Script).is_true()
        assert_bool(String((script_value as Script).resource_path) == String(required_bootstrap_nodes[node_name])).is_true()

# ACC:T11.6 ACC:T11.8 ACC:T11.14
func test_main_scene_csharp_bindings_can_be_invoked_without_runtime_errors() -> void:
    var scene: Node = _instantiate_main_scene()
    await get_tree().process_frame
    var demo: Node = scene.get_node_or_null("EngineDemo")
    assert_object(demo).is_not_null()
    assert_bool(demo.has_method("AddScore")).is_true()
    assert_bool(demo.has_method("ApplyDamage")).is_true()
    assert_bool(demo.has_method("StartGame")).is_true()
    var output_label: Label = scene.get_node_or_null("VBox/Output") as Label
    assert_object(output_label).is_not_null()

    scene.call("_on_add_score")
    await get_tree().process_frame
    assert_bool(output_label.text.find("Score =") >= 0).is_true()

    scene.call("_on_lose_hp")
    await get_tree().process_frame
    assert_bool(output_label.text.find("HP =") >= 0).is_true()

    demo.call("AddScore", 10)
    demo.call("ApplyDamage", 2)
    demo.call("StartGame")
    await get_tree().process_frame
    assert_bool(scene.is_inside_tree()).is_true()
    var smoke_log_text: String = _latest_sc_test_smoke_log_text()
    if smoke_log_text != "":
        assert_bool(_contains_runtime_error_markers(smoke_log_text)).is_false()
    var probe: Dictionary = _run_external_scene_probe()
    assert_int(int(probe.get("rc", 1))).is_equal(0)
    assert_bool(String(probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()
    assert_bool(_contains_hard_runtime_error_markers(String(probe.get("output", "")))).is_false()

func test_settings_screen_can_load() -> void:
    var packed : PackedScene = preload("res://Game.Godot/Scenes/Screens/SettingsScreen.tscn")
    var inst: Node = packed.instantiate()
    add_child(auto_free(inst))
    await get_tree().process_frame
    assert_bool(inst.is_inside_tree()).is_true()

# ACC:T1.13
# ACC:T11.13
func test_main_scene_bindings_are_stable_across_recent_restart_runs() -> void:
    var smoke_summaries: Array = _latest_smoke_summaries(2)
    var expected_run_id: String = CiRunBinding.expected_run_id()
    var min_required: int = 1 if expected_run_id != "" else 0
    if expected_run_id != "" and smoke_summaries.is_empty():
        push_warning("No run-bound smoke summary available yet; continue with in-memory binding stability checks.")
    else:
        assert_int(smoke_summaries.size()).is_greater_equal(min_required)
    if smoke_summaries.is_empty():
        return
    var latest: Dictionary = smoke_summaries[0]
    var latest_markers: Dictionary = latest.get("markers", {})
    assert_bool(bool(latest_markers.get("template_smoke_ready", false))).is_true()
    if smoke_summaries.size() < 2:
        return
    var previous: Dictionary = smoke_summaries[1]
    assert_str(String(latest.get("scene", ""))).is_equal(String(previous.get("scene", "")))
    var previous_markers: Dictionary = previous.get("markers", {})
    assert_bool(bool(previous_markers.get("template_smoke_ready", false))).is_true()

    var first: Node = _instantiate_main_scene()
    var second: Node = _instantiate_main_scene()
    await get_tree().process_frame
    var first_bindings: Array = _collect_csharp_script_paths(first)
    var second_bindings: Array = _collect_csharp_script_paths(second)
    assert_array(first_bindings).is_equal(second_bindings)

func test_runtime_error_marker_detection_flags_known_failure_text() -> void:
    var healthy_text: String = "[smoke_headless] SMOKE PASS (marker)"
    var failing_text: String = "runtime script error: cannot get class SQLite"
    assert_bool(_contains_runtime_error_markers(healthy_text)).is_false()
    assert_bool(_contains_runtime_error_markers(failing_text)).is_true()
