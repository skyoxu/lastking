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

# acceptance: ACC:T26.13
func test_requires_remote_storage_operation_ids_to_verify_real_api_calls() -> void:
	var bridge := _new_bridge("task26-binding-evidence", "STEAM_REMOTE_STORAGE_REAL", true, "steam_binding")
	var payload := JSON.stringify({"coins": 10, "rev": 1})
	var save_result = bridge.call("SaveWithCloudSync", "slot_binding", "steam_binding", payload, true) as Dictionary
	var load_result = bridge.call("LoadWithCloudSync", "slot_binding", "steam_binding", true) as Dictionary
	var operation_ids = bridge.call("GetCloudOperationIds") as Array

	assert_that(bool(save_result.get("ok", false))).is_true()
	assert_that(bool(load_result.get("ok", false))).is_true()
	assert_that(str(load_result.get("loaded_from", ""))).is_equal("cloud")
	assert_that(operation_ids.size()).is_equal(2)
	assert_that(str(operation_ids[0])).contains("steam-upload-")
	assert_that(str(operation_ids[1])).contains("steam-download-")

func test_rejects_local_only_mock_backend_even_if_operation_names_match() -> void:
	var bridge := _new_bridge("task26-binding-reject", "LOCAL_MOCK", true, "steam_binding")
	var payload := JSON.stringify({"coins": 20, "rev": 1})
	var verdict = bridge.call("SaveWithCloudSync", "slot_binding_reject", "steam_binding", payload, true) as Dictionary
	var operation_ids = bridge.call("GetCloudOperationIds") as Array

	assert_that(bool(verdict.get("ok", true))).is_false()
	assert_that(str(verdict.get("reason_code", ""))).is_equal("steam_remote_storage_required")
	assert_that(str(verdict.get("backend", ""))).is_equal("LOCAL_MOCK")
	assert_that(operation_ids.size()).is_equal(1)

func test_accepts_real_remote_storage_evidence_for_logged_in_account() -> void:
	var bridge := _new_bridge("task26-binding-accept", "STEAM_REMOTE_STORAGE_REAL", true, "steam_binding")
	var payload := JSON.stringify({"coins": 30, "rev": 1})
	var save_result = bridge.call("SaveWithCloudSync", "slot_binding_accept", "steam_binding", payload, true) as Dictionary
	var operation_ids = bridge.call("GetCloudOperationIds") as Array

	assert_that(bool(save_result.get("ok", false))).is_true()
	assert_that(bool(save_result.get("uploaded", false))).is_true()
	assert_that(str(save_result.get("reason_code", ""))).is_equal("ok")
	assert_that(operation_ids.size()).is_equal(1)
	assert_that(str(operation_ids[0])).contains("steam-upload-")

func test_requires_real_api_probe_when_strict_evidence_mode_enabled() -> void:
	var bridge := _new_bridge("task26-binding-real-api", "STEAM_REMOTE_STORAGE_REAL", true, "steam_binding", true)
	var payload := JSON.stringify({"coins": 44, "rev": 2})
	var result = bridge.call("SaveWithCloudSync", "slot_binding_real", "steam_binding", payload, true) as Dictionary
	var require_real_evidence := _require_real_steam_evidence_lane()

	if Engine.has_singleton("Steam"):
		assert_that(bool(result.get("ok", false))).is_true()
		assert_that(bool(result.get("real_api_checked", false))).is_true()
		assert_that(str(result.get("evidence_source", ""))).is_equal("steam_remote_storage_methods")
	else:
		assert_that(bool(result.get("ok", true))).is_false()
		assert_that(str(result.get("reason_code", ""))).is_equal("steam_api_unavailable")
		assert_that(str(result.get("evidence_source", ""))).contains("steam_singleton")
		if require_real_evidence:
			assert_that(false).is_true()
