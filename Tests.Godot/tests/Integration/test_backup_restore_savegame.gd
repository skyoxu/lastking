extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const SAVE_BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"

func _new_db(name: String) -> Node:
    var db = null
    if ClassDB.class_exists("SqliteDataStore"):
        db = ClassDB.instantiate("SqliteDataStore")
    else:
        var s = load("res://Game.Godot/Adapters/SqliteDataStore.cs")
        if s == null or not s.has_method("new"):
            push_warning("SKIP: CSharpScript.new() unavailable, skip DB new")
            return null
        db = s.new()
    db.name = name
    get_tree().get_root().add_child(auto_free(db))
    await get_tree().process_frame
    if not db.has_method("TryOpen"):
        await get_tree().process_frame
    return db

static func _mkdirs_user(abs_path: String) -> bool:
    var base := abs_path.get_base_dir()
    if not base.begins_with("user://"):
        return false
    var rel := base.substr("user://".length())
    var d := DirAccess.open("user://")
    if d == null:
        return false
    return d.make_dir_recursive(rel) == OK

static func _copy_file(src: String, dst: String) -> bool:
    if not _mkdirs_user(dst):
        return false
    var r := FileAccess.open(src, FileAccess.READ)
    if r == null:
        return false
    var data := r.get_buffer(r.get_length())
    var w := FileAccess.open(dst, FileAccess.WRITE)
    if w == null:
        return false
    w.store_buffer(data)
    var wal := src + "-wal"
    var shm := src + "-shm"
    if FileAccess.file_exists(wal):
        var rw := FileAccess.open(wal, FileAccess.READ)
        if rw != null:
            var dw := FileAccess.open(dst + "-wal", FileAccess.WRITE)
            if dw != null:
                dw.store_buffer(rw.get_buffer(rw.get_length()))
    if FileAccess.file_exists(shm):
        var rs := FileAccess.open(shm, FileAccess.READ)
        if rs != null:
            var ds := FileAccess.open(dst + "-shm", FileAccess.WRITE)
            if ds != null:
                ds.store_buffer(rs.get_buffer(rs.get_length()))
    return true

