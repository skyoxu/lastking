extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"

func _new_bridge(storage_key: String, backend: String, logged_in: bool, account_id: String) -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	bridge.call("ResetRuntime", storage_key, false, 20250425, 10, 10, 15)
	bridge.call("ResetCloudRuntime", backend, logged_in, account_id)
	return bridge

# acceptance: ACC:T26.4
func test_save_trigger_uploads_active_save_to_steam_cloud_and_reports_success() -> void:
	var bridge := _new_bridge("task26-upload-success", "STEAM_REMOTE_STORAGE_REAL", true, "steam_upload")
	var result = bridge.call(
		"SaveWithCloudSync",
		"auto:steam_upload",
		"steam_upload",
		JSON.stringify({"hp": 88, "rev": 1}),
		true
	) as Dictionary
	var operation_id := str(bridge.call("LastCloudOperationId"))
	var operation_ids := bridge.call("GetCloudOperationIds") as Array

	assert_bool(bool(result.get("ok", false))).is_true()
	assert_bool(bool(result.get("uploaded", false))).is_true()
	assert_str(str(result.get("reason_code", ""))).is_equal("ok")
	assert_str(operation_id).contains("steam-upload-")
	assert_int(operation_ids.size()).is_equal(1)

func test_save_trigger_reports_failure_when_backend_is_not_remote_storage() -> void:
	var bridge := _new_bridge("task26-upload-fail", "LOCAL_MOCK", true, "steam_upload")
	var result = bridge.call(
		"SaveWithCloudSync",
		"auto:steam_upload",
		"steam_upload",
		JSON.stringify({"hp": 77, "rev": 2}),
		true
	) as Dictionary
	var operation_ids := bridge.call("GetCloudOperationIds") as Array

	assert_bool(bool(result.get("ok", true))).is_false()
	assert_bool(bool(result.get("uploaded", true))).is_false()
	assert_str(str(result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
	assert_int(operation_ids.size()).is_equal(1)
