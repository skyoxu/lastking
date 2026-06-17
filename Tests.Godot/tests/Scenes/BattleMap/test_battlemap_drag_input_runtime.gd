extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func test_screen_level_pointer_bridge_should_drive_drag_preview_and_release_cancel() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var motion := InputEventMouseMotion.new()
	motion.position = Vector2(300, 760)
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var moving_preview: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(moving_preview.get("visible", false) == true).is_true()
	assert_that(moving_preview.get("position", Vector2.ZERO)).is_equal(Vector2(276, 736))

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = Vector2(300, 760)
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(1)

	var after_release: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(controller.call("has_active_placement") == false).is_true()
	assert_bool(after_release.get("visible", true) == false).is_true()

func test_screen_level_pointer_bridge_should_release_on_hovered_slot_when_pointer_is_over_tile() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)

	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	var summary: Dictionary = bridge.call("GetSummary")
	assert_bool(summary.get("mg_tower_built", false) == true).is_true()
	assert_bool(controller.call("has_active_placement") == false).is_true()

func test_drag_release_building_should_not_arm_post_place_probe_or_stall_followup_frames() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)

	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(90)

	var summary: Dictionary = bridge.call("GetSummary")
	var preview: Dictionary = controller.call("get_drag_preview_state")
	assert_bool(summary.get("mg_tower_built", false) == true).is_true()
	assert_bool(controller.call("has_active_placement") == false).is_true()
	assert_bool(preview.get("visible", true) == false).is_true()
	assert_int(int(screen.get("_post_place_probe_frames"))).is_equal(0)


