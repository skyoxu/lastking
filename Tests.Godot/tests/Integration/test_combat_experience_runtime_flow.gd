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


# ACC:T47.2
# ACC:T48.4
# ACC:T49.5
# ACC:T50.4
# ACC:T51.3
# ACC:T52.2
# ACC:T53.1
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
	assert_int(int(result.get("dead_units_retired", 0))).is_greater_equal(1)
	assert_int(int(result.get("active_combat_nodes_after_cleanup", -1))).is_equal(2)
	assert_bool(bool(result.get("dead_unit_targetable_after_cleanup", true))).is_false()

	assert_bool(bridge.has_node("Battlefield/MgTower")).is_true()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_true()
	assert_bool(bridge.has_node("Battlefield/FriendlyUnit")).is_true()
	assert_bool(bridge.has_node("Battlefield/EnemyUnit")).is_true()
	assert_bool(bridge.has_node("Battlefield/DeadEnemy")).is_false()
	assert_bool(bridge.has_node("Battlefield/Projectile")).is_false()

	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var feedback_label := _label(hud, "FeedbackLayer/FeedbackLabel")
	var outcome_label := _label(hud, "FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label := _label(hud, "FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	assert_str(pressure_label.text).contains("spawned=2")
	assert_bool(feedback_label.visible).is_true()
	assert_str(feedback_label.text).contains("Victory!")
	assert_str(outcome_label.text).contains("Outcome: win")
	assert_str(prompt_label.text).contains("reinforce frontline")
