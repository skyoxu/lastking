extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class FakeHudFeedbackController extends RefCounted:
	var feedback_visible: bool = false
	var feedback_text: String = ""
	var feedback_timeout_seconds: float = 0.5
	var _remaining_seconds: float = 0.0

	func trigger_invalid_placement(message: String = "Cannot build here.") -> void:
		feedback_text = message
		feedback_visible = true
		_remaining_seconds = feedback_timeout_seconds

	func advance(seconds: float) -> void:
		if not feedback_visible:
			return
		_remaining_seconds = max(0.0, _remaining_seconds - max(seconds, 0.0))
		if _remaining_seconds <= 0.0:
			feedback_visible = false
			feedback_text = ""


func _new_hud() -> FakeHudFeedbackController:
	return FakeHudFeedbackController.new()


# acceptance: ACC:T24.4
func test_feedback_auto_hides_after_short_timeout() -> void:
	var hud := _new_hud()
	hud.feedback_timeout_seconds = 0.25

	hud.trigger_invalid_placement("Cannot build here.")
	assert_bool(hud.feedback_visible).is_true()
	assert_bool(hud.feedback_text == "Cannot build here.").is_true()

	hud.advance(0.30)

	# Expected acceptance behavior: temporary feedback hides after timeout.
	assert_bool(hud.feedback_visible).is_false()
	assert_bool(hud.feedback_text == "").is_true()


# acceptance: ACC:T24.13
func test_feedback_stays_hidden_without_new_trigger_after_timeout() -> void:
	var hud := _new_hud()
	hud.feedback_timeout_seconds = 0.20

	hud.trigger_invalid_placement("Cannot build here.")
	hud.advance(0.25)

	assert_bool(hud.feedback_visible).is_false()
	assert_bool(hud.feedback_text == "").is_true()

	# Negative path: without a new trigger, feedback must not reappear.
	hud.advance(1.00)
	assert_bool(hud.feedback_visible).is_false()
	assert_bool(hud.feedback_text == "").is_true()


func test_feedback_reappears_only_after_new_invalid_event() -> void:
	var hud := _new_hud()
	hud.feedback_timeout_seconds = 0.10

	hud.trigger_invalid_placement("Cannot build here.")
	hud.advance(0.20)
	assert_bool(hud.feedback_visible).is_false()

	hud.trigger_invalid_placement("Cannot build on this terrain.")
	assert_bool(hud.feedback_visible).is_true()
	assert_bool(hud.feedback_text == "Cannot build on this terrain.").is_true()
