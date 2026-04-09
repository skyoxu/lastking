extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")

func _new_runtime() -> Dictionary:
	var root_node = Node.new()
	get_tree().get_root().add_child(auto_free(root_node))
	var runtime = ResidenceEconomyRuntimeProbe.new()
	runtime.set("TaxPerTick", 10)
	runtime.set("TaxPerTickLevel2", 15)
	root_node.add_child(runtime)
	await get_tree().process_frame
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 100, 150, 5)
	return {
		"root_node": root_node,
		"runtime": runtime,
	}

func _free_runtime(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()

# ACC:T14.11
# Every 15-second income tick should equal the sum of all eligible residence taxes.
func test_income_tick_equals_sum_of_all_eligible_residence_taxes() -> void:
	var data = await _new_runtime()
	var runtime = data["runtime"]

	runtime.call("PlaceResidenceWithLevelForTest", 1)
	runtime.call("PlaceResidenceWithLevelForTest", 2)
	runtime.call("AdvanceSeconds", 14)
	assert_int(int(runtime.get("Gold"))).is_equal(100)

	runtime.call("AdvanceSeconds", 1)

	assert_int(int(runtime.get("Gold"))).is_equal(125)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(11)

	_free_runtime(data)

func test_income_tick_ignores_unbuilt_or_unsettleable_residences() -> void:
	var data = await _new_runtime()
	var runtime = data["runtime"]

	runtime.call("ApplyBlockedPlacementForTest")
	runtime.call("PlaceResidenceWithLevelForTest", 1)
	runtime.call("AdvanceSeconds", 15)

	assert_bool(bool(runtime.get("LastPlacementAcceptedForTest"))).is_false()
	assert_int(int(runtime.get("Gold"))).is_equal(110)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(8)

	_free_runtime(data)
