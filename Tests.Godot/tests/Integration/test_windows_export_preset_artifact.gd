extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const CANONICAL_EXPORT_PRESET_FILE: String = "export_presets.cfg"
const WINDOWS_PLATFORM_TOKENS: Array[String] = ["Windows Desktop", "Windows"]
const CI_DATE_PATTERN_LENGTH: int = 10
const EXPORT_PROBE_TIMEOUT_SEC: int = 10

var _cached_export_report: Dictionary = {}

func _canonical_export_preset_candidates() -> PackedStringArray:
    return PackedStringArray([
        "res://%s" % CANONICAL_EXPORT_PRESET_FILE,
        "res://../%s" % CANONICAL_EXPORT_PRESET_FILE
    ])

func _expected_windows_artifact_candidates() -> PackedStringArray:
    return PackedStringArray([
        "build/Game.exe",
        "build/windows/Game.exe",
        "build/WindowsDesktop/Game.exe"
    ])

func _first_existing_file(paths: PackedStringArray) -> String:
    for path in paths:
        if FileAccess.file_exists(path):
            return path
    return ""

func _contains_windows_platform_marker(cfg_text: String) -> bool:
    for token in WINDOWS_PLATFORM_TOKENS:
        if cfg_text.findn(token) >= 0:
            return true
    return false

func _extract_export_path(cfg_text: String) -> String:
    var marker: String = "export_path=\""
    var start_index: int = cfg_text.find(marker)
    if start_index < 0:
        return ""
    var value_start: int = start_index + marker.length()
    var value_end: int = cfg_text.find("\"", value_start)
    if value_end < 0:
        return ""
    return cfg_text.substr(value_start, value_end - value_start)

func _read_json(path: String) -> Dictionary:
    if path == "" or not FileAccess.file_exists(path):
        return {}
    var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
    if parsed is Dictionary:
        return parsed
    return {}

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
            if not FileAccess.file_exists(summary_candidate):
                continue
            if entry > latest_date:
                latest_date = entry
    dir.list_dir_end()
    if latest_date == "":
        return ""
    return ci_root.path_join(latest_date)

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

func _startup_probe_arguments(executable_path: String) -> PackedStringArray:
    return PackedStringArray([executable_path, "--headless", "--quit-after", str(EXPORT_PROBE_TIMEOUT_SEC)])

func _to_absolute(project_path: String) -> String:
    return ProjectSettings.globalize_path(project_path)

func _is_executable_path(path: String) -> bool:
    return path.to_lower().ends_with(".exe")

func _join_lines(lines: Array) -> String:
    var text: String = ""
    for line in lines:
        text += str(line)
        text += "\n"
    return text

func _source_contains_probe_copy_fallback() -> bool:
    var sources: PackedStringArray = PackedStringArray([
        "res://tests/Integration/test_windows_export_preset_artifact.gd",
        "res://tests/Integration/test_windows_export_startup_flow.gd"
    ])
    var forbidden_tokens: PackedStringArray = PackedStringArray([
        "_ensure_" + "export_probe_executable(",
        "copy_" + "absolute(",
        "fileaccess." + "copy(",
        "diraccess." + "copy("
    ])
    for source in sources:
        if not FileAccess.file_exists(source):
            continue
        var text: String = FileAccess.get_file_as_string(source).to_lower()
        for token in forbidden_tokens:
            if text.find(String(token).to_lower()) >= 0:
                return true
    return false

func _run_windows_export(absolute_export_path: String) -> Dictionary:
    var export_dir: String = absolute_export_path.get_base_dir()
    if export_dir == "":
        return {"rc": 1, "output": "missing_export_directory"}
    var make_error: int = DirAccess.make_dir_recursive_absolute(export_dir)
    if make_error != OK and make_error != ERR_ALREADY_EXISTS:
        return {"rc": 1, "output": "mkdir_failed:%d" % make_error}

    var editor_bin: String = OS.get_executable_path()
    if editor_bin == "" or not FileAccess.file_exists(editor_bin):
        return {"rc": 1, "output": "missing_editor_binary"}
    var project_root: String = ProjectSettings.globalize_path("res://../").simplify_path()
    var args: PackedStringArray = PackedStringArray([
        "--headless",
        "--path",
        project_root,
        "--export-debug",
        "Windows Desktop",
        absolute_export_path
    ])
    var output: Array = []
    var rc: int = OS.execute(editor_bin, args, output, true, false)
    var command_line: String = "%s --headless --path %s --export-debug \"Windows Desktop\" %s" % [editor_bin, project_root, absolute_export_path]
    return {"rc": rc, "output": _join_lines(output), "command": command_line}

