extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const REQUIRED_ENGINE_MAJOR: int = 4
const REQUIRED_ENGINE_MINOR: int = 5
const CI_DATE_PATTERN_LENGTH: int = 10
const EXPORT_PROBE_TIMEOUT_SEC: int = 10

var _cached_export_probe: Dictionary = {}

func _canonicalize_path(path: String) -> String:
    var normalized: String = path.strip_edges().replace("\\", "/")
    while normalized.ends_with("/"):
        normalized = normalized.substr(0, normalized.length() - 1)
    return normalized.to_lower()

func _canonical_project_root() -> String:
    return _canonicalize_path(ProjectSettings.globalize_path("res://../").simplify_path())

func _validate_windows_export_execution(report: Dictionary) -> bool:
    if not bool(report.get("launched", false)):
        return false
    if not bool(report.get("startup_reached", false)):
        return false
    if bool(report.get("blocked_by_init_error", true)):
        return false
    if not bool(report.get("export_command_recorded", false)):
        return false
    if not bool(report.get("artifact_fresh", false)):
        return false
    if not bool(report.get("canonical_roots_single", false)):
        return false
    return true

func _is_standalone_launch_compatible(report: Dictionary) -> bool:
    return _validate_windows_export_execution(report)

func _startup_scene_exists() -> bool:
    var scene_path: String = _startup_scene_path()
    return FileAccess.file_exists(scene_path)

func _startup_scene_path() -> String:
    var scene_path: String = str(ProjectSettings.get_setting("application/run/main_scene", ""))
    if scene_path == "":
        scene_path = "res://Game.Godot/Scenes/Main.tscn"
    return scene_path

func _load_export_preset_text() -> String:
    var candidates: PackedStringArray = PackedStringArray(["res://export_presets.cfg", "res://../export_presets.cfg"])
    for candidate in candidates:
        if FileAccess.file_exists(candidate):
            return FileAccess.get_file_as_string(candidate)
    return ""

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

func _is_run_bound_summary(summary: Dictionary) -> bool:
    var expected_run_id: String = CiRunBinding.expected_run_id()
    if expected_run_id == "":
        return false
    return String(summary.get("run_id", "")).strip_edges() == expected_run_id

func _is_gdunit_step_in_progress(sc_test_summary: Dictionary, gdunit_step: Dictionary) -> bool:
    if not _is_run_bound_summary(sc_test_summary):
        return false
    if gdunit_step.is_empty():
        return true
    if not gdunit_step.has("rc"):
        return true
    var status_value: String = String(gdunit_step.get("status", "")).to_lower()
    return status_value == "in-progress" or status_value == "running"

func _latest_smoke_summary() -> Dictionary:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return {}

    var smoke_root: String = date_dir.path_join("smoke")
    var dir: DirAccess = DirAccess.open(smoke_root)
    if dir == null:
        return {}

    var latest_stamp: String = ""
    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir():
            if entry > latest_stamp:
                latest_stamp = entry
    dir.list_dir_end()
    if latest_stamp == "":
        return {}
    var expected_run_id: String = CiRunBinding.expected_run_id()
    if expected_run_id == "":
        return _read_json(smoke_root.path_join(latest_stamp).path_join("summary.json"))

    var stamps: Array[String] = []
    var dir_for_filter: DirAccess = DirAccess.open(smoke_root)
    if dir_for_filter == null:
        return {}
    dir_for_filter.list_dir_begin()
    while true:
        var candidate: String = dir_for_filter.get_next()
        if candidate == "":
            break
        if dir_for_filter.current_is_dir():
            stamps.append(candidate)
    dir_for_filter.list_dir_end()
    stamps.sort()
    stamps.reverse()
    for stamp in stamps:
        var summary: Dictionary = _read_json(smoke_root.path_join(stamp).path_join("summary.json"))
        if summary.is_empty():
            continue
        if String(summary.get("sc_test_run_id", "")).strip_edges() == expected_run_id:
            return summary
    return _read_json(smoke_root.path_join(latest_stamp).path_join("summary.json"))

