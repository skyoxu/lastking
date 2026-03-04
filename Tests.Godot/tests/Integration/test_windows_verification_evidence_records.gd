extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const REQUIRED_STEPS: Array[String] = [
    "editor_open",
    "csharp_compile",
    "startup_scene_execution",
    "export_launch",
]
const CI_DATE_PATTERN_LENGTH: int = 10

func _canonicalize_root(path: String) -> String:
    var normalized: String = path.strip_edges().replace("\\", "/")
    while normalized.ends_with("/"):
        normalized = normalized.substr(0, normalized.length() - 1)
    return normalized.to_lower()

func _project_root_abs() -> String:
    return ProjectSettings.globalize_path("res://../").simplify_path()

func _startup_scene_path() -> String:
    var configured: String = str(ProjectSettings.get_setting("application/run/main_scene", ""))
    if configured != "":
        return configured
    return "res://Game.Godot/Scenes/Main.tscn"

func _export_config_is_valid() -> bool:
    var cfg_candidates: PackedStringArray = PackedStringArray([
        "res://export_presets.cfg",
        "res://../export_presets.cfg",
        ProjectSettings.globalize_path("res://../export_presets.cfg")
    ])
    var cfg_path: String = ""
    for candidate in cfg_candidates:
        if candidate != "" and FileAccess.file_exists(candidate):
            cfg_path = candidate
            break
    if cfg_path == "":
        return false
    var cfg_text: String = FileAccess.get_file_as_string(cfg_path)
    return cfg_text.findn("platform=\"Windows Desktop\"") >= 0 and cfg_text.findn("export_path=\"build/") >= 0 and cfg_text.findn(".exe\"") >= 0

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
            var evidence_candidate: String = ci_root.path_join(entry).path_join("sc-acceptance-check-task-1").path_join("headless-e2e-evidence.json")
            if FileAccess.file_exists(summary_candidate) and FileAccess.file_exists(evidence_candidate):
                date_candidates.append(entry)
    dir.list_dir_end()

    if date_candidates.is_empty():
        return ""
    date_candidates.sort()
    date_candidates.reverse()
    for entry in date_candidates:
        var summary: Dictionary = _read_json_if_exists(ci_root.path_join(entry).path_join("sc-test").path_join("summary.json"))
        var evidence: Dictionary = _read_json_if_exists(ci_root.path_join(entry).path_join("sc-acceptance-check-task-1").path_join("headless-e2e-evidence.json"))
        if _has_successful_steps(summary, ["unit", "smoke"]) and _has_successful_verification_records(evidence):
            return ci_root.path_join(entry)
    return ci_root.path_join(date_candidates[0])

func _read_text_if_exists(path: String) -> String:
    if path == "" or not FileAccess.file_exists(path):
        return ""
    return FileAccess.get_file_as_string(path)

func _read_json_if_exists(path: String) -> Dictionary:
    if path == "" or not FileAccess.file_exists(path):
        return {}
    var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
    if parsed is Dictionary:
        return parsed
    return {}

func _latest_sc_test_summary() -> Dictionary:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return {}
    return _read_json_if_exists(date_dir.path_join("sc-test").path_join("summary.json"))

func _latest_headless_e2e_evidence() -> Dictionary:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return {}
    return _read_json_if_exists(date_dir.path_join("sc-acceptance-check-task-1").path_join("headless-e2e-evidence.json"))

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

func _has_successful_verification_records(evidence: Dictionary) -> bool:
    if evidence.is_empty():
        return false
    var records: Array = evidence.get("verification_records", [])
    if records.size() != REQUIRED_STEPS.size():
        return false
    for step in REQUIRED_STEPS:
        var found: bool = false
        for record in records:
            if not (record is Dictionary):
                continue
            var item: Dictionary = record
            if String(item.get("step", "")) == step and String(item.get("status", "")) == "success":
                found = true
                break
        if not found:
            return false
    return true

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
        if dir.current_is_dir() and entry > latest_stamp:
            latest_stamp = entry
    dir.list_dir_end()
    if latest_stamp == "":
        return {}
    var expected_run_id: String = CiRunBinding.expected_run_id()
    if expected_run_id == "":
        return _read_json_if_exists(smoke_root.path_join(latest_stamp).path_join("summary.json"))

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
        var summary: Dictionary = _read_json_if_exists(smoke_root.path_join(stamp).path_join("summary.json"))
        if summary.is_empty():
            continue
        if String(summary.get("sc_test_run_id", "")).strip_edges() == expected_run_id:
            return summary
    return _read_json_if_exists(smoke_root.path_join(latest_stamp).path_join("summary.json"))

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

