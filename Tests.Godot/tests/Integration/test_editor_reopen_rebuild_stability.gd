extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const ROOT_PROJECT_CONFIG := "res://../project.godot"
const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"
const ROOT_SCRIPT_PATH := "res://Game.Godot/Scripts/Main.gd"
const HEADLESS_PROBE_TIMEOUT_SEC := 5

func _canonicalize_root(path_value: String) -> String:
    var normalized := path_value.strip_edges().replace("\\", "/")
    while normalized.length() > 1 and normalized.ends_with("/"):
        normalized = normalized.substr(0, normalized.length() - 1)
    if normalized.length() >= 2 and normalized.substr(1, 1) == ":":
        normalized = normalized.substr(0, 1).to_lower() + normalized.substr(1, normalized.length() - 1)
    return normalized

func _project_root_abs() -> String:
    return ProjectSettings.globalize_path("res://../").simplify_path()

func _project_text() -> String:
    return FileAccess.get_file_as_string(ROOT_PROJECT_CONFIG)

func _project_main_scene() -> String:
    var text := _project_text()
    for line in text.split("\n"):
        var trimmed := line.strip_edges()
        if trimmed.begins_with("run/main_scene="):
            return trimmed.trim_prefix("run/main_scene=").trim_prefix("\"").trim_suffix("\"")
    return ""

func _instantiate_main_scene() -> Node:
    var packed_scene := load(MAIN_SCENE_PATH) as PackedScene
    assert_object(packed_scene).is_not_null()
    var scene_root := packed_scene.instantiate()
    assert_object(scene_root).is_not_null()
    add_child(auto_free(scene_root))
    await get_tree().process_frame
    return scene_root

func _root_script_path(scene_root: Node) -> String:
    var script_value: Variant = scene_root.get_script()
    if script_value is Script:
        return String(script_value.resource_path)
    return ""

func _read_bootstrap_fingerprint() -> Dictionary:
    var scene_root := await _instantiate_main_scene()
    return {
        "project_root": _canonicalize_root(_project_root_abs()),
        "main_scene_path": _project_main_scene(),
        "root_script_path": _root_script_path(scene_root),
        "has_input_mapper": scene_root.has_node("InputMapper"),
        "has_settings_loader": scene_root.has_node("SettingsLoader"),
        "has_screen_navigator": scene_root.has_node("ScreenNavigator")
    }

func _join_lines(lines: Array) -> String:
    var text := ""
    for line in lines:
        text += str(line)
        text += "\n"
    return text

func _run_external_headless_probe(scene_path: String) -> Dictionary:
    var editor_bin := OS.get_executable_path()
    if editor_bin == "" or not FileAccess.file_exists(editor_bin):
        return {"rc": 1, "output": "missing_editor_binary"}
    var project_root := _project_root_abs()
    var args := PackedStringArray([
        "--headless",
        "--path",
        project_root,
        "--scene",
        scene_path,
        "--quit-after",
        str(HEADLESS_PROBE_TIMEOUT_SEC)
    ])
    var output: Array = []
    var rc := OS.execute(editor_bin, args, output, true, false)
    return {
        "rc": rc,
        "output": _join_lines(output)
    }

func _read_json_if_exists(path_value: String) -> Dictionary:
    if path_value == "" or not FileAccess.file_exists(path_value):
        return {}
    var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path_value))
    if parsed is Dictionary:
        return parsed
    return {}

func _bound_ci_date_dir() -> String:
    var ci_root := ProjectSettings.globalize_path("res://../logs/ci").simplify_path()
    return CiRunBinding.find_ci_date_dir_by_run_id(ci_root)

func _bound_sc_test_summary() -> Dictionary:
    var date_dir := _bound_ci_date_dir()
    if date_dir == "":
        return {}
    return _read_json_if_exists(date_dir.path_join("sc-test").path_join("summary.json"))

func _bound_smoke_summary() -> Dictionary:
    var date_dir := _bound_ci_date_dir()
    if date_dir == "":
        return {}
    var smoke_root := date_dir.path_join("smoke")
    var dir := DirAccess.open(smoke_root)
    if dir == null:
        return {}
    var expected_run_id := CiRunBinding.expected_run_id()
    var latest_name := ""
    var latest_summary: Dictionary = {}
    dir.list_dir_begin()
    while true:
        var entry := dir.get_next()
        if entry == "":
            break
        if not dir.current_is_dir():
            continue
        var summary_path := smoke_root.path_join(entry).path_join("summary.json")
        var summary := _read_json_if_exists(summary_path)
        if summary.is_empty():
            continue
        if expected_run_id != "" and String(summary.get("sc_test_run_id", "")).strip_edges() != expected_run_id:
            continue
        if entry > latest_name:
            latest_name = entry
            latest_summary = summary
    dir.list_dir_end()
    return latest_summary

func _find_step(steps: Array, step_name: String) -> Dictionary:
    for step_variant in steps:
        if step_variant is Dictionary and String(step_variant.get("name", "")) == step_name:
            return step_variant
    return {}

func _gate_steps_are_successful(sc_test_summary: Dictionary) -> bool:
    if sc_test_summary.is_empty():
        return false
    var steps: Array = sc_test_summary.get("steps", [])
    for required_name in ["unit", "smoke"]:
        var step := _find_step(steps, required_name)
        if step.is_empty():
            return false
        if String(step.get("status", "")) != "ok":
            return false
        if step.has("rc") and step.get("rc") != null and int(step.get("rc", -1)) != 0:
            return false
    return true

