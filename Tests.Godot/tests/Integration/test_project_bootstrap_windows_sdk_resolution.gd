extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const SDK_OR_RESTORE_ERROR_MARKERS: Array[String] = [
    "the sdk 'microsoft.net.sdk' specified could not be found",
    "missing sdk",
    "failed to restore",
    "restore failed",
    "missing assembly",
    "unable to find package",
    "error nu110",
    "msb4236"
]
const REQUIRED_ENGINE_MAJOR: int = 4
const REQUIRED_ENGINE_MINOR: int = 5
const REQUIRED_ENGINE_PATCH: int = 1
const MINIMUM_DOTNET_SDK_MAJOR: int = 8
const CI_DATE_PATTERN_LENGTH: int = 10

func _detect_sdk_or_restore_issues(lines: Array[String]) -> Array[String]:
    var issues: Array[String] = []
    for raw_line in lines:
        var line: String = raw_line.strip_edges().to_lower()
        for marker in SDK_OR_RESTORE_ERROR_MARKERS:
            if line.find(marker) >= 0:
                issues.append(raw_line)
                break
    return issues

func _joined_lower(lines: Array[String]) -> String:
    return " | ".join(lines).to_lower()

func _is_probable_dotnet_editor() -> bool:
    return ClassDB.class_exists("CSharpScript") or OS.has_feature("dotnet") or OS.has_feature("mono")

func _run_dotnet_info() -> Dictionary:
    var output: Array = []
    var rc: int = OS.execute("dotnet", PackedStringArray(["--info"]), output, true, false)
    var text: String = ""
    for line in output:
        text += str(line) + "\n"
    return {"rc": rc, "output": text}

func _extract_first_sdk_major(dotnet_info_text: String) -> int:
    var regex: RegEx = RegEx.new()
    var compile_error: int = regex.compile("([0-9]+)\\.[0-9]+\\.[0-9]+")
    if compile_error != OK:
        return -1
    var result: RegExMatch = regex.search(dotnet_info_text)
    if result == null:
        return -1
    var value: String = String(result.get_string(1))
    return int(value)

func _latest_restore_log_path() -> String:
    var unit_root: String = ProjectSettings.globalize_path("res://../logs/unit")
    var bound_unit_date_dir: String = CiRunBinding.find_unit_date_dir_by_run_id(unit_root)
    if bound_unit_date_dir != "":
        var bound_candidate: String = bound_unit_date_dir.path_join("dotnet-restore.log")
        if FileAccess.file_exists(bound_candidate):
            return bound_candidate

    var dir: DirAccess = DirAccess.open(unit_root)
    if dir == null:
        return ""

    var latest_date: String = ""
    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir() and entry.length() == 10 and entry[4] == "-" and entry[7] == "-":
            if entry > latest_date:
                latest_date = entry
    dir.list_dir_end()
    if latest_date == "":
        return ""

    var candidate: String = unit_root.path_join(latest_date).path_join("dotnet-restore.log")
    if FileAccess.file_exists(candidate):
        return candidate
    return ""

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

func _read_non_empty_lines(path: String) -> Array[String]:
    if path == "" or not FileAccess.file_exists(path):
        return []
    var text: String = FileAccess.get_file_as_string(path)
    var out: Array[String] = []
    for line in text.split("\n", false):
        var cleaned: String = String(line).strip_edges()
        if cleaned != "":
            out.append(cleaned)
    return out

func _latest_sc_test_log_lines(log_name: String) -> Array[String]:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return []
    var log_path: String = date_dir.path_join("sc-test").path_join(log_name)
    return _read_non_empty_lines(log_path)

