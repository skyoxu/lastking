extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class AchievementManagerHarness extends RefCounted:
	var _day_thresholds_by_id: Dictionary
	var _unlocked: Dictionary = {}
	var unlock_notifications: Array[String] = []
	var _max_survived_day: int = 0

	func _init(day_thresholds_by_id: Dictionary) -> void:
		_day_thresholds_by_id = day_thresholds_by_id.duplicate(true)

	func record_survived_day(day_number: int) -> void:
		if day_number > _max_survived_day:
			_max_survived_day = day_number
		_evaluate_unlocks()

	func is_unlocked(achievement_id: String) -> bool:
		return bool(_unlocked.get(achievement_id, false))

	func _evaluate_unlocks() -> void:
		for achievement_id in _day_thresholds_by_id.keys():
			if is_unlocked(achievement_id):
				continue
			var required_day: int = int(_day_thresholds_by_id[achievement_id])

			if _max_survived_day >= required_day:
				_unlocked[achievement_id] = true
				unlock_notifications.append("unlock:%s" % achievement_id)


# acceptance: ACC:T27.5
func test_unlocks_and_shows_notification_when_survival_reaches_day_threshold() -> void:
	var sut := AchievementManagerHarness.new({"survive_day_15": 15})

	sut.record_survived_day(15)

	assert_that(sut.is_unlocked("survive_day_15")).is_true()
	assert_that(sut.unlock_notifications.size()).is_equal(1)
	if sut.unlock_notifications.size() == 1:
		assert_that(sut.unlock_notifications[0]).is_equal("unlock:survive_day_15")


func test_does_not_unlock_or_notify_before_day_threshold() -> void:
	var sut := AchievementManagerHarness.new({"survive_day_15": 15})

	sut.record_survived_day(14)

	assert_that(sut.is_unlocked("survive_day_15")).is_false()
	assert_that(sut.unlock_notifications.size()).is_equal(0)
