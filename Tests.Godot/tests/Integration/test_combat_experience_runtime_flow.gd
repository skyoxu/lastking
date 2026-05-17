extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const COMBAT_EXPERIENCE_BRIDGE := "res://Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs"
const _SETTINGS_CFG_PATH := "user://settings.cfg"

var _bus: Node
var _damage_numbers_snapshot_pending := false
var _damage_numbers_snapshot := {}

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))


func after() -> void:
	if _damage_numbers_snapshot_pending:
		_restore_damage_numbers_setting(_damage_numbers_snapshot)
		_damage_numbers_snapshot_pending = false
		_damage_numbers_snapshot = {}


func _request_hud_action(screen: Control, action_code: String) -> void:
	var hud: Node = screen.get_node("BattleHud")
	hud.call("RequestBattleAction", action_code)

func _hud() -> Node:
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud


func _label(hud: Node, path: String) -> Label:
	return hud.get_node(path) as Label


func _emit_castle_hp(bus: Node, current_hp: int, previous_hp: int = 100) -> void:
	var payload := "{\"Day\":9,\"PreviousHp\":%d,\"CurrentHp\":%d}" % [previous_hp, current_hp]
	bus.call("PublishSimple", "core.lastking.castle.hp_changed", "ut", payload)


func _pressure_state_from_label_text(text: String) -> String:
	var lowered := text.to_lower()
	if lowered.find("critical") >= 0:
		return "critical"
	if lowered.find("danger") >= 0:
		return "danger"
	if lowered.find("warning") >= 0:
		return "warning"
	if lowered.find("stable") >= 0:
		return "stable"
	return "unknown"


func _assert_pressure_state_exact(hud: Node, bus: Node, hp: int, expected_state: String) -> void:
	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	_emit_castle_hp(bus, hp)
	for _i in range(6):
		await get_tree().process_frame
	var text := pressure_label.text.to_lower()
	assert_str(_pressure_state_from_label_text(text)).is_equal(expected_state)
	assert_bool(text.find("critical") >= 0 if expected_state == "critical" else text.find("critical") < 0).is_true()
	assert_bool(text.find("danger") >= 0 if expected_state == "danger" else text.find("danger") < 0).is_true()
	assert_bool(text.find("warning") >= 0 if expected_state == "warning" else text.find("warning") < 0).is_true()
	assert_bool(text.find("stable") >= 0 if expected_state == "stable" else text.find("stable") < 0).is_true()


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


func _write_damage_numbers_setting(enabled: bool) -> void:
	var cfg := ConfigFile.new()
	cfg.load(_SETTINGS_CFG_PATH)
	cfg.set_value("settings", "combat_damage_numbers_enabled", enabled)
	var err := cfg.save(_SETTINGS_CFG_PATH)
	assert_int(int(err)).is_equal(int(OK))


func _snapshot_damage_numbers_setting() -> Dictionary:
	var cfg := ConfigFile.new()
	var result := {
		"had_primary": false,
		"primary_value": true,
		"had_legacy": false,
		"legacy_value": true,
	}
	var err := cfg.load(_SETTINGS_CFG_PATH)
	if err != OK and err != ERR_FILE_NOT_FOUND:
		return result
	if cfg.has_section_key("settings", "combat_damage_numbers_enabled"):
		result["had_primary"] = true
		result["primary_value"] = bool(cfg.get_value("settings", "combat_damage_numbers_enabled", true))
	if cfg.has_section_key("settings", "damage_numbers_enabled"):
		result["had_legacy"] = true
		result["legacy_value"] = bool(cfg.get_value("settings", "damage_numbers_enabled", true))
	return result


func _restore_damage_numbers_setting(snapshot: Dictionary) -> void:
	var cfg := ConfigFile.new()
	cfg.load(_SETTINGS_CFG_PATH)
	if bool(snapshot.get("had_primary", false)):
		cfg.set_value("settings", "combat_damage_numbers_enabled", bool(snapshot.get("primary_value", true)))
	else:
		if cfg.has_section_key("settings", "combat_damage_numbers_enabled"):
			cfg.erase_section_key("settings", "combat_damage_numbers_enabled")
	if bool(snapshot.get("had_legacy", false)):
		cfg.set_value("settings", "damage_numbers_enabled", bool(snapshot.get("legacy_value", true)))
	else:
		if cfg.has_section_key("settings", "damage_numbers_enabled"):
			cfg.erase_section_key("settings", "damage_numbers_enabled")
	var err := cfg.save(_SETTINGS_CFG_PATH)
	assert_int(int(err)).is_equal(int(OK))


