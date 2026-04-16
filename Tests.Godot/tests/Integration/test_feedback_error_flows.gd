extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class LegacyFeedbackPipeline:
    var events: Array = []

    func _find_existing(category: String, reason_code: String) -> Dictionary:
        for item in events:
            var event: Dictionary = item
            if String(event.get("category", "")) == category and String(event.get("reason_code", "")) == reason_code:
                return event
        return {}

    func report_invalid_placement(reason_code: String) -> void:
        var existing := _find_existing("invalid_placement", reason_code)
        if not existing.is_empty():
            existing["repeat_count"] = int(existing.get("repeat_count", 1)) + 1
            return
        _append_event("invalid_placement", reason_code, "ui.invalid_action.%s" % reason_code, "warning", "placement_rejected=%s" % reason_code)

    func report_blocked_action(reason_code: String, blocker: String) -> void:
        var existing := _find_existing("blocked_action", reason_code)
        if not existing.is_empty():
            existing["repeat_count"] = int(existing.get("repeat_count", 1)) + 1
            return
        _append_event("blocked_action", reason_code, "ui.blocked_action.%s" % reason_code, "warning", "blocked_by=%s;reason=%s" % [blocker, reason_code])

    func report_load_failure(reason_code: String, slot_id: String) -> void:
        _append_event(
            "load_failure",
            reason_code,
            "ui.load_failure.%s" % reason_code,
            "error",
            "slot=%s;reason=%s" % [slot_id, reason_code]
        )

    func report_migration_failure(reason_code: String, slot_id: String) -> void:
        _append_event("migration_failure", reason_code, "ui.migration_failure.%s" % reason_code, "error", "slot=%s;reason=%s" % [slot_id, reason_code])

    func _append_event(category: String, reason_code: String, message_key: String, severity: String, details: String) -> void:
        events.append({
            "category": category,
            "reason_code": reason_code,
            "message_key": message_key,
            "severity": severity,
            "details": details,
            "repeat_count": 1
        })

func _events_by_category(items: Array, category: String) -> Array:
    var matches: Array = []
    for item in items:
        var event: Dictionary = item
        if String(event.get("category", "")) == category:
            matches.append(event)
    return matches

func _latest_by_category(items: Array, category: String) -> Dictionary:
    for i in range(items.size() - 1, -1, -1):
        var event: Dictionary = items[i]
        if String(event.get("category", "")) == category:
            return event
    return {}

# ACC:T24.21
func test_repeated_invalid_and_blocked_errors_remain_stable_without_duplicate_feedback_entries() -> void:
    var pipeline := LegacyFeedbackPipeline.new()

    pipeline.report_invalid_placement("tile_occupied")
    pipeline.report_invalid_placement("tile_occupied")
    pipeline.report_invalid_placement("tile_occupied")
    pipeline.report_blocked_action("insufficient_resources", "resource_gate")
    pipeline.report_blocked_action("insufficient_resources", "resource_gate")

    var invalid_events := _events_by_category(pipeline.events, "invalid_placement")
    assert_int(invalid_events.size()).is_equal(1)
    if invalid_events.size() != 1:
        return

    var blocked_events := _events_by_category(pipeline.events, "blocked_action")
    assert_int(blocked_events.size()).is_equal(1)
    if blocked_events.size() != 1:
        return

    var invalid_feedback: Dictionary = invalid_events[0]
    assert_str(String(invalid_feedback.get("reason_code", ""))).is_equal("tile_occupied")
    assert_str(String(invalid_feedback.get("message_key", ""))).is_equal("ui.invalid_action.tile_occupied")
    assert_int(int(invalid_feedback.get("repeat_count", -1))).is_equal(3)

    var blocked_feedback: Dictionary = blocked_events[0]
    assert_str(String(blocked_feedback.get("reason_code", ""))).is_equal("insufficient_resources")
    assert_str(String(blocked_feedback.get("message_key", ""))).is_equal("ui.blocked_action.insufficient_resources")
    assert_str(String(blocked_feedback.get("severity", ""))).is_equal("warning")
    assert_int(int(blocked_feedback.get("repeat_count", -1))).is_equal(2)

# ACC:T24.22
func test_feedback_diagnostics_expose_reason_code_message_key_and_details_for_blocked_load_and_migration_errors() -> void:
    var pipeline := LegacyFeedbackPipeline.new()

    pipeline.report_blocked_action("run_continue_blocked", "chapter_locked")
    pipeline.report_load_failure("json_parse_error", "slot_a")
    pipeline.report_migration_failure("missing_required_field", "slot_a")

    var blocked := _latest_by_category(pipeline.events, "blocked_action")
    var load_failed := _latest_by_category(pipeline.events, "load_failure")
    var migration_failed := _latest_by_category(pipeline.events, "migration_failure")

    assert_bool(blocked.is_empty()).is_false()
    assert_bool(load_failed.is_empty()).is_false()
    assert_bool(migration_failed.is_empty()).is_false()

    assert_str(String(blocked.get("reason_code", ""))).is_equal("run_continue_blocked")
    assert_str(String(blocked.get("message_key", ""))).is_equal("ui.blocked_action.run_continue_blocked")
    assert_str(String(blocked.get("details", ""))).contains("chapter_locked")

    assert_str(String(load_failed.get("reason_code", ""))).is_equal("json_parse_error")
    assert_str(String(load_failed.get("message_key", ""))).is_equal("ui.load_failure.json_parse_error")
    assert_str(String(load_failed.get("details", ""))).contains("slot=slot_a")

    assert_str(String(migration_failed.get("reason_code", ""))).is_equal("missing_required_field")
    assert_str(String(migration_failed.get("message_key", ""))).is_equal("ui.migration_failure.missing_required_field")
    assert_str(String(migration_failed.get("message_key", ""))).is_not_equal("ui.error.unknown")
    assert_str(String(migration_failed.get("details", ""))).contains("slot=slot_a")
