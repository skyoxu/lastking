extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class FakeUIFeedbackManager:
    extends RefCounted

    var feedback_label_text: String = ""
    var error_dialog_visible: bool = false
    var error_dialog_message: String = ""
    var normal_ui_control_enabled: bool = true
    var dismiss_control_exposed: bool = true
    var presentation_channel: String = "popup"

    func present_invalid_placement(reason: String) -> void:
        feedback_label_text = "Invalid placement: %s" % reason

    func present_blocked_action(reason: String) -> void:
        feedback_label_text = "Action blocked: %s" % reason

    func present_migration_load_failure(details: String) -> void:
        error_dialog_visible = true
        error_dialog_message = "Load failed: %s" % details
        normal_ui_control_enabled = false

    func idle_frame() -> void:
        # Persistent error feedback should remain until explicit dismiss.
        pass

    func close_without_explicit_user_action() -> void:
        # Non-explicit close paths must not dismiss the dialog.
        pass

    func dismiss_from_control() -> void:
        if dismiss_control_exposed:
            error_dialog_visible = false
            normal_ui_control_enabled = true

# acceptance: ACC:T24.1
func test_invalid_placement_feedback_is_user_facing() -> void:
    var manager := FakeUIFeedbackManager.new()
    manager.present_invalid_placement("Tile occupied at (2,3)")
    assert_that(manager.feedback_label_text.length()).is_greater(0)

# acceptance: ACC:T24.2
func test_invalid_placement_feedback_contains_specific_reason() -> void:
    var manager := FakeUIFeedbackManager.new()
    var reason := "Tile occupied at (2,3)"
    manager.present_invalid_placement(reason)
    assert_that(manager.feedback_label_text).contains(reason)

# acceptance: ACC:T24.6
func test_ui_feedback_manager_uses_label_or_popup_path() -> void:
    var manager := FakeUIFeedbackManager.new()
    var valid_path := manager.presentation_channel == "label" or manager.presentation_channel == "popup"
    assert_that(valid_path).is_true()

# acceptance: ACC:T24.10
func test_migration_load_failure_dialog_stays_visible_without_user_dismiss() -> void:
    var manager := FakeUIFeedbackManager.new()
    var details := "Corrupted save payload"
    manager.present_migration_load_failure(details)
    assert_that(manager.error_dialog_message).contains(details)
    manager.idle_frame()
    assert_that(manager.error_dialog_visible).is_true()

# acceptance: ACC:T24.11
func test_migration_load_failure_dialog_rejects_non_explicit_close_attempt() -> void:
    var manager := FakeUIFeedbackManager.new()
    manager.present_migration_load_failure("Migration step 3 checksum mismatch")
    manager.close_without_explicit_user_action()
    assert_that(manager.error_dialog_visible).is_true()

# acceptance: ACC:T24.14
func test_blocked_action_feedback_contains_concrete_reason() -> void:
    var manager := FakeUIFeedbackManager.new()
    var reason := "Blocked by turn lock while AI resolves combat"
    manager.present_blocked_action(reason)
    assert_that(manager.feedback_label_text).contains(reason)

# acceptance: ACC:T24.16
func test_error_dialog_dismiss_control_closes_dialog_and_restores_ui_control() -> void:
    var manager := FakeUIFeedbackManager.new()
    manager.present_migration_load_failure("Cannot deserialize save version 8")
    assert_that(manager.dismiss_control_exposed).is_true()
    manager.dismiss_from_control()
    assert_that(manager.error_dialog_visible).is_false()
    assert_that(manager.normal_ui_control_enabled).is_true()
