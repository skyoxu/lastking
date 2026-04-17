extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"

func _new_bridge(storage_key: String = "task26-cloud-sync", backend: String = "STEAM_REMOTE_STORAGE_REAL", logged_in: bool = true, account_id: String = "steam_default") -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", storage_key, false, 20250425, 10, 10, 15)
    bridge.call("ResetCloudRuntime", backend, logged_in, account_id)
    return bridge

func _snapshot_text(bridge: Node) -> String:
    return str(bridge.call("SnapshotStateJson"))

# acceptance: ACC:T26.1
func test_cloud_enabled_save_and_load_execute_sync_integration_path() -> void:
    var bridge := _new_bridge("task26-acc1", "STEAM_REMOTE_STORAGE_REAL", true, "steam_100")
    var save_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_100",
        "steam_100",
        JSON.stringify({"health": 90, "score": 12, "level": 2}),
        true
    ) as Dictionary
    var load_result = bridge.call("LoadWithCloudSync", "auto:steam_100", "steam_100", true) as Dictionary
    var op_ids = bridge.call("GetCloudOperationIds") as Array

    assert_that(bool(save_result.get("ok", false))).is_true()
    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("cloud")
    assert_that(op_ids.size()).is_equal(2)
    assert_that(str(op_ids[0])).contains("steam-upload-")
    assert_that(str(op_ids[1])).contains("steam-download-")

# acceptance: ACC:T26.2
func test_windows_baseline_cloud_sync_sample_flow_passes_with_expected_results() -> void:
    var bridge := _new_bridge("task26-acc2", "STEAM_REMOTE_STORAGE_REAL", true, "steam_report")
    var save_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_report",
        "steam_report",
        JSON.stringify({"health": 61, "score": 120, "level": 3}),
        true
    ) as Dictionary
    assert_that(bool(save_result.get("ok", false))).is_true()
    assert_that(bool(save_result.get("uploaded", false))).is_true()

    bridge.call("SaveRaw", "auto:steam_report", JSON.stringify({"health": 1, "score": 0, "level": 1}))
    var load_result = bridge.call("LoadWithCloudSync", "auto:steam_report", "steam_report", true) as Dictionary
    var snapshot_text := _snapshot_text(bridge)
    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("cloud")
    assert_that(snapshot_text).contains("\"health\":61")
    assert_that(snapshot_text).contains("\"score\":120")

    var reject_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_report",
        "steam_intruder",
        JSON.stringify({"health": 999, "score": 999, "level": 9}),
        true
    ) as Dictionary
    assert_that(bool(reject_result.get("rejected", false))).is_true()
    assert_that(str(reject_result.get("reason_code", ""))).is_equal("ownership_mismatch")

    var op_ids = bridge.call("GetCloudOperationIds") as Array
    assert_that(op_ids.size()).is_equal(2)

# acceptance: ACC:T26.4
func test_save_trigger_uploads_active_save_file_and_reports_success() -> void:
    var bridge := _new_bridge("task26-acc4", "STEAM_REMOTE_STORAGE_REAL", true, "steam_200")
    var result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_200",
        "steam_200",
        JSON.stringify({"health": 88, "score": 42, "level": 1}),
        true
    ) as Dictionary
    var operation_id := str(bridge.call("LastCloudOperationId"))

    assert_that(bool(result.get("ok", false))).is_true()
    assert_that(bool(result.get("uploaded", false))).is_true()
    assert_that(operation_id).contains("steam-upload-")

# acceptance: ACC:T26.17
func test_account_binding_enforces_single_auto_slot_per_account() -> void:
    var bridge := _new_bridge("task26-acc17", "STEAM_REMOTE_STORAGE_REAL", true, "steam_A")

    var save_a1 = bridge.call("SaveWithCloudSync", "auto:shared", "steam_A", JSON.stringify({"health": 1, "score": 1, "level": 1}), false) as Dictionary
    var save_a2 = bridge.call("SaveWithCloudSync", "auto:shared", "steam_A", JSON.stringify({"health": 2, "score": 2, "level": 1}), false) as Dictionary
    var save_b = bridge.call("SaveWithCloudSync", "auto:shared", "steam_B", JSON.stringify({"health": 3, "score": 3, "level": 1}), false) as Dictionary

    assert_that(bool(save_a1.get("ok", false))).is_true()
    assert_that(bool(save_a2.get("ok", false))).is_true()
    assert_that(bool(save_b.get("rejected", false))).is_true()
    assert_that(str(save_b.get("reason_code", ""))).is_equal("ownership_mismatch")

