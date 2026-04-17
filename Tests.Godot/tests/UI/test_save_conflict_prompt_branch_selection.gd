extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task26-ui-branch", false, 20250425, 10, 10, 15)
    bridge.call("ResetCloudRuntime", "STEAM_REMOTE_STORAGE_REAL", true, "steam_ui")
    return bridge

# acceptance: ACC:T26.11
func test_local_choice_applies_local_data_without_auto_cloud_overwrite() -> void:
    var bridge := _new_bridge()
    var local_payload := "{\"health\":12,\"score\":3,\"level\":1}"
    var cloud_payload := "{\"health\":99,\"score\":8,\"level\":1}"
    bridge.call("SaveRaw", "auto:ui-branch", local_payload)
    var result = bridge.call(
        "ResolveCloudConflict",
        "rev_local",
        local_payload,
        "rev_cloud",
        cloud_payload,
        "local"
    ) as Dictionary

    assert_bool(bool(result.get("prompt_required", true))).is_false()
    assert_bool(bool(result.get("applied_local", false))).is_true()
    assert_bool(bool(result.get("applied_cloud", true))).is_false()
    assert_bool(bool(result.get("cloud_overwrite_scheduled", true))).is_false()
    var resolved_revision := str(result.get("resolved_revision", ""))
    var resolved_payload := str(result.get("resolved_payload", ""))
    assert_str(resolved_revision).contains("rev_local")
    assert_str(resolved_payload).contains("\"health\":12")
    assert_str(resolved_payload).contains("\"score\":3")
    assert_str(resolved_payload).is_not_equal(cloud_payload)

func test_cloud_choice_applies_cloud_data_for_current_operation() -> void:
    var bridge := _new_bridge()
    var local_payload := "{\"health\":40,\"score\":5,\"level\":1}"
    var cloud_payload := "{\"health\":120,\"score\":9,\"level\":1}"
    bridge.call("SaveRaw", "auto:ui-branch", local_payload)
    var result = bridge.call(
        "ResolveCloudConflict",
        "rev_local",
        local_payload,
        "rev_cloud",
        cloud_payload,
        "cloud"
    ) as Dictionary

    assert_bool(bool(result.get("prompt_required", true))).is_false()
    assert_bool(bool(result.get("applied_local", true))).is_false()
    assert_bool(bool(result.get("applied_cloud", false))).is_true()
    assert_bool(bool(result.get("cloud_overwrite_scheduled", true))).is_false()
    var resolved_revision := str(result.get("resolved_revision", ""))
    var resolved_payload := str(result.get("resolved_payload", ""))
    var snapshot_text := str(bridge.call("SnapshotStateJson"))
    assert_str(resolved_revision).contains("rev_cloud")
    assert_str(resolved_payload).contains("\"health\":120")
    assert_str(resolved_payload).contains("\"score\":9")
    assert_str(resolved_payload).is_not_equal(local_payload)
    assert_str(snapshot_text).contains("\"health\":120")
    assert_str(snapshot_text).contains("\"score\":9")
