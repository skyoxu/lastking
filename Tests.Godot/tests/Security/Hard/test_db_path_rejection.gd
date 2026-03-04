extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _new_db() -> Node:
    var db: Node = null
    if ClassDB.class_exists("SqliteDataStore"):
        db = ClassDB.instantiate("SqliteDataStore")
    else:
        var s: Script = load("res://Game.Godot/Adapters/SqliteDataStore.cs")
        db = Node.new()
        db.set_script(s)
    db.name = "DbPathSecurity"
    get_tree().get_root().add_child(auto_free(db))
    await get_tree().process_frame
    if not db.has_method("TryOpen"):
        await get_tree().process_frame
    return db


func test_sqlite_path_security_rejects_absolute_and_traversal() -> void:
    var db: Node = await _new_db()

    var ok_user: bool = db.TryOpen("user://security_path_ok.db")
    assert_bool(ok_user).is_true()

    var absolute_open_result: bool = db.TryOpen("C:/temp/security_path_bad.db")
    assert_bool(absolute_open_result).is_false()

    var trav: bool = db.TryOpen("user://../security_path_bad.db")
    assert_bool(trav).is_false()
