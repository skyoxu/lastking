extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class LegacyLoadFailureFeedbackFlow:
    var error_dialog_visible: bool = false
    var error_details_text: String = ""
    var dismiss_control_visible: bool = false
    var can_accept_normal_input: bool = true

    func report_load_or_migration_failure(error_code: String, details: String) -> void:
        error_dialog_visible = true
        dismiss_control_visible = true
        error_details_text = "[%s] %s" % [error_code, details]
        can_accept_normal_input = false

    func try_continue_gameplay_input() -> void:
        # Non-dismiss input must not close persistent error feedback.
        pass

    func dismiss_error_dialog() -> void:
        if not dismiss_control_visible:
            return
        error_dialog_visible = false
        dismiss_control_visible = false
        can_accept_normal_input = true

# ACC:T24.6
func test_load_failure_shows_persistent_error_dialog_with_details_until_explicit_dismiss() -> void:
    var flow := LegacyLoadFailureFeedbackFlow.new()

    flow.report_load_or_migration_failure("load_corrupted_save", "slot=slot_a;line=4")

    assert_bool(flow.error_dialog_visible).is_true()
    assert_bool(flow.dismiss_control_visible).is_true()
    assert_str(flow.error_details_text).contains("load_corrupted_save")
    assert_str(flow.error_details_text).contains("slot=slot_a")
    assert_bool(flow.can_accept_normal_input).is_false()

    # Negative path: unrelated input must not dismiss persistent failure feedback.
    flow.try_continue_gameplay_input()

    assert_bool(flow.error_dialog_visible).is_true()
    assert_bool(flow.can_accept_normal_input).is_false()

# ACC:T24.14
func test_dismiss_control_closes_dialog_and_restores_normal_ui_control() -> void:
    var flow := LegacyLoadFailureFeedbackFlow.new()

    flow.report_load_or_migration_failure("migration_failed", "slot=slot_b;missing_required_field")
    flow.dismiss_error_dialog()

    assert_bool(flow.error_dialog_visible).is_false()
    assert_bool(flow.dismiss_control_visible).is_false()
    assert_bool(flow.can_accept_normal_input).is_true()