# ACC:T47.2
# ACC:T48.4
# ACC:T49.5
# ACC:T50.4
# ACC:T51.3
# ACC:T52.2
# ACC:T53.1
# ACC:T56.7
# ACC:T63.1
# ACC:T63.2
# ACC:T63.4
# ACC:T63.5
# ACC:T63.6
# ACC:T63.8
# ACC:T64.1
# ACC:T64.7
# ACC:T64.8
# ACC:T64.9
# ACC:T64.10
# ACC:T64.11
func test_player_visible_combat_experience_runs_from_building_and_training_to_death_cleanup_and_summary() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()

	var hud := await _hud()
	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame
	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var feedback_label := _label(hud, "FeedbackLayer/FeedbackLabel")
	var pressure_panel: PanelContainer = hud.get_node("FeedbackLayer/PressurePanel")
	var outcome_label := _label(hud, "FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label := _label(hud, "FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")
	var hit_flash_visible := feedback_label.visible
	var wall_pressure_emphasis_active := pressure_label.text.to_lower().find("high") >= 0 or pressure_label.text.to_lower().find("critical") >= 0

	# Negative path baseline: without combat trigger, local feedback stays in neutral state.
	assert_str(pressure_label.text.to_lower()).contains("n/a")
	assert_bool(pressure_label.text.to_lower().find("critical") < 0).is_true()
	assert_bool(pressure_label.text.to_lower().find("high") < 0).is_true()
	assert_bool(pressure_panel.visible).is_false()
	assert_str(prompt_label.text.to_lower()).contains("n/a")
	assert_str(outcome_label.text.to_lower()).contains("n/a")
	assert_bool(feedback_label.visible).is_false()
	assert_str(feedback_label.text).is_equal("")
	assert_bool(hit_flash_visible).is_false()
	assert_bool(wall_pressure_emphasis_active).is_false()

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

	assert_str(pressure_label.text).contains("hp=42")
	assert_bool(
		pressure_label.text.to_lower().find("warning") >= 0
		or pressure_label.text.to_lower().find("danger") >= 0
		or pressure_label.text.to_lower().find("critical") >= 0
	).is_true()
	assert_bool(pressure_label.text.to_lower().find("n/a") < 0).is_true()
	assert_bool(pressure_panel.visible).is_true()
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.length() > 0).is_true()
	hit_flash_visible = feedback_label.visible
	wall_pressure_emphasis_active = (
		pressure_label.text.to_lower().find("warning") >= 0
		or pressure_label.text.to_lower().find("danger") >= 0
		or pressure_label.text.to_lower().find("critical") >= 0
	)
	assert_bool(hit_flash_visible).is_true()
	assert_bool(wall_pressure_emphasis_active).is_true()
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=9") >= 0).is_true()
	assert_str(outcome_label.text.to_lower()).contains("n/a")
	assert_str(prompt_label.text.to_lower()).contains("n/a")


# ACC:T56.7
# ACC:T64.2
# ACC:T67.2
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
# ACC:T64.3
# ACC:T67.3

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
				
	var summary_before: Dictionary = bridge.call("GetSummary")
	_assert_summary_has_required_keys(summary_before)
	var before_enemy := int(summary_before.get("enemy_units_spawned", 0))
	var before_exchanges := int(summary_before.get("combat_exchanges", 0))
	var before_retired := int(summary_before.get("dead_units_retired", 0))

	# Negative path: out-of-order actions should be blocked and not mutate bridge summary counters.
	_request_hud_action(screen, "exchange")
	_request_hud_action(screen, "cleanup")
	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	var summary_blocked: Dictionary = bridge.call("GetSummary")
	_assert_summary_has_required_keys(summary_blocked)
	assert_int(int(summary_blocked.get("enemy_units_spawned", 0))).is_equal(before_enemy)
	assert_int(int(summary_blocked.get("combat_exchanges", 0))).is_equal(before_exchanges)
	assert_int(int(summary_blocked.get("dead_units_retired", 0))).is_equal(before_retired)

	# Positive path: after wave spawn, exchange/cleanup/finish should drive bridge state transitions.
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
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
# ACC:T64.4
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
	var path: Line2D = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/Path")

	assert_bool(bridge.has_method("GetActorSnapshots")).is_true()
	assert_bool(bridge.has_method("AdvanceSimulation")).is_true()
	assert_bool(path.visible).is_false()
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
# ACC:T63.1
# ACC:T63.2
# ACC:T63.4
# ACC:T63.5
# ACC:T63.6
# ACC:T63.8
# ACC:T64.5
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
	var spawn_a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var spawn_b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")

	# ACC:T63.4 ownership boundary remains unchanged when local feedback is enabled.
	assert_str(str(screen.get_node("Background").get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(screen.get_node("WaveTimer").get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(screen.get_node("LegacyPrototypeRoot").get_meta("ownership_container"))).is_equal("legacy_prototype")

	# ACC:T63.5 layer boundary remains observable through adapter->bridge calls.
	assert_bool(bridge.has_method("SpawnEnemyWavePhase")).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
	var summary_before: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary_before.get("enemy_units_spawned", 0))).is_equal(0)

	var weak_a := spawn_a.color.a
	var weak_b := spawn_b.color.a
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	var summary_after_wave: Dictionary = bridge.call("GetSummary")
	# ACC:T63.5 behavior boundary: coordinator input delegates to runtime bridge state transition.
	# The runtime summary changes deterministically (+2 enemies) without introducing UI-owned rule branches.
	assert_int(int(summary_after_wave.get("enemy_units_spawned", 0))).is_equal(int(summary_before.get("enemy_units_spawned", 0)) + 2)
	# ACC:T63.6 trigger-to-feedback path is auditable: WaveBtn -> spawn cue alpha + status update.
	assert_str(status_label.text.to_lower()).contains("wave")
	assert_bool(spawn_a.color.a > weak_a and spawn_b.color.a > weak_b).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)
	assert_int(int(bridge.call("GetActorSnapshots").size())).is_greater_equal(1)

	await get_tree().create_timer(2.0).timeout
	assert_bool(spawn_a.color.a >= 0.9 and spawn_b.color.a >= 0.9).is_true()

	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	for _i in range(24):
		await get_tree().process_frame

	assert_bool(spawn_a.color.a <= weak_a + 0.05 and spawn_b.color.a <= weak_b + 0.05).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)

	var summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(summary.get("combat_exchanges", 0))).is_greater_equal(1)


