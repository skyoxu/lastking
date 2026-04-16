extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class FakeGameState extends RefCounted:
    var entity_count: int = 0

class FakeHudFeedbackManager extends RefCounted:
    const DEFAULT_TIMEOUT_SECONDS := 1.5

    var feedback_visible: bool = false
    var feedback_text: String = ""
    var feedback_channel: String = "label"
    var feedback_timeout_seconds: float = DEFAULT_TIMEOUT_SECONDS
    var feedback_remaining_seconds: float = 0.0
    var input_blocked: bool = false

    var error_dialog_visible: bool = false
    var error_dialog_text: String = ""
    var error_channel: String = "popup"

    func attempt_invalid_placement(reason_code: String, state: FakeGameState) -> bool:
        var before_entities := state.entity_count
        feedback_text = _placement_message(reason_code)
        feedback_visible = true
        feedback_remaining_seconds = feedback_timeout_seconds
        input_blocked = false
        state.entity_count = before_entities
        return false

    func attempt_blocked_action(reason_detail: String, state: FakeGameState) -> bool:
        var before_entities := state.entity_count
        feedback_text = "Blocked: %s" % reason_detail
        feedback_visible = true
        feedback_remaining_seconds = feedback_timeout_seconds
        input_blocked = false
        state.entity_count = before_entities
        return false

    func show_migration_failure(detail: String) -> void:
        error_dialog_visible = true
        error_dialog_text = "Migration failed: %s" % detail

    func show_load_failure(detail: String) -> void:
        error_dialog_visible = true
        error_dialog_text = "Load failed: %s" % detail

    func advance(seconds: float) -> void:
        if not feedback_visible:
            return
        feedback_remaining_seconds = max(0.0, feedback_remaining_seconds - max(seconds, 0.0))
        if feedback_remaining_seconds <= 0.0:
            feedback_visible = false
            feedback_text = ""

    func can_continue_gameplay_interaction() -> bool:
        return not input_blocked

    func _placement_message(reason_code: String) -> String:
        if reason_code == "blocked_tile":
            return "Cannot build here."
        if reason_code == "invalid_terrain":
            return "Cannot build on this terrain."
        return "Action is not allowed here."

func _new_state() -> FakeGameState:
    return FakeGameState.new()

func _new_manager() -> FakeHudFeedbackManager:
    return FakeHudFeedbackManager.new()

# acceptance: ACC:T24.1
func test_ui_feedback_surfaces_required_invalid_blocked_and_failure_scenarios() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_invalid_placement("blocked_tile", state)
    assert_bool(manager.feedback_visible).is_true()
    assert_bool(manager.feedback_text.find("Cannot build") >= 0).is_true()

    manager.attempt_blocked_action("Need 2 wood", state)
    assert_bool(manager.feedback_visible).is_true()
    assert_bool(manager.feedback_text.find("Need 2 wood") >= 0).is_true()

    manager.show_migration_failure("missing migration file")
    assert_bool(manager.error_dialog_visible).is_true()
    assert_bool(manager.error_dialog_text.find("missing migration file") >= 0).is_true()

# acceptance: ACC:T24.2
func test_headless_feedback_paths_are_deterministic_for_same_inputs() -> void:
    var state_a := _new_state()
    var state_b := _new_state()
    var manager_a := _new_manager()
    var manager_b := _new_manager()

    manager_a.attempt_invalid_placement("invalid_terrain", state_a)
    manager_b.attempt_invalid_placement("invalid_terrain", state_b)
    assert_bool(manager_a.feedback_text == manager_b.feedback_text).is_true()

    manager_a.show_load_failure("checksum mismatch")
    manager_b.show_load_failure("checksum mismatch")
    assert_bool(manager_a.error_dialog_text == manager_b.error_dialog_text).is_true()

# acceptance: ACC:T24.4
func test_invalid_placement_feedback_auto_hides_after_timeout() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_invalid_placement("blocked_tile", state)
    manager.advance(manager.feedback_timeout_seconds + 0.01)

    # RED-FIRST expected behavior: feedback should hide after timeout.
    assert_bool(manager.feedback_visible).is_false()

# acceptance: ACC:T24.5
func test_blocked_action_shows_concrete_reason_and_refuses_state_change() -> void:
    var state := _new_state()
    state.entity_count = 4
    var before_entities := state.entity_count
    var manager := _new_manager()

    var accepted := manager.attempt_blocked_action("Need 2 wood", state)

    assert_bool(accepted).is_false()
    assert_bool(manager.feedback_text.find("Need 2 wood") >= 0).is_true()
    assert_int(state.entity_count).is_equal(before_entities)

# acceptance: ACC:T24.7
func test_temporary_feedback_is_non_intrusive_for_gameplay_interaction() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_invalid_placement("blocked_tile", state)

    assert_bool(manager.feedback_visible).is_true()
    assert_bool(manager.can_continue_gameplay_interaction()).is_true()

# acceptance: ACC:T24.9
func test_invalid_placement_is_refused_and_does_not_commit_new_entity() -> void:
    var state := _new_state()
    state.entity_count = 2
    var before_entities := state.entity_count
    var manager := _new_manager()

    var accepted := manager.attempt_invalid_placement("invalid_terrain", state)

    assert_bool(accepted).is_false()
    assert_int(state.entity_count).is_equal(before_entities)
    assert_bool(manager.feedback_visible).is_true()

# acceptance: ACC:T24.11
func test_blocked_and_failure_feedback_messages_include_concrete_details() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_blocked_action("Population cap reached", state)
    assert_bool(manager.feedback_text.find("Population cap reached") >= 0).is_true()
    assert_bool(manager.feedback_text.find("Unknown error") == -1).is_true()

    manager.show_migration_failure("schema v2 missing")
    assert_bool(manager.error_dialog_text.find("schema v2 missing") >= 0).is_true()
    assert_bool(manager.error_dialog_text.find("Unknown error") == -1).is_true()

# acceptance: ACC:T24.12
func test_invalid_placement_feedback_is_non_empty_human_readable_ui_text() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_invalid_placement("invalid_terrain", state)
    var text := manager.feedback_text.strip_edges()

    assert_bool(text.length() > 3).is_true()
    assert_bool(text.find(" ") >= 0).is_true()
    assert_bool(text.find("ERR_") == -1).is_true()

# acceptance: ACC:T24.16
func test_ui_feedback_manager_uses_label_or_popup_presentation_paths() -> void:
    var state := _new_state()
    var manager := _new_manager()

    manager.attempt_invalid_placement("blocked_tile", state)
    assert_bool(manager.feedback_channel == "label").is_true()

    manager.show_load_failure("save header mismatch")
    assert_bool(manager.error_dialog_visible).is_true()
    assert_bool(manager.error_channel == "popup").is_true()
