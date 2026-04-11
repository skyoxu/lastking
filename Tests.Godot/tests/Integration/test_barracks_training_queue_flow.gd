extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _new_bridge() -> Node:
	var bridge = preload("res://Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs").new()
	add_child(auto_free(bridge))
	bridge.call("ResetRuntime", 240, 120, 3)
	return bridge

# ACC:T16.2
func test_training_queue_flow_acceptance_should_pass_on_windows_baseline() -> void:
	var bridge = _new_bridge()
	var first: Dictionary = bridge.call("EnqueueUpfront", "spearman", 3, 40, 10)
	var second: Dictionary = bridge.call("EnqueueUpfront", "archer", 2, 30, 8)
	assert_bool(first.get("accepted", false)).is_true()
	assert_bool(second.get("accepted", false)).is_true()
	assert_int(int(bridge.call("GetQueueLength"))).is_equal(2)
	assert_int(int(bridge.call("GetGold"))).is_equal(170)
	assert_int(int(bridge.call("GetIron"))).is_equal(102)

	var partial_tick: Dictionary = bridge.call("Tick", 2)
	assert_int(int(partial_tick.get("queue_length", -1))).is_equal(2)
	assert_array(partial_tick.get("completed_units", [])).is_empty()

	var first_complete: Dictionary = bridge.call("Tick", 1)
	assert_int(int(first_complete.get("queue_length", -1))).is_equal(1)
	assert_array(first_complete.get("completed_units", [])).is_equal(["spearman"])

	var second_complete: Dictionary = bridge.call("Tick", 2)
	assert_int(int(second_complete.get("queue_length", -1))).is_equal(0)
	assert_array(second_complete.get("completed_units", [])).is_equal(["archer"])

# ACC:T16.9
func test_training_queue_signals_should_reflect_enqueue_cancel_complete_changes() -> void:
	var bridge = _new_bridge()
	var ui_panel := Control.new()
	add_child(auto_free(ui_panel))
	var queue_label := Label.new()
	var resource_label := Label.new()
	ui_panel.add_child(queue_label)
	ui_panel.add_child(resource_label)
	var enqueue_events: Array = []
	var cancel_events: Array = []
	var complete_events: Array = []
	bridge.connect("QueueEnqueued", func(queue_length: int, gold: int, iron: int) -> void:
		enqueue_events.append({"len": queue_length, "gold": gold, "iron": iron})
		queue_label.text = "Queue:%d" % queue_length
		resource_label.text = "Gold:%d Iron:%d" % [gold, iron])
	bridge.connect("QueueCancelled", func(queue_length: int, gold: int, iron: int) -> void:
		cancel_events.append({"len": queue_length, "gold": gold, "iron": iron})
		queue_label.text = "Queue:%d" % queue_length
		resource_label.text = "Gold:%d Iron:%d" % [gold, iron])
	bridge.connect("QueueCompleted", func(unit_type: String, queue_length: int, gold: int, iron: int) -> void:
		complete_events.append({"unit": unit_type, "len": queue_length, "gold": gold, "iron": iron})
		queue_label.text = "Queue:%d" % queue_length
		resource_label.text = "Gold:%d Iron:%d" % [gold, iron])

	bridge.call("EnqueueUpfront", "spearman", 2, 40, 10)
	bridge.call("EnqueueUpfront", "archer", 2, 30, 8)
	bridge.call("CancelAt", 1)
	bridge.call("Tick", 2)

	assert_int(enqueue_events.size()).is_equal(2)
	assert_int(cancel_events.size()).is_equal(1)
	assert_int(complete_events.size()).is_equal(1)
	assert_str(str(complete_events[0]["unit"])).is_equal("spearman")
	assert_int(int(enqueue_events[0]["len"])).is_equal(1)
	assert_int(int(enqueue_events[0]["gold"])).is_equal(200)
	assert_int(int(enqueue_events[0]["iron"])).is_equal(110)
	assert_int(int(enqueue_events[1]["len"])).is_equal(2)
	assert_int(int(enqueue_events[1]["gold"])).is_equal(170)
	assert_int(int(enqueue_events[1]["iron"])).is_equal(102)
	assert_int(int(cancel_events[0]["len"])).is_equal(1)
	assert_int(int(cancel_events[0]["gold"])).is_equal(200)
	assert_int(int(cancel_events[0]["iron"])).is_equal(110)
	assert_int(int(complete_events[0]["len"])).is_equal(0)
	assert_int(int(complete_events[0]["gold"])).is_equal(200)
	assert_int(int(complete_events[0]["iron"])).is_equal(110)
	assert_str(queue_label.text).is_equal("Queue:0")
	assert_str(resource_label.text).is_equal("Gold:200 Iron:110")

	var bridge_multi = _new_bridge()
	var multi_complete_events: Array = []
	bridge_multi.connect("QueueCompleted", func(unit_type: String, queue_length: int, gold: int, iron: int) -> void:
		multi_complete_events.append({"unit": unit_type, "len": queue_length, "gold": gold, "iron": iron}))
	bridge_multi.call("EnqueueUpfront", "spearman", 1, 20, 5)
	bridge_multi.call("EnqueueUpfront", "archer", 1, 25, 7)
	var multi_tick: Dictionary = bridge_multi.call("Tick", 2)
	assert_array(multi_tick.get("completed_units", [])).is_equal(["spearman", "archer"])
	assert_int(multi_complete_events.size()).is_equal(2)
	assert_int(int(multi_complete_events[0]["len"])).is_equal(1)
	assert_int(int(multi_complete_events[1]["len"])).is_equal(0)
	assert_str(str(multi_complete_events[0]["unit"])).is_equal("spearman")
	assert_str(str(multi_complete_events[1]["unit"])).is_equal("archer")

# ACC:T16.19
func test_training_queue_flow_should_cover_multi_unit_cancel_and_refund_trace() -> void:
	var bridge = _new_bridge()
	bridge.call("EnqueueUpfront", "spearman", 4, 60, 20)
	bridge.call("EnqueueUpfront", "archer", 2, 30, 8)
	assert_int(int(bridge.call("GetGold"))).is_equal(150)
	assert_int(int(bridge.call("GetIron"))).is_equal(92)

	bridge.call("Tick", 1)
	var cancel_head: Dictionary = bridge.call("CancelAt", 0)
	assert_bool(cancel_head.get("accepted", false)).is_true()
	assert_int(int(cancel_head.get("refunded_gold", -1))).is_equal(60)
	assert_int(int(cancel_head.get("refunded_iron", -1))).is_equal(20)
	assert_int(int(bridge.call("GetGold"))).is_equal(210)
	assert_int(int(bridge.call("GetIron"))).is_equal(112)

	var done: Dictionary = bridge.call("Tick", 2)
	assert_array(done.get("completed_units", [])).is_equal(["archer"])
	assert_int(int(bridge.call("GetQueueLength"))).is_equal(0)
