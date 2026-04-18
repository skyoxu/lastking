extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Achievements/AchievementRuntimeTestBridge.cs"

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	var bridge = script.new()
	add_child(auto_free(bridge))
	return bridge


# acceptance: ACC:T27.8
func test_first_time_unlock_syncs_to_steam_when_integration_is_active() -> void:
	var bridge := _new_bridge()
	var result = bridge.call(
		"SimulateSteamSync",
		true,
		"session-adapter",
		["ACH_WIN_FIRST_BATTLE", "ACH_WIN_FIRST_BATTLE"]
	) as Dictionary
	var sync_ids: Array = result.get("steam_sync_ids", [])
	assert_int(int(result.get("steam_sync_count", 0))).is_equal(1)
	assert_that(sync_ids).is_equal(["ACH_WIN_FIRST_BATTLE"])


func test_steam_sync_ignores_unlocks_when_integration_is_inactive() -> void:
	var bridge := _new_bridge()
	var result = bridge.call(
		"SimulateSteamSync",
		false,
		"session-adapter-disabled",
		["ACH_WIN_FIRST_BATTLE"]
	) as Dictionary
	assert_int(int(result.get("steam_sync_count", 1))).is_equal(0)
