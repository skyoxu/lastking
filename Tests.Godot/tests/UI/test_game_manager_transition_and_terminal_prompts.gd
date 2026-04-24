extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))

func _hud() -> Node:
	var hud = preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud

func _publish(type_name: String, payload: Dictionary) -> void:
	_bus.PublishSimple(type_name, "ut", JSON.stringify(payload))

func _feedback_label(hud: Node) -> Label:
	return hud.get_node("FeedbackLayer/FeedbackLabel")

# acceptance: ACC:T19.1
# acceptance: ACC:T42.3
# acceptance: ACC:T42.10
func test_terminal_and_reward_runtime_events_should_drive_hud_feedback_surfaces() -> void:
	var hud = await _hud()
	var feedback_label := _feedback_label(hud)

	_publish("core.lastking.reward.offered", {
		"day_number": 5,
		"is_elite_night": true,
		"is_boss_night": false,
		"option_a": "artifact+1",
		"option_b": "gold+600",
		"option_c": "tech+3"
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("Reward offered.") >= 0).is_true()
	assert_bool(feedback_label.text.find("artifact+1") >= 0).is_true()
	assert_bool(feedback_label.text.find("gold+600") >= 0).is_true()
	assert_bool(feedback_label.text.find("tech+3") >= 0).is_true()

	_publish("core.run.state.transitioned", {
		"outcome": "Win",
		"day": 15,
		"castle_hp": 80
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("Victory!") >= 0).is_true()
	assert_bool(feedback_label.text.find("day=15") >= 0).is_true()

	_publish("core.run.state.transitioned", {
		"outcome": "Loss",
		"day": 8,
		"castle_hp": 0
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("Defeat.") >= 0).is_true()
	assert_bool(feedback_label.text.find("day=8") >= 0).is_true()

func test_terminal_feedback_should_ignore_non_terminal_outcome() -> void:
	var hud = await _hud()
	var feedback_label := _feedback_label(hud)

	_publish("core.run.state.transitioned", {
		"outcome": "NONE",
		"day": 3,
		"castle_hp": 90
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_false()
