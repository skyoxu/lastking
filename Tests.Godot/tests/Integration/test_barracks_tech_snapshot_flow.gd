extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BarracksTrainingQueueBridge = preload("res://Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs")

func _new_bridge() -> Node:
	var bridge = BarracksTrainingQueueBridge.new()
	add_child(auto_free(bridge))
	bridge.call("ResetRuntime", 240, 120, 3)
	return bridge

func _as_dictionary(value: Variant) -> Dictionary:
	if typeof(value) == TYPE_DICTIONARY:
		return value
	return {}

func _as_float(value: Variant, fallback: float = -1.0) -> float:
	var value_type = typeof(value)
	if value_type == TYPE_FLOAT or value_type == TYPE_INT:
		return float(value)
	return fallback

# ACC:T17.22
# acceptance: tech effects propagate through runtime snapshots for barracks stat computation,
# and the next snapshot after unlock state change reflects new multipliers.
func test_next_snapshot_reflects_unlock_state_change_for_barracks_stats() -> void:
	var bridge = _new_bridge()

	var baseline_snapshot = _as_dictionary(bridge.call("GetBarracksTechSnapshotForTest", "spearman"))
	var baseline_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", "spearman"))

	var apply_result = _as_dictionary(
		bridge.call("TryApplyTechModifierForTest", "tech_barracks_attack_i", "spearman", "attack", 1.20)
	)
	var next_snapshot = _as_dictionary(bridge.call("GetBarracksTechSnapshotForTest", "spearman"))
	var next_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", "spearman"))

	assert_that(apply_result.get("accepted", false)).is_equal(true)

	var baseline_multiplier = _as_float(baseline_snapshot.get("attack_multiplier", 1.0), 1.0)
	var next_multiplier = _as_float(next_snapshot.get("attack_multiplier", -1.0))
	assert_bool(next_multiplier > baseline_multiplier).is_true()

	var baseline_attack = _as_float(baseline_stats.get("attack", 0.0), 0.0)
	var next_attack = _as_float(next_stats.get("attack", -1.0))
	assert_bool(next_attack > baseline_attack).is_true()

func test_already_produced_snapshot_remains_unchanged_after_new_unlock() -> void:
	var bridge = _new_bridge()

	var first_unlock = _as_dictionary(
		bridge.call("TryApplyTechModifierForTest", "tech_barracks_attack_i", "spearman", "attack", 1.10)
	)
	assert_that(first_unlock.get("accepted", false)).is_equal(true)

	var produced_snapshot = _as_dictionary(bridge.call("GetBarracksTechSnapshotForTest", "spearman"))
	var produced_multiplier_before = _as_float(produced_snapshot.get("attack_multiplier", -1.0))

	var second_unlock = _as_dictionary(
		bridge.call("TryApplyTechModifierForTest", "tech_barracks_attack_ii", "spearman", "attack", 1.25)
	)
	assert_that(second_unlock.get("accepted", false)).is_equal(true)

	var current_snapshot = _as_dictionary(bridge.call("GetBarracksTechSnapshotForTest", "spearman"))
	var produced_multiplier_after = _as_float(produced_snapshot.get("attack_multiplier", -1.0))
	var current_multiplier = _as_float(current_snapshot.get("attack_multiplier", -1.0))

	assert_that(produced_multiplier_before).is_equal(1.10)
	assert_that(current_multiplier).is_equal(1.25)
	assert_that(produced_multiplier_after).is_equal(produced_multiplier_before)
