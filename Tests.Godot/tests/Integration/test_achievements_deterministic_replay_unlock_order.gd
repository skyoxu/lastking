extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Achievements/AchievementRuntimeTestBridge.cs"
const EXPECTED_UNLOCK_IDS := ["BATTLE_STREAK", "RICH_START"]
const EXPECTED_TRIGGER_POINTS := [0, 2]

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	return bridge


func _build_events() -> Array:
	return [
		{"type": "battle_won", "value": 3},
		{"type": "battle_won", "value": 4},
		{"type": "gold_earned", "value": 120},
		{"type": "gold_earned", "value": 200}
	]

# acceptance: ACC:T27.3
func test_replay_same_inputs_unlocks_same_ids_order_and_trigger_points() -> void:
	var bridge := _new_bridge()
	var first_run = bridge.call("SimulateUnlockNotifications", _build_events()) as Dictionary
	var second_run = bridge.call("SimulateUnlockNotifications", _build_events()) as Dictionary

	assert_that(first_run.get("unlock_ids", [])).is_equal(EXPECTED_UNLOCK_IDS)
	assert_that(second_run.get("unlock_ids", [])).is_equal(EXPECTED_UNLOCK_IDS)
	assert_that(first_run.get("unlock_ids", [])).is_equal(second_run.get("unlock_ids", []))
	assert_that(first_run.get("unlock_trigger_indices", [])).is_equal(EXPECTED_TRIGGER_POINTS)
	assert_that(second_run.get("unlock_trigger_indices", [])).is_equal(EXPECTED_TRIGGER_POINTS)
	assert_that(first_run.get("unlock_trigger_indices", [])).is_equal(second_run.get("unlock_trigger_indices", []))

# acceptance: ACC:T27.6
func test_replay_does_not_add_unlocks_and_keeps_core_loop_signature_unchanged() -> void:
	var bridge := _new_bridge()
	var baseline_run = bridge.call("SimulateUnlockNotifications", _build_events()) as Dictionary
	var replay_run = bridge.call("SimulateUnlockNotifications", _build_events()) as Dictionary

	var baseline_ids: Array = baseline_run.get("unlock_ids", [])
	var replay_ids: Array = replay_run.get("unlock_ids", [])
	assert_that(replay_ids.size()).is_equal(baseline_ids.size())
	assert_that(replay_ids).is_equal(baseline_ids)
	assert_that(replay_ids).contains_exactly(["BATTLE_STREAK", "RICH_START"])
