extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CiRunBinding = preload("res://tests/Helpers/ci_run_binding.gd")
const CI_DATE_PATTERN_LENGTH: int = 10

func _has_file_with_extension_in_dir(dir_path: String, extension: String) -> bool:
    var dir: DirAccess = DirAccess.open(dir_path)
    if dir == null:
        return false

    dir.list_dir_begin()
    while true:
        var entry: String = dir.get_next()
        if entry == "":
            break
        if dir.current_is_dir():
            continue
        if entry.to_lower().ends_with(extension):
            dir.list_dir_end()
            return true
    dir.list_dir_end()
    return false

func _has_root_file_with_extension(extension: String) -> bool:
    return _has_file_with_extension_in_dir("res://", extension) or _has_file_with_extension_in_dir(ProjectSettings.globalize_path("res://../"), extension)

func _project_config_text() -> String:
    if not FileAccess.file_exists("res://project.godot"):
        return ""
    return FileAccess.get_file_as_string("res://project.godot")

func _is_csharp_creation_metadata(meta: Dictionary) -> bool:
    return String(meta.get("creation_mode", "")).to_lower() == "csharp" and bool(meta.get("language_conversion_required", true)) == false

func _detect_creation_metadata_from_project() -> Dictionary:
    var has_dotnet_section: bool = _project_config_text().find("[dotnet]") >= 0
    var has_project_files: bool = _has_root_file_with_extension(".csproj") and _has_root_file_with_extension(".sln")
    var has_assembly_name: bool = ProjectSettings.has_setting("dotnet/project/assembly_name") and str(ProjectSettings.get_setting("dotnet/project/assembly_name", "")).strip_edges() != ""
    var creation_mode: String = "csharp" if has_dotnet_section and has_project_files and has_assembly_name else "unknown"
    return {
        "creation_mode": creation_mode,
        "language_conversion_required": not (has_dotnet_section and has_project_files and has_assembly_name)
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
            var summary_candidate: String = ci_root.path_join(entry).path_join("sc-acceptance-check-task-1").path_join("summary.json")
            if not FileAccess.file_exists(summary_candidate):
                continue
            if entry > latest_date:
                latest_date = entry
    dir.list_dir_end()
    if latest_date == "":
        return ""
    return ci_root.path_join(latest_date)

func _latest_headless_e2e_evidence() -> Dictionary:
    var date_dir: String = _latest_ci_date_dir()
    if date_dir == "":
        return {}
    var path: String = date_dir.path_join("sc-acceptance-check-task-1").path_join("headless-e2e-evidence.json")
    if not FileAccess.file_exists(path):
        return {}
    var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
    if parsed is Dictionary:
        return parsed
    return {}

# ACC:T1.5
# The project should be created in C# mode and open directly without language conversion.
func test_project_uses_csharp_mode_markers_from_bootstrap() -> void:
    assert_bool(ProjectSettings.has_setting("dotnet/project/assembly_name")).is_true()
    var assembly_name: String = str(ProjectSettings.get_setting("dotnet/project/assembly_name", ""))
    assert_str(assembly_name).is_not_empty()
    var project_config: String = _project_config_text()
    assert_bool(project_config.find("[dotnet]") >= 0).is_true()
    assert_bool(_has_root_file_with_extension(".csproj")).is_true()
    assert_bool(_has_root_file_with_extension(".sln")).is_true()

# ACC:T1.18
# Smoke check for C# bootstrap surface used by editor-side compile flow.
func test_csharp_script_type_is_available_for_editor_compile_smoke() -> void:
    var metadata: Dictionary = _detect_creation_metadata_from_project()
    assert_bool(_is_csharp_creation_metadata(metadata)).is_true()
    var evidence: Dictionary = _latest_headless_e2e_evidence()
    if evidence.is_empty():
        assert_bool(ClassDB.class_exists("CSharpScript")).is_true()
        return
    assert_bool(evidence.has("creation_mode_at_bootstrap")).is_true()
    assert_bool(evidence.has("conversion_required")).is_true()
    var creation_mode: String = String(evidence.get("creation_mode_at_bootstrap", "")).strip_edges().to_lower()
    assert(creation_mode == "csharp", "creation_mode_at_bootstrap must be csharp")
    assert_bool(bool(evidence.get("conversion_required", true))).is_false()
    assert_bool(ClassDB.class_exists("CSharpScript")).is_true()

func test_creation_metadata_rejects_non_csharp_mode() -> void:
    var bad_meta: Dictionary = {
        "creation_mode": "gdscript",
        "language_conversion_required": true
    }
    assert_bool(_is_csharp_creation_metadata(bad_meta)).is_false()
