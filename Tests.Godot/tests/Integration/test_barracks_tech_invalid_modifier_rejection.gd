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

# acceptance: ACC:T17.24
func test_invalid_tech_modifier_is_rejected_and_runtime_results_remain_unchanged() -> void:
	var bridge = _new_bridge()

	var baseline_multipliers = _as_dictionary(bridge.call("GetTrainingMultipliersForTest", "spearman"))
	var baseline_stats = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", "spearman"))

	var rejection = _as_dictionary(
		bridge.call("TryApplyTechModifierForTest", "tech_overcap_attack", "spearman", "attack", 9.99)
	)

	var multipliers_after_reject = _as_dictionary(bridge.call("GetTrainingMultipliersForTest", "spearman"))
	var stats_after_reject = _as_dictionary(bridge.call("PreviewTrainedUnitStatsForTest", "spearman"))

	assert_that(rejection.get("accepted", true)).is_equal(false)
	assert_that(str(rejection.get("reason", ""))).is_equal("modifier_out_of_range")
	assert_that(multipliers_after_reject).is_equal(baseline_multipliers)
	assert_that(stats_after_reject).is_equal(baseline_stats)
