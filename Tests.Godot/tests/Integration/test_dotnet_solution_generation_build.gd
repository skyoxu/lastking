extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DOTNET_COMMAND := "dotnet"
const MAX_SCAN_DEPTH := 5
const SKIP_DIRS := [".git", ".godot", ".taskmaster", ".vs", "bin", "obj"]

var _cached_solution_build_smoke: Dictionary = {}

func _canonical_root_abs() -> String:
    return ProjectSettings.globalize_path("res://../").simplify_path()

func _is_under_canonical_root(path_value: String) -> bool:
    var normalized_path := path_value.simplify_path().replace("\\", "/")
    var normalized_root := _canonical_root_abs().replace("\\", "/")
    return normalized_path.find(normalized_root) == 0

func _candidate_roots() -> Array:
    var roots: Array = []
    var primary := ProjectSettings.globalize_path("res://").simplify_path()
    roots.append(primary)

    var parent := ProjectSettings.globalize_path("res://../").simplify_path()
    if parent != primary and FileAccess.file_exists(parent.path_join("AGENTS.md")):
        roots.append(parent)

    return roots

func _collect_dotnet_artifacts_from(start_dir: String, depth: int, sln_files: Array, csproj_files: Array) -> void:
    if depth > MAX_SCAN_DEPTH:
        return

    var dir := DirAccess.open(start_dir)
    if dir == null:
        return

    dir.list_dir_begin()
    while true:
        var name := dir.get_next()
        if name == "":
            break
        if name == "." or name == "..":
            continue

        var full_path := start_dir.path_join(name)
        if dir.current_is_dir():
            if SKIP_DIRS.has(name):
                continue
            _collect_dotnet_artifacts_from(full_path, depth + 1, sln_files, csproj_files)
            continue

        var lowered := name.to_lower()
        if lowered.ends_with(".sln"):
            sln_files.append(full_path)
        elif lowered.ends_with(".csproj"):
            csproj_files.append(full_path)
    dir.list_dir_end()

func _unique_sorted_strings(values: Array) -> Array:
    var seen := {}
    var result: Array = []
    for value in values:
        if not seen.has(value):
            seen[value] = true
            result.append(value)
    result.sort()
    return result

func _discover_dotnet_artifacts() -> Dictionary:
    var sln_files: Array = []
    var csproj_files: Array = []

    for root in _candidate_roots():
        _collect_dotnet_artifacts_from(str(root), 0, sln_files, csproj_files)

    return {
        "sln_files": _unique_sorted_strings(sln_files),
        "csproj_files": _unique_sorted_strings(csproj_files)
    }

func _select_solution_file(sln_files: Array) -> String:
    if sln_files.is_empty():
        return ""

    var selected := str(sln_files[0])
    var selected_depth := selected.count("/")
    for candidate_value in sln_files:
        var candidate := str(candidate_value)
        var candidate_depth := candidate.count("/")
        if candidate_depth < selected_depth:
            selected = candidate
            selected_depth = candidate_depth
    return selected

func _join_lines(lines: Array) -> String:
    var text := ""
    for line in lines:
        text += str(line)
        text += "\n"
    return text

func _run_dotnet(arguments: PackedStringArray) -> Dictionary:
    var output: Array = []
    var exit_code := OS.execute(DOTNET_COMMAND, arguments, output, true, false)
    return {
        "exit_code": exit_code,
        "output": _join_lines(output)
    }

func _run_solution_build_smoke(solution_path: String) -> Dictionary:
    var cached_solution: String = String(_cached_solution_build_smoke.get("solution", ""))
    if cached_solution == solution_path and not _cached_solution_build_smoke.is_empty():
        return _cached_solution_build_smoke
    var result := _run_dotnet(PackedStringArray(["build", solution_path, "-nologo", "-v", "minimal"]))
    _cached_solution_build_smoke = {
        "solution": solution_path,
        "exit_code": int(result.get("exit_code", 1)),
        "output": String(result.get("output", ""))
    }
    return _cached_solution_build_smoke

func _contains_restore_failure_markers(output: String) -> bool:
    var lowered := output.to_lower()
    var markers := [
        "error nu1101",
        "error nu1102",
        "error msb4236",
        "failed to restore",
        "it was not possible to find any installed .net sdk",
        "a compatible installed .net sdk for global.json version"
    ]
    for marker in markers:
        if lowered.find(marker) >= 0:
            return true
    return false

# ACC:T1.12
func test_generated_solution_and_projects_are_discoverable() -> void:
    var artifacts := _discover_dotnet_artifacts()
    var sln_files: Array = artifacts["sln_files"]
    var csproj_files: Array = artifacts["csproj_files"]

    assert(sln_files.size() > 0, "Expected at least one .sln file to exist.")
    assert(csproj_files.size() > 0, "Expected at least one .csproj file to exist.")

    var first_solution := str(sln_files[0])
    var first_project := str(csproj_files[0])
    assert(FileAccess.file_exists(first_solution), "Discovered .sln path must exist.")
    assert(FileAccess.file_exists(first_project), "Discovered .csproj path must exist.")
    assert(_is_under_canonical_root(first_solution), "Discovered .sln must be under canonical root.")
    assert(_is_under_canonical_root(first_project), "Discovered .csproj must be under canonical root.")
    var selected_solution := _select_solution_file(sln_files)
    assert(selected_solution != "", "A valid .sln file path is required for build smoke test.")
    var build_result := _run_solution_build_smoke(selected_solution)
    assert(int(build_result.get("exit_code", 1)) == 0, "dotnet build failed.\n%s" % str(build_result.get("output", "")))
    assert(not _contains_restore_failure_markers(str(build_result.get("output", ""))), "Build output contains restore or SDK failure markers.")

# ACC:T1.19
func test_dotnet_sdk_resolution_and_solution_build_smoke() -> void:
    var artifacts := _discover_dotnet_artifacts()
    var sln_files: Array = artifacts["sln_files"]
    assert(sln_files.size() > 0, "Cannot run dotnet build smoke test without a .sln file.")

    var sdk_info := _run_dotnet(PackedStringArray(["--info"]))
    assert(int(sdk_info["exit_code"]) == 0, "dotnet --info failed.\n%s" % str(sdk_info["output"]))

    var selected_solution := _select_solution_file(sln_files)
    assert(selected_solution != "", "A valid .sln file path is required for build smoke test.")
    assert(_is_under_canonical_root(selected_solution), "Selected solution must belong to canonical root.")

    var build_result := _run_solution_build_smoke(selected_solution)
    assert(int(build_result.get("exit_code", 1)) == 0, "dotnet build failed.\n%s" % str(build_result.get("output", "")))
    assert(not _contains_restore_failure_markers(str(build_result.get("output", ""))), "Build output contains restore or SDK failure markers.")

func test_solution_build_fails_for_invalid_solution_path() -> void:
    var canonical_root := _canonical_root_abs()
    var fake_solution := canonical_root.path_join("does-not-exist.sln")
    var build_result := _run_dotnet(PackedStringArray(["build", fake_solution, "-nologo", "-v", "minimal"]))
    assert(int(build_result.get("exit_code", 0)) != 0, "Invalid solution path must fail build.")

func test_restore_failure_marker_detection_is_deterministic() -> void:
    var healthy_output := "Build succeeded. 0 Warning(s) 0 Error(s)."
    var failing_output := "error NU1101: Unable to find package Example.Package."

    assert(not _contains_restore_failure_markers(healthy_output), "Healthy output should not be marked as a restore failure.")
    assert(_contains_restore_failure_markers(failing_output), "Known restore failure output should be detected.")
