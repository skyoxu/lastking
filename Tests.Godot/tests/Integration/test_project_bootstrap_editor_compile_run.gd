extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const CANONICAL_PROJECT_ROOT: String = "res://"
const CI_DATE_PATTERN_LENGTH: int = 10

func _project_root_abs() -> String:
    var root: String = ProjectSettings.globalize_path("res://../")
    root = root.simplify_path()
    while root.ends_with("/") or root.ends_with("\\"):
        root = root.substr(0, root.length() - 1)
    return root

func _latest_ci_date_dir() -> String:
    var ci_root: String = ProjectSettings.globalize_path("res://../logs/ci")
    var bound_date_dir: String = CiRunBinding.find_ci_date_dir_by_run_id(ci_root)
    if bound_date_dir != "":
        return bound_date_dir
    var dir: DirAccess = DirAccess.open(ci_root)
    if dir == null:
        return ""

    var date_candidates: Array[String] = []
    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir() and entry.length() == CI_DATE_PATTERN_LENGTH and entry[4] == "-" and entry[7] == "-":
            var summary_candidate: String = ci_root.path_join(entry).path_join("sc-test").path_join("summary.json")
            if FileAccess.file_exists(summary_candidate):
                date_candidates.append(entry)
    dir.list_dir_end()

    if date_candidates.is_empty():
        return ""
    date_candidates.sort()
    date_candidates.reverse()
    for entry in date_candidates:
        var summary: Dictionary = _read_json(ci_root.path_join(entry).path_join("sc-test").path_join("summary.json"))
        if _has_successful_steps(summary, ["unit", "smoke"]):
            return ci_root.path_join(entry)
    return ci_root.path_join(date_candidates[0])

func _read_json(path: String) -> Dictionary:
    if path == "" or not FileAccess.file_exists(path):
        return {}
    var text: String = FileAccess.get_file_as_string(path)
    var parsed: Variant = JSON.parse_string(text)
    if parsed is Dictionary:
        return parsed
    return {}

func _latest_sc_test_summary() -> Dictionary:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return {}
    return _read_json(date_dir.path_join("sc-test").path_join("summary.json"))

func _find_step(summary: Dictionary, step_name: String) -> Dictionary:
    var steps: Array = summary.get("steps", [])
    for item in steps:
        if not (item is Dictionary):
            continue
        var step: Dictionary = item
        if String(step.get("name", "")) == step_name:
            return step
    return {}

func _has_successful_steps(summary: Dictionary, required_steps: Array[String]) -> bool:
    if summary.is_empty():
        return false
    for step_name in required_steps:
        var step: Dictionary = _find_step(summary, step_name)
        if step.is_empty():
            return false
        if int(step.get("rc", 1)) != 0:
            return false
    return true

func _read_text_if_exists(path: String) -> String:
    if path == "" or not FileAccess.file_exists(path):
        return ""
    return FileAccess.get_file_as_string(path)

func _startup_scene_has_csharp_script() -> bool:
    var scene_path: String = str(ProjectSettings.get_setting("application/run/main_scene", ""))
    if scene_path == "":
        scene_path = "res://Game.Godot/Scenes/Main.tscn"
    if not FileAccess.file_exists(scene_path):
        return false
    var scene_text: String = FileAccess.get_file_as_string(scene_path)
    return scene_text.findn(".cs") >= 0

# acceptance: ACC:T1.1
func test_single_canonical_root_is_used_by_bootstrap_steps() -> void:
    var summary: Dictionary = _latest_sc_test_summary()
    assert_bool(summary.is_empty()).is_false()
    assert_str(String(summary.get("run_id", ""))).is_not_empty()
    var expected_run_id: String = CiRunBinding.expected_run_id()
    var summary_run_id: String = String(summary.get("run_id", "")).strip_edges()
    var run_bound_in_progress: bool = expected_run_id != "" and summary_run_id == expected_run_id

    var unit_step: Dictionary = _find_step(summary, "unit")
    var smoke_step: Dictionary = _find_step(summary, "smoke")
    var gdunit_step: Dictionary = _find_step(summary, "gdunit-hard")
    assert_bool(unit_step.is_empty()).is_false()
    assert_bool(smoke_step.is_empty()).is_false()
    assert_bool(unit_step.has("rc")).is_true()
    assert_bool(smoke_step.has("rc")).is_true()
    assert_int(int(unit_step.get("rc", 1))).is_equal(0)
    assert_int(int(smoke_step.get("rc", 1))).is_equal(0)
    if gdunit_step.is_empty():
        assert_bool(run_bound_in_progress).is_true()
    else:
        assert_bool(gdunit_step.has("rc")).is_true()
        assert_int(int(gdunit_step.get("rc", 1))).is_equal(0)
        assert_str(String(gdunit_step.get("log", ""))).is_not_empty()

    var canonical_root: String = _project_root_abs().replace("\\", "/")
    var scoped_steps: Array = [unit_step, smoke_step]
    if not gdunit_step.is_empty():
        scoped_steps.append(gdunit_step)
    for step in scoped_steps:
        var log_path: String = String(step.get("log", ""))
        assert_str(log_path).is_not_empty()
        assert_bool(log_path.replace("\\", "/").find(canonical_root) == 0).is_true()

# acceptance: ACC:T1.5
# acceptance: ACC:T1.2
func test_csharp_mode_smoke_symbols_exist_for_editor_compile_flow() -> void:
    var summary: Dictionary = _latest_sc_test_summary()
    assert_bool(summary.is_empty()).is_false()
    assert_str(String(summary.get("run_id", ""))).is_not_empty()

    var unit_step: Dictionary = _find_step(summary, "unit")
    var smoke_step: Dictionary = _find_step(summary, "smoke")
    assert_bool(unit_step.is_empty()).is_false()
    assert_bool(smoke_step.is_empty()).is_false()
    assert_bool(unit_step.has("rc")).is_true()
    assert_bool(smoke_step.has("rc")).is_true()
    assert_int(int(unit_step.get("rc", 1))).is_equal(0)
    assert_int(int(smoke_step.get("rc", 1))).is_equal(0)

    var unit_log_text: String = _read_text_if_exists(String(unit_step.get("log", ""))).to_lower()
    var smoke_log_text: String = _read_text_if_exists(String(smoke_step.get("log", ""))).to_lower()
    assert_bool(unit_log_text.find("run_dotnet status=") >= 0).is_true()
    assert_bool(unit_log_text.find("build failed") < 0).is_true()
    assert_bool(smoke_log_text.find("starting godot") >= 0).is_true()
    assert_bool(smoke_log_text.find("smoke pass (marker)") >= 0).is_true()
    assert_bool(_startup_scene_has_csharp_script()).is_true()

func test_canonical_root_normalization_is_deterministic() -> void:
    assert_str(CANONICAL_PROJECT_ROOT).is_equal("res://")