func _run_export_startup_probe(absolute_export_path: String) -> Dictionary:
    var args: PackedStringArray = PackedStringArray(["--headless", "--quit-after", str(EXPORT_PROBE_TIMEOUT_SEC)])
    var output: Array = []
    var rc: int = OS.execute(absolute_export_path, args, output, true, false)
    return {"rc": rc, "output": _join_lines(output)}

func _collect_export_report() -> Dictionary:
    if not _cached_export_report.is_empty():
        return _cached_export_report

    var cfg_path: String = _first_existing_file(_canonical_export_preset_candidates())
    var cfg_text: String = FileAccess.get_file_as_string(cfg_path) if cfg_path != "" else ""
    var configured_export_path: String = _extract_export_path(cfg_text)
    var absolute_artifact: String = _to_absolute("res://../" + configured_export_path)
    var artifact_exists_before: bool = FileAccess.file_exists(absolute_artifact)
    var artifact_mtime_before: int = int(FileAccess.get_modified_time(absolute_artifact)) if artifact_exists_before else -1
    var export_started_at: int = int(Time.get_unix_time_from_system())
    var export_result: Dictionary = _run_windows_export(absolute_artifact)
    var export_rc: int = int(export_result.get("rc", 1))
    var export_output: String = String(export_result.get("output", ""))
    var export_command_line: String = String(export_result.get("command", ""))
    var artifact_exists_after: bool = FileAccess.file_exists(absolute_artifact)
    var artifact_mtime_after: int = int(FileAccess.get_modified_time(absolute_artifact)) if artifact_exists_after else -1
    var artifact_fresh: bool = artifact_exists_after and (
        artifact_mtime_after >= export_started_at
        or artifact_mtime_before < 0
        or artifact_mtime_after > artifact_mtime_before
    )
    var probe_result: Dictionary = _run_export_startup_probe(absolute_artifact) if export_rc == 0 and artifact_exists_after else {"rc": 1, "output": "export_not_executed"}
    var probe_output: String = String(probe_result.get("output", ""))
    var startup_marker_seen: bool = probe_output.findn("[TEMPLATE_SMOKE_READY]") >= 0
    var blocked_by_init_error: bool = int(probe_result.get("rc", 1)) != 0 or probe_output.findn("blocked") >= 0 or probe_output.findn("initialization error") >= 0
    var export_command_recorded: bool = export_output.findn("savepack") >= 0 or export_output.findn("exported") >= 0

    var summary: Dictionary = _latest_sc_test_summary()
    var smoke_step: Dictionary = _find_step(summary, "smoke")
    var gdunit_step: Dictionary = _find_step(summary, "gdunit-hard")

    _cached_export_report = {
        "cfg_path": cfg_path,
        "configured_export_path": configured_export_path,
        "absolute_artifact": absolute_artifact,
        "export_rc": export_rc,
        "export_output": export_output,
        "export_command_line": export_command_line,
        "probe_rc": int(probe_result.get("rc", 1)),
        "probe_output": probe_output,
        "launched": export_rc == 0 and artifact_exists_after and int(probe_result.get("rc", 1)) == 0,
        "startup_reached": startup_marker_seen,
        "blocked_by_init_error": blocked_by_init_error,
        "artifact_exists_after": artifact_exists_after,
        "artifact_fresh": artifact_fresh,
        "export_command_recorded": export_command_recorded,
        "produced_by_export": export_rc == 0 and export_command_recorded and artifact_exists_after,
        "smoke_step_rc": int(smoke_step.get("rc", 1)),
        "gdunit_step_rc": int(gdunit_step.get("rc", 1))
    }
    return _cached_export_report

func _validate_export_execution(report: Dictionary) -> bool:
    return bool(report.get("launched", false)) \
        and bool(report.get("startup_reached", false)) \
        and not bool(report.get("blocked_by_init_error", true)) \
        and bool(report.get("artifact_fresh", false)) \
        and bool(report.get("export_command_recorded", false)) \
        and bool(report.get("produced_by_export", false))

