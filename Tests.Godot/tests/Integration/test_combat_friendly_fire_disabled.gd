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
# ACC:T48.5
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
# acceptance: ACC:T51.3
# ACC:T52.2
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

# acceptance: ACC:T51.3
func test_aoe_resolution_damages_only_valid_hostiles_and_keeps_non_hostiles_or_invalid_targets_unchanged() -> void:
	var probe := _new_probe()
	var targets := [
		{"id": "enemy_near", "team_id": 2, "distance": 0.0, "damageable": true},
		{"id": "enemy_mid", "team_id": 2, "distance": 2.0, "damageable": true},
		{"id": "enemy_far", "team_id": 2, "distance": 4.0, "damageable": true},
		{"id": "friendly", "team_id": 1, "distance": 1.0, "damageable": true},
		{"id": "invalid_enemy", "team_id": 2, "distance": 1.0, "damageable": false},
		{"id": "player_proxy", "team_id": 1, "distance": 0.5, "damageable": true}
	]
	var aoe: Dictionary = probe.call("SimulateAreaDamageRuntime", targets, true, 1, 20, 3.0, 0.25, 0.4, 5, 12)
	var results: Array = aoe.get("results", [])
	var by_id := {}
	for item in results:
		by_id[str(item.get("target_id", ""))] = item

	assert_int(int(aoe.get("committed_count", -1))).is_equal(2)
	assert_int(int(aoe.get("out_of_radius_count", -1))).is_equal(1)

	assert_int(int(Dictionary(by_id["enemy_near"]).get("resolved_damage", -1))).is_equal(12)
	assert_int(int(Dictionary(by_id["enemy_mid"]).get("resolved_damage", -1))).is_equal(10)
	assert_str(str(Dictionary(by_id["enemy_far"]).get("outcome", ""))).is_equal("out_of_radius")
	assert_str(str(Dictionary(by_id["friendly"]).get("outcome", ""))).is_equal("friendly_fire_refused")
	assert_str(str(Dictionary(by_id["player_proxy"]).get("outcome", ""))).is_equal("friendly_fire_refused")
	assert_str(str(Dictionary(by_id["invalid_enemy"]).get("outcome", ""))).is_equal("invalid_target")
