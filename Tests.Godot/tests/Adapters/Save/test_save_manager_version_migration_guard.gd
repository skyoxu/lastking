extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task25-migration-guard", false, 20250425, 10, 10, 15)
    return bridge

func _seed_slot(bridge: Node) -> void:
    var state_json = JSON.stringify({
        "id": "migration-seed",
        "level": 9,
        "score": 900,
        "health": 77,
        "inventory": ["wood", "stone", "gold"],
        "x": 3.25,
        "y": 7.5
    })
    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, state_json))).is_true()

func _make_incompatible_payload(raw_json: String) -> String:
    var source := raw_json
    assert_that(source).contains("\"Version\":\"1.0.0\"")
    return source.replace("\"Version\":\"1.0.0\"", "\"Version\":\"0.0.0\"")

# ACC:T25.11
func test_load_validates_version_before_runtime_mutation() -> void:
    var bridge = _new_bridge()
    _seed_slot(bridge)
    var before_state = str(bridge.call("SnapshotStateJson"))

    var raw = str(bridge.call("LoadRaw", AUTOSAVE_PATH))
    assert_bool(raw.length() > 0).is_true()
    var incompatible = _make_incompatible_payload(raw)
    assert_bool(bool(bridge.call("SaveRaw", AUTOSAVE_PATH, incompatible))).is_true()

    var ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var reason = str(bridge.call("LastLoadReasonCode"))
    var after_state = str(bridge.call("SnapshotStateJson"))

    assert_bool(ok).is_false()
    assert_that(reason).is_equal("migration_version_incompatible")
    assert_that(after_state).is_equal(before_state)

# ACC:T25.12
func test_incompatible_version_is_rejected_with_reason_and_runtime_state_unchanged() -> void:
    var bridge = _new_bridge()
    _seed_slot(bridge)
    var before_state = str(bridge.call("SnapshotStateJson"))

    var raw = str(bridge.call("LoadRaw", AUTOSAVE_PATH))
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