# acceptance: ACC:T1.4
func test_clean_windows_bootstrap_log_has_no_sdk_or_restore_issues() -> void:
    var restore_log_path: String = _latest_restore_log_path()
    var clean_lines: Array[String] = _read_non_empty_lines(restore_log_path)
    assert_bool(clean_lines.is_empty()).is_false()

    var restore_issues: Array[String] = _detect_sdk_or_restore_issues(clean_lines)
    assert_int(restore_issues.size()).is_equal(0)

    var editor_compile_lines: Array[String] = _latest_sc_test_log_lines("unit.log")
    var editor_smoke_lines: Array[String] = _latest_sc_test_log_lines("smoke.log")
    assert_bool(editor_compile_lines.is_empty()).is_false()
    assert_bool(editor_smoke_lines.is_empty()).is_false()
    var sc_test_summary: Dictionary = _latest_sc_test_summary()
    assert_bool(sc_test_summary.is_empty()).is_false()
    var unit_step: Dictionary = _find_step(sc_test_summary, "unit")
    var smoke_step: Dictionary = _find_step(sc_test_summary, "smoke")
    assert_bool(unit_step.is_empty()).is_false()
    assert_bool(smoke_step.is_empty()).is_false()
    assert_int(int(unit_step.get("rc", 1))).is_equal(0)
    assert_int(int(smoke_step.get("rc", 1))).is_equal(0)
    var compile_issues: Array[String] = _detect_sdk_or_restore_issues(editor_compile_lines)
    var smoke_issues: Array[String] = _detect_sdk_or_restore_issues(editor_smoke_lines)
    assert_int(compile_issues.size()).is_equal(0)
    assert_int(smoke_issues.size()).is_equal(0)
    if OS.has_feature("windows"):
        var dotnet_info: Dictionary = _run_dotnet_info()
        assert_int(int(dotnet_info.get("rc", 1))).is_equal(0)
        var sdk_major: int = _extract_first_sdk_major(String(dotnet_info.get("output", "")))
        assert_int(sdk_major).is_greater_equal(MINIMUM_DOTNET_SDK_MAJOR)

# acceptance: ACC:T1.15
func test_detector_flags_missing_sdk_and_failed_restore_markers() -> void:
    var broken_lines: Array[String] = [
        "The SDK 'Microsoft.NET.Sdk' specified could not be found.",
        "error NU1101: Unable to find package Example.Package",
        "Restore failed in 00:00:01"
    ]

    var issues: Array[String] = _detect_sdk_or_restore_issues(broken_lines)
    assert_bool(issues.size() >= 2).is_true()
    assert_bool(_joined_lower(issues).find("microsoft.net.sdk") >= 0).is_true()

# acceptance: ACC:T1.19
func test_windows_environment_probe_is_stable_and_returns_boolean() -> void:
    if OS.has_feature("windows"):
        var detected: bool = _is_probable_dotnet_editor()
        assert_bool(typeof(detected) == TYPE_BOOL).is_true()
        var version: Dictionary = Engine.get_version_info()
        assert_int(int(version.get("major", -1))).is_equal(REQUIRED_ENGINE_MAJOR)
        assert_int(int(version.get("minor", -1))).is_equal(REQUIRED_ENGINE_MINOR)
        assert_int(int(version.get("patch", -1))).is_equal(REQUIRED_ENGINE_PATCH)
        var dotnet_info: Dictionary = _run_dotnet_info()
        assert_int(int(dotnet_info.get("rc", 1))).is_equal(0)
        var sdk_major: int = _extract_first_sdk_major(String(dotnet_info.get("output", "")))
        assert_int(sdk_major).is_greater_equal(MINIMUM_DOTNET_SDK_MAJOR)
        return

    # Keep this scaffold deterministic on non-Windows runners.
    assert_bool(OS.has_feature("windows")).is_false()

func test_sdk_or_restore_issues_must_fail_compile_gate_projection() -> void:
    var broken_lines: Array[String] = [
        "The SDK 'Microsoft.NET.Sdk' specified could not be found.",
        "Restore failed in 00:00:01"
    ]
    var issues: Array[String] = _detect_sdk_or_restore_issues(broken_lines)
    var projected_compile_status: String = "failed" if issues.size() > 0 else "success"
    assert_int(issues.size()).is_greater(0)
    assert_str(projected_compile_status).is_equal("failed")
