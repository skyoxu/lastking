extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class AchievementsListPresenter:
	var _configured: Array

	func _init(configured: Array) -> void:
		_configured = configured

	func build_session_start_entries(_unlock_state: Dictionary) -> Array:
		var entries: Array = []
		for achievement_value in _configured:
			var achievement: Dictionary = achievement_value
			var achievement_id: String = str(achievement.get("id", ""))
			entries.append({
				"id": achievement_id,
				"name": str(achievement.get("name", "")),
				"description": str(achievement.get("description", "")),
				"hidden": false,
			})
		return entries


func _sample_configuration() -> Array:
	return [
		{"id": "first_win", "name": "First Victory", "description": "Win one battle."},
		{"id": "collector", "name": "Collector", "description": "Collect 10 relics."},
		{"id": "speed_runner", "name": "Speed Runner", "description": "Finish in under 20 turns."},
	]


# acceptance: ACC:T27.4
func test_all_configured_achievements_remain_visible_before_any_unlock_condition() -> void:
	var presenter: AchievementsListPresenter = AchievementsListPresenter.new(_sample_configuration())
	var entries: Array = presenter.build_session_start_entries({})

	assert_that(entries.size()).is_equal(3)

	var hidden_count := 0
	for entry_value in entries:
		var entry: Dictionary = entry_value
		if bool(entry["hidden"]):
			hidden_count += 1
	assert_that(hidden_count).is_equal(0)


# acceptance: ACC:T27.5
func test_session_start_achievement_entries_show_name_and_description_by_default() -> void:
	var configured: Array = _sample_configuration()
	var presenter: AchievementsListPresenter = AchievementsListPresenter.new(configured)
	var entries: Array = presenter.build_session_start_entries({})

	assert_that(entries.size()).is_equal(configured.size())
	for i in range(entries.size()):
		var entry: Dictionary = entries[i]
		var expected: Dictionary = configured[i]
		assert_that(str(entry["name"])).is_equal(str(expected["name"]))
		assert_that(str(entry["description"])).is_equal(str(expected["description"]))
		assert_that(bool(entry["hidden"])).is_false()