func _root_dir_exists(relative_path: String) -> bool:
    return DirAccess.dir_exists_absolute(ProjectSettings.globalize_path("res://../" + relative_path))

func _canonical_structure_holds() -> bool:
    return _root_dir_exists("scripts")         and _root_dir_exists("Game.Godot/Scenes")         and _root_dir_exists("Game.Godot/Assets")         and _root_dir_exists("Game.Godot/Scripts/Config")         and not _root_dir_exists("Scenes")         and not _root_dir_exists("Assets")         and not _root_dir_exists("config")

# ACC:T11.12
func test_reopen_validation_uses_canonical_t1_root_and_real_main_bindings() -> void:
    assert_bool(FileAccess.file_exists(ROOT_PROJECT_CONFIG)).is_true()

    var fingerprint := await _read_bootstrap_fingerprint()
    assert_str(String(fingerprint.get("project_root", ""))).is_equal(_canonicalize_root(_project_root_abs()))
    assert_str(String(fingerprint.get("main_scene_path", ""))).is_equal(MAIN_SCENE_PATH)
    assert_str(String(fingerprint.get("root_script_path", ""))).is_equal(ROOT_SCRIPT_PATH)

    var expected_run_id := CiRunBinding.expected_run_id()
    if expected_run_id == "":
        return

    var smoke_summary := _bound_smoke_summary()
    assert_bool(not smoke_summary.is_empty()).is_true()
    assert_str(String(smoke_summary.get("sc_test_run_id", ""))).is_equal(expected_run_id)
    assert_str(String(smoke_summary.get("scene", ""))).is_equal(MAIN_SCENE_PATH)

# ACC:T11.15
func test_required_gate_steps_are_successful_when_run_bound_sc_test_evidence_exists() -> void:
    var expected_run_id := CiRunBinding.expected_run_id()
    var sc_test_summary := _bound_sc_test_summary()
    if expected_run_id == "":
        return

    assert_bool(not sc_test_summary.is_empty()).is_true()
    assert_str(String(sc_test_summary.get("run_id", ""))).is_equal(expected_run_id)
    assert_bool(_gate_steps_are_successful(sc_test_summary)).is_true()

# ACC:T11.15
func test_gate_step_evaluation_rejects_missing_or_failed_steps() -> void:
    var missing_smoke := {
        "steps": [
            {"name": "unit", "status": "ok", "rc": 0}
        ]
    }
    var failed_smoke := {
        "steps": [
            {"name": "unit", "status": "ok", "rc": 0},
            {"name": "smoke", "status": "fail", "rc": 1}
        ]
    }
    var failed_unit := {
        "steps": [
            {"name": "unit", "status": "fail", "rc": 1},
            {"name": "smoke", "status": "ok", "rc": 0}
        ]
    }

    assert_bool(_gate_steps_are_successful({})).is_false()
    assert_bool(_gate_steps_are_successful(missing_smoke)).is_false()
    assert_bool(_gate_steps_are_successful(failed_smoke)).is_false()
    assert_bool(_gate_steps_are_successful(failed_unit)).is_false()

# ACC:T11.13
func test_reopen_rebuild_keeps_canonical_structure_without_drift() -> void:
    var first_probe := _run_external_headless_probe(MAIN_SCENE_PATH)
    var second_probe := _run_external_headless_probe(MAIN_SCENE_PATH)

    assert_int(int(first_probe.get("rc", 1))).is_equal(0)
    assert_int(int(second_probe.get("rc", 1))).is_equal(0)
    assert_bool(_canonical_structure_holds()).is_true()

# ACC:T11.22
func test_main_scene_binding_stays_stable_across_real_headless_restart_probes() -> void:
    var fingerprint_before := await _read_bootstrap_fingerprint()
    var first_probe := _run_external_headless_probe(MAIN_SCENE_PATH)
    var second_probe := _run_external_headless_probe(MAIN_SCENE_PATH)
    var fingerprint_after := await _read_bootstrap_fingerprint()

    assert_int(int(first_probe.get("rc", 1))).is_equal(0)
    assert_int(int(second_probe.get("rc", 1))).is_equal(0)
    assert_bool(String(first_probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()
    assert_bool(String(second_probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()
    assert_str(String(fingerprint_before.get("main_scene_path", ""))).is_equal(String(fingerprint_after.get("main_scene_path", "")))
    assert_str(String(fingerprint_before.get("root_script_path", ""))).is_equal(String(fingerprint_after.get("root_script_path", "")))
    assert_bool(bool(fingerprint_after.get("has_input_mapper", false))).is_true()
    assert_bool(bool(fingerprint_after.get("has_settings_loader", false))).is_true()
    assert_bool(bool(fingerprint_after.get("has_screen_navigator", false))).is_true()

# ACC:T11.27
func test_reopen_stability_evidence_is_bound_to_the_current_sc_test_run() -> void:
    var expected_run_id := CiRunBinding.expected_run_id()
    if expected_run_id == "":
        return

    var sc_test_summary := _bound_sc_test_summary()
    var smoke_summary := _bound_smoke_summary()

    assert_bool(not sc_test_summary.is_empty()).is_true()
    assert_bool(not smoke_summary.is_empty()).is_true()
    assert_str(String(sc_test_summary.get("run_id", ""))).is_equal(expected_run_id)
    assert_str(String(smoke_summary.get("sc_test_run_id", ""))).is_equal(expected_run_id)
    assert_str(String(smoke_summary.get("scene", ""))).is_equal(_project_main_scene())
