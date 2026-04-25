extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task25-load-feedback", false, 20250425, 10, 10, 15)
    return bridge

func _seed_slot(bridge: Node) -> void:
    var state_json = JSON.stringify({
        "id": "slot-seed",
        "level": 6,
        "score": 222,
        "health": 90,
        "inventory": ["wood", "stone", "iron"],
        "x": 12.5,
        "y": -1.5
    })
    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, state_json))).is_true()

# ACC:T25.7
# ACC:T45.4
func test_load_returns_deterministic_failure_feedback_and_unchanged_state_when_autosave_missing() -> void:
    var bridge = _new_bridge()
    var before_state = str(bridge.call("SnapshotStateJson"))
    assert_bool(bool(bridge.call("DeleteSlot", AUTOSAVE_PATH))).is_true()

    var ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var reason = str(bridge.call("LastLoadReasonCode"))
    var message_key = str(bridge.call("LastFeedbackMessageKey"))
    var after_state = str(bridge.call("SnapshotStateJson"))

    assert_bool(ok).is_false()
    assert_that(reason).is_equal("missing_autosave")
    assert_that(message_key).contains("ui.load_failure.")
    assert_that(after_state).is_equal(before_state)

# ACC:T25.9
# ACC:T45.1
# ACC:T45.3
func test_load_fails_with_explicit_feedback_and_no_partial_state_on_corrupt_content() -> void:
    var bridge = _new_bridge()
    _seed_slot(bridge)
    var before_state = str(bridge.call("SnapshotStateJson"))
    assert_bool(bool(bridge.call("SaveRaw", AUTOSAVE_PATH, "{ invalid json payload"))).is_true()

    var ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var reason = str(bridge.call("LastLoadReasonCode"))
    var message_key = str(bridge.call("LastFeedbackMessageKey"))
    var after_state = str(bridge.call("SnapshotStateJson"))

    assert_bool(ok).is_false()
    assert_that(reason).is_equal("deserialize_failed")
    assert_that(message_key).contains("ui.load_failure.")
    assert_that(after_state).is_equal(before_state)

# ACC:T25.14
# ACC:T45.6
func test_load_reports_explicit_deserialization_failure_and_keeps_state_unchanged() -> void:
    var bridge = _new_bridge()
    _seed_slot(bridge)
    var before_state = str(bridge.call("SnapshotStateJson"))
    bridge.call("SimulateNextLoadIoFailure")

    var ok = bool(bridge.call("LoadWithFeedback", AUTOSAVE_PATH))
    var reason = str(bridge.call("LastLoadReasonCode"))
    var message_key = str(bridge.call("LastFeedbackMessageKey"))
    var after_state = str(bridge.call("SnapshotStateJson"))

    assert_bool(ok).is_false()
    assert_that(reason).is_equal("invalid_content")
    assert_that(message_key).contains("ui.load_failure.")
    assert_that(after_state).is_equal(before_state)
