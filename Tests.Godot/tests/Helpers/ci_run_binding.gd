extends RefCounted

const CI_DATE_PATTERN_LENGTH: int = 10

static func expected_run_id() -> String:
    return String(OS.get_environment("SC_TEST_RUN_ID")).strip_edges()

static func _is_date_dir_name(name: String) -> bool:
    return name.length() == CI_DATE_PATTERN_LENGTH and name[4] == "-" and name[7] == "-"

static func _sorted_date_entries(root_path: String) -> Array[String]:
    var out: Array[String] = []
    var dir: DirAccess = DirAccess.open(root_path)
    if dir == null:
        return out

    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir() and _is_date_dir_name(entry):
            out.append(entry)
    dir.list_dir_end()
    out.sort()
    out.reverse()
    return out

static func _read_text_if_exists(path: String) -> String:
    if path == "" or not FileAccess.file_exists(path):
        return ""
    return FileAccess.get_file_as_string(path).strip_edges()

static func find_ci_date_dir_by_run_id(ci_root: String) -> String:
    var expected: String = expected_run_id()
    if expected == "":
        return ""

    for entry in _sorted_date_entries(ci_root):
        var run_id_path: String = ci_root.path_join(entry).path_join("sc-test").path_join("run_id.txt")
        if _read_text_if_exists(run_id_path) == expected:
            return ci_root.path_join(entry)
    return ""

static func find_unit_date_dir_by_run_id(unit_root: String) -> String:
    var expected: String = expected_run_id()
    if expected == "":
        return ""

    for entry in _sorted_date_entries(unit_root):
        var run_id_path: String = unit_root.path_join(entry).path_join("run_id.txt")
        if _read_text_if_exists(run_id_path) == expected:
            return unit_root.path_join(entry)
    return ""
