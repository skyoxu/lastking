extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BarracksTrainingQueueBridge = preload("res://Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs")
const UNIT_TYPE = "spearman"

func _new_bridge() -> Node:
    var bridge = BarracksTrainingQueueBridge.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", 240, 120, 3)
    return bridge

func _as_dictionary(value: Variant) -> Dictionary:
    if typeof(value) == TYPE_DICTIONARY:
        return value
    return {}

func _pick_numeric(source: Dictionary, candidate_keys: Array) -> float:
    for key in candidate_keys:
        if source.has(key):
            var value = source.get(key)
            var value_type = typeof(value)
            if value_type == TYPE_INT or value_type == TYPE_FLOAT:
                return float(value)
    return NAN

func _assert_stat_matches_multiplier(
    baseline_stats: Dictionary,
    trained_stats: Dictionary,
    multipliers: Dictionary,
    baseline_keys: Array,
    trained_keys: Array,
    multiplier_keys: Array
) -> void:
    var baseline_value = _pick_numeric(baseline_stats, baseline_keys)
    var trained_value = _pick_numeric(trained_stats, trained_keys)
    var multiplier_value = _pick_numeric(multipliers, multiplier_keys)

    assert_bool(not is_nan(baseline_value)).is_true()
    assert_bool(not is_nan(trained_value)).is_true()
    assert_bool(not is_nan(multiplier_value)).is_true()

    var expected_value = baseline_value * multiplier_value
    var delta = abs(trained_value - expected_value)
    assert_float(delta).is_less_equal(0.0001)

# ACC:T17.7
# acceptance: unlocked tech nodes scale barracks-trained stats as baseline x current multipliers.
func test_unlocked_tech_nodes_scale_barracks_stats_by_current_multipliers() -> void:
    var bridge = _new_bridge()

    var baseline_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", UNIT_TYPE))

    var attack_unlock = _as_dictionary(
        bridge.call("TryApplyTechModifierForTest", "tech_barracks_attack_i", UNIT_TYPE, "attack", 1.20)
    )
    var hp_unlock = _as_dictionary(
        bridge.call("TryApplyTechModifierForTest", "tech_barracks_hp_i", UNIT_TYPE, "hp", 1.15)
    )

    var enqueue_result = _as_dictionary(bridge.call("EnqueueUpfront", UNIT_TYPE, 1, 0, 0))
    var tick_result = _as_dictionary(bridge.call("Tick", 1))

    var multipliers = _as_dictionary(bridge.call("GetTrainingMultipliersForTest", UNIT_TYPE))
    var trained_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", UNIT_TYPE))

    assert_that(attack_unlock.get("accepted", false)).is_equal(true)
    assert_that(hp_unlock.get("accepted", false)).is_equal(true)
    assert_that(enqueue_result.get("accepted", false)).is_equal(true)
    assert_array(Array(tick_result.get("completed_units", []))).is_equal([UNIT_TYPE])

    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["attack", "damage"],
        ["attack", "damage"],
        ["attack_multiplier", "attack", "damage_multiplier", "damage"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["hp", "hit_points", "health"],
        ["hp", "hit_points", "health"],
        ["hp_multiplier", "hp", "health_multiplier", "health"]
    )

# ACC:T17.11
# acceptance: in-game unlock + train validation confirms all configured stat channels use baseline x multipliers.
func test_unlock_train_and_validate_attack_speed_damage_production_speed_range_hp_and_cost() -> void:
    var bridge = _new_bridge()
    var baseline_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", UNIT_TYPE))

    var unlock_plan = [
        {"tech_id": "tech_t17_attack_speed_10", "stat": "attack_speed", "multiplier": 1.10},
        {"tech_id": "tech_t17_damage_15", "stat": "damage", "multiplier": 1.15},
        {"tech_id": "tech_t17_production_speed_20", "stat": "production_speed", "multiplier": 1.20},
        {"tech_id": "tech_t17_range_25", "stat": "range", "multiplier": 1.25},
        {"tech_id": "tech_t17_hp_30", "stat": "hp", "multiplier": 1.30},
        {"tech_id": "tech_t17_cost_90", "stat": "cost", "multiplier": 0.90}
    ]

    for step in unlock_plan:
        var unlock_result = _as_dictionary(
            bridge.call(
                "TryApplyTechModifierForTest",
                str(step.get("tech_id", "")),
                UNIT_TYPE,
                str(step.get("stat", "")),
                float(step.get("multiplier", 1.0))
            )
        )
        assert_that(unlock_result.get("accepted", false)).is_equal(true)

    var enqueue_result = _as_dictionary(bridge.call("EnqueueUpfront", UNIT_TYPE, 1, 0, 0))
    var tick_result = _as_dictionary(bridge.call("Tick", 1))

    var multipliers = _as_dictionary(bridge.call("GetTrainingMultipliersForTest", UNIT_TYPE))
    var trained_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", UNIT_TYPE))

    assert_that(enqueue_result.get("accepted", false)).is_equal(true)
    assert_array(Array(tick_result.get("completed_units", []))).is_equal([UNIT_TYPE])

    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["attack_speed", "attackspeed", "atk_speed"],
        ["attack_speed", "attackspeed", "atk_speed"],
        ["attack_speed_multiplier", "attack_speed", "attackspeed_multiplier", "attackspeed"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["damage", "attack"],
        ["damage", "attack"],
        ["damage_multiplier", "damage", "attack_multiplier", "attack"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["production_speed", "training_speed", "train_speed"],
        ["production_speed", "training_speed", "train_speed"],
        ["production_speed_multiplier", "production_speed", "training_speed_multiplier", "training_speed"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["range"],
        ["range"],
        ["range_multiplier", "range"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["hp", "hit_points", "health"],
        ["hp", "hit_points", "health"],
        ["hp_multiplier", "hp", "health_multiplier", "health"]
    )
    _assert_stat_matches_multiplier(
        baseline_stats,
        trained_stats,
        multipliers,
        ["cost", "gold_cost", "training_cost"],
        ["cost", "gold_cost", "training_cost"],
        ["cost_multiplier", "cost", "gold_cost_multiplier", "gold_cost", "training_cost_multiplier", "training_cost"]
    )
