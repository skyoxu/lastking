extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task26-ui-conflict-block", false, 20250425, 10, 10, 15)
    bridge.call("ResetCloudRuntime", "STEAM_REMOTE_STORAGE_REAL", true, "steam_ui")
    return bridge

# acceptance: ACC:T26.6
func test_conflict_requires_prompt_before_any_version_is_applied() -> void:
    var bridge := _new_bridge()
    var outcome = bridge.call(
        "ResolveCloudConflict",
        "rev_local",
        "{\"health\":50}",
        "rev_cloud",
        "{\"health\":80}",
        "none"
    ) as Dictionary

    assert_bool(bool(outcome.get("prompt_required", false))).is_true()
    assert_bool(bool(outcome.get("applied_local", true))).is_false()
    assert_bool(bool(outcome.get("applied_cloud", true))).is_false()

func test_conflict_keeps_local_state_unchanged_until_user_decision() -> void:
    var bridge := _new_bridge()
    bridge.call("SaveRaw", "auto:ui-local", JSON.stringify({"health": 66, "score": 7, "level": 1}))
    var before := str(bridge.call("LoadRaw", "auto:ui-local"))
    var outcome = bridge.call(
        "ResolveCloudConflict",
        "rev_local",
        "{\"health\":66,\"score\":7}",
        "rev_cloud",
        "{\"health\":99,\"score\":20}",
        "none"
    ) as Dictionary
    var after := str(bridge.call("LoadRaw", "auto:ui-local"))

    assert_bool(bool(outcome.get("prompt_required", false))).is_true()
    assert_str(after).is_equal(before)
