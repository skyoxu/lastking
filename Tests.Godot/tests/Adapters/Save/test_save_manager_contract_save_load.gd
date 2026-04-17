extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task25-contract-save-load", false, 20250425, 10, 10, 15)
    return bridge

func _state_json(id: String, level: int, score: int, health: int) -> String:
    return JSON.stringify({
        "id": id,
        "level": level,
        "score": score,
        "health": health,
        "inventory": ["wood", "stone"],
        "x": 5.5,
        "y": -2.0
    })

func _state_dict(json_text: String) -> Dictionary:
    var parsed = JSON.parse_string(json_text)
    assert_that(parsed).is_not_null()
    return parsed as Dictionary

func _make_incompatible_payload(raw_json: String) -> String:
    var source := raw_json
    assert_that(source).contains("\"Version\":\"1.0.0\"")
    return source.replace("\"Version\":\"1.0.0\"", "\"Version\":\"0.0.0\"")

func test_roundtrip_missing_io_and_corruption_paths_produce_deterministic_audit_evidence() -> void:
    var bridge = _new_bridge()
    var baseline = _state_json("slot-a", 6, 120, 90)

    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, baseline))).is_true()
    assert_bool(bool(bridge.call("LoadSlot", AUTOSAVE_PATH))).is_true()
    var loaded_state := _state_dict(str(bridge.call("SnapshotStateJson")))
    assert_that(loaded_state.get("id")).is_equal("slot-a")
    assert_that(int(loaded_state.get("score", -1))).is_equal(120)

    assert_bool(bool(bridge.call("DeleteSlot", AUTOSAVE_PATH))).is_true()
    var missing_ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var missing_reason = str(bridge.call("LastLoadReasonCode"))
    assert_bool(missing_ok).is_false()
    assert_that(missing_reason).is_equal("missing_autosave")

    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, baseline))).is_true()
    bridge.call("SimulateNextSaveIoFailure")
    var failed_save_state = _state_json("slot-failed-save", 3, 33, 66)
    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, failed_save_state))).is_false()
    assert_bool(bool(bridge.call("LoadSlot", AUTOSAVE_PATH))).is_true()
    var after_failed_save := _state_dict(str(bridge.call("SnapshotStateJson")))
    assert_that(after_failed_save.get("id")).is_equal("slot-a")

    bridge.call("SimulateNextLoadIoFailure")
    var io_load_ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var io_reason = str(bridge.call("LastLoadReasonCode"))
    assert_bool(io_load_ok).is_false()
    assert_that(io_reason).is_equal("invalid_content")

    assert_bool(bool(bridge.call("SaveRaw", AUTOSAVE_PATH, "{ broken payload"))).is_true()
    var corrupt_ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var corrupt_reason = str(bridge.call("LastLoadReasonCode"))
    assert_bool(corrupt_ok).is_false()
    assert_that(corrupt_reason).is_equal("deserialize_failed")

# acceptance: ACC:T25.15
func test_version_mismatch_must_refuse_load_and_keep_previous_snapshot_unchanged() -> void:
    var bridge = _new_bridge()
    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, _state_json("baseline", 8, 444, 99)))).is_true()
    assert_bool(bool(bridge.call("LoadSlot", AUTOSAVE_PATH))).is_true()
    var before_state := str(bridge.call("SnapshotStateJson"))

    var raw = str(bridge.call("LoadRaw", AUTOSAVE_PATH))
    assert_bool(raw.length() > 0).is_true()
    var incompatible = _make_incompatible_payload(raw)
    assert_bool(bool(bridge.call("SaveRaw", AUTOSAVE_PATH, incompatible))).is_true()

    var ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var reason = str(bridge.call("LastLoadReasonCode"))
    var message_key = str(bridge.call("LastFeedbackMessageKey"))
    var after_state = str(bridge.call("SnapshotStateJson"))

    assert_bool(ok).is_false()
    assert_that(reason).is_equal("migration_version_incompatible")
    assert_that(message_key).contains("ui.migration_failure.")
    assert_that(after_state).is_equal(before_state)
