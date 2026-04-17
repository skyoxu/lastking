extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const REQUIRE_REAL_STEAM_EVIDENCE_ENV := "SC_REQUIRE_REAL_STEAM_EVIDENCE"

func _require_real_steam_evidence_lane() -> bool:
	return String(OS.get_environment(REQUIRE_REAL_STEAM_EVIDENCE_ENV)).strip_edges() == "1"

func _new_bridge(storage_key: String, backend: String, logged_in: bool, account_id: String, require_real_api: bool = false) -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	bridge.call("ResetRuntime", storage_key, false, 20250425, 10, 10, 15)
	bridge.call("ResetCloudRuntime", backend, logged_in, account_id, require_real_api)
	return bridge

# ACC:T26.1
func test_cloud_enabled_save_load_route_through_account_bound_steam_storage() -> void:
	var bridge := _new_bridge("task26-real-ops-ok", "STEAM_REMOTE_STORAGE_REAL", true, "steam_user_1001")
	var payload := JSON.stringify({"coins": 32, "revision": 1})
	var save_result = bridge.call("SaveWithCloudSync", "slot_1", "steam_user_1001", payload, true) as Dictionary
	var load_result = bridge.call("LoadWithCloudSync", "slot_1", "steam_user_1001", true) as Dictionary
	var operation_ids = bridge.call("GetCloudOperationIds") as Array

	assert_bool(bool(save_result.get("ok", false))).is_true()
	assert_bool(bool(save_result.get("uploaded", false))).is_true()
	assert_bool(bool(load_result.get("ok", false))).is_true()
	assert_str(str(load_result.get("loaded_from", ""))).is_equal("cloud")
	assert_int(operation_ids.size()).is_equal(2)
	assert_str(str(operation_ids[0])).contains("steam-upload-")
	assert_str(str(operation_ids[1])).contains("steam-download-")

# ACC:T26.13
func test_cloud_sync_rejects_local_only_backend_for_logged_in_account() -> void:
	var bridge := _new_bridge("task26-real-ops-reject", "LOCAL_MOCK", true, "steam_user_1001")
	var payload := JSON.stringify({"xp": 7, "revision": 1})
	var save_result = bridge.call("SaveWithCloudSync", "slot_2", "steam_user_1001", payload, true) as Dictionary
	var operation_ids = bridge.call("GetCloudOperationIds") as Array

	assert_bool(bool(save_result.get("ok", true))).is_false()
	assert_str(str(save_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
	assert_str(str(save_result.get("backend", ""))).is_equal("LOCAL_MOCK")
	assert_int(operation_ids.size()).is_equal(1)

# ACC:T26.13
func test_cloud_sync_reports_real_steam_remote_storage_evidence_when_runtime_has_steam_api() -> void:
	var bridge := _new_bridge("task26-real-ops-evidence", "STEAM_REMOTE_STORAGE_REAL", true, "steam_user_2001", true)
	var payload := JSON.stringify({"xp": 18, "revision": 2})
	var save_result = bridge.call("SaveWithCloudSync", "slot_3", "steam_user_2001", payload, true) as Dictionary
	var evidence_source := str(save_result.get("evidence_source", ""))
	var require_real_evidence := _require_real_steam_evidence_lane()

	if Engine.has_singleton("Steam"):
		assert_bool(bool(save_result.get("ok", false))).is_true()
		assert_bool(bool(save_result.get("real_api_checked", false))).is_true()
		assert_str(evidence_source).is_equal("steam_remote_storage_methods")
	else:
		assert_bool(bool(save_result.get("ok", true))).is_false()
		assert_str(str(save_result.get("reason_code", ""))).is_equal("steam_api_unavailable")
		assert_str(evidence_source).contains("steam_singleton")
		if require_real_evidence:
			assert_bool(false).is_true()
