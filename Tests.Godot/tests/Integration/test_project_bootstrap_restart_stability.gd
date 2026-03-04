extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const MAIN_SCENE_KEY: String = "application/run/main_scene"
const PROJECT_NAME_KEY: String = "application/config/name"
const CI_DATE_PATTERN_LENGTH: int = 10
const RESTART_PROBE_TIMEOUT_SEC: int = 5

func _startup_fingerprint() -> Dictionary:
    var main_scene_path: String = str(ProjectSettings.get_setting(MAIN_SCENE_KEY, ""))
    if main_scene_path == "":
        main_scene_path = "res://Game.Godot/Scenes/Main.tscn"
    var project_name: String = str(ProjectSettings.get_setting(PROJECT_NAME_KEY, ""))
    var has_main_scene_file: bool = main_scene_path.begins_with("res://") and FileAccess.file_exists(main_scene_path)

    var root_name: String = ""
    var root_type: String = ""
    if has_main_scene_file:
        var packed_scene: Resource = load(main_scene_path)
        if packed_scene is PackedScene:
            var scene_instance: Node = (packed_scene as PackedScene).instantiate()
            if scene_instance != null:
                root_name = str(scene_instance.name)
                root_type = scene_instance.get_class()
                scene_instance.queue_free()

    return {
        "project_name": project_name,
        "main_scene_path": main_scene_path,
        "has_main_scene_file": has_main_scene_file,
        "root_name": root_name,
        "root_type": root_type
    }

func _canonical_boot_key(fingerprint: Dictionary) -> String:
    return "%s|%s|%s" % [
        str(fingerprint.get("project_name", "")),
        str(fingerprint.get("main_scene_path", "")),
        str(fingerprint.get("root_type", ""))
    ]

func _join_lines(lines: Array) -> String:
    var text: String = ""
    for line in lines:
        text += str(line)
        text += "\n"
    return text

func _startup_scene_sha256(scene_path: String) -> String:
    if scene_path == "" or not FileAccess.file_exists(scene_path):
        return ""
    var absolute_scene_path: String = ProjectSettings.globalize_path(scene_path)
    return FileAccess.get_sha256(absolute_scene_path)

func _startup_scene_mtime(scene_path: String) -> int:
    if scene_path == "" or not FileAccess.file_exists(scene_path):
        return -1
    var absolute_scene_path: String = ProjectSettings.globalize_path(scene_path)
    return int(FileAccess.get_modified_time(absolute_scene_path))

func _run_external_headless_startup_probe(scene_path: String) -> Dictionary:
    var editor_bin: String = OS.get_executable_path()
    if editor_bin == "" or not FileAccess.file_exists(editor_bin):
        return {"rc": 1, "output": "missing_editor_binary"}
    var project_root: String = ProjectSettings.globalize_path("res://../").simplify_path()
    var args: PackedStringArray = PackedStringArray([
        "--headless",
        "--path",
        project_root,
        "--scene",
        scene_path,
        "--quit-after",
        str(RESTART_PROBE_TIMEOUT_SEC)
    ])
    var output: Array = []
    var rc: int = OS.execute(editor_bin, args, output, true, false)
    return {
        "rc": rc,
        "output": _join_lines(output),
        "command": "%s --headless --path %s --scene %s --quit-after %s" % [editor_bin, project_root, scene_path, str(RESTART_PROBE_TIMEOUT_SEC)],
        "headless_arg_present": args.has("--headless"),
        "path_arg_present": args.has("--path"),
        "scene_arg_present": args.has("--scene")
    }

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