# acceptance: ACC:T26.19
func test_sync_flow_covers_cloud_sync_ownership_check_allow_and_reject_branches() -> void:
    var bridge := _new_bridge("task26-acc19", "STEAM_REMOTE_STORAGE_REAL", true, "steam_owner")

    var allow_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_owner",
        "steam_owner",
        JSON.stringify({"health": 33, "score": 3, "level": 1}),
        true
    ) as Dictionary
    var reject_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_owner",
        "steam_intruder",
        JSON.stringify({"health": 44, "score": 4, "level": 1}),
        true
    ) as Dictionary
    var op_ids = bridge.call("GetCloudOperationIds") as Array

    assert_that(bool(allow_result.get("ok", false))).is_true()
    assert_that(bool(reject_result.get("rejected", false))).is_true()
    assert_that(str(reject_result.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(op_ids.size()).is_equal(1)

# acceptance: ACC:T26.20
func test_ownership_mismatch_aborts_sync_and_keeps_local_save_unchanged() -> void:
    var bridge := _new_bridge("task26-acc20", "STEAM_REMOTE_STORAGE_REAL", true, "steam_777")
    var seed = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_777",
        "steam_777",
        JSON.stringify({"health": 10, "score": 10, "level": 1}),
        false
    ) as Dictionary
    assert_that(bool(seed.get("ok", false))).is_true()
    var before_state := _snapshot_text(bridge)

    var result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_777",
        "steam_other",
        JSON.stringify({"health": 999, "score": 999, "level": 9}),
        true
    ) as Dictionary
    var after_state := _snapshot_text(bridge)
    var op_ids = bridge.call("GetCloudOperationIds") as Array

    assert_that(bool(result.get("rejected", false))).is_true()
    assert_that(str(result.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(op_ids.size()).is_equal(0)
    assert_that(after_state).is_equal(before_state)

# acceptance: ACC:T26.12
func test_no_valid_steam_login_skips_cloud_sync_with_user_visible_status_and_local_save_load_succeeds() -> void:
    var bridge := _new_bridge("task26-acc12", "STEAM_REMOTE_STORAGE_REAL", false, "steam_missing")
    var save_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_missing",
        "steam_missing",
        JSON.stringify({"health": 55, "score": 9, "level": 1}),
        true
    ) as Dictionary
    var load_result = bridge.call("LoadWithCloudSync", "auto:steam_missing", "steam_missing", true) as Dictionary

    assert_that(bool(save_result.get("ok", true))).is_false()
    assert_that(str(save_result.get("reason_code", ""))).is_equal("steam_login_required")
    assert_that(str(bridge.call("LastCloudStatusCode"))).is_equal("steam_login_required")
    assert_that(str(bridge.call("LastCloudStatusMessage"))).contains("No valid Steam login")
    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("local")

# acceptance: ACC:T26.5
func test_uploaded_cloud_state_restores_after_local_state_is_replaced() -> void:
    var bridge := _new_bridge("task26-acc5", "STEAM_REMOTE_STORAGE_REAL", true, "steam_restore")
    var slot := "auto:steam_restore"
    var save_result = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_restore",
        JSON.stringify({"health": 61, "score": 120, "level": 3}),
        true
    ) as Dictionary
    assert_that(bool(save_result.get("ok", false))).is_true()

    bridge.call("SaveRaw", slot, JSON.stringify({"health": 1, "score": 0, "level": 1}))
    var load_result = bridge.call("LoadWithCloudSync", slot, "steam_restore", true) as Dictionary
    var snapshot_text := _snapshot_text(bridge)

    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("cloud")
    assert_that(snapshot_text).contains("\"health\":61")
    assert_that(snapshot_text).contains("\"score\":120")

