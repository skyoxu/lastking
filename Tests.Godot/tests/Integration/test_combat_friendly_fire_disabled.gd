extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"
const PLAYER_ATTACK_HITBOX_SCENE := "res://Game.Godot/Scenes/Combat/PlayerAttackHitbox.tscn"
const PLAYER_LAYER: int = 1 << 0
const FRIENDLY_LAYER: int = 1 << 1
const ENEMY_LAYER: int = 1 << 2
const PLAYER_ATTACK_LAYER: int = 1 << 3

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

func _new_player_attack_hitbox() -> Area2D:
	var packed_scene: PackedScene = load(PLAYER_ATTACK_HITBOX_SCENE)
	var hitbox: Area2D = packed_scene.instantiate()
	add_child(hitbox)
	return hitbox

func _simulate_runtime_hits(probe: Node, attack_mask: int, targets: Array) -> Dictionary:
	var friendly_damage_events := 0
	var player_damage_events := 0
	var enemy_damage_events := 0
	for target in targets:
		var layer := int(target.get("layer", 0))
		var team := str(target.get("team", ""))
		var hit := bool(probe.call("CanHitLayer", attack_mask, layer))
		if not hit:
			continue
		if team == "friendly":
			friendly_damage_events += 1
		elif team == "player":
			player_damage_events += 1
		elif team == "enemy":
			enemy_damage_events += 1
	return {
		"friendly_damage_events": friendly_damage_events,
		"player_damage_events": player_damage_events,
		"enemy_damage_events": enemy_damage_events
	}

# acceptance: ACC:T6.2
func test_player_attack_collision_mask_excludes_player_and_friendly_layers() -> void:
	var probe := _new_probe()
	var player_attack_hitbox := _new_player_attack_hitbox()
	var attack_mask := player_attack_hitbox.collision_mask

	assert_bool(bool(probe.call("CanHitLayer", attack_mask, ENEMY_LAYER))).is_true()
	assert_bool(bool(probe.call("CanHitLayer", attack_mask, PLAYER_LAYER))).is_false()
	assert_bool(bool(probe.call("CanHitLayer", attack_mask, FRIENDLY_LAYER))).is_false()
	assert_bool(bool(probe.call("IsFriendlyFirePrevented", attack_mask, FRIENDLY_LAYER, PLAYER_LAYER))).is_true()

# acceptance: ACC:T6.9
# acceptance: ACC:T6.12
func test_full_player_attack_run_keeps_friendly_and_player_damage_zero() -> void:
	var probe := _new_probe()
	var player_attack_hitbox := _new_player_attack_hitbox()
	var attack_mask := player_attack_hitbox.collision_mask
	var targets := [
		{"team": "enemy", "layer": ENEMY_LAYER},
		{"team": "friendly", "layer": FRIENDLY_LAYER},
		{"team": "enemy", "layer": ENEMY_LAYER},
		{"team": "player", "layer": PLAYER_LAYER}
	]
	var summary: Dictionary = _simulate_runtime_hits(probe, attack_mask, targets)
	assert_int(int(summary["enemy_damage_events"])).is_equal(2)
	assert_int(int(summary["friendly_damage_events"])).is_equal(0)
	assert_int(int(summary["player_damage_events"])).is_equal(0)

func test_priority_selection_blocked_fallback_and_fixed_seed_are_deterministic() -> void:
	var probe := _new_probe()
	var candidates: Array = [
		{
			"id": "alpha",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 4,
			"blocks_route_to_higher_priority": true
		},
		{
			"id": "beta",
			"class": "unit",
			"reachable": false,
			"blocked": true,
			"path_points": 0,
			"distance": 2,
			"blocks_route_to_higher_priority": true
		}
	]

	var first_pick: Dictionary = probe.call("SelectTarget", candidates)
	var second_pick: Dictionary = probe.call("SelectTarget", candidates)
	assert_bool(bool(first_pick.get("is_fallback_attack", false))).is_true()
	assert_str(str(first_pick.get("target_id", ""))).is_equal("beta")
	assert_str(str(first_pick.get("target_id", ""))).is_equal(str(second_pick.get("target_id", "")))

func test_wrong_collision_mask_allows_friendly_or_player_hits() -> void:
	var probe := _new_probe()
	var wrong_attack_mask := ENEMY_LAYER | FRIENDLY_LAYER | PLAYER_LAYER
	var targets := [
		{"team": "enemy", "layer": ENEMY_LAYER},
		{"team": "friendly", "layer": FRIENDLY_LAYER},
		{"team": "player", "layer": PLAYER_LAYER}
	]
	var summary: Dictionary = _simulate_runtime_hits(probe, wrong_attack_mask, targets)

	assert_int(int(summary["enemy_damage_events"])).is_equal(1)
	assert_int(int(summary["friendly_damage_events"])).is_equal(1)
	assert_int(int(summary["player_damage_events"])).is_equal(1)