# acceptance: ACC:T1.3
func test_project_bootstrap_fingerprint_is_stable_across_two_external_restart_probes() -> void:
    var first: Dictionary = _startup_fingerprint()
    var second: Dictionary = _startup_fingerprint()

    assert_that(first.get("project_name", "") != "").is_equal(true)
    assert_that(first.get("main_scene_path", "") != "").is_equal(true)
    assert_that(first.get("has_main_scene_file", false)).is_equal(true)

    var first_key: String = _canonical_boot_key(first)
    var second_key: String = _canonical_boot_key(second)
    assert_that(first_key == second_key).is_equal(true)
    assert_that(first == second).is_equal(true)
    var startup_scene_path: String = String(first.get("main_scene_path", ""))
    assert_str(startup_scene_path).is_not_empty()
    var first_probe: Dictionary = _run_external_headless_startup_probe(startup_scene_path)
    var second_probe: Dictionary = _run_external_headless_startup_probe(startup_scene_path)
    assert_int(int(first_probe.get("rc", 1))).is_equal(0)
    assert_int(int(second_probe.get("rc", 1))).is_equal(0)
    assert_bool(bool(first_probe.get("headless_arg_present", false))).is_true()
    assert_bool(bool(first_probe.get("path_arg_present", false))).is_true()
    assert_bool(bool(first_probe.get("scene_arg_present", false))).is_true()
    assert_bool(bool(second_probe.get("headless_arg_present", false))).is_true()
    assert_bool(bool(second_probe.get("path_arg_present", false))).is_true()
    assert_bool(bool(second_probe.get("scene_arg_present", false))).is_true()
    assert_bool(String(first_probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()
    assert_bool(String(second_probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()

    var smoke_summaries: Array = _latest_smoke_summaries(2)
    var expected_run_id: String = CiRunBinding.expected_run_id()
    var min_required: int = 1 if expected_run_id != "" else 2
    assert_int(smoke_summaries.size()).is_greater_equal(min_required)
    if smoke_summaries.is_empty():
        return
    var latest: Dictionary = smoke_summaries[0]
    var latest_run_id: String = String(latest.get("runId", ""))
    assert_str(latest_run_id).is_not_empty()
    if expected_run_id != "":
        assert_str(String(latest.get("sc_test_run_id", ""))).is_equal(expected_run_id)
    if smoke_summaries.size() < 2:
        return
    var previous: Dictionary = smoke_summaries[1]
    var previous_run_id: String = String(previous.get("runId", ""))
    assert_str(previous_run_id).is_not_empty()
    assert_bool(latest_run_id != previous_run_id).is_true()
    assert_str(String(latest.get("scene", ""))).is_equal(String(previous.get("scene", "")))
    var latest_markers: Dictionary = latest.get("markers", {})
    var previous_markers: Dictionary = previous.get("markers", {})
    assert_bool(bool(latest_markers.get("template_smoke_ready", false))).is_true()
    assert_bool(bool(previous_markers.get("template_smoke_ready", false))).is_true()

# acceptance: ACC:T1.26
# acceptance: ACC:T1.23
func test_external_restart_probe_does_not_rewrite_startup_scene_file() -> void:
    var fingerprint: Dictionary = _startup_fingerprint()
    var startup_scene_path: String = String(fingerprint.get("main_scene_path", ""))
    assert_str(startup_scene_path).is_not_empty()
    assert_bool(FileAccess.file_exists(startup_scene_path)).is_true()
    var hash_before: String = _startup_scene_sha256(startup_scene_path)
    var mtime_before: int = _startup_scene_mtime(startup_scene_path)
    assert_str(hash_before).is_not_empty()
    assert_int(mtime_before).is_greater_equal(0)
    var probe: Dictionary = _run_external_headless_startup_probe(startup_scene_path)
    assert_int(int(probe.get("rc", 1))).is_equal(0)
    assert_bool(String(probe.get("output", "")).findn("[TEMPLATE_SMOKE_READY]") >= 0).is_true()
    var hash_after: String = _startup_scene_sha256(startup_scene_path)
    var mtime_after: int = _startup_scene_mtime(startup_scene_path)
    assert_str(hash_after).is_equal(hash_before)
    assert_int(mtime_after).is_equal(mtime_before)
