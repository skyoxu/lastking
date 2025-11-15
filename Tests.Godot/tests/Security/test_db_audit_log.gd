extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _new_db(name: String) -> Node:
    var db = null
    if ClassDB.class_exists("SqliteDataStore"):
        db = ClassDB.instantiate("SqliteDataStore")
    else:
        var s = load("res://Game.Godot/Adapters/SqliteDataStore.cs")
        db = Node.new()
        db.set_script(s)
    db.name = name
    get_tree().get_root().add_child(auto_free(db))
    await get_tree().process_frame
    if not db.has_method("TryOpen"):
        await get_tree().process_frame
    return db

func _today_dir() -> String:
    var d = Time.get_datetime_dict_from_system()
    var y = d.year
    var m = d.month
    var day = d.day
    return "%04d-%02d-%02d" % [y, m, day]

func test_db_open_denied_writes_audit_log() -> void:
    var db = await _new_db("SqlDb")
    # trigger security failure: only user:// allowed
    var ok = db.TryOpen("res://should_fail.db")
    assert_bool(ok).is_false()
    await get_tree().process_frame
    # default audit path is relative project: logs/ci/<YYYY-MM-DD>/security-audit.jsonl
    var log_path = "res://logs/ci/%s/security-audit.jsonl" % _today_dir()
    # ensure folder scan flush
    await get_tree().process_frame
    assert_bool(FileAccess.file_exists(log_path)).is_true()
    var content = FileAccess.get_file_as_string(log_path)
    assert_str(content).contains('"action":"db.open.fail"')

