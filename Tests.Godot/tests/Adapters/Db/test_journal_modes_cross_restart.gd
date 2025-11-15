extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _new_db(name: String) -> Node:
    var db = null
    if ClassDB.class_exists("SqliteDataStore"):
        db = ClassDB.instantiate("SqliteDataStore")
    else:
        var s = load("res://Game.Godot/Adapters/SqliteDataStore.cs")
        db = s.new()
    db.name = name
    get_tree().get_root().add_child(auto_free(db))
    await get_tree().process_frame
    if not db.has_method("TryOpen"):
        await get_tree().process_frame
    return db

func _force_managed() -> Node:
    var helper = preload("res://Game.Godot/Adapters/Db/DbTestHelper.cs").new()
    add_child(auto_free(helper))
    helper.ForceManaged()
    return helper

func _abs(p: String) -> String:
    return ProjectSettings.globalize_path(p)

func test_wal_creates_wal_sidecar_and_delete_mode_not() -> void:
    # WAL
    var helper = _force_managed()
    helper.SetEnv("GD_DB_JOURNAL", "WAL")
    var wal_path = "user://utdb_%s/journal_wal.db" % Time.get_unix_time_from_system()
    var db1 = await _new_db("SqlDb")
    assert_bool(db1.TryOpen(wal_path)).is_true()
    db1.Execute("CREATE TABLE IF NOT EXISTS t(a INTEGER);")
    db1.Execute("INSERT INTO t(a) VALUES(1);")
    await get_tree().process_frame
    assert_bool(FileAccess.file_exists(_abs(wal_path) + "-wal")).is_true()
    db1.Close()

    # DELETE (no -wal expected)
    helper.SetEnv("GD_DB_JOURNAL", "DELETE")
    var del_path = "user://utdb_%s/journal_del.db" % Time.get_unix_time_from_system()
    var db2 = await _new_db("SqlDb2")
    assert_bool(db2.TryOpen(del_path)).is_true()
    db2.Execute("CREATE TABLE IF NOT EXISTS t(a INTEGER);")
    db2.Execute("INSERT INTO t(a) VALUES(1);")
    await get_tree().process_frame
    assert_bool(FileAccess.file_exists(_abs(del_path) + "-wal")).is_false()
    db2.Close()
