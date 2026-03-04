extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REQUIRED_DIRS: Dictionary = {
    "core_contracts": ["res://../Game.Core/Contracts", "res://Game.Core/Contracts"],
    "godot_scripts": ["res://Game.Godot/Scripts", "res://../Game.Godot/Scripts"],
    "godot_scenes": ["res://Game.Godot/Scenes", "res://../Game.Godot/Scenes"],
    "godot_assets": ["res://Game.Godot/Assets", "res://../Game.Godot/Assets"]
}

func _resolve_existing_dir(candidates: Array) -> String:
    for candidate in candidates:
        var path: String = str(candidate)
        if DirAccess.open(path) != null:
            return path
        var absolute_path: String = ProjectSettings.globalize_path(path)
        if absolute_path != path and DirAccess.open(absolute_path) != null:
            return absolute_path
    return ""

func _list_files_recursive(root: String, limit: int = 256) -> PackedStringArray:
    var files: PackedStringArray = PackedStringArray()
    if root.is_empty():
        return files

    var pending: Array[String] = [root]
    while pending.size() > 0 and files.size() < limit:
        var current: String = str(pending.pop_back())
        var dir: DirAccess = DirAccess.open(current)
        if dir == null:
            continue

        dir.list_dir_begin()
        var entry: String = dir.get_next()
        while entry != "":
            if entry != "." and entry != "..":
                var full_path: String = current.path_join(entry)
                if dir.current_is_dir():
                    pending.append(full_path)
                else:
                    files.append(full_path)
                    if files.size() >= limit:
                        break
            entry = dir.get_next()
        dir.list_dir_end()

    return files

func _read_project_config_text() -> String:
    var candidates: PackedStringArray = PackedStringArray(["res://project.godot", "res://../project.godot"])
    var merged: String = ""
    for candidate in candidates:
        if FileAccess.file_exists(candidate):
            merged += "\n" + FileAccess.get_file_as_string(candidate)
    if merged == "":
        return ""
    var extra_sources: PackedStringArray = PackedStringArray(["res://../export_presets.cfg", "res://../Game.Godot/Scenes/Main.tscn"])
    for source in extra_sources:
        if FileAccess.file_exists(source):
            merged += "\n" + FileAccess.get_file_as_string(source)
    return merged

func _contains_project_reference(files: PackedStringArray, config_text: String) -> bool:
    if config_text.is_empty():
        return false
    var canonical_root: String = ProjectSettings.globalize_path("res://../").simplify_path().replace("\\", "/")

    for file_path in files:
        var normalized_file_path: String = String(file_path).replace("\\", "/")
        if normalized_file_path.find(canonical_root) == 0:
            var relative_from_root: String = normalized_file_path.substr(canonical_root.length()).trim_prefix("/")
            normalized_file_path = "res://" + relative_from_root
        if normalized_file_path.begins_with("res://../"):
            normalized_file_path = "res://" + normalized_file_path.substr("res://../".length())
        if config_text.find(normalized_file_path) >= 0:
            return true
    return false

func _directory_has_project_reference(key: String, files: PackedStringArray, config_text: String) -> bool:
    if key == "core_contracts":
        var csproj_candidates: PackedStringArray = PackedStringArray([
            "res://../Game.Core/Game.Core.csproj",
            "res://Game.Core/Game.Core.csproj"
        ])
        for candidate in csproj_candidates:
            if FileAccess.file_exists(candidate):
                return true
        return false
    return _contains_project_reference(files, config_text)

func _join_items(items: PackedStringArray) -> String:
    var values: Array[String] = []
    for item in items:
        values.append(item)
    return ", ".join(values)

# acceptance: ACC:T1.7
func test_project_structure_has_baseline_directories_with_referenced_files() -> void:
    assert_bool(FileAccess.file_exists("res://project.godot")).is_true()

    var config_text: String = _read_project_config_text()
    var missing_dirs: PackedStringArray = PackedStringArray()
    var non_referenced_dirs: PackedStringArray = PackedStringArray()

    for key in REQUIRED_DIRS.keys():
        var dir_path: String = _resolve_existing_dir(REQUIRED_DIRS[key])
        if dir_path.is_empty():
            missing_dirs.append(str(key))
            continue

        var files: PackedStringArray = _list_files_recursive(dir_path)
        assert_bool(files.size() > 0).is_true()

        if not _directory_has_project_reference(String(key), files, config_text):
            non_referenced_dirs.append(str(key))

    assert_int(missing_dirs.size()).is_equal(0)
    assert_int(non_referenced_dirs.size()).is_equal(0)

func test_project_name_setting_is_non_empty() -> void:
    assert_bool(ProjectSettings.has_setting("application/config/name")).is_true()
    var project_name: String = str(ProjectSettings.get_setting("application/config/name")).strip_edges()
    assert_bool(project_name.length() > 0).is_true()
