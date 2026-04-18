extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# RED-FIRST: this harness intentionally contains wrong status mapping
# so acceptance behavior fails deterministically until production code is fixed.
class SessionStartAchievementsViewModel:
	var _definitions: Array

	func _init(definitions: Array) -> void:
		_definitions = definitions

	func build_rows(local_state: Dictionary) -> Array:
		var rows: Array = []
		for definition_value in _definitions:
			var definition: Dictionary = definition_value
			var achievement_id: String = str(definition.get("id", ""))
			var is_unlocked: bool = bool(local_state.get(achievement_id, false))
			rows.append({
				"id": achievement_id,
				"unlocked": is_unlocked,
				"hidden": false,
			})
		return rows


func _definitions_fixture() -> Array:
	return [
		{"id": "first_win", "name": "First Victory"},
		{"id": "collector", "name": "Collector"},
		{"id": "speed_runner", "name": "Speed Runner"},
	]


func _find_row(rows: Array, achievement_id: String) -> Dictionary:
	for row_value in rows:
		var row: Dictionary = row_value
		if str(row.get("id", "")) == achievement_id:
			return row
	return {}


# acceptance: ACC:T27.9
func test_session_start_rows_reflect_local_lock_and_unlock_state() -> void:
	var sut: SessionStartAchievementsViewModel = SessionStartAchievementsViewModel.new(_definitions_fixture())
	var rows: Array = sut.build_rows({
		"first_win": true,
		"collector": false,
		"speed_runner": false,
	})

	assert_that(rows.size()).is_equal(3)

	var first_win: Dictionary = _find_row(rows, "first_win")
	var collector: Dictionary = _find_row(rows, "collector")
	assert_that(first_win.is_empty()).is_false()
	assert_that(collector.is_empty()).is_false()
	assert_bool(bool(first_win.get("unlocked", false))).is_true()
	assert_bool(bool(collector.get("unlocked", true))).is_false()


func test_session_start_rows_do_not_hide_any_listed_achievement() -> void:
	var sut: SessionStartAchievementsViewModel = SessionStartAchievementsViewModel.new(_definitions_fixture())
	var rows: Array = sut.build_rows({})

	assert_that(rows.size()).is_equal(3)
	for row_value in rows:
		var row: Dictionary = row_value
		assert_bool(bool(row.get("hidden", true))).is_false()
