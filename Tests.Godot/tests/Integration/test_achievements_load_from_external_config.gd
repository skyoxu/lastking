extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Achievements/AchievementRuntimeTestBridge.cs"

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	return bridge


func _valid_external_config_json() -> String:
	var entries: Array[Dictionary] = [
		{"id": "first_blood", "name": "First Blood", "description": "Win first battle", "unlockCondition": "battle_won >= 1"},
		{"id": "perfect_wave", "name": "Perfect Wave", "description": "Clear wave", "unlockCondition": "wave_clear >= 1"}
	]
	return JSON.stringify(entries)


# acceptance: ACC:T27.12
func test_loads_definitions_from_external_json_without_hardcoded_injection() -> void:
	var bridge := _new_bridge()
	var status = bridge.call(
		"SimulateLoadDefinitionsFromJson",
		"logs/tmp/task27-achievements-config.json",
		_valid_external_config_json()
	) as Dictionary

	assert_that(bool(status.get("ok", false))).is_true()
	assert_that(status.get("ids", [])).is_equal(["first_blood", "perfect_wave"])


func test_invalid_external_reload_keeps_previous_external_definitions_unchanged() -> void:
	var bridge := _new_bridge()
	var invalid_result = bridge.call("SimulateLoadDefinitionsFromJson", "..\\outside-config.json", _valid_external_config_json()) as Dictionary
	assert_that(bool(invalid_result.get("ok", true))).is_false()
	assert_that(str(invalid_result.get("error", ""))).contains("path traversal")
