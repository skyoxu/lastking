extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")

func _new_runtime(tax_per_tick: int = 6, population_cap_delta: int = 3) -> Dictionary:
	var root_node = Node.new()
	var runtime = ResidenceEconomyRuntimeProbe.new()
	runtime.set("TaxPerTick", tax_per_tick)
	runtime.set("PopulationCapDelta", population_cap_delta)
	root_node.add_child(runtime)
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 100, 150, 5)
	runtime.call("ApplyPlacementResult", true)
	return {
		"root_node": root_node,
		"runtime": runtime,
	}

func _free_runtime(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()

func _run_observation_sequence(frame_dt: float) -> Dictionary:
	var data = _new_runtime(6, 4)
	var runtime = data["runtime"]
	runtime.call("AdvanceSimulation", 10.0, frame_dt)
	runtime.call("AdvanceSimulation", 20.0, frame_dt)
	runtime.call("AdvanceSimulation", 15.0, frame_dt)
	var result = {
		"gold": int(runtime.get("Gold")),
		"population_cap": int(runtime.get("PopulationCap")),
	}
	_free_runtime(data)
	return result

# acceptance: ACC:T14.12
func test_residence_tax_timer_node_is_bound_to_15s_and_writes_to_resource_manager() -> void:
	var data = _new_runtime(9, 2)
	var runtime = data["runtime"]
	var timer = runtime.get("TaxTimer") as Timer

	assert_that(timer is Timer).is_true()
	assert_that(timer.wait_time).is_equal(15.0)
	assert_that(timer.one_shot).is_false()
	assert_int(timer.timeout.get_connections().size()).is_greater(0)

	runtime.call("TriggerTimeoutForTest")

	assert_int(int(runtime.get("Gold"))).is_equal(109)

	_free_runtime(data)

# acceptance: ACC:T14.2
func test_residence_tax_changes_gold_only_on_15_second_boundaries() -> void:
	var data = _new_runtime(5, 1)
	var runtime = data["runtime"]

	runtime.call("AdvanceSeconds", 14)
	assert_int(int(runtime.get("Gold"))).is_equal(100)

	runtime.call("AdvanceSeconds", 1)
	assert_int(int(runtime.get("Gold"))).is_equal(105)

	runtime.call("AdvanceSeconds", 14)
	assert_int(int(runtime.get("Gold"))).is_equal(105)

	runtime.call("AdvanceSeconds", 1)
	assert_int(int(runtime.get("Gold"))).is_equal(110)

	_free_runtime(data)

# acceptance: ACC:T14.3
func test_residence_tax_stays_zero_before_first_timeout_and_settles_once_per_timeout() -> void:
	var data = _new_runtime(4, 3)
	var runtime = data["runtime"]

	runtime.call("AdvanceSeconds", 14)
	assert_int(int(runtime.get("Gold"))).is_equal(100)

	runtime.call("AdvanceSeconds", 1)
	assert_int(int(runtime.get("Gold"))).is_equal(104)

	runtime.call("AdvanceSeconds", 0)
	assert_int(int(runtime.get("Gold"))).is_equal(104)

	runtime.call("AdvanceSeconds", 15)
	assert_int(int(runtime.get("Gold"))).is_equal(108)

	_free_runtime(data)

# acceptance: ACC:T14.5
func test_single_residence_observed_for_45_seconds_ticks_only_at_15_30_45() -> void:
	var data = _new_runtime(7, 2)
	var runtime = data["runtime"]

	runtime.call("AdvanceSeconds", 45)

	assert_int(int(runtime.get("Gold"))).is_equal(121)

	_free_runtime(data)

# acceptance: ACC:T14.15
func test_residence_economy_is_identical_for_same_build_and_time_sequence_across_frame_rates() -> void:
	var run_60fps_like = _run_observation_sequence(1.0)
	var run_120fps_like = _run_observation_sequence(0.5)

	assert_that(run_60fps_like["gold"]).is_equal(run_120fps_like["gold"])
	assert_that(run_60fps_like["population_cap"]).is_equal(run_120fps_like["population_cap"])
