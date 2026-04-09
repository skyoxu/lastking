extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")

func _new_runtime(tax_per_tick: int = 7) -> Dictionary:
	var root_node = Node.new()
	get_tree().get_root().add_child(auto_free(root_node))
	var runtime = ResidenceEconomyRuntimeProbe.new()
	runtime.set("TaxPerTick", tax_per_tick)
	root_node.add_child(runtime)
	await get_tree().process_frame
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 100, 150, 5)
	runtime.call("ApplyPlacementResult", true)
	await get_tree().process_frame
	return {
		"root_node": root_node,
		"runtime": runtime,
	}

func _free_runtime(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()

# acceptance: ACC:T14.15
func test_residence_tax_timer_node_binding_requires_15_second_cadence_and_connected_timeout() -> void:
	var data = await _new_runtime(6)
	var runtime = data["runtime"]
	var timer = runtime.get("TaxTimer") as Timer

	assert_that(timer is Timer).is_true()
	assert_that(timer.name).is_equal("ResidenceTaxTimer")
	assert_that(timer.wait_time).is_equal(15.0)
	assert_that(timer.one_shot).is_false()
	assert_int(timer.timeout.get_connections().size()).is_greater(0)

	_free_runtime(data)

func test_residence_tax_timer_timeout_writes_tax_into_resource_manager() -> void:
	var data = await _new_runtime(9)
	var runtime = data["runtime"]

	runtime.call("TriggerTimeoutForTest")

	assert_int(int(runtime.get("Gold"))).is_equal(109)

	_free_runtime(data)

func test_residence_tax_timer_does_not_write_before_timeout_signal() -> void:
	var data = await _new_runtime(5)
	var runtime = data["runtime"]

	assert_int(int(runtime.get("Gold"))).is_equal(100)

	_free_runtime(data)