func _new_cloud_bridge(storage_key: String, backend: String, logged_in: bool, account_id: String) -> Node:
    var script = load(SAVE_BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", storage_key, false, 20250425, 10, 10, 15)
    bridge.call("ResetCloudRuntime", backend, logged_in, account_id)
    return bridge

func _parse_snapshot(snapshot_text: String) -> Dictionary:
    var parsed = JSON.parse_string(snapshot_text)
    if parsed is Dictionary:
        return parsed as Dictionary
    return {}

func _assert_full_payload_match(uploaded: Dictionary, restored: Dictionary) -> void:
    assert_that(restored.size()).is_equal(uploaded.size())
    for key in uploaded.keys():
        assert_bool(restored.has(key)).is_true()
        assert_that(restored.get(key)).is_equal(uploaded.get(key))

func test_backup_restore_savegame() -> void:
    var path = "user://utdb_%s/sg_bak.db" % Time.get_unix_time_from_system()
    var db = await _new_db("SqlDb")
    if db == null:
        push_warning("SKIP: missing C# instantiate, skip test")
        return
    # Force managed provider
    var helper_sc = load("res://Game.Godot/Adapters/Db/DbTestHelper.cs")
    if helper_sc == null or not helper_sc.has_method("new"):
        push_warning("SKIP: helper C# unavailable, skip")
        return
    var helper = helper_sc.new()
    add_child(auto_free(helper))
    helper.ForceManaged()
    var tries := 20
    while not db.has_method("TryOpen") and tries > 0:
        await get_tree().process_frame
        tries -= 1
    assert_bool(db.has_method("TryOpen")).is_true()
    var ok = db.TryOpen(path)
    assert_bool(ok).is_true()
    helper.ExecSql("PRAGMA journal_mode=DELETE;")
    # Ensure schema exists and clean
    helper.CreateSchema()
    helper.ClearAll()

    var bridge_sc = load("res://Game.Godot/Adapters/Db/RepositoryTestBridge.cs")
    if bridge_sc == null or not bridge_sc.has_method("new"):
        push_warning("SKIP: RepositoryTestBridge C# unavailable, skip")
        return
    var bridge = bridge_sc.new()
    add_child(auto_free(bridge))
    var username = "u_%s" % Time.get_unix_time_from_system()
    assert_bool(bridge.UpsertUser(username)).is_true()
    var uid = bridge.FindUserId(username)
    assert_that(uid).is_not_null()
    var json = '{"hp": 55, "ts": %d}' % Time.get_unix_time_from_system()
    assert_bool(bridge.UpsertSave(uid, 1, json)).is_true()

    # checkpoint WAL → close → copy to backup
    helper.ExecSql("PRAGMA wal_checkpoint(TRUNCATE);")
    db.Close()
    await get_tree().process_frame
    var backup_dir = "user://backup_%s" % Time.get_unix_time_from_system()
    var backup_path = "%s/%s" % [backup_dir, path.get_file()]
    assert_bool(_copy_file(path, backup_path)).is_true()

    # open from backup and verify
    tries = 10
    while not db.has_method("TryOpen") and tries > 0:
        await get_tree().process_frame
        tries -= 1
    assert_bool(db.has_method("TryOpen")).is_true()
    var ok2 = db.TryOpen(backup_path)
    assert_bool(ok2).is_true()
    # schema fallback to avoid Nil if copy missed table (tests only)
    if db.has_method("TableExists") and not db.TableExists("saves"):
        helper.CreateSchema()
    var bridge2_sc = load("res://Game.Godot/Adapters/Db/RepositoryTestBridge.cs")
    if bridge2_sc == null or not bridge2_sc.has_method("new"):
        push_warning("SKIP: RepositoryTestBridge C# unavailable, skip")
        return
    var bridge2 = bridge2_sc.new()
    add_child(auto_free(bridge2))
    await get_tree().process_frame
    var got = bridge2.GetSaveData(uid, 1)
    assert_str(str(got)).contains('"hp": 55')

# ACC:T26.5
# ACC:T26.10
func test_cloud_upload_restart_and_download_restores_same_payload() -> void:
    var slot := "auto:steam_restore"
    var payload_json := JSON.stringify({"health": 61, "score": 120, "level": 1, "rev": 3})
    var bridge := _new_cloud_bridge("task26-cloud-restore-upload", "STEAM_REMOTE_STORAGE_REAL", true, "steam_restore")
    var save_result = bridge.call("SaveWithCloudSync", slot, "steam_restore", payload_json, true) as Dictionary
    assert_bool(bool(save_result.get("ok", false))).is_true()
    assert_bool(bool(save_result.get("uploaded", false))).is_true()
    var uploaded_snapshot := _parse_snapshot(str(bridge.call("SnapshotStateJson")))
    assert_bool(bool(bridge.call("SaveRaw", slot, JSON.stringify({"health": 1, "score": 1, "level": 1})))).is_true()
    bridge.call("ResetRuntimeKeepCloudState", "task26-cloud-restore-restart", false, 20250425, 10, 10, 15)
    var load_result = bridge.call("LoadWithCloudSync", slot, "steam_restore", true) as Dictionary
    var snapshot_text := str(bridge.call("SnapshotStateJson"))
    var snapshot := _parse_snapshot(snapshot_text)

    assert_bool(bool(load_result.get("ok", false))).is_true()
    assert_str(str(load_result.get("loaded_from", ""))).is_equal("cloud")
    _assert_full_payload_match(uploaded_snapshot, snapshot)

# ACC:T26.16
func test_cloud_restore_keeps_critical_fields_consistent_after_local_reset() -> void:
    var slot := "auto:steam_restore_fields"
    var payload_json := JSON.stringify({"health": 72, "score": 8, "level": 1, "rev": 5})
    var bridge := _new_cloud_bridge("task26-cloud-fields-upload", "STEAM_REMOTE_STORAGE_REAL", true, "steam_restore_fields")
    var save_result = bridge.call("SaveWithCloudSync", slot, "steam_restore_fields", payload_json, true) as Dictionary
    assert_bool(bool(save_result.get("ok", false))).is_true()
    var uploaded_snapshot := _parse_snapshot(str(bridge.call("SnapshotStateJson")))
    assert_bool(bool(bridge.call("SaveRaw", slot, JSON.stringify({"health": 9, "score": 0, "level": 1})))).is_true()
    bridge.call("ResetRuntimeKeepCloudState", "task26-cloud-fields-restart", false, 20250425, 10, 10, 15)
    var load_result = bridge.call("LoadWithCloudSync", slot, "steam_restore_fields", true) as Dictionary
    var snapshot_text := str(bridge.call("SnapshotStateJson"))
    var snapshot := _parse_snapshot(snapshot_text)

    assert_bool(bool(load_result.get("ok", false))).is_true()
    assert_str(str(load_result.get("reason_code", ""))).is_equal("ok")
    _assert_full_payload_match(uploaded_snapshot, snapshot)