# acceptance: ACC:T26.7
# acceptance: ACC:T26.16
func test_upload_failure_surfaces_status_and_preserves_local_state() -> void:
    var bridge := _new_bridge("task26-acc7-upload", "STEAM_REMOTE_STORAGE_REAL", true, "steam_fail")
    var slot := "auto:steam_fail"
    bridge.call("SaveWithCloudSync", slot, "steam_fail", JSON.stringify({"health": 41, "score": 5, "level": 1}), false)
    var before_state := _snapshot_text(bridge)
    var before_load: String = str(bridge.call("LoadRaw", slot))
    bridge.call("ResetCloudRuntime", "LOCAL_MOCK", true, "steam_fail")

    var save_result = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_fail",
        JSON.stringify({"health": 99, "score": 99, "level": 9}),
        true
    ) as Dictionary
    var after_state := _snapshot_text(bridge)
    var after_load: String = str(bridge.call("LoadRaw", slot))

    assert_that(bool(save_result.get("ok", true))).is_false()
    assert_that(str(save_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(save_result.get("status_message", ""))).contains("not Steam Remote Storage")
    assert_that(after_state).is_equal(before_state)
    assert_that(after_load).is_equal(before_load)

# acceptance: ACC:T26.7
func test_download_failure_surfaces_status_and_preserves_local_state() -> void:
    var bridge := _new_bridge("task26-acc7-download", "STEAM_REMOTE_STORAGE_REAL", true, "steam_fail_download")
    var slot := "auto:steam_fail_download"
    bridge.call("SaveWithCloudSync", slot, "steam_fail_download", JSON.stringify({"health": 30, "score": 2, "level": 1}), false)
    var before_state := _snapshot_text(bridge)
    bridge.call("ResetCloudRuntime", "LOCAL_MOCK", true, "steam_fail_download")

    var load_result = bridge.call("LoadWithCloudSync", slot, "steam_fail_download", true) as Dictionary
    var after_state := _snapshot_text(bridge)

    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("local")
    assert_that(str(load_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(load_result.get("status_message", ""))).contains("not Steam Remote Storage")
    assert_that(after_state).is_equal(before_state)

# acceptance: ACC:T26.8
# acceptance: ACC:T26.14
# acceptance: ACC:T26.18
func test_cross_account_read_write_overwrite_are_rejected_without_affecting_owner_state() -> void:
    var bridge := _new_bridge("task26-acc8", "STEAM_REMOTE_STORAGE_REAL", true, "steam_owner")
    var slot := "auto:cross-account-slot"
    var owner_save = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_owner",
        JSON.stringify({"health": 77, "score": 11, "level": 2}),
        true
    ) as Dictionary
    assert_that(bool(owner_save.get("ok", false))).is_true()
    var owner_before := _snapshot_text(bridge)

    var intruder_save = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_intruder",
        JSON.stringify({"health": 500, "score": 500, "level": 9}),
        true
    ) as Dictionary
    var intruder_load = bridge.call("LoadWithCloudSync", slot, "steam_intruder", true) as Dictionary
    var owner_after := _snapshot_text(bridge)

    assert_that(bool(intruder_save.get("rejected", false))).is_true()
    assert_that(str(intruder_save.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(bool(intruder_load.get("rejected", false))).is_true()
    assert_that(str(intruder_load.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(owner_after).is_equal(owner_before)

# acceptance: ACC:T26.9
# acceptance: ACC:T26.12
func test_local_workflow_remains_usable_when_cloud_unavailable() -> void:
    var bridge := _new_bridge("task26-acc9", "LOCAL_MOCK", true, "steam_local_only")
    var slot := "auto:steam_local_only"
    var save_result = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_local_only",
        JSON.stringify({"health": 65, "score": 7, "level": 1}),
        true
    ) as Dictionary
    var load_result = bridge.call("LoadWithCloudSync", slot, "steam_local_only", true) as Dictionary

    assert_that(bool(save_result.get("ok", true))).is_false()
    assert_that(str(save_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(bool(load_result.get("ok", false))).is_true()
    assert_that(str(load_result.get("loaded_from", ""))).is_equal("local")

# acceptance: ACC:T26.12
func test_empty_account_id_is_rejected_for_save_and_load_without_state_pollution() -> void:
    var bridge := _new_bridge("task26-acc12-empty-account", "STEAM_REMOTE_STORAGE_REAL", true, "steam_empty_owner")
    var slot := "auto:steam_empty_owner"
    var seeded = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_empty_owner",
        JSON.stringify({"health": 48, "score": 8, "level": 1}),
        false
    ) as Dictionary
    assert_that(bool(seeded.get("ok", false))).is_true()
    var before_state := _snapshot_text(bridge)

    var save_result = bridge.call(
        "SaveWithCloudSync",
        slot,
        "",
        JSON.stringify({"health": 99, "score": 99, "level": 9}),
        true
    ) as Dictionary
    var load_result = bridge.call("LoadWithCloudSync", slot, "", true) as Dictionary
    var after_state := _snapshot_text(bridge)

    assert_that(bool(save_result.get("ok", true))).is_false()
    assert_that(str(save_result.get("reason_code", ""))).is_equal("invalid_account")
    assert_that(bool(load_result.get("ok", true))).is_false()
    assert_that(str(load_result.get("reason_code", ""))).is_equal("invalid_account")
    assert_that(after_state).is_equal(before_state)

# acceptance: ACC:T26.15
func test_ownership_validation_rejects_mismatch_and_keeps_local_state_unchanged_before_apply() -> void:
    var bridge := _new_bridge("task26-acc15", "STEAM_REMOTE_STORAGE_REAL", true, "steam_owner15")
    var slot := "auto:steam_owner15"
    bridge.call("SaveWithCloudSync", slot, "steam_owner15", JSON.stringify({"health": 42, "score": 6, "level": 1}), false)
    var before_state := _snapshot_text(bridge)

    var rejected = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_other15",
        JSON.stringify({"health": 999, "score": 999, "level": 9}),
        true
    ) as Dictionary
    var after_state := _snapshot_text(bridge)

    assert_that(bool(rejected.get("rejected", false))).is_true()
    assert_that(str(rejected.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(after_state).is_equal(before_state)

# acceptance: ACC:T26.15
func test_metadata_validation_rejects_mismatch_for_same_owner_and_keeps_state_unchanged() -> void:
    var bridge := _new_bridge("task26-acc15-metadata", "STEAM_REMOTE_STORAGE_REAL", true, "steam_owner15_meta")
    var slot := "auto:steam_owner15_meta"
    var seeded = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_owner15_meta",
        JSON.stringify({"health": 42, "score": 6, "level": 1}),
        true
    ) as Dictionary
    assert_that(bool(seeded.get("ok", false))).is_true()
    bridge.call("SaveRaw", slot, JSON.stringify({"health": 77, "score": 17, "level": 2}))
    var before_state := _snapshot_text(bridge)
    bridge.call(
        "InjectCloudPayloadForTesting",
        slot,
        "steam_owner15_meta",
        JSON.stringify({"health": 43, "score": 66, "level": 1}),
        false
    )

    var rejected = bridge.call("LoadWithCloudSync", slot, "steam_owner15_meta", true) as Dictionary
    var after_state := _snapshot_text(bridge)

    assert_that(bool(rejected.get("rejected", false))).is_true()
    assert_that(str(rejected.get("reason_code", ""))).is_equal("metadata_mismatch")
    assert_that(str(rejected.get("operation_id", ""))).contains("steam-download-")
    assert_that(str(rejected.get("evidence_source", ""))).is_equal("metadata_binding_check")
    assert_that(after_state).is_equal(before_state)

# acceptance: ACC:T26.16
func test_failure_paths_emit_structured_diagnostics_with_reason_and_status_message() -> void:
    var bridge := _new_bridge("task26-acc16", "LOCAL_MOCK", true, "steam_diag")
    var save_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_diag",
        "steam_diag",
        JSON.stringify({"health": 15, "score": 2, "level": 1}),
        true
    ) as Dictionary
    var load_result = bridge.call("LoadWithCloudSync", "auto:steam_diag", "steam_diag", true) as Dictionary
    var second_save_result = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_diag",
        "steam_diag",
        JSON.stringify({"health": 18, "score": 3, "level": 1}),
        true
    ) as Dictionary
    var second_load_result = bridge.call("LoadWithCloudSync", "auto:steam_diag", "steam_diag", true) as Dictionary

    assert_that(str(save_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(save_result.get("status_message", ""))).contains("not Steam Remote Storage")
    assert_that(str(save_result.get("operation_id", ""))).contains("steam-upload-")
    assert_that(str(save_result.get("evidence_source", ""))).is_equal("backend_not_remote_storage")
    assert_that(str(load_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(load_result.get("status_message", ""))).contains("not Steam Remote Storage")
    assert_that(str(load_result.get("operation_id", ""))).contains("steam-download-")
    assert_that(str(load_result.get("evidence_source", ""))).is_equal("backend_not_remote_storage")
    assert_that(str(second_save_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(second_load_result.get("reason_code", ""))).is_equal("steam_remote_storage_required")
    assert_that(str(second_save_result.get("evidence_source", ""))).is_equal("backend_not_remote_storage")
    assert_that(str(second_load_result.get("evidence_source", ""))).is_equal("backend_not_remote_storage")

# acceptance: ACC:T26.16
func test_regression_chain_covers_upload_download_conflict_and_failure_recovery_paths() -> void:
    var bridge := _new_bridge("task26-acc16-regression", "STEAM_REMOTE_STORAGE_REAL", true, "steam_chain")
    var slot := "auto:steam_chain"

    var upload = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_chain",
        JSON.stringify({"health": 25, "score": 5, "level": 1}),
        true
    ) as Dictionary
    var download = bridge.call("LoadWithCloudSync", slot, "steam_chain", true) as Dictionary
    var conflict = bridge.call("ResolveCloudConflict", "rev_local", "{\"health\":25}", "rev_cloud", "{\"health\":99}", "none") as Dictionary
    bridge.call("ResetCloudRuntime", "LOCAL_MOCK", true, "steam_chain")
    var failure = bridge.call(
        "SaveWithCloudSync",
        slot,
        "steam_chain",
        JSON.stringify({"health": 30, "score": 7, "level": 1}),
        true
    ) as Dictionary

    assert_that(bool(upload.get("ok", false))).is_true()
    assert_that(str(download.get("loaded_from", ""))).is_equal("cloud")
    assert_that(bool(conflict.get("prompt_required", false))).is_true()
    assert_that(str(failure.get("reason_code", ""))).is_equal("steam_remote_storage_required")

# acceptance: ACC:T26.15
func test_load_rejects_owner_mismatch_and_keeps_state_unchanged() -> void:
    var bridge := _new_bridge("task26-load-owner-mismatch", "STEAM_REMOTE_STORAGE_REAL", true, "steam_owner")
    var seed = bridge.call(
        "SaveWithCloudSync",
        "auto:steam_owner_slot",
        "steam_owner",
        JSON.stringify({"health": 23, "score": 4, "level": 1}),
        false
    ) as Dictionary
    assert_that(bool(seed.get("ok", false))).is_true()
    var before_state := _snapshot_text(bridge)

    var load_result = bridge.call("LoadWithCloudSync", "auto:steam_owner_slot", "steam_intruder", true) as Dictionary
    var after_state := _snapshot_text(bridge)

    assert_that(bool(load_result.get("ok", true))).is_false()
    assert_that(bool(load_result.get("rejected", false))).is_true()
    assert_that(str(load_result.get("reason_code", ""))).is_equal("ownership_mismatch")
    assert_that(after_state).is_equal(before_state)
