extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# RED-FIRST: this harness intentionally keeps invalid behavior so acceptance tests fail
# until production achievement UI/event wiring enforces unlock guards.
class AchievementUiHarness:
	extends RefCounted

	signal unlock_notified(achievement_id: String)

	var unlocked: Dictionary = {
		"first_win": false
	}
	var sync_calls: Array[String] = []

	func handle_unlock_request(achievement_id: String, is_currently_unlockable: bool) -> void:
		if not is_currently_unlockable:
			return
		if unlocked.has(achievement_id):
			unlocked[achievement_id] = true
			unlock_notified.emit(achievement_id)
			sync_calls.append(achievement_id)

	func handle_event(event_name: String, condition_met: bool, mapped_achievement_id: String) -> void:
		if event_name.is_empty():
			return
		if not condition_met:
			return
		if unlocked.has(mapped_achievement_id):
			unlocked[mapped_achievement_id] = true
			unlock_notified.emit(mapped_achievement_id)
			sync_calls.append(mapped_achievement_id)


class NotificationRecorder:
	extends RefCounted

	var notifications: Array[String] = []

	func record(achievement_id: String) -> void:
		notifications.append(achievement_id)


# acceptance: ACC:T27.7
func test_invalid_unlock_request_keeps_unlock_state_unchanged_and_emits_no_notification_or_sync() -> void:
	var sut: AchievementUiHarness = AchievementUiHarness.new()
	var recorder: NotificationRecorder = NotificationRecorder.new()
	sut.unlock_notified.connect(Callable(recorder, "record"))

	sut.handle_unlock_request("first_win", false)

	assert_bool(bool(sut.unlocked["first_win"])).is_false()
	assert_int(recorder.notifications.size()).is_equal(0)
	assert_int(sut.sync_calls.size()).is_equal(0)


# acceptance: ACC:T27.10
func test_event_not_meeting_condition_keeps_achievement_locked_and_emits_no_unlock_notification() -> void:
	var sut: AchievementUiHarness = AchievementUiHarness.new()
	var recorder: NotificationRecorder = NotificationRecorder.new()
	sut.unlock_notified.connect(Callable(recorder, "record"))

	sut.handle_event("enemy_defeated", false, "first_win")

	assert_bool(bool(sut.unlocked["first_win"])).is_false()
	assert_int(recorder.notifications.size()).is_equal(0)
	assert_int(sut.sync_calls.size()).is_equal(0)