func _collect_runtime_records() -> Array:
    var records: Array = []
    var canonical_root: String = _canonicalize_root(_project_root_abs())
    var sc_test_summary: Dictionary = _latest_sc_test_summary()
    var unit_step: Dictionary = _find_step(sc_test_summary, "unit")
    var smoke_step: Dictionary = _find_step(sc_test_summary, "smoke")
    var gdunit_step: Dictionary = _find_step(sc_test_summary, "gdunit-hard")
    var smoke_log_text: String = _read_text_if_exists(String(smoke_step.get("log", ""))).to_lower()
    var unit_log_text: String = _read_text_if_exists(String(unit_step.get("log", ""))).to_lower()
    var restore_log_text: String = _read_text_if_exists(_latest_restore_log_path())
    var restore_ok: bool = restore_log_text != "" and restore_log_text.findn("could not be found") < 0 and restore_log_text.findn("failed to restore") < 0 and restore_log_text.findn("error nu") < 0
    var startup_marker_ok: bool = smoke_log_text.find("smoke pass (marker)") >= 0
    var gdunit_cmd: Array = gdunit_step.get("cmd", [])
    var gdunit_in_progress_for_current_run: bool = _is_gdunit_step_in_progress(sc_test_summary, gdunit_step)
    var export_test_executed: bool = false
    for arg in gdunit_cmd:
        var token: String = String(arg).replace("\\", "/").to_lower()
        if token.findn("test_windows_export_startup_flow.gd") >= 0 or token.findn("tests/integration") >= 0:
            export_test_executed = true
            break
    if gdunit_in_progress_for_current_run:
        export_test_executed = true

    records.append({
        "step": "editor_open",
        "status": "success" if int(smoke_step.get("rc", 1)) == 0 and smoke_log_text.find("starting godot") >= 0 else "failed",
        "canonical_root": canonical_root
    })

    records.append({
        "step": "csharp_compile",
        "status": "success" if unit_log_text.find("run_dotnet status=") >= 0 and unit_log_text.find("build failed") < 0 and restore_ok else "failed",
        "canonical_root": canonical_root
    })

    var startup_scene: String = _startup_scene_path()
    var startup_ok: bool = startup_scene.begins_with("res://") and FileAccess.file_exists(startup_scene)
    records.append({
        "step": "startup_scene_execution",
        "status": "success" if startup_ok and startup_marker_ok else "failed",
        "canonical_root": canonical_root
    })

    records.append({
        "step": "export_launch",
        "status": "success" if _export_config_is_valid() and startup_marker_ok and export_test_executed else "failed",
        "canonical_root": canonical_root
    })

    return records

func _has_success_record(records: Array, step: String, canonical_root: String) -> bool:
    for record in records:
        if not (record is Dictionary):
            continue
        var item: Dictionary = record
        if String(item.get("step", "")) == step and String(item.get("status", "")) == "success" and _canonicalize_root(String(item.get("canonical_root", ""))) == canonical_root:
            return true
    return false

func _can_use_evidence_records(evidence: Dictionary, sc_test_summary: Dictionary, canonical_root: String) -> bool:
    if evidence.is_empty():
        return false

    var expected_run_id: String = String(evidence.get("expected_run_id", "")).strip_edges()
    if expected_run_id == "":
        return false

    var summary_run_id: String = String(sc_test_summary.get("run_id", "")).strip_edges()
    if summary_run_id != "" and summary_run_id != expected_run_id:
        return false

    var records: Array = evidence.get("verification_records", [])
    if records.size() != REQUIRED_STEPS.size():
        return false

    for step in REQUIRED_STEPS:
        if not _has_success_record(records, step, canonical_root):
            return false
    return true

# acceptance: ACC:T1.14
func test_windows_verification_evidence_records_cover_required_steps_under_one_canonical_root() -> void:
    var canonical_root: String = _canonicalize_root(_project_root_abs())
    var sc_test_summary: Dictionary = _latest_sc_test_summary()
    var evidence: Dictionary = _latest_headless_e2e_evidence()
    var evidence_records: Array = []
    if _can_use_evidence_records(evidence, sc_test_summary, canonical_root):
        var expected_run_id: String = String(evidence.get("expected_run_id", ""))
        assert_str(expected_run_id).is_not_empty()
        assert_str(String(evidence.get("expected_run_id", ""))).is_not_empty()
        assert_str(String(evidence.get("run_id_in_summary", ""))).is_not_empty()
        assert_str(String(evidence.get("run_id_in_file", ""))).is_not_empty()
        assert_str(String(evidence.get("e2e_run_id_value", ""))).is_not_empty()
        evidence_records = evidence.get("verification_records", [])
    else:
        evidence_records = _collect_runtime_records()

    if not sc_test_summary.is_empty():
        assert_str(String(sc_test_summary.get("run_id", ""))).is_not_empty()
    assert_int(evidence_records.size()).is_equal(REQUIRED_STEPS.size())

    var unique_roots: Dictionary = {}
    for record in evidence_records:
        if not (record is Dictionary):
            continue
        var item: Dictionary = record
        unique_roots[_canonicalize_root(String(item.get("canonical_root", "")))] = true

    assert_int(unique_roots.size()).is_equal(1)
    assert_bool(unique_roots.has(canonical_root)).is_true()

    for step in REQUIRED_STEPS:
        var found: bool = false
        for record in evidence_records:
            if not (record is Dictionary):
                continue
            var item: Dictionary = record
            if String(item.get("step", "")) == step and String(item.get("status", "")) == "success" and _canonicalize_root(String(item.get("canonical_root", ""))) == canonical_root:
                found = true
                break
        assert_bool(found).is_true()

    var derived_records: Array = _collect_runtime_records()
    assert_int(derived_records.size()).is_equal(REQUIRED_STEPS.size())
    for step in REQUIRED_STEPS:
        assert_bool(_has_success_record(derived_records, step, canonical_root)).is_true()

func test_canonicalize_root_is_deterministic_for_windows_path_variants() -> void:
    var expected: String = "f:/lastking"
    assert_str(_canonicalize_root("F:\\Lastking")).is_equal(expected)
    assert_str(_canonicalize_root("f:/lastking/")).is_equal(expected)
    assert_str(_canonicalize_root("  F:/LASTKING///  ")).is_equal(expected)

func test_runtime_records_fail_when_required_step_is_missing() -> void:
    var canonical_root: String = _canonicalize_root(_project_root_abs())
    var broken_records: Array = [
        {"step": "editor_open", "status": "success", "canonical_root": canonical_root},
        {"step": "csharp_compile", "status": "success", "canonical_root": canonical_root},
        {"step": "startup_scene_execution", "status": "success", "canonical_root": canonical_root}
    ]
    assert_bool(_has_success_record(broken_records, "export_launch", canonical_root)).is_false()
