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


func _assert_summary_has_required_keys(summary: Dictionary) -> void:
	var required_keys := [
		"friendly_units_deployed",
		"enemy_units_spawned",
		"combat_exchanges",
		"dead_units_retired",
		"active_combat_nodes_after_cleanup",
		"castle_hp",
	]
	for key_variant in required_keys:
		var key := str(key_variant)
		assert_bool(summary.has(key)).is_true()


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


# ACC:T57.2

func test_battle_map_control_actions_should_delegate_through_runtime_bridge_and_keep_summary_machine_resolvable() -> void:
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
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	var summary_before: Dictionary = bridge.call("GetSummary")
	_assert_summary_has_required_keys(summary_before)
	var before_enemy := int(summary_before.get("enemy_units_spawned", 0))
	var before_exchanges := int(summary_before.get("combat_exchanges", 0))
	var before_retired := int(summary_before.get("dead_units_retired", 0))

	# Negative path: out-of-order actions should be blocked and not mutate bridge summary counters.
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await get_tree().process_frame
	var summary_blocked: Dictionary = bridge.call("GetSummary")
	_assert_summary_has_required_keys(summary_blocked)
	assert_int(int(summary_blocked.get("enemy_units_spawned", 0))).is_equal(before_enemy)
	assert_int(int(summary_blocked.get("combat_exchanges", 0))).is_equal(before_exchanges)
	assert_int(int(summary_blocked.get("dead_units_retired", 0))).is_equal(before_retired)

	# Positive path: after wave spawn, exchange/cleanup/finish should drive bridge state transitions.
	wave_btn.emit_signal("pressed")
	await get_tree().process_frame
	exchange_btn.emit_signal("pressed")
	await get_tree().process_frame
	cleanup_btn.emit_signal("pressed")
	await get_tree().process_frame
	finish_btn.emit_signal("pressed")
	await get_tree().process_frame
	var summary_after: Dictionary = bridge.call("GetSummary")
	_assert_summary_has_required_keys(summary_after)
	assert_int(int(summary_after.get("enemy_units_spawned", 0))).is_greater_equal(before_enemy + 1)
	assert_int(int(summary_after.get("combat_exchanges", 0))).is_greater_equal(before_exchanges + 1)
	assert_int(int(summary_after.get("dead_units_retired", 0))).is_greater_equal(before_retired)
	assert_bool(bool(summary_after.get("mg_tower_built", true))).is_false()
	assert_bool(bool(summary_after.get("barracks_built", true))).is_false()
	assert_int(int(summary_after.get("friendly_units_deployed", -1))).is_equal(0)
	assert_bool(bridge.has_node("Battlefield/MgTower")).is_false()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_false()
	assert_int(int(summary_after.get("enemy_units_spawned", 0))).is_equal(before_enemy + 2)

	# Coordinator-only guarantee: root handler delegates to bridge and never triggers the full fallback flow.
	assert_bool(bridge.has_method("RunCompleteCombatExperienceForTest")).is_true()
	assert_bool(bridge.has_node("Battlefield/MgTower")).is_false()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_false()
	var fallback_summary: Dictionary = bridge.call("RunCompleteCombatExperienceForTest")
	_assert_summary_has_required_keys(fallback_summary)
	assert_bool(bridge.has_node("Battlefield/MgTower")).is_true()
	assert_bool(bridge.has_node("Battlefield/Barracks")).is_true()
	assert_int(int(fallback_summary.get("enemy_units_spawned", 0))).is_equal(2)
	assert_int(int(fallback_summary.get("combat_exchanges", 0))).is_greater_equal(1)


# ACC:T62.2
func test_path_readability_is_expressed_through_enemy_actor_view_motion() -> void:
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
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var background: Node = screen.get_node("Background")
	var path: Line2D = screen.get_node("Background/Path")

	assert_bool(bridge.has_method("GetActorSnapshots")).is_true()
	assert_bool(bridge.has_method("AdvanceSimulation")).is_true()
	assert_bool(path.visible).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)

	bridge.call("SpawnEnemyWavePhase")
	await get_tree().process_frame
	var snapshots_before: Array = bridge.call("GetActorSnapshots")
	var before_progress := {}
	for item in snapshots_before:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		if not bool(snapshot.get("is_moving_enemy", false)):
			continue
		var actor_name := String(snapshot.get("name", ""))
		before_progress[actor_name] = float(snapshot.get("path_progress", 0.0))
	for _i in range(6):
		bridge.call("AdvanceSimulation", 0.2)
	var snapshots_after: Array = bridge.call("GetActorSnapshots")
	assert_int(snapshots_after.size()).is_equal(snapshots_before.size())
	var found_progress := false
	for item in snapshots_after:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		if not bool(snapshot.get("is_moving_enemy", false)):
			continue
		var actor_name := String(snapshot.get("name", ""))
		var after_progress := float(snapshot.get("path_progress", 0.0))
		if before_progress.has(actor_name):
			assert_bool(after_progress >= float(before_progress[actor_name])).is_true()
		if after_progress > 0.0:
			found_progress = true
	assert_bool(found_progress).is_true()


# ACC:T62.11
# ACC:T62.12
func test_spawn_cues_and_path_readability_survive_full_battle_loop() -> void:
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
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var background: Node = screen.get_node("Background")
	var spawn_a: ColorRect = screen.get_node("Background/EnemySpawnA")
	var spawn_b: ColorRect = screen.get_node("Background/EnemySpawnB")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	var weak_a := spawn_a.color.a
	var weak_b := spawn_b.color.a
	wave_btn.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(spawn_a.color.a > weak_a and spawn_b.color.a > weak_b).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)
	assert_int(int(bridge.call("GetActorSnapshots").size())).is_greater_equal(1)

	exchange_btn.emit_signal("pressed")
	await get_tree().process_frame
	cleanup_btn.emit_signal("pressed")
	await get_tree().process_frame
	finish_btn.emit_signal("pressed")
	await get_tree().process_frame
	for _i in range(24):
		await get_tree().process_frame

	assert_bool(spawn_a.color.a <= weak_a + 0.05 and spawn_b.color.a <= weak_b + 0.05).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)

	var summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(summary.get("combat_exchanges", 0))).is_greater_equal(1)
