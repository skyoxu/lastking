extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _new_db(node_name: String) -> Node:
    var db: Node = null
    if ClassDB.class_exists("SqliteDataStore"):
        db = ClassDB.instantiate("SqliteDataStore")
    else:
        var s: Script = load("res://Game.Godot/Adapters/SqliteDataStore.cs")
        db = Node.new()
        db.set_script(s)
    db.name = node_name
    get_tree().get_root().add_child(auto_free(db))
    await get_tree().process_frame
    if not db.has_method("TryOpen"):
        await get_tree().process_frame
    return db


func _today_dir() -> String:
    var d = Time.get_datetime_dict_from_system()
    return "%04d-%02d-%02d" % [d.year, d.month, d.day]


func _audit_path() -> String:
    var audit_root: String = str(OS.get_environment("AUDIT_LOG_ROOT")).strip_edges()
    if audit_root != "":
        return audit_root.path_join("security-audit.jsonl")
    return ProjectSettings.globalize_path("res://logs/ci/%s/security-audit.jsonl" % _today_dir())


func _remove_audit_file() -> void:
    var p: String = _audit_path()
    if FileAccess.file_exists(p):
        var absolute_path: String = p
        if p.begins_with("res://") or p.begins_with("user://"):
            absolute_path = ProjectSettings.globalize_path(p)
        DirAccess.remove_absolute(absolute_path)


func test_open_denied_writes_audit_log() -> void:
    _remove_audit_file()

    var db: Node = await _new_db("DbAuditOpenFail")
    var ok: bool = db.TryOpen("C:/temp/security_open_denied.db")
    assert_bool(ok).is_false()

    await get_tree().process_frame

    var p: String = _audit_path()
    assert_bool(FileAccess.file_exists(p)).is_true()

    var txt: String = FileAccess.get_file_as_string(p)
    assert_str(txt).is_not_empty()

    var lines: Array = txt.split("\n", false)
    var found := false
    for i in range(lines.size()):
        var raw: String = lines[i].strip_edges()
        if raw == "":
            continue
        var parsed: Variant = JSON.parse_string(raw)
        if parsed == null:
            continue
        var action: String = str(parsed.get("action", "")).to_lower()
        if action == "db.open.fail":
            found = true
            break

    assert_bool(found).is_true()