func test_screen_level_placed_tower_should_fire_after_enemy_wave_spawns() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)

	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)

	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)

	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("SpawnEnemyWavePhase")
	for _i in range(40):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var summary: Dictionary = bridge.call("GetSummary")
		if int(summary.get("projectiles_created", 0)) > 0:
			break

	var summary_after: Dictionary = bridge.call("GetSummary")
	assert_bool(summary_after.get("mg_tower_built", false) == true).is_true()
	assert_int(int(summary_after.get("projectiles_created", 0))).is_greater(0)

	var feedback: Node = screen.get_node("FeedbackController")
	var debug_state: Dictionary = feedback.call("get_runtime_visual_debug_state")
	var building_visuals: Array = debug_state.get("building_visuals", [])
	assert_bool(building_visuals.has("MgTower") or building_visuals.has("MgTower_InnerCastleRegionSlot_06_05")).is_true()
	assert_int(int(debug_state.get("enemy_tokens", 0))).is_greater(0)
	assert_int(int(debug_state.get("spawned_attack_effects_total", 0))).is_greater_equal(1)
	assert_object(screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/AttackEffectLayer/TowerMuzzleFlash")).is_not_null()
	assert_object(screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/AttackEffectLayer/TowerProjectile")).is_not_null()
	assert_object(screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/AttackEffectLayer/TowerImpact")).is_not_null()


func test_enemy_token_should_flash_on_hit_and_fade_on_death() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")
	var feedback: Node = screen.get_node("FeedbackController")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "grunt_a",
      "cost": 10,
      "hp": 20,
      "dmg": 5,
      "move_speed": 12,
      "range": 30,
      "attack_interval": 1000,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "grunt_b",
      "cost": 10,
      "hp": 20,
      "dmg": 5,
      "move_speed": 12,
      "range": 30,
      "attack_interval": 1000,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	var dying_started := false
	var dying_actor_name := ""
	for _i in range(40):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var all_states: Dictionary = feedback.call("get_all_enemy_token_debug_states")
		for actor_name_variant in all_states.keys():
			var actor_name := str(actor_name_variant)
			var state: Dictionary = all_states[actor_name] as Dictionary
			if state == null:
				continue
			if state.get("dying", false) == true:
				dying_started = true
				dying_actor_name = actor_name
				break
		if dying_started:
			break

	var visual_debug_state: Dictionary = feedback.call("get_runtime_visual_debug_state")
	assert_int(int(visual_debug_state.get("spawned_attack_effects_total", 0))).is_greater(0)
	assert_bool(dying_started).is_true()
	assert_bool(not dying_actor_name.is_empty()).is_true()

	for _i in range(20):
		bridge.call("AdvanceSimulation", 0.1)
		await _await_frames(1)

	var final_state: Dictionary = feedback.call("get_enemy_token_debug_state", dying_actor_name)
	var removed: bool = final_state.get("exists", true) == false
	var fading: bool = final_state.get("dying", false) == true and float(final_state.get("death_fade", 1.0)) < 0.24
	assert_bool(removed or fading).is_true()


func test_enemy_tokens_should_use_distinct_visual_tiers_for_grunt_and_elite() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var feedback: Node = screen.get_node("FeedbackController")

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "grunt_a",
      "cost": 10,
      "hp": 20,
      "dmg": 5,
      "move_speed": 12,
      "range": 30,
      "attack_interval": 1000,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "elite_b",
      "cost": 20,
      "hp": 40,
      "dmg": 8,
      "move_speed": 10,
      "range": 30,
      "attack_interval": 1200,
      "armor": 1,
      "tags": ["elite"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": true,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")
	await _await_frames(2)

	var grunt_state: Dictionary = feedback.call("get_enemy_token_debug_state", "EnemyUnit1")
	var elite_state: Dictionary = feedback.call("get_enemy_token_debug_state", "EnemyUnit2")

	assert_bool(grunt_state.get("exists", false) == true).is_true()
	assert_bool(elite_state.get("exists", false) == true).is_true()
	assert_that(str(grunt_state.get("visual_tier", ""))).is_equal("grunt")
	assert_that(str(elite_state.get("visual_tier", ""))).is_equal("elite")
	assert_that(str(grunt_state.get("sprite_sheet_path", ""))).is_not_equal(str(elite_state.get("sprite_sheet_path", "")))


func test_placed_tower_should_fire_multiple_times_against_durable_enemies() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "tank_a",
      "cost": 20,
      "hp": 120,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "tank_b",
      "cost": 20,
      "hp": 120,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	for _i in range(80):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)

	var summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary.get("projectiles_created", 0))).is_greater(1)


func test_placed_tower_should_retarget_second_enemy_after_first_enemy_dies() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "front_enemy",
      "cost": 10,
      "hp": 25,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "back_enemy",
      "cost": 10,
      "hp": 120,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	var second_enemy_took_damage := false
	for _i in range(120):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var snapshots: Array = bridge.call("GetActorSnapshots")
		for item in snapshots:
			var snapshot := item as Dictionary
			if snapshot == null:
				continue
			if int(snapshot.get("hp", 120)) <= 0:
				continue
			if int(snapshot.get("hp", 120)) < 120:
				second_enemy_took_damage = true
				break
		if second_enemy_took_damage:
			break

	assert_bool(second_enemy_took_damage).is_true()


func test_two_placed_towers_should_fire_simultaneously() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	for slot_id in ["InnerCastleRegionSlot_05_05", "InnerCastleRegionSlot_06_05"]:
		controller.call("begin_drag_building", "tower_alpha")
		await _await_frames(1)
		var slot_position: Vector2 = battlefield.call("get_slot_position", slot_id)
		var pointer_position := viewport.position + slot_position + Vector2(24, 24)
		var motion := InputEventMouseMotion.new()
		motion.position = pointer_position
		screen.call("debug_handle_pointer_input", motion)
		await _await_frames(1)
		var release := InputEventMouseButton.new()
		release.button_index = MOUSE_BUTTON_LEFT
		release.pressed = false
		release.position = pointer_position
		screen.call("debug_handle_pointer_input", release)
		await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "tank_a",
      "cost": 20,
      "hp": 200,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "tank_b",
      "cost": 20,
      "hp": 200,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")
	for _i in range(30):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var summary_progress: Dictionary = bridge.call("GetSummary")
		if int(summary_progress.get("projectiles_created", 0)) >= 2:
			break

	var placed_slots: Dictionary = bridge.call("GetPlacedBuildingSlots")
	assert_int(placed_slots.size()).is_equal(2)
	var tower_debug: Dictionary = bridge.call("GetTowerCombatDebugSnapshot")
	assert_int(int(tower_debug.get("tower_count", 0))).is_equal(2)
	var summary: Dictionary = bridge.call("GetSummary")
	assert_int(int(summary.get("projectiles_created", 0))).is_greater_equal(2)


