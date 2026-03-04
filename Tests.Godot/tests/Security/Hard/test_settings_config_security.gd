extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func test_settings_configfile_path_security() -> void:
    var safe: Node = preload("res://Game.Godot/Adapters/Security/SafeConfig.gd").new()
    add_child(auto_free(safe))
    var sections: Dictionary = { "s": { "k": "v" } }
    # allow: user://
    var ok: int = safe.save_user("user://ok_settings.cfg", sections)
    assert_int(ok).is_equal(0)

    # deny: absolute path
    var absolute_save_result: int = safe.save_user("C:/temp/bad_settings.cfg", sections)
    assert_bool(absolute_save_result != 0).is_true()

    # deny: traversal
    var traversal_save_result: int = safe.save_user("user://../bad_settings.cfg", sections)
    assert_bool(traversal_save_result != 0).is_true()
