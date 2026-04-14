extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const EnemyAiScript = preload("res://Game.Godot/Scripts/Combat/EnemyAi.cs")


func _enemy_ai() -> Node:
    return EnemyAiScript.new()


func _candidate(id: String, target_class: String, reachable: bool, blocked: bool, path_points: int, distance: int) -> Dictionary:
    return {
        "id": id,
        "class": target_class,
        "reachable": reachable,
        "blocked": blocked,
        "path_points": path_points,
        "distance": distance,
        "blocks_route_to_higher_priority": false
    }


func test_in_range_enemy_unit_is_selected() -> void:
    var enemy_ai := _enemy_ai()
    var candidates: Array = [
        _candidate("unit_in_range", "unit", true, false, 5, 3),
        _candidate("castle_far", "castle", true, false, 7, 8)
    ]

    var decision: Dictionary = enemy_ai.SelectTarget(candidates)

    assert_str(str(decision.get("target_id", ""))).is_equal("unit_in_range")
    assert_str(str(decision.get("target_class", ""))).is_equal("Unit")


# acceptance: ACC:T20.5
func test_out_of_range_or_invalid_targets_remain_unselected() -> void:
    var enemy_ai := _enemy_ai()
    var candidates: Array = [
        _candidate("enemy_out_of_range", "unit", true, false, 0, 2),
        _candidate("enemy_blocked", "unit", true, true, 3, 1),
        _candidate("invalid_decoration", "decoration", true, false, 0, 1)
    ]

    var decision: Dictionary = enemy_ai.SelectTarget(candidates)

    assert_str(str(decision.get("target_id", ""))).is_equal("")
    assert_bool(bool(decision.get("is_fallback_attack", false))).is_false()


# acceptance: ACC:T20.10
func test_only_reachable_enemy_target_is_selected() -> void:
    var enemy_ai := _enemy_ai()
    var candidates: Array = [
        _candidate("enemy_reachable", "unit", true, false, 4, 4),
        _candidate("enemy_unreachable", "unit", true, false, 0, 1),
        _candidate("enemy_blocked", "unit", true, true, 3, 1)
    ]

    var decision: Dictionary = enemy_ai.SelectTarget(candidates)

    assert_str(str(decision.get("target_id", ""))).is_equal("enemy_reachable")
    assert_bool(bool(decision.get("is_fallback_attack", false))).is_false()