func test_two_placed_towers_should_prefer_split_targets_when_two_enemies_are_in_range() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	for slot_id in ["InnerCastleRegionSlot_05_05", "InnerCastleRegionSlot_06_05"]:
		controller.call("begin_drag_building", "tower_alpha")
		await _await_frames(1)
		var slot_position: Vector2 = battlefield.call("get_slot_position", slot_id)
		var pointer_position := viewport.position + slot_position + Vector2(24, 24)
		var motion := InputEventMouseMotion.new()
		motion.position = pointer_position
		screen.call("debug_handle_pointer_input", motion)
		await _await_frames(1)
		var release := InputEventMouseButton.new()
		release.button_index = MOUSE_BUTTON_LEFT
		release.pressed = false
		release.position = pointer_position
		screen.call("debug_handle_pointer_input", release)
		await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "tank_a",
      "cost": 20,
      "hp": 200,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "tank_b",
      "cost": 20,
      "hp": 200,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	var split_targets := false
	for _i in range(30):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var tower_debug: Dictionary = bridge.call("GetTowerCombatDebugSnapshot")
		var tower_targets_variant: Variant = tower_debug.get("tower_targets", [])
		if tower_targets_variant is Array:
			var names: Dictionary = {}
			for item in tower_targets_variant:
				var entry := item as Dictionary
				if entry == null:
					continue
				var target_name := str(entry.get("target_name", "n/a"))
				if target_name == "n/a":
					continue
				names[target_name] = true
			if names.size() >= 2:
				split_targets = true
				break

	assert_bool(split_targets).is_true()


func test_placed_tower_should_prefer_finishing_lower_hp_enemy_in_range() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "low_hp_enemy",
      "cost": 10,
      "hp": 20,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "high_hp_enemy",
      "cost": 10,
      "hp": 120,
      "dmg": 5,
      "move_speed": 8,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	var low_hp_enemy_removed_first := false
	for _i in range(80):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var snapshots: Array = bridge.call("GetActorSnapshots")
		var low_hp_exists := false
		var high_hp_exists := false
		for item in snapshots:
			var snapshot := item as Dictionary
			if snapshot == null:
				continue
			var hp_value := int(snapshot.get("hp", 0))
			if hp_value <= 0:
				continue
			if hp_value <= 20:
				low_hp_exists = true
			if hp_value >= 95:
				high_hp_exists = true
		if not low_hp_exists and high_hp_exists:
			low_hp_enemy_removed_first = true
			break

	assert_bool(low_hp_enemy_removed_first).is_true()


