extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


const COMBAT_EXPERIENCE_BRIDGE := "res://Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs"

var _bus: Node

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))


func _hud() -> Node:
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud


func _label(hud: Node, path: String) -> Label:
	return hud.get_node(path) as Label


func _battlefield_children_with_prefix(bridge: Node, prefix: String) -> Array[String]:
	var names: Array[String] = []
	var battlefield := bridge.get_node("Battlefield")
	for child in battlefield.get_children():
		var child_name := str(child.name)
		if child_name.begins_with(prefix):
			names.append(child_name)
	return names


func _assert_battlefield_actors_exact(bridge: Node, prefix: String, expected_names: Array[String]) -> void:
	var names := _battlefield_children_with_prefix(bridge, prefix)
	assert_int(names.size()).is_equal(expected_names.size())
	for i in range(expected_names.size()):
		assert_str(names[i]).is_equal(expected_names[i])


# ACC:T47.2
# ACC:T48.4
# ACC:T49.5
# ACC:T50.4
# ACC:T51.3
# ACC:T52.2
# ACC:T53.1
# ACC:T56.7
func test_player_visible_combat_experience_runs_from_building_and_training_to_death_cleanup_and_summary() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()

	var hud := await _hud()
	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame

	var result: Dictionary = bridge.call("RunCompleteCombatExperienceForTest")
	await get_tree().process_frame

	assert_bool(bool(result.get("mg_tower_built", false))).is_true()
	assert_bool(bool(result.get("barracks_built", false))).is_true()
	assert_int(int(result.get("friendly_units_deployed", 0))).is_greater_equal(1)
	assert_int(int(result.get("enemy_units_spawned", 0))).is_greater_equal(1)
	assert_int(int(result.get("projectiles_created", 0))).is_greater_equal(1)
	assert_int(int(result.get("combat_exchanges", 0))).is_greater_equal(1)
	assert_int(int(result.get("dead_units_retired", 0))).is_equal(0)
	assert_int(int(result.get("active_combat_nodes_after_cleanup", -1))).is_equal(3)
	assert_bool(bool(result.get("dead_unit_targetable_after_cleanup", true))).is_false()

	assert_bool(bridge.has_node("Battlefield/MgTower")).is_true()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_true()
	_assert_battlefield_actors_exact(bridge, "FriendlyUnit", ["FriendlyUnit1"])
	_assert_battlefield_actors_exact(bridge, "EnemyUnit", ["EnemyUnit1", "EnemyUnit2"])
	assert_bool(bridge.has_node("Battlefield/DeadEnemy")).is_false()
	assert_bool(bridge.has_node("Battlefield/Projectile")).is_false()

	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var feedback_label := _label(hud, "FeedbackLayer/FeedbackLabel")
	var outcome_label := _label(hud, "FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label := _label(hud, "FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	assert_str(pressure_label.text).contains("hp=42")
	assert_bool(feedback_label.visible).is_true()
	assert_str(feedback_label.text.to_lower()).contains("victory")
	assert_str(outcome_label.text).contains("Outcome: win")
	assert_str(prompt_label.text).contains("reinforce frontline")


# ACC:T56.7
func test_runtime_bridge_entrypoints_remain_reachable_after_ownership_isolation() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge := screen.get_node_or_null("CombatExperienceRuntimeBridge")
	var wave_timer := screen.get_node_or_null("WaveTimer")
	assert_object(bridge).is_not_null()
	assert_object(wave_timer).is_not_null()
	assert_bool(bridge.has_method("ResetForInteractiveRun")).is_true()
	assert_bool(bridge.has_method("BuildPhase")).is_true()
	assert_bool(bridge.has_method("SpawnEnemyWavePhase")).is_true()
	assert_bool(bridge.has_method("ResolveCombatExchangePhase")).is_true()
	assert_bool(bridge.has_method("CleanupDeadUnitsPhase")).is_true()
	assert_bool(bridge.has_method("PublishOutcomePhase")).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
	assert_bool(bridge.has_method("GetActorSnapshots")).is_true()
	assert_bool(bridge.has_method("AdvanceSimulation")).is_true()
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(wave_timer.get_meta("ownership_container"))).is_equal("runtime_bridge")

	bridge.call("ResetForInteractiveRun")
	bridge.call("BuildPhase")
	bridge.call("TrainFriendlyUnitPhase")
	bridge.call("SpawnEnemyWavePhase")
	bridge.call("ResolveCombatExchangePhase")
	bridge.call("CleanupDeadUnitsPhase")
	bridge.call("PublishOutcomePhase")
	await get_tree().process_frame

	var summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary.get("friendly_units_deployed", 0))).is_greater_equal(1)
	assert_int(int(summary.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(summary.get("combat_exchanges", 0))).is_greater_equal(1)
	assert_int(int(summary.get("active_combat_nodes_after_cleanup", -1))).is_equal(3)
	assert_bool(bool(summary.get("dead_unit_targetable_after_cleanup", true))).is_false()
	_assert_battlefield_actors_exact(bridge, "FriendlyUnit", ["FriendlyUnit1"])
	_assert_battlefield_actors_exact(bridge, "EnemyUnit", ["EnemyUnit1", "EnemyUnit2"])
	assert_bool(bridge.has_node("Battlefield/MgTower")).is_true()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_true()
