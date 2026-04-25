extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T46.1
# ACC:T46.3
func test_hud_scene_contains_config_audit_and_migration_surfaces() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    var config_audit_panel: PanelContainer = scene.get_node("FeedbackLayer/ConfigAuditPanel")
    var migration_status_dialog: PanelContainer = scene.get_node("FeedbackLayer/MigrationStatusDialog")
    var report_metadata_panel: PanelContainer = scene.get_node("FeedbackLayer/ReportMetadataPanel")
    var audit_summary: Label = scene.get_node("FeedbackLayer/ConfigAuditPanel/VBox/AuditSummaryLabel")
    var migration_status: Label = scene.get_node("FeedbackLayer/MigrationStatusDialog/VBox/MigrationStatusLabel")
    var report_metadata: Label = scene.get_node("FeedbackLayer/ReportMetadataPanel/VBox/ReportMetadataLabel")

    assert_object(config_audit_panel).is_not_null()
    assert_object(migration_status_dialog).is_not_null()
    assert_object(report_metadata_panel).is_not_null()
    assert_str(audit_summary.text).contains("Config:")
    assert_str(migration_status.text).contains("Migration:")
    assert_str(report_metadata.text).contains("Metadata:")

# ACC:T46.3
# ACC:T46.4
func test_hud_config_audit_surfaces_refresh_with_reason_code_and_metadata() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    scene.call("ApplyConfigAuditView", {
        "active_config": "balance.promoted.json",
        "schema_status": "valid",
        "fallback_policy": "keep-last-known-good",
        "migration_status": "failed",
        "reason_code": "CFG_PARSE_ERROR",
        "report_metadata": "config_hash=abc123"
    })
    await get_tree().process_frame

    var audit_summary: Label = scene.get_node("FeedbackLayer/ConfigAuditPanel/VBox/AuditSummaryLabel")
    var migration_status: Label = scene.get_node("FeedbackLayer/MigrationStatusDialog/VBox/MigrationStatusLabel")
    var report_metadata: Label = scene.get_node("FeedbackLayer/ReportMetadataPanel/VBox/ReportMetadataLabel")
    var refresh_button: Button = scene.get_node("FeedbackLayer/ConfigAuditPanel/VBox/RefreshButton")
    var retry_button: Button = scene.get_node("FeedbackLayer/MigrationStatusDialog/VBox/RetryButton")

    assert_str(audit_summary.text).contains("balance.promoted.json")
    assert_str(audit_summary.text).contains("valid")
    assert_str(audit_summary.text).contains("keep-last-known-good")
    assert_str(migration_status.text).contains("failed")
    assert_str(migration_status.text).contains("CFG_PARSE_ERROR")
    assert_str(report_metadata.text).contains("config_hash=abc123")
    assert_bool(refresh_button.disabled).is_false()
    assert_bool(retry_button.disabled).is_false()

# ACC:T46.3
# ACC:T46.5
func test_hud_config_audit_surfaces_keep_deterministic_fallback_when_payload_is_partial_or_invalid() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame

    scene.call("ApplyConfigAuditView", {
        "active_config": "balance.baseline.json",
        "schema_status": "valid",
        "fallback_policy": "keep-last-known-good",
        "migration_status": "ok",
        "report_metadata": "config_hash=seeded"
    })
    await get_tree().process_frame

    var audit_summary: Label = scene.get_node("FeedbackLayer/ConfigAuditPanel/VBox/AuditSummaryLabel")
    var migration_status: Label = scene.get_node("FeedbackLayer/MigrationStatusDialog/VBox/MigrationStatusLabel")
    var report_metadata: Label = scene.get_node("FeedbackLayer/ReportMetadataPanel/VBox/ReportMetadataLabel")

    scene.call("ApplyConfigAuditView", {
        "active_config": 42,
        "migration_status": true
    })
    await get_tree().process_frame

    assert_str(audit_summary.text).contains("Config: 42")
    assert_str(audit_summary.text).contains("Schema: n/a")
    assert_str(audit_summary.text).contains("Fallback: n/a")
    assert_str(migration_status.text).contains("Migration: True")
    assert_str(report_metadata.text).contains("Metadata: n/a")
