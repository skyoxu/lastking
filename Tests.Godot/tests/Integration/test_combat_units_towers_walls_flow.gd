extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const EnemyAiScript = preload("res://Game.Godot/Scripts/Combat/EnemyAi.cs")
const FRIENDLY_LAYER := 1 << 1
const PLAYER_LAYER := 1 << 2
const ENEMY_LAYER := 1 << 3


class CombatEntity:
    var id: String
    var team: int
    var kind: String
    var hp: int

    func _init(p_id: String, p_team: int, p_kind: String, p_hp: int) -> void:
        id = p_id
        team = p_team
        kind = p_kind
        hp = p_hp


func _entity(p_id: String, p_team: int, p_kind: String, p_hp: int) -> CombatEntity:
    return CombatEntity.new(p_id, p_team, p_kind, p_hp)


func _target_layer(entity: CombatEntity) -> int:
    if entity.team == 1:
        return FRIENDLY_LAYER
    return ENEMY_LAYER


func _attack_mask(attacker: CombatEntity) -> int:
    if attacker.team == 1:
        return ENEMY_LAYER
    return FRIENDLY_LAYER


func _run_combat_round(enemy_ai: Node, events: Array[Dictionary], entities: Dictionary) -> void:
    for event in events:
        var attacker := entities.get(event["attacker_id"]) as CombatEntity
        var target := entities.get(event["target_id"]) as CombatEntity
        if attacker == null or target == null:
            continue
        if target.hp <= 0:
            continue

        var attack_mask := _attack_mask(attacker)
        var target_layer := _target_layer(target)
        var friendly_fire_prevented: bool = bool(enemy_ai.IsFriendlyFirePrevented(attack_mask, FRIENDLY_LAYER, PLAYER_LAYER))
        if friendly_fire_prevented and attacker.team == target.team:
            continue
        if not bool(enemy_ai.CanHitLayer(attack_mask, target_layer)):
            continue

        target.hp = max(target.hp - int(event["damage"]), 0)


# ACC:T20.6
# Towers should damage enemies only and must not damage same-team units or walls.
func test_tower_damages_enemy_but_not_allied_unit_or_wall() -> void:
    var enemy_ai := EnemyAiScript.new()
    var entities := {
        "tower_blue": _entity("tower_blue", 1, "tower", 100),
        "unit_blue": _entity("unit_blue", 1, "unit", 80),
        "wall_blue": _entity("wall_blue", 1, "wall", 180),
        "unit_red": _entity("unit_red", 2, "unit", 90)
    }

    var events: Array[Dictionary] = [
        {"attacker_id": "tower_blue", "target_id": "unit_red", "damage": 20},
        {"attacker_id": "tower_blue", "target_id": "unit_blue", "damage": 20},
        {"attacker_id": "tower_blue", "target_id": "wall_blue", "damage": 20}
    ]

    _run_combat_round(enemy_ai, events, entities)

    assert_int((entities["unit_red"] as CombatEntity).hp).is_equal(70)
    assert_int((entities["unit_blue"] as CombatEntity).hp).is_equal(80)
    assert_int((entities["wall_blue"] as CombatEntity).hp).is_equal(180)


# ACC:T20.18
# High-density combat must keep allied unit/building HP unchanged while enemy HP decreases.
func test_high_density_combat_keeps_allied_hp_unchanged_and_reduces_enemy_hp() -> void:
    var enemy_ai := EnemyAiScript.new()
    var entities := {
        "tower_blue_a": _entity("tower_blue_a", 1, "tower", 100),
        "tower_blue_b": _entity("tower_blue_b", 1, "tower", 100),
        "unit_blue": _entity("unit_blue", 1, "unit", 120),
        "wall_blue": _entity("wall_blue", 1, "wall", 240),
        "unit_red_a": _entity("unit_red_a", 2, "unit", 120),
        "unit_red_b": _entity("unit_red_b", 2, "unit", 120),
        "wall_red": _entity("wall_red", 2, "wall", 240)
    }

    var events: Array[Dictionary] = [
        {"attacker_id": "tower_blue_a", "target_id": "unit_red_a", "damage": 30},
        {"attacker_id": "tower_blue_b", "target_id": "unit_red_b", "damage": 30},
        {"attacker_id": "tower_blue_a", "target_id": "wall_red", "damage": 25},
        {"attacker_id": "tower_blue_b", "target_id": "unit_blue", "damage": 25},
        {"attacker_id": "tower_blue_a", "target_id": "wall_blue", "damage": 25}
    ]

    _run_combat_round(enemy_ai, events, entities)

    assert_int((entities["unit_blue"] as CombatEntity).hp).is_equal(120)
    assert_int((entities["wall_blue"] as CombatEntity).hp).is_equal(240)
    assert_int((entities["unit_red_a"] as CombatEntity).hp).is_equal(90)
    assert_int((entities["unit_red_b"] as CombatEntity).hp).is_equal(90)
    assert_int((entities["wall_red"] as CombatEntity).hp).is_equal(215)