# ACC:T63.2
# ACC:T64.6
func test_damage_number_toggle_should_hide_then_restore_damage_number_rendering() -> void:
	var bridge_script := load(COMBAT_EXPERIENCE_BRIDGE)
	assert_object(bridge_script).is_not_null()
	var hud := await _hud()
	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))
	await get_tree().process_frame
	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var feedback_label := _label(hud, "FeedbackLayer/FeedbackLabel")
	var prompt_label := _label(hud, "FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	var snapshot := _snapshot_damage_numbers_setting()
	_damage_numbers_snapshot = snapshot
	_damage_numbers_snapshot_pending = true
	bridge.call("ResetForInteractiveRun")
	bridge.call("BuildPhase")
	bridge.call("TrainFriendlyUnitPhase")
	bridge.call("SpawnEnemyWavePhase")
	await get_tree().process_frame

	# Negative path: with toggle off, no local damage number feedback should render.
	_write_damage_numbers_setting(false)
	bridge.call("ResolveCombatExchangePhase")
	bridge.call("PublishOutcomePhase")
	await get_tree().process_frame
	assert_int(_battlefield_children_with_prefix(bridge, "DamageNumber").size()).is_equal(0)
	# Keep non-damage local feedback alive while damage numbers are disabled.
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.length() > 0).is_true()
	assert_bool(
		pressure_label.text.to_lower().find("warning") >= 0
		or pressure_label.text.to_lower().find("danger") >= 0
		or pressure_label.text.to_lower().find("critical") >= 0
	).is_true()
	assert_str(prompt_label.text.to_lower()).contains("n/a")

	# Positive path: restoring toggle should restore damage number rendering.
	_write_damage_numbers_setting(true)
	bridge.call("ResolveCombatExchangePhase")
	await get_tree().process_frame
	assert_int(_battlefield_children_with_prefix(bridge, "DamageNumber").size()).is_greater_equal(1)

	# Absence semantics after trigger ends: cleanup phase should clear transient local feedback.
	bridge.call("CleanupDeadUnitsPhase")
	await get_tree().process_frame
	assert_int(_battlefield_children_with_prefix(bridge, "DamageNumber").size()).is_equal(0)
	_restore_damage_numbers_setting(snapshot)
	_damage_numbers_snapshot_pending = false
	_damage_numbers_snapshot = {}