func test_placed_tower_should_prefer_more_advanced_enemy_when_hp_is_equal() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_alpha")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("LoadEnemyRuntimeConfigForTest", """
{
  "time": { "day_seconds": 240, "night_seconds": 120 },
  "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
  "channels": { "elite": "elite", "boss": "boss" },
  "spawn": { "cadence_seconds": 10 },
  "boss": { "count": 2 },
  "battle": { "castle_start_hp": 100 },
  "enemies": [
    {
      "id": "front_enemy",
      "cost": 10,
      "hp": 80,
      "dmg": 5,
      "move_speed": 12,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    },
    {
      "id": "back_enemy",
      "cost": 10,
      "hp": 80,
      "dmg": 5,
      "move_speed": 4,
      "range": 30,
      "attack_interval": 1500,
      "armor": 0,
      "tags": ["base"],
      "spawn_weight": 1,
      "min_day": 1,
      "max_day": 15,
      "is_elite": false,
      "is_boss": false
    }
  ]
}
""")
	bridge.call("SpawnEnemyWavePhase")

	var preferred_front_enemy := false
	for _i in range(80):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var snapshots: Array = bridge.call("GetActorSnapshots")
		var first_enemy_hp := -1
		var second_enemy_hp := -1
		for item in snapshots:
			var snapshot := item as Dictionary
			if snapshot == null:
				continue
			var actor_name := str(snapshot.get("name", ""))
			if actor_name == "EnemyUnit1":
				first_enemy_hp = int(snapshot.get("hp", -1))
			elif actor_name == "EnemyUnit2":
				second_enemy_hp = int(snapshot.get("hp", -1))
		if first_enemy_hp >= 0 and second_enemy_hp >= 0 and first_enemy_hp < second_enemy_hp:
			preferred_front_enemy = true
			break

	assert_bool(preferred_front_enemy).is_true()


func test_enemy_tokens_should_render_at_formal_sprite_size() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var feedback: Node = screen.get_node("FeedbackController")
	bridge.call("SpawnEnemyWavePhase")
	await _await_frames(2)

	var enemy_state: Dictionary = feedback.call("get_enemy_token_debug_state", "EnemyUnit1")
	assert_bool(enemy_state.get("exists", false) == true).is_true()
	assert_that(enemy_state.get("size", Vector2.ZERO)).is_equal(Vector2(16, 16))


func test_tower_beta_should_place_and_use_its_own_combat_profile() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var controller: Node = screen.get_node("BuildPlacementController")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var battlefield: Node = screen.get_node("Background")
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")

	controller.call("begin_drag_building", "tower_beta")
	await _await_frames(1)
	var slot_position: Vector2 = battlefield.call("get_slot_position", "InnerCastleRegionSlot_06_05")
	var pointer_position := viewport.position + slot_position + Vector2(24, 24)
	var motion := InputEventMouseMotion.new()
	motion.position = pointer_position
	screen.call("debug_handle_pointer_input", motion)
	await _await_frames(1)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.pressed = false
	release.position = pointer_position
	screen.call("debug_handle_pointer_input", release)
	await _await_frames(2)

	bridge.call("SpawnEnemyWavePhase")
	for _i in range(40):
		bridge.call("AdvanceSimulation", 0.2)
		await _await_frames(1)
		var summary_progress: Dictionary = bridge.call("GetSummary")
		if int(summary_progress.get("projectiles_created", 0)) > 0:
			break

	var placed_slots: Dictionary = bridge.call("GetPlacedBuildingSlots")
	assert_str(str(placed_slots.get("InnerCastleRegionSlot_06_05", ""))).is_equal("tower_beta")
	var tower_debug: Dictionary = bridge.call("GetTowerCombatDebugSnapshot")
	var tower_targets_variant: Variant = tower_debug.get("tower_targets", [])
	var matched_profile := false
	if tower_targets_variant is Array:
		for item in tower_targets_variant:
			var entry := item as Dictionary
			if entry == null:
				continue
			if not str(entry.get("tower_name", "")).begins_with("SniperTower"):
				continue
			assert_float(float(entry.get("range_px", 0.0))).is_equal(420.0)
			assert_int(int(entry.get("attack_damage", 0))).is_equal(14)
			assert_float(float(entry.get("attack_interval_seconds", 0.0))).is_equal(1.2)
			assert_str(str(entry.get("targeting_mode", ""))).is_equal("frontline_pressure_split")
			matched_profile = true
			break
	assert_bool(matched_profile).is_true()
