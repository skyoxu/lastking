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

func _run_seeded_unlock_sequence(seed_value: int, unlock_sequence: Array, unit_type: String) -> Dictionary:
	var bridge = _new_bridge()
	var seed_result = _as_dictionary(bridge.call("SetTechDeterministicSeedForTest", seed_value))
	var applied_trace: Array = []

	for step in unlock_sequence:
		var item = Dictionary(step)
		var apply_result = _as_dictionary(
			bridge.call(
				"TryApplyTechModifierForTest",
				str(item.get("tech_id", "")),
				str(item.get("unit_type", unit_type)),
				str(item.get("stat", "")),
				float(item.get("multiplier_delta", 1.0))
			)
		)
		applied_trace.append({
			"tech_id": str(item.get("tech_id", "")),
			"accepted": bool(apply_result.get("accepted", false)),
			"reason": str(apply_result.get("reason", "")),
		})

	var final_multipliers = _as_dictionary(bridge.call("GetTrainingMultipliersForTest", unit_type))
	var trained_unit_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", unit_type))

	var enqueue_result = _as_dictionary(bridge.call("EnqueueUpfront", unit_type, 1, 0, 0))
	var tick_result = _as_dictionary(bridge.call("Tick", 1))

	return {
		"seed_result": seed_result,
		"applied_trace": applied_trace,
		"enqueue_result": enqueue_result,
		"completed_units": Array(tick_result.get("completed_units", [])),
		"final_multipliers": final_multipliers,
		"trained_unit_stats": trained_unit_stats,
	}

# ACC:T17.25
# acceptance: fixed seeds with identical config and unlock sequences must produce identical
# tech outcomes, including final multipliers and barracks-trained unit stats.
func test_barracks_tech_seeded_runs_with_identical_inputs_produce_identical_outcomes() -> void:
	var unlock_sequence = [
		{
			"tech_id": "tech_barracks_attack_i",
			"unit_type": "spearman",
			"stat": "attack",
			"multiplier_delta": 1.10,
		},
		{
			"tech_id": "tech_barracks_hp_i",
			"unit_type": "spearman",
			"stat": "hp",
			"multiplier_delta": 1.05,
		},
	]

	var run_a = _run_seeded_unlock_sequence(1701, unlock_sequence, "spearman")
	var run_b = _run_seeded_unlock_sequence(1701, unlock_sequence, "spearman")

	assert_bool(Dictionary(run_a.get("seed_result", {})).is_empty()).is_false()
	assert_that(Dictionary(run_a.get("enqueue_result", {})).get("accepted", false)).is_equal(true)
	assert_array(Array(run_a.get("completed_units", []))).is_equal(["spearman"])
	assert_bool(Dictionary(run_a.get("final_multipliers", {})).is_empty()).is_false()
	assert_bool(Dictionary(run_a.get("trained_unit_stats", {})).is_empty()).is_false()
	assert_that(run_a).is_equal(run_b)

func test_barracks_tech_seeded_runs_with_sequence_change_should_change_outcome() -> void:
	var base_sequence = [
		{
			"tech_id": "tech_barracks_attack_i",
			"unit_type": "spearman",
			"stat": "attack",
			"multiplier_delta": 1.10,
		},
	]
	var changed_sequence = [
		{
			"tech_id": "tech_barracks_hp_i",
			"unit_type": "spearman",
			"stat": "hp",
			"multiplier_delta": 1.10,
		},
	]

	var base_run = _run_seeded_unlock_sequence(1701, base_sequence, "spearman")
	var changed_run = _run_seeded_unlock_sequence(1701, changed_sequence, "spearman")

	var multipliers_same = Dictionary(base_run.get("final_multipliers", {})) == Dictionary(changed_run.get("final_multipliers", {}))
	var stats_same = Dictionary(base_run.get("trained_unit_stats", {})) == Dictionary(changed_run.get("trained_unit_stats", {}))

	assert_bool(multipliers_same).is_false()
	assert_bool(stats_same).is_false()
