extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const COMBAT_EXPERIENCE_BRIDGE := "res://Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs"

func test_training_should_consume_runtime_resources_instead_of_only_spawning_unit() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()

	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame

	bridge.call("ResetForInteractiveRun")
	var before_summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(before_summary.get("resource_gold", -1))).is_equal(120)

	bridge.call("TrainFriendlyUnitPhase")
	var after_summary: Dictionary = bridge.call("GetSummary")

	assert_int(int(after_summary.get("friendly_units_deployed", 0))).is_equal(1)
	assert_int(int(after_summary.get("resource_gold", -1))).is_less(int(before_summary.get("resource_gold", -1)))

func test_publish_outcome_should_not_reset_runtime_resources_after_build_and_training_spend() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()

	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame

	bridge.call("ResetForInteractiveRun")
	bridge.call("PlaceBuildingAtSlot", "tower_alpha", "InnerCastleRegionSlot_03_00")
	bridge.call("TrainFriendlyUnitPhase")
	var spent_summary: Dictionary = bridge.call("GetSummary")
	var spent_gold := int(spent_summary.get("resource_gold", -1))
	assert_int(spent_gold).is_less(120)

	bridge.call("PublishOutcomePhase")
	var after_outcome: Dictionary = bridge.call("GetSummary")
	assert_int(int(after_outcome.get("resource_gold", -1))).is_equal(spent_gold)

func test_wall_breach_should_end_enemy_advance_before_castle_hp_drops_after_breach() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()

	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame

	bridge.call("ResetForInteractiveRun")
	bridge.call("ConfigureDurabilityForTest", 100, 5)
	bridge.call("SpawnEnemyWavePhase")

	for _i in range(120):
		bridge.call("AdvanceSimulation", 0.1)
		var current: Dictionary = bridge.call("GetSummary")
		if str(current.get("defeat_reason", "")) == "wall_breached":
			break

	var breached_summary: Dictionary = bridge.call("GetSummary")
	assert_str(str(breached_summary.get("defeat_reason", ""))).is_equal("wall_breached")
	assert_int(int(breached_summary.get("castle_hp", -1))).is_equal(100)

	for _i in range(200):
		bridge.call("AdvanceSimulation", 0.1)

	var after_breach: Dictionary = bridge.call("GetSummary")
	assert_str(str(after_breach.get("defeat_reason", ""))).is_equal("wall_breached")
	assert_int(int(after_breach.get("castle_hp", -1))).is_equal(100)