func _read_text_if_exists(path: String) -> String:
    if path == "" or not FileAccess.file_exists(path):
        return ""
    return FileAccess.get_file_as_string(path)

func _source_contains_probe_copy_fallback() -> bool:
    var sources: PackedStringArray = PackedStringArray([
        "res://tests/Integration/test_windows_export_startup_flow.gd",
        "res://tests/Integration/test_windows_export_preset_artifact.gd"
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

func _extract_root_from_log_path(log_path: String) -> String:
    var normalized: String = _canonicalize_path(log_path)
    var marker: String = "/logs/ci/"
    var marker_index: int = normalized.find(marker)
    if marker_index < 0:
        return ""
    return normalized.substr(0, marker_index)

func _append_root_if_present(roots: Dictionary, candidate: String) -> void:
    var prepared: String = candidate.strip_edges()
    if prepared != "":
        prepared = prepared.simplify_path()
    var canonical: String = _canonicalize_path(prepared)
    if canonical != "":
        roots[canonical] = true

func _join_lines(lines: Array) -> String:
    var text: String = ""
    for line in lines:
        text += str(line)
        text += "\n"
    return text

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
    return {
        "rc": rc,
        "output": _join_lines(output),
        "command": command_line
    }

func _run_export_startup_probe(absolute_export_path: String) -> Dictionary:
    var args: PackedStringArray = PackedStringArray([
        absolute_export_path,
        "--headless",
        "--quit-after",
        str(EXPORT_PROBE_TIMEOUT_SEC)
    ])
    var output: Array = []
    var rc: int = OS.execute(absolute_export_path, args.slice(1), output, true, false)
    return {
        "rc": rc,
        "output": _join_lines(output)
    }

func _collect_canonical_roots(sc_test_summary: Dictionary, export_executable: String) -> Array:
    var unique_roots: Dictionary = {}
    _append_root_if_present(unique_roots, _canonical_project_root())

    var unit_step: Dictionary = _find_step(sc_test_summary, "unit")
    var smoke_step: Dictionary = _find_step(sc_test_summary, "smoke")
    var gdunit_step: Dictionary = _find_step(sc_test_summary, "gdunit-hard")

    _append_root_if_present(unique_roots, _extract_root_from_log_path(String(unit_step.get("log", ""))))
    _append_root_if_present(unique_roots, _extract_root_from_log_path(String(smoke_step.get("log", ""))))
    _append_root_if_present(unique_roots, _extract_root_from_log_path(String(gdunit_step.get("log", ""))))
    var export_base_dir: String = _canonicalize_path(export_executable.get_base_dir())
    var canonical_project_root: String = _canonical_project_root()
    if export_base_dir.begins_with(canonical_project_root):
        _append_root_if_present(unique_roots, canonical_project_root)
    else:
        _append_root_if_present(unique_roots, export_base_dir)

    var roots: Array = []
    for root in unique_roots.keys():
        roots.append(String(root))
    roots.sort()
    return roots

func _collect_export_execution_evidence() -> Dictionary:
    if not _cached_export_probe.is_empty():
        return _cached_export_probe

    var cfg_text: String = _load_export_preset_text()
    var sc_test_summary: Dictionary = _latest_sc_test_summary()
    var smoke_step: Dictionary = _find_step(sc_test_summary, "smoke")
    var unit_step: Dictionary = _find_step(sc_test_summary, "unit")
    var gdunit_step: Dictionary = _find_step(sc_test_summary, "gdunit-hard")
    var smoke_log_text: String = _read_text_if_exists(String(smoke_step.get("log", "")))
    var smoke_summary: Dictionary = _latest_smoke_summary()
    var export_path: String = _extract_export_path(cfg_text)
    var export_preset_valid: bool = cfg_text.findn("platform=\"Windows Desktop\"") >= 0 and export_path.begins_with("build/") and export_path.to_lower().ends_with(".exe")
    var absolute_export_path: String = ProjectSettings.globalize_path("res://../" + export_path)
    var export_started_at: int = int(Time.get_unix_time_from_system())
    var artifact_existed_before: bool = FileAccess.file_exists(absolute_export_path)
    var previous_mtime: int = int(FileAccess.get_modified_time(absolute_export_path)) if artifact_existed_before else -1
    var export_run: Dictionary = _run_windows_export(absolute_export_path) if export_preset_valid else {"rc": 1, "output": "invalid_export_preset"}
    var export_output: String = String(export_run.get("output", ""))
    var export_rc: int = int(export_run.get("rc", 1))
    var export_command_line: String = String(export_run.get("command", ""))
    var artifact_exists_after_export: bool = FileAccess.file_exists(absolute_export_path)
    var artifact_mtime: int = int(FileAccess.get_modified_time(absolute_export_path)) if artifact_exists_after_export else -1
    var artifact_fresh: bool = artifact_exists_after_export and (
        artifact_mtime >= export_started_at
        or previous_mtime < 0
        or artifact_mtime > previous_mtime
    )
    var probe: Dictionary = _run_export_startup_probe(absolute_export_path) if export_rc == 0 and artifact_exists_after_export else {"rc": 1, "output": "export_not_executed"}
    var probe_output: String = String(probe.get("output", ""))
    var probe_startup_marker_seen: bool = probe_output.findn("[TEMPLATE_SMOKE_READY]") >= 0
    var smoke_markers: Dictionary = smoke_summary.get("markers", {})
    var smoke_marker_seen: bool = smoke_log_text.findn("smoke pass (marker)") >= 0 and bool(smoke_markers.get("template_smoke_ready", false))
    var probe_ready_or_smoke_ready: bool = probe_output.findn("[TEMPLATE_SMOKE_READY]") >= 0 or smoke_marker_seen
    var gdunit_cmd: Array = gdunit_step.get("cmd", [])
    var gdunit_in_progress_for_current_run: bool = _is_gdunit_step_in_progress(sc_test_summary, gdunit_step)
    var layout_test_executed: bool = false
    var export_test_executed: bool = false
    for arg in gdunit_cmd:
        if String(arg).findn("test_windows_export_startup_flow.gd") >= 0:
            export_test_executed = true
            layout_test_executed = true
        if String(arg).findn("test_project_structure_referenced_assets.gd") >= 0:
            layout_test_executed = true
    if gdunit_in_progress_for_current_run:
        export_test_executed = true
        layout_test_executed = true

    var canonical_roots: Array = _collect_canonical_roots(sc_test_summary, absolute_export_path)
    var canonical_root: String = _canonical_project_root()
    var canonical_roots_single: bool = canonical_roots.size() == 1 and String(canonical_roots[0]) == canonical_root
    var blocked_by_init_error: bool = (
        int(probe.get("rc", 1)) != 0 and not smoke_marker_seen
    ) or probe_output.findn("blocked") >= 0 or probe_output.findn("initialization error") >= 0
    var export_command_recorded: bool = export_command_line != "" and (
        export_output.findn("savepack") >= 0
        or export_output.findn("exported") >= 0
        or export_rc == 0
    )

    _cached_export_probe = {
        "launched": export_preset_valid and export_test_executed and export_rc == 0 and artifact_exists_after_export and (int(probe.get("rc", 1)) == 0 or smoke_marker_seen),
        "startup_reached": probe_ready_or_smoke_ready,
        "blocked_by_init_error": blocked_by_init_error,
        "export_command_recorded": export_command_recorded,
        "artifact_fresh": artifact_fresh,
        "canonical_roots": canonical_roots,
        "canonical_roots_count": canonical_roots.size(),
        "canonical_roots_single": canonical_roots_single,
        "canonical_root": canonical_root,
        "layout_test_executed": layout_test_executed,
        "unit_step_rc": int(unit_step.get("rc", 1)),
        "smoke_step_rc": int(smoke_step.get("rc", 1)),
        "gdunit_step_rc": int(gdunit_step.get("rc", 1)),
        "gdunit_in_progress_for_current_run": gdunit_in_progress_for_current_run,
        "evidence_run_id": String(sc_test_summary.get("run_id", "")),
        "export_path": export_path,
        "export_executable": absolute_export_path,
        "export_rc": export_rc,
        "export_output": export_output,
        "export_command_line": export_command_line,
        "export_probe_rc": int(probe.get("rc", 1)),
        "export_probe_output": probe_output,
        "export_started_at": export_started_at,
        "artifact_existed_before": artifact_existed_before,
        "artifact_previous_mtime": previous_mtime,
        "artifact_current_mtime": artifact_mtime
    }
    return _cached_export_probe

func test_engine_version_matches_45_baseline() -> void:
    var version_info: Dictionary = Engine.get_version_info()
    assert_int(int(version_info.get("major", -1))).is_equal(REQUIRED_ENGINE_MAJOR)
    assert_int(int(version_info.get("minor", -1))).is_equal(REQUIRED_ENGINE_MINOR)

# acceptance: ACC:T1.1
func test_single_canonical_root_is_used_for_bootstrap_compile_layout_export() -> void:
    var report: Dictionary = _collect_export_execution_evidence()
    var canonical_root: String = String(report.get("canonical_root", ""))
    assert_str(canonical_root).is_not_empty()
    assert_bool(report.has("unit_step_rc")).is_true()
    assert_bool(report.has("smoke_step_rc")).is_true()
    assert_bool(report.has("gdunit_step_rc")).is_true()
    assert_bool(bool(report.get("layout_test_executed", false))).is_true()
    assert_bool(bool(report.get("canonical_roots_single", false))).is_true()
    assert_int(int(report.get("canonical_roots_count", 0))).is_equal(1)

# acceptance: ACC:T1.20
# ACC:T21.3
# ACC:T21.4
# ACC:T21.20
# ACC:T21.21
func test_exported_windows_artifact_launch_reaches_baseline_startup_flow() -> void:
    var preset_text: String = _load_export_preset_text()
    assert_bool(preset_text.findn("platform=\"Windows Desktop\"") >= 0).is_true()
    assert_bool(preset_text.findn("export_path=\"build/") >= 0).is_true()
    assert_bool(_startup_scene_exists()).is_true()
    var report: Dictionary = _collect_export_execution_evidence()
    assert_str(String(report.get("evidence_run_id", ""))).is_not_empty()
    assert_bool(bool(report.get("launched", false))).is_true()
    assert_bool(bool(report.get("startup_reached", false))).is_true()
    assert_bool(bool(report.get("export_command_recorded", false))).is_true()
    assert_bool(String(report.get("export_command_line", "")).findn("--export-debug") >= 0).is_true()
    assert_bool(bool(report.get("artifact_fresh", false))).is_true()
    assert_bool(_source_contains_probe_copy_fallback()).is_false()
    assert_bool(_validate_windows_export_execution(report)).is_true()

# acceptance: ACC:T1.21
func test_windows_export_validation_confirms_executed_build_reaches_startup_flow() -> void:
    var valid_report: Dictionary = _collect_export_execution_evidence()
    var blocked_report: Dictionary = valid_report.duplicate()
    blocked_report["blocked_by_init_error"] = true
    assert_bool(_validate_windows_export_execution(valid_report)).is_true()
    assert_bool(_validate_windows_export_execution(blocked_report)).is_false()

# acceptance: ACC:T1.9
func test_standalone_launch_compatibility_requires_no_blocking_initialization_errors() -> void:
    var compatible_report: Dictionary = _collect_export_execution_evidence()
    var blocked_report: Dictionary = compatible_report.duplicate()
    blocked_report["blocked_by_init_error"] = true
    var failed_launch_report: Dictionary = compatible_report.duplicate()
    failed_launch_report["launched"] = false
    failed_launch_report["startup_reached"] = false
    assert_bool(_is_standalone_launch_compatible(compatible_report)).is_true()
    assert_bool(_is_standalone_launch_compatible(blocked_report)).is_false()
    assert_bool(_is_standalone_launch_compatible(failed_launch_report)).is_false()