# ACC:T1.8
# ACC:T21.20
# ACC:T21.22
func test_windows_export_preset_scaffold_uses_canonical_candidates_and_exe_artifacts() -> void:
    var preset_candidates: PackedStringArray = _canonical_export_preset_candidates()
    assert(preset_candidates.size() >= 1)
    assert(preset_candidates[0] == "res://export_presets.cfg")

    var artifact_candidates: PackedStringArray = _expected_windows_artifact_candidates()
    assert(artifact_candidates.size() >= 1)
    for candidate in artifact_candidates:
        assert(_is_executable_path(candidate))

    var report: Dictionary = _collect_export_report()
    assert_bool(report.is_empty()).is_false()
    assert_bool(String(report.get("cfg_path", "")) != "").is_true()
    var cfg_text: String = FileAccess.get_file_as_string(String(report.get("cfg_path", "")))
    assert_bool(_contains_windows_platform_marker(cfg_text)).is_true()
    assert_bool(cfg_text.findn("platform=\"Windows Desktop\"") >= 0).is_true()
    assert_bool(String(report.get("configured_export_path", "")).begins_with("build/")).is_true()
    assert_bool(_is_executable_path(String(report.get("configured_export_path", "")))).is_true()
    assert_bool(report.has("smoke_step_rc")).is_true()
    assert_bool(report.has("gdunit_step_rc")).is_true()
    assert_int(int(report.get("export_rc", 1))).is_equal(0)
    assert_int(int(report.get("probe_rc", 1))).is_equal(0)
    assert_bool(String(report.get("export_command_line", "")).findn("--export-debug") >= 0).is_true()
    assert_bool(bool(report.get("artifact_exists_after", false))).is_true()
    assert_bool(bool(report.get("artifact_fresh", false))).is_true()
    assert_bool(bool(report.get("export_command_recorded", false))).is_true()
    assert_bool(bool(report.get("produced_by_export", false))).is_true()
    assert_bool(_source_contains_probe_copy_fallback()).is_false()
    assert_bool(_validate_export_execution(report)).is_true()

# ACC:T1.20
func test_windows_export_startup_probe_command_is_deterministic_and_windows_safe() -> void:
    var cfg_path: String = _first_existing_file(_canonical_export_preset_candidates())
    assert_bool(cfg_path != "").is_true()
    var configured_export_path: String = _extract_export_path(FileAccess.get_file_as_string(cfg_path))
    assert_bool(configured_export_path != "").is_true()
    var absolute_artifact: String = _to_absolute("res://../" + configured_export_path)
    var probe_args: PackedStringArray = _startup_probe_arguments(absolute_artifact)

    assert(probe_args.size() == 4)
    assert(_is_executable_path(probe_args[0]))
    assert(probe_args.has("--headless"))
    assert(probe_args.has("--quit-after"))
    var report: Dictionary = _collect_export_report()
    assert_int(int(report.get("export_rc", 1))).is_equal(0)
    assert_int(int(report.get("probe_rc", 1))).is_equal(0)
    assert_bool(String(report.get("probe_output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()

# ACC:T1.21
func test_export_validation_rejects_non_executable_or_missing_startup_marker() -> void:
    assert_bool(_is_executable_path("build/Game.exe")).is_true()
    assert_bool(_is_executable_path("build/Game.pck")).is_false()
    var valid_report: Dictionary = _collect_export_report()
    assert_bool(_validate_export_execution(valid_report)).is_true()

    var blocked_report: Dictionary = valid_report.duplicate()
    blocked_report["blocked_by_init_error"] = true
    var missing_startup_report: Dictionary = valid_report.duplicate()
    missing_startup_report["startup_reached"] = false
    var forged_artifact_report: Dictionary = valid_report.duplicate()
    forged_artifact_report["produced_by_export"] = false
    forged_artifact_report["export_command_recorded"] = false

    assert_bool(_validate_export_execution(blocked_report)).is_false()
    assert_bool(_validate_export_execution(missing_startup_report)).is_false()
    assert_bool(_validate_export_execution(forged_artifact_report)).is_false()
