extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ResidenceEconomyRuntimeProbe = preload("res://Game.Godot/Scripts/Runtime/ResidenceEconomyRuntimeProbe.cs")
const EventBusAdapter = preload("res://Game.Godot/Adapters/EventBusAdapter.cs")

var _seen_tax_events: Array = []

func _on_domain_event(type, _source, data_json, _id, _spec, _ct, _ts) -> void:
	if str(type) != "core.lastking.tax.collected":
		return
	var payload = JSON.parse_string(str(data_json))
	_seen_tax_events.append(payload)

func _new_runtime_in_tree(tax_per_tick: int = 7, apply_build: bool = true, install_event_bus: bool = false) -> Dictionary:
	var scene_root = Node.new()
	get_tree().get_root().add_child(auto_free(scene_root))
	var runtime = ResidenceEconomyRuntimeProbe.new()
	runtime.set("TaxPerTick", tax_per_tick)
	scene_root.add_child(runtime)

	var bus: Node = get_tree().get_root().get_node_or_null("EventBus")
	if install_event_bus and bus == null:
		bus = EventBusAdapter.new()
		bus.name = "EventBus"
		get_tree().get_root().add_child(auto_free(bus))

	await get_tree().process_frame
	runtime.call("EnsureReadyForTest")
	runtime.call("SetBaselineForTest", 100, 150, 5)
	if apply_build:
		runtime.call("ApplyPlacementResult", true)
		await get_tree().process_frame
	return {
		"root_node": scene_root,
		"runtime": runtime,
		"event_bus": bus,
	}

func _free_runtime(data: Dictionary) -> void:
	var root_node = data["root_node"] as Node
	if root_node != null:
		root_node.queue_free()

# acceptance: ACC:T14.1
func test_residence_tax_timer_starts_only_after_successful_build() -> void:
	var data = await _new_runtime_in_tree(7, false, false)
	var runtime = data["runtime"]
	var timer = runtime.get("TaxTimer") as Timer

	assert_that(timer is Timer).is_true()
	assert_bool(timer.is_stopped()).is_true()
	assert_bool(bool(runtime.get("IsTaxScheduleRunning"))).is_false()

	runtime.call("ApplyPlacementResult", true)
	await get_tree().process_frame

	assert_bool(timer.is_stopped()).is_false()
	assert_bool(bool(runtime.get("IsTaxScheduleRunning"))).is_true()

	_free_runtime(data)

func test_residence_tax_timeout_emits_tax_collected_domain_event() -> void:
	_seen_tax_events.clear()
	var data = await _new_runtime_in_tree(9, true, true)
	var runtime = data["runtime"]
	var bus = data["event_bus"] as Node

	bus.connect("DomainEventEmitted", Callable(self, "_on_domain_event"), CONNECT_ONE_SHOT)

	runtime.call("TriggerTimeoutForTest")
	await get_tree().process_frame

	assert_int(_seen_tax_events.size()).is_equal(1)
	if _seen_tax_events.is_empty():
		_free_runtime(data)
		return
	assert_int(int(_seen_tax_events[0]["GoldDelta"])).is_equal(9)
	assert_int(int(_seen_tax_events[0]["TotalGold"])).is_equal(109)
	assert_str(str(_seen_tax_events[0]["ResidenceId"])).is_not_empty()

	_free_runtime(data)
