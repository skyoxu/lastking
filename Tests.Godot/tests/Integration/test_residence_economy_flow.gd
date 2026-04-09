extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")

func _new_flow() -> Dictionary:
	var root_node = Node.new()
	get_tree().get_root().add_child(auto_free(root_node))
	var runtime = ResidenceEconomyRuntimeProbe.new()
	runtime.set("PopulationCapDelta", 3)
	runtime.set("TaxPerTick", 7)
	runtime.set("TaxPerTickLevel2", 11)
	root_node.add_child(runtime)
	await get_tree().process_frame
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 0, 150, 5)
	return {
		"root_node": root_node,
		"runtime": runtime,
	}

func _free_flow(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()

# acceptance: ACC:T14.1
func test_successful_build_requires_cap_increase_and_tax_schedule_started() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("ApplyPlacementResult", true)

	assert_int(int(runtime.get("PopulationCap"))).is_equal(8)
	assert_bool(bool(runtime.get("IsTaxScheduleRunning"))).is_true()

	_free_flow(data)

# acceptance: ACC:T14.13
func test_population_cap_growth_requires_additional_successful_placements() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("ApplyPlacementResult", true)
	var cap_after_first = int(runtime.get("PopulationCap"))
	runtime.call("ApplyPlacementResult", false)
	runtime.call("ApplyPlacementResult", true)

	assert_int(cap_after_first).is_equal(8)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(11)

	_free_flow(data)

# acceptance: ACC:T14.4
func test_successful_placement_increases_population_cap_immediately_and_once_per_action() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("ApplyPlacementResult", true)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(8)

	runtime.call("ApplyPlacementResult", false)
	assert_int(int(runtime.get("PopulationCap"))).is_equal(8)

	_free_flow(data)

# acceptance: ACC:T14.6
func test_tax_settlement_uses_level_config_value_when_present() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("PlaceResidenceWithLevelForTest", 2)
	runtime.call("AdvanceSeconds", 15)

	assert_int(int(runtime.get("Gold"))).is_equal(11)

	_free_flow(data)

# acceptance: ACC:T14.8
func test_no_built_residence_produces_no_tax_income() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("AdvanceSeconds", 45)

	assert_int(int(runtime.get("Gold"))).is_equal(0)

	_free_flow(data)

# acceptance: ACC:T14.9
func test_runtime_flow_build_once_then_gain_gold_every_15_seconds() -> void:
	var data = await _new_flow()
	var runtime = data["runtime"]

	runtime.call("ApplyPlacementResult", true)
	runtime.call("AdvanceSeconds", 15)
	runtime.call("AdvanceSeconds", 15)

	assert_int(int(runtime.get("PopulationCap"))).is_equal(8)
	assert_int(int(runtime.get("Gold"))).is_equal(14)

	_free_flow(data)
