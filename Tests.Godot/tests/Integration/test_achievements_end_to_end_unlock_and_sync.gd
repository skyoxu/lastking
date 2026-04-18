extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Achievements/AchievementRuntimeTestBridge.cs"

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	return bridge


func _default_definitions() -> Array:
	return [
		{
			"id": "city_builder",
			"required_enemy_defeats": 2,
			"required_gold": 0,
			"is_hidden": false,
		},
		{
			"id": "turn_runner",
			"required_enemy_defeats": 0,
			"required_gold": 3,
			"is_hidden": false,
		},
	]


func _find_row(rows: Array, achievement_id: String) -> Dictionary:
	for row_value in rows:
		var row: Dictionary = row_value
		if str(row.get("id", "")) == achievement_id:
			return row
	return {}


# acceptance: ACC:T27.1
func test_unlocks_only_when_deterministic_conditions_are_met() -> void:
	var bridge := _new_bridge()
	var result = bridge.call(
		"EvaluateVisibilityAndUnlock",
		_default_definitions(),
		[
			{"type": "enemy_defeated", "value": 1},
			{"type": "enemy_defeated", "value": 1}
		]
	) as Dictionary
	var rows: Array = result.get("rows", [])

	var city_builder := _find_row(rows, "city_builder")
	var turn_runner := _find_row(rows, "turn_runner")
	assert_bool(bool(city_builder.get("unlocked", false))).is_true()
	assert_bool(bool(turn_runner.get("unlocked", false))).is_false()


# acceptance: ACC:T27.2
func test_repeatable_action_sequence_produces_same_unlock_outcome() -> void:
	var bridge := _new_bridge()
	var events: Array = [
		{"type": "enemy_defeated", "value": 1},
		{"type": "enemy_defeated", "value": 1},
		{"type": "gold_earned", "value": 3}
	]
	var first = bridge.call("EvaluateVisibilityAndUnlock", _default_definitions(), events) as Dictionary
	var second = bridge.call("EvaluateVisibilityAndUnlock", _default_definitions(), events) as Dictionary

	assert_that(first.get("rows", [])).is_equal(second.get("rows", []))


# acceptance: ACC:T27.8
func test_steam_enabled_syncs_and_disabled_still_updates_local_ui_flow() -> void:
	var bridge := _new_bridge()
	var enabled = bridge.call(
		"SimulateSteamSync",
		true,
		"session-a",
		["city_builder", "city_builder"]
	) as Dictionary
	var disabled = bridge.call(
		"SimulateSteamSync",
		false,
		"session-b",
		["city_builder"]
	) as Dictionary

	assert_int(int(enabled.get("steam_sync_count", 0))).is_equal(1)
	assert_int(int(disabled.get("steam_sync_count", 1))).is_equal(0)


# acceptance: ACC:T27.13
func test_achievement_list_reflects_locked_and_unlocked_state() -> void:
	var bridge := _new_bridge()
	var initial = bridge.call("BuildSessionStartRows", _default_definitions()) as Dictionary
	var initial_rows: Array = initial.get("rows", [])
	var before_city_builder: Dictionary = _find_row(initial_rows, "city_builder")
	assert_that(before_city_builder.is_empty()).is_false()
	assert_that(bool(before_city_builder.get("unlocked", true))).is_false()

	var updated = bridge.call(
		"EvaluateVisibilityAndUnlock",
		_default_definitions(),
		[
			{"type": "enemy_defeated", "value": 1},
			{"type": "enemy_defeated", "value": 1}
		]
	) as Dictionary
	var updated_rows: Array = updated.get("rows", [])
	var after_city_builder: Dictionary = _find_row(updated_rows, "city_builder")
	assert_that(after_city_builder.is_empty()).is_false()
	assert_that(bool(after_city_builder.get("unlocked", false))).is_true()
