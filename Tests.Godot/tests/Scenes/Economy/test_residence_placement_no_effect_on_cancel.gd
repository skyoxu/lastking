extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")

func _new_runtime() -> Dictionary:
	var root_node = Node.new()
	get_tree().get_root().add_child(auto_free(root_node))
	var runtime = ResidenceEconomyRuntimeProbe.new()
	root_node.add_child(runtime)
	await get_tree().process_frame
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 120, 150, 5)
	return {
		"root_node": root_node,
		"runtime": runtime,
	}

func _free_runtime(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()


# acceptance: ACC:T14.10
func test_cancelled_residence_placement_keeps_gold_and_population_cap_unchanged_and_does_not_start_tax_schedule() -> void:
	var data = await _new_runtime()
	var runtime = data["runtime"]
	var gold_before = int(runtime.get("Gold"))
	var population_cap_before = int(runtime.get("PopulationCap"))
	var timer = runtime.get("TaxTimer") as Timer

	runtime.call("ApplyPlacementResult", false)
	runtime.call("AdvanceSeconds", 30)

	assert_int(int(runtime.get("Gold"))).is_equal(gold_before)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(population_cap_before)
	assert_bool(bool(runtime.get("IsTaxScheduleRunning"))).is_false()
	assert_that(timer is Timer).is_true()
	assert_bool(timer.is_stopped()).is_true()

	_free_runtime(data)


func test_invalid_residence_placement_keeps_state_unchanged_and_prevents_tax_ticks() -> void:
	var data = await _new_runtime()
	var runtime = data["runtime"]
	var gold_before = int(runtime.get("Gold"))
	var population_cap_before = int(runtime.get("PopulationCap"))
	var timer = runtime.get("TaxTimer") as Timer

	runtime.call("ApplyBlockedPlacementForTest")
	runtime.call("AdvanceSeconds", 45)

	assert_bool(bool(runtime.get("LastPlacementAcceptedForTest"))).is_false()
	assert_int(int(runtime.get("Gold"))).is_equal(gold_before)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(population_cap_before)
	assert_bool(bool(runtime.get("IsTaxScheduleRunning"))).is_false()
	assert_that(timer is Timer).is_true()
	assert_bool(timer.is_stopped()).is_true()

	_free_runtime(data)
