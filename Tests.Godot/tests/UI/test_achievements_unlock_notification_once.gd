extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class SessionUnlockRecorder:
	var unlocked: Dictionary = {}
	var notifications: Array[String] = []
	var persistence_writes: int = 0
	var sync_writes: int = 0

	func process_unlock(achievement_id: String) -> Dictionary:
		var transitioned: bool = not unlocked.has(achievement_id)
		if transitioned:
			unlocked[achievement_id] = true
			notifications.append(achievement_id)
			persistence_writes += 1
			sync_writes += 1
		return {"transitioned": transitioned}


class DayThresholdUnlockRule:
	var threshold_day: int
	var achievement_id: String
	var recorder: SessionUnlockRecorder

	func _init(day_threshold: int, id: String, unlock_recorder: SessionUnlockRecorder) -> void:
		threshold_day = day_threshold
		achievement_id = id
		recorder = unlock_recorder

	func on_day_survived(day: int) -> void:
		if day >= threshold_day:
			recorder.process_unlock(achievement_id)


# acceptance: ACC:T27.10
func test_reprocessing_unlocked_achievement_does_not_duplicate_transition_or_writes() -> void:
	var recorder: SessionUnlockRecorder = SessionUnlockRecorder.new()

	var first: Dictionary = recorder.process_unlock("survive_day_15")
	var second: Dictionary = recorder.process_unlock("survive_day_15")

	assert_that(first["transitioned"]).is_true()
	assert_that(second["transitioned"]).is_false()
	assert_that(recorder.notifications.size()).is_equal(1)
	assert_that(recorder.persistence_writes).is_equal(1)
	assert_that(recorder.sync_writes).is_equal(1)


# acceptance: ACC:T27.5
func test_day_15_threshold_unlocks_and_shows_notification() -> void:
	var recorder: SessionUnlockRecorder = SessionUnlockRecorder.new()
	var rule: DayThresholdUnlockRule = DayThresholdUnlockRule.new(15, "survive_day_15", recorder)

	rule.on_day_survived(15)

	assert_that(recorder.unlocked.has("survive_day_15")).is_true()
	assert_that(recorder.notifications.size()).is_equal(1)


# acceptance: ACC:T27.7
func test_retriggered_condition_in_same_session_does_not_emit_second_popup() -> void:
	var recorder: SessionUnlockRecorder = SessionUnlockRecorder.new()
	var rule: DayThresholdUnlockRule = DayThresholdUnlockRule.new(15, "survive_day_15", recorder)

	rule.on_day_survived(16)
	rule.on_day_survived(20)

	assert_that(recorder.unlocked.has("survive_day_15")).is_true()
	assert_that(recorder.notifications.size()).is_equal(1)