# ACC:T64.2
# ACC:T64.3
# ACC:T64.4
# ACC:T64.5
func test_pressure_state_mapping_should_cover_exact_four_states_and_keep_summary_channel_stable() -> void:
	var hud := await _hud()
	var bus := get_node_or_null("/root/EventBus")
	assert_object(bus).is_not_null()
	var pressure_label := _label(hud, "FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var prompt_label := _label(hud, "FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	# Four-state set and mutual exclusion.
	await _assert_pressure_state_exact(hud, bus, 90, "stable")
	await _assert_pressure_state_exact(hud, bus, 55, "warning")
	await _assert_pressure_state_exact(hud, bus, 35, "danger")
	await _assert_pressure_state_exact(hud, bus, 15, "critical")

	# Input unchanged -> visible output unchanged.
	_emit_castle_hp(bus, 35, 40)
	for _i in range(6):
		await get_tree().process_frame
	var first_pressure := pressure_label.text
	var first_prompt := prompt_label.text
	_emit_castle_hp(bus, 35, 35)
	for _i in range(6):
		await get_tree().process_frame
	assert_str(pressure_label.text).is_equal(first_pressure)
	assert_str(prompt_label.text).is_equal(first_prompt)

	# Runtime prompt is escalation callout, not top-bar summary replacement.
	bus.call("PublishSimple", "core.lastking.ui_feedback.raised", "ut", "{\"Code\":\"run_continue_blocked\",\"MessageKey\":\"ui.blocked_action.combat_exchange\",\"Details\":\"escalation\"}")
	for _i in range(6):
		await get_tree().process_frame
	assert_str(prompt_label.text.to_lower()).contains("n/a")
	assert_bool(pressure_label.text.to_lower().find("danger") >= 0).is_true()


# ACC:T67.1
# ACC:T67.2
# ACC:T67.3
# ACC:T67.4
# ACC:T67.5
# ACC:T67.6
# ACC:T67.7
# ACC:T67.8
# ACC:T70.1
# ACC:T70.13
# ACC:T70.14
# ACC:T70.15
# ACC:T70.6
# ACC:T70.7
#
# Keep T70 anchors adjacent to concrete test entrypoints so acceptance-anchor
# validation can map each item to a focused scenario.
func test_daily_settlement_modal_should_block_progress_until_reward_is_selected() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var outcome_controller: Node = screen.get_node("OutcomeController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var reward_a: Button = screen.get_node("DailySettlementModal/VBox/Rewards/RewardA")
	var reward_b: Button = screen.get_node("DailySettlementModal/VBox/Rewards/RewardB")
	var reward_c: Button = screen.get_node("DailySettlementModal/VBox/Rewards/RewardC")
	var summary_label: Label = screen.get_node("DailySettlementModal/VBox/Summary")
	var evidence_hp: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/HpEvidence")
	var evidence_kills: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/KillEvidence")
	var evidence_reward_summary: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/RewardSummaryEvidence")
	var evidence_resources: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/ResourceEvidence")
	var evidence_defeat_reason: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/DefeatReasonEvidence")
	var expand_context_btn: Button = screen.get_node("DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn")
	var runtime_context_payload: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload")
	var rewards_box: VBoxContainer = screen.get_node("DailySettlementModal/VBox/Rewards")
	var hud := main.get_node("RuntimeUi/HUD")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")

	# Drive battle to completion so non-terminal settlement modal opens.
	assert_bool(bridge.has_method("ForceOutcomeForTest")).is_true()
	bridge.call("ForceOutcomeForTest", "settlement", 42)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame

	assert_bool(modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(outcome_label.text.to_lower().find("outcome:") >= 0).is_true()
	assert_bool(summary_label.text.find("rewards=3") >= 0).is_true()
	assert_bool(summary_label.text.find("reward_summary=") >= 0).is_true()
	assert_bool(summary_label.text.find("HP=") >= 0).is_true()
	assert_bool(summary_label.text.find("kills=") >= 0).is_true()
	assert_bool(summary_label.text.find("gold=120") >= 0).is_true()
	assert_bool(summary_label.text.find("iron=44") >= 0).is_true()
	assert_bool(summary_label.text.find("pop=26") >= 0).is_true()
	assert_bool(evidence_hp.text.find("42") >= 0).is_true()
	assert_bool(evidence_kills.text.find("0") >= 0).is_true()
	assert_bool(evidence_reward_summary.visible).is_false()
	assert_bool(evidence_resources.text.find("gold=120") >= 0).is_true()
	assert_bool(evidence_resources.text.find("iron=44") >= 0).is_true()
	assert_bool(evidence_resources.text.find("pop=26") >= 0).is_true()
	assert_bool(evidence_defeat_reason.visible).is_false()
	assert_bool(runtime_context_payload.visible).is_false()
	assert_str(expand_context_btn.text).is_equal("Show Runtime Context")
	expand_context_btn.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(runtime_context_payload.visible).is_true()
	assert_str(expand_context_btn.text).is_equal("Hide Runtime Context")
	var context_payload := runtime_context_payload.text
	assert_bool(context_payload.find("\"outcome\":\"settlement\"") >= 0).is_true()
	assert_bool(context_payload.find("\"castle_hp\":42") >= 0).is_true()
	assert_bool(context_payload.find("\"resource_gold\":120") >= 0).is_true()
	var summary_for_context: Dictionary = bridge.call("GetSummary")
	assert_str(String(summary_for_context.get("outcome", ""))).is_equal("settlement")
	assert_int(int(summary_for_context.get("castle_hp", -1))).is_equal(42)
	assert_int(int(summary_for_context.get("resource_gold", -1))).is_equal(120)
	assert_int(int(summary_for_context.get("resource_iron", -1))).is_equal(44)
	assert_int(int(summary_for_context.get("resource_population_cap", -1))).is_equal(26)
	assert_int(rewards_box.get_child_count()).is_equal(3)
	assert_bool(not reward_a.disabled).is_true()
	assert_bool(not reward_b.disabled).is_true()
	assert_bool(not reward_c.disabled).is_true()
	assert_bool(not reward_a.text.is_empty()).is_true()
	assert_bool(not reward_b.text.is_empty()).is_true()
	assert_bool(not reward_c.text.is_empty()).is_true()

	# While modal is open, progression actions must be blocked.
	var blocked_before := status_label.text
	var summary_before: Dictionary = bridge.call("GetSummary")
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	assert_bool(status_label.text != blocked_before).is_true()
	assert_bool(status_label.text.to_lower().find("resolve reward first") >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before)

	var blocked_after_wave := status_label.text
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	assert_str(status_label.text).is_equal(blocked_after_wave)
	assert_bool(status_label.text.to_lower().find("resolve reward first") >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before)

	var blocked_after_exchange := status_label.text
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	assert_str(status_label.text).is_equal(blocked_after_exchange)
	assert_bool(status_label.text.to_lower().find("resolve reward first") >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before)

	var blocked_after_cleanup := status_label.text
	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	assert_str(status_label.text).is_equal(blocked_after_cleanup)
	assert_bool(status_label.text.to_lower().find("resolve reward first") >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before)

	# Resolve one reward to close modal and unblock progression.
	reward_a.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_bool(status_label.text.to_lower().find("settlement resolved") >= 0).is_true()
	var status_after_resolve := status_label.text
	var summary_after_resolve: Dictionary = bridge.call("GetSummary")
	# Single-consume guard: duplicate reward callback must not trigger extra transition side effects.
	assert_bool(screen.has_method("_on_settlement_reward_selected")).is_false()
	assert_bool(outcome_controller.has_method("_on_settlement_reward_selected")).is_true()
	outcome_controller.call("_on_settlement_reward_selected", 1)
	await get_tree().process_frame
	assert_bool(modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_str(status_label.text).is_equal(status_after_resolve)
	assert_that(bridge.call("GetSummary")).is_equal(summary_after_resolve)


# ACC:T67.2
# ACC:T70.2
# ACC:T70.8
func test_daily_settlement_modal_should_ignore_invalid_reward_selection_index() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var outcome_controller: Node = screen.get_node("OutcomeController")

	assert_bool(screen.get_node("CombatExperienceRuntimeBridge").has_method("ForceOutcomeForTest")).is_true()
	screen.get_node("CombatExperienceRuntimeBridge").call("ForceOutcomeForTest", "settlement", 42)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame

	assert_bool(modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(screen.has_method("_on_settlement_reward_selected")).is_false()
	assert_bool(outcome_controller.has_method("_on_settlement_reward_selected")).is_true()
	var runtime_summary_before: Dictionary = bridge.call("GetSummary")
	assert_str(String(runtime_summary_before.get("outcome", ""))).is_equal("settlement")
	assert_int(int(runtime_summary_before.get("castle_hp", -1))).is_equal(42)
	var status_before := status_label.text
	outcome_controller.call("_on_settlement_reward_selected", 99)
	await get_tree().process_frame
	assert_bool(modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_str(status_label.text).is_equal(status_before)
	var runtime_summary_after: Dictionary = bridge.call("GetSummary")
	assert_that(runtime_summary_after).is_equal(runtime_summary_before)
	# T70.8: while unresolved modal is visible, no implicit transition should mutate UI snapshot.
	var frozen_summary := String(screen.get_node("DailySettlementModal/VBox/Summary").text)
	for _i in range(6):
		await get_tree().process_frame
	assert_str(String(screen.get_node("DailySettlementModal/VBox/Summary").text)).is_equal(frozen_summary)
	assert_that(bridge.call("GetSummary")).is_equal(runtime_summary_before)
	assert_bool(modal.visible).is_true()


# ACC:T70.16
func test_settlement_confirm_should_be_rejected_when_no_resolved_result_is_active() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var victory_modal: PanelContainer = screen.get_node("VictoryOutcomeModal")
	var defeat_modal: PanelContainer = screen.get_node("DefeatOutcomeModal")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var outcome_controller: Node = screen.get_node("OutcomeController")

	assert_bool(modal.visible).is_false()
	assert_bool(victory_modal.visible).is_false()
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_bool(screen.has_method("_on_settlement_reward_selected")).is_false()
	assert_bool(outcome_controller.has_method("_on_settlement_reward_selected")).is_true()
	var status_before := status_label.text
	var summary_before: Dictionary = bridge.call("GetSummary")

	# No resolved settlement result is active; confirm input must be ignored.
	outcome_controller.call("_on_settlement_reward_selected", 0)
	await get_tree().process_frame

	assert_bool(modal.visible).is_false()
	assert_bool(victory_modal.visible).is_false()
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_str(status_label.text).is_equal(status_before)
	assert_that(bridge.call("GetSummary")).is_equal(summary_before)


# ACC:T70.1
# ACC:T70.12
# ACC:T70.14
func test_daily_settlement_evidence_panel_should_hide_non_applicable_fields_for_same_resolved_result_payload() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var hp_evidence: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/HpEvidence")
	var kills_evidence: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/KillEvidence")
	var reward_summary_evidence: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/RewardSummaryEvidence")
	var resources_evidence: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/ResourceEvidence")
	var defeat_reason_evidence: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/DefeatReasonEvidence")
	var expand_context_btn: Button = screen.get_node("DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn")
	var runtime_context_payload: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload")
	var outcome_controller: Node = screen.get_node("OutcomeController")

	var partial_summary := {
		"outcome": "settlement",
		"castle_hp": 21,
		"dead_units_retired": 2,
		"resource_gold": 7,
		"resource_iron": 3,
		"resource_population_cap": 5
	}
	outcome_controller.call("open_outcome", partial_summary)
	await get_tree().process_frame

	assert_bool(modal.visible).is_true()
	assert_bool(hp_evidence.visible).is_true()
	assert_bool(kills_evidence.visible).is_true()
	assert_bool(resources_evidence.visible).is_true()
	assert_bool(defeat_reason_evidence.visible).is_false()
	assert_str(hp_evidence.text).is_equal("Final HP: 21")
	assert_str(kills_evidence.text).is_equal("Kills: 2")
	assert_str(resources_evidence.text).is_equal("Resources: gold=7, iron=3, pop=5")
	# non-applicable reward_summary field should be hidden when summary payload omits it.
	assert_bool(reward_summary_evidence.visible).is_false()
	assert_bool(runtime_context_payload.visible).is_false()
	expand_context_btn.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(runtime_context_payload.visible).is_true()
	var payload_text := runtime_context_payload.text
	assert_bool(payload_text.find("\"outcome\":\"settlement\"") >= 0).is_true()
	assert_bool(payload_text.find("\"castle_hp\":21") >= 0).is_true()
	assert_bool(payload_text.find("\"resource_gold\":7") >= 0).is_true()
	assert_bool(payload_text.find("\"reward_summary\"") < 0).is_true()

	outcome_controller.call("close_all")
	await get_tree().process_frame
	assert_bool(modal.visible).is_false()

	var summary_with_reward := partial_summary.duplicate()
	summary_with_reward["reward_summary"] = "Reward A,Reward B,Reward C"
	outcome_controller.call("open_outcome", summary_with_reward)
	await get_tree().process_frame

	assert_bool(modal.visible).is_true()
	assert_bool(reward_summary_evidence.visible).is_true()
	assert_str(reward_summary_evidence.text).is_equal("Reward Summary: Reward A,Reward B,Reward C")
	assert_bool(runtime_context_payload.visible).is_false()
	expand_context_btn.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(runtime_context_payload.visible).is_true()
	var payload_text_with_reward := runtime_context_payload.text
	assert_bool(payload_text_with_reward.find("\"castle_hp\":21") >= 0).is_true()
	assert_bool(payload_text_with_reward.find("\"reward_summary\":\"Reward A,Reward B,Reward C\"") >= 0).is_true()
	assert_bool(payload_text_with_reward.find("\"resource_gold\":7") >= 0).is_true()

	outcome_controller.call("close_all")
	await get_tree().process_frame
	assert_bool(modal.visible).is_false()


# ACC:T67.2
# ACC:T70.11
func test_daily_settlement_modal_should_reject_invalid_reward_option_count() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var outcome_controller: Node = screen.get_node("OutcomeController")
	var settlement_options = outcome_controller.call("get_settlement_options")
	assert_int(typeof(settlement_options)).is_equal(TYPE_ARRAY)
	var original_options: Array = (settlement_options as Array).duplicate()
	assert_bool(screen.has_method("SetSettlementOptionsForTest")).is_false()
	assert_bool(outcome_controller.has_method("SetSettlementOptionsForTest")).is_true()
	outcome_controller.call("SetSettlementOptionsForTest", ["OnlyOne"])

	assert_bool(bridge.has_method("ForceOutcomeForTest")).is_true()
	bridge.call("ForceOutcomeForTest", "settlement", 42)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame

	assert_bool(modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_bool(status_label.text.to_lower().find("invalid") >= 0).is_true()
	# T70.11: invalid option count rejection must keep deterministic flow stable.
	assert_bool(status_label.text.to_lower().find("settlement resolved") < 0).is_true()
	outcome_controller.call("SetSettlementOptionsForTest", original_options)


# ACC:T67.3
# ACC:T69.1 ACC:T69.4
# ACC:T70.3
func test_defeat_outcome_modal_should_open_immediately_when_active_battle_crosses_terminal_hp_boundary() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var defeat_modal: PanelContainer = screen.get_node("DefeatOutcomeModal")
	var defeat_summary: Label = screen.get_node("DefeatOutcomeModal/VBox/Summary")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
			
	assert_bool(bridge.has_method("ConfigureDurabilityForTest")).is_true()
	bridge.call("ConfigureDurabilityForTest", 42, 1)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()

	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()

	bridge.call("ConfigureDurabilityForTest", 42, 0)
	await get_tree().process_frame
	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(defeat_summary.text.to_lower().find("wall breached") >= 0).is_true()
	assert_bool(status_label.text.to_lower().find("finished") < 0).is_true()
	# T70.3: failure mapping should remain stable until explicit transition input.
	var defeat_summary_before := String(defeat_summary.text)
	for _i in range(6):
		await get_tree().process_frame
	assert_str(defeat_summary.text).is_equal(defeat_summary_before)
	assert_bool(defeat_modal.visible).is_true()

	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()


# ACC:T69.2 ACC:T69.3 ACC:T69.6 ACC:T69.7 ACC:T69.8 ACC:T69.9 ACC:T69.10
func test_defeat_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var settlement_modal: PanelContainer = screen.get_node("DailySettlementModal")
	var victory_modal: PanelContainer = screen.get_node("VictoryOutcomeModal")
	var defeat_modal: PanelContainer = screen.get_node("DefeatOutcomeModal")
	var defeat_title: Label = screen.get_node("DefeatOutcomeModal/VBox/Title")
	var defeat_summary: Label = screen.get_node("DefeatOutcomeModal/VBox/Summary")
	var defeat_hint: Label = screen.get_node("DefeatOutcomeModal/VBox/Hint")
	var action_box: VBoxContainer = screen.get_node("DefeatOutcomeModal/VBox/Actions")
	var return_btn: Button = screen.get_node("DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn")
	var restart_btn: Button = screen.get_node("DefeatOutcomeModal/VBox/Actions/RestartBtn")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var hud := main.get_node("RuntimeUi/HUD")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")

	assert_bool(settlement_modal.visible).is_false()
	assert_bool(victory_modal.visible).is_false()
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(bridge.has_method("ForceDefeatStateForTest")).is_true()
	bridge.call("ForceDefeatStateForTest", "wall_breached", 42, 0)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame

	assert_bool(settlement_modal.visible).is_false()
	assert_bool(victory_modal.visible).is_false()
	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	var defeat_summary_state: Dictionary = bridge.call("GetSummary")
	assert_str(String(defeat_summary_state.get("defeat_reason", ""))).is_equal("wall_breached")
	assert_int(int(defeat_summary_state.get("castle_hp", -1))).is_equal(42)
	assert_int(int(defeat_summary_state.get("wall_hp", -1))).is_equal(0)
	assert_bool(defeat_title.text.to_lower().find("defeat") >= 0).is_true()
	assert_bool(defeat_summary.text.to_lower().find("wall breached") >= 0).is_true()
	assert_bool(defeat_summary.text.to_lower().find("run ended") >= 0).is_true()
	assert_bool(defeat_hint.text.to_lower().find("cannot be resumed") >= 0).is_true()
	assert_int(action_box.get_child_count()).is_equal(2)
	assert_str(return_btn.text).is_equal("Return to Main Menu")
	assert_str(restart_btn.text).is_equal("Restart")
	assert_bool(not return_btn.disabled).is_true()
	assert_bool(not restart_btn.disabled).is_true()
	assert_bool(outcome_label.text.to_lower().find("outcome: loss") < 0).is_true()

	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	assert_bool(outcome_label.text.to_lower().find("outcome: loss") < 0).is_true()

	return_btn.emit_signal("pressed")
	await get_tree().process_frame
	await get_tree().process_frame
	assert_bool(get_tree().paused).is_false()
	var main_menu: Node = main.get_node("RuntimeUi/MainMenu")
	assert_bool(bool(main_menu.get("visible"))).is_true()

	var restart_screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(restart_screen))
	await get_tree().process_frame
	await get_tree().process_frame
	screen = restart_screen
	bridge = screen.get_node("CombatExperienceRuntimeBridge")
	settlement_modal = screen.get_node("DailySettlementModal")
	victory_modal = screen.get_node("VictoryOutcomeModal")
	defeat_modal = screen.get_node("DefeatOutcomeModal")
	defeat_title = screen.get_node("DefeatOutcomeModal/VBox/Title")
	defeat_summary = screen.get_node("DefeatOutcomeModal/VBox/Summary")
	defeat_hint = screen.get_node("DefeatOutcomeModal/VBox/Hint")
	action_box = screen.get_node("DefeatOutcomeModal/VBox/Actions")
	return_btn = screen.get_node("DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn")
	status_label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	restart_btn = screen.get_node("DefeatOutcomeModal/VBox/Actions/RestartBtn")
	assert_bool(defeat_modal.visible).is_false()

	bridge.call("ForceDefeatStateForTest", "wall_breached", 0, 14)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	assert_bool(settlement_modal.visible).is_false()
	assert_bool(victory_modal.visible).is_false()
	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	defeat_summary_state = bridge.call("GetSummary")
	assert_str(String(defeat_summary_state.get("defeat_reason", ""))).is_equal("wall_breached")
	assert_int(int(defeat_summary_state.get("castle_hp", -1))).is_equal(0)
	assert_int(int(defeat_summary_state.get("wall_hp", -1))).is_equal(14)
	assert_bool(defeat_summary.text.to_lower().find("wall breached") >= 0).is_true()

	var status_before := status_label.text
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	assert_bool(status_label.text != status_before).is_true()
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	assert_bool(defeat_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()

	# Negative path: when HP stays above zero, defeat modal must not appear.
	restart_btn.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	bridge.call("ForceOutcomeForTest", "win", 42)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame
	assert_bool(defeat_modal.visible).is_false()
	assert_bool(status_label.text.to_lower().find("finished") >= 0).is_true()


# ACC:T68.1
# ACC:T68.4
# ACC:T68.6
# ACC:T68.8
# ACC:T70.10
func test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var settlement_modal: PanelContainer = screen.get_node("DailySettlementModal")
	var victory_modal: PanelContainer = screen.get_node("VictoryOutcomeModal")
	var victory_title: Label = screen.get_node("VictoryOutcomeModal/VBox/Title")
	var victory_summary: Label = screen.get_node("VictoryOutcomeModal/VBox/Summary")
	var victory_hint: Label = screen.get_node("VictoryOutcomeModal/VBox/Hint")
	var action_box: VBoxContainer = screen.get_node("VictoryOutcomeModal/VBox/Actions")
	var return_btn: Button = screen.get_node("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn")
	var restart_btn: Button = screen.get_node("VictoryOutcomeModal/VBox/Actions/RestartBtn")
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")
	var hud := main.get_node("RuntimeUi/HUD")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")

	assert_bool(bridge.has_method("ForceOutcomeForTest")).is_true()
	bridge.call("ForceOutcomeForTest", "win", 42)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	_request_hud_action(screen, "exchange")
	await get_tree().process_frame
	_request_hud_action(screen, "cleanup")
	await get_tree().process_frame
	_request_hud_action(screen, "finish")
	await get_tree().process_frame

	assert_bool(settlement_modal.visible).is_false()
	assert_bool(victory_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(outcome_label.text.to_lower().find("n/a") >= 0).is_true()
	assert_bool(victory_title.text.to_lower().find("victory") >= 0).is_true()
	assert_bool(victory_hint.text.to_lower().find("cannot be resumed") >= 0).is_true()
	assert_bool(victory_summary.text.find("HP=42") >= 0).is_true()
	assert_bool(victory_summary.text.find("kills=") >= 0).is_true()
	assert_bool(victory_summary.text.find("gold=120") >= 0).is_true()
	assert_bool(victory_summary.text.find("iron=44") >= 0).is_true()
	assert_bool(victory_summary.text.find("pop=26") >= 0).is_true()
	assert_int(action_box.get_child_count()).is_equal(2)

	assert_str(return_btn.text).is_equal("Return to Main Menu")
	assert_str(restart_btn.text).is_equal("Restart")
	assert_bool(not return_btn.disabled).is_true()
	assert_bool(not restart_btn.disabled).is_true()

	var status_before := status_label.text
	var runtime_summary_before_blocked: Dictionary = bridge.call("GetSummary")
	assert_str(String(runtime_summary_before_blocked.get("outcome", ""))).is_equal("win")
	assert_str(String(runtime_summary_before_blocked.get("defeat_reason", ""))).is_equal("")
	assert_int(int(runtime_summary_before_blocked.get("castle_hp", -1))).is_equal(42)
	assert_int(int(runtime_summary_before_blocked.get("resource_gold", -1))).is_equal(120)
	assert_int(int(runtime_summary_before_blocked.get("resource_iron", -1))).is_equal(44)
	assert_int(int(runtime_summary_before_blocked.get("resource_population_cap", -1))).is_equal(26)
	_request_hud_action(screen, "wave")
	await get_tree().process_frame
	assert_bool(status_label.text != status_before).is_true()
	assert_bool(status_label.text.to_lower().find("terminal outcome") >= 0).is_true()
	assert_bool(status_label.text.to_lower().find("continue battle") < 0).is_true()
	assert_bool(victory_modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	var runtime_summary_after_blocked: Dictionary = bridge.call("GetSummary")
	assert_that(runtime_summary_after_blocked).is_equal(runtime_summary_before_blocked)
	# T70.10: assert behavior semantics, not API existence.
	assert_str(String(runtime_summary_after_blocked.get("outcome", ""))).is_equal("win")
	assert_str(String(runtime_summary_after_blocked.get("defeat_reason", ""))).is_equal("")


# ACC:T68.7
# ACC:T70.4
# ACC:T70.5
func test_victory_outcome_modal_should_stay_centered_and_preserve_runtime_ownership_boundaries() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var modal: PanelContainer = screen.get_node("VictoryOutcomeModal")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var background: Node = screen.get_node("Background")
	var margin: Node = screen.get_node("LegacyPrototypeRoot")

	assert_float(modal.anchor_left).is_equal(0.5)
	assert_float(modal.anchor_right).is_equal(0.5)
	assert_float(modal.anchor_top).is_equal(0.5)
	assert_float(modal.anchor_bottom).is_equal(0.5)
	assert_float(modal.offset_left + modal.offset_right).is_equal(0.0)
	assert_float(modal.offset_top + modal.offset_bottom).is_equal(0.0)

	assert_str(str(background.get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(margin.get_meta("ownership_container"))).is_equal("legacy_prototype")


# ACC:T67.1
# ACC:T67.4
# ACC:T67.5
# ACC:T67.6
# ACC:T67.7
# ACC:T67.8
func test_daily_settlement_modal_should_stay_centered_and_preserve_runtime_ownership_boundaries() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var nav := main.get_node_or_null("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await get_tree().process_frame
	await get_tree().process_frame

	var screen: Node = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var background: Node = screen.get_node("Background")
	var margin: Node = screen.get_node("LegacyPrototypeRoot")

	assert_float(modal.anchor_left).is_equal(0.5)
	assert_float(modal.anchor_right).is_equal(0.5)
	assert_float(modal.anchor_top).is_equal(0.5)
	assert_float(modal.anchor_bottom).is_equal(0.5)
	assert_float(modal.offset_left + modal.offset_right).is_equal(0.0)
	assert_float(modal.offset_top + modal.offset_bottom).is_equal(0.0)

	assert_str(str(background.get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(margin.get_meta("ownership_container"))).is_equal("legacy_prototype")
	var runtime_summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(runtime_summary.get("enemy_units_spawned", 0))).is_equal(0)
