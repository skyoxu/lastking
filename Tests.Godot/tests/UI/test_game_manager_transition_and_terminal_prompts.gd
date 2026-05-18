extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
	var stale_bus := get_tree().get_root().get_node_or_null("EventBus")
	if stale_bus != null:
		if stale_bus.get_parent() != null:
			stale_bus.get_parent().remove_child(stale_bus)
		stale_bus.free()
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

func _runtime_prompt_panel(hud: Node) -> Control:
	return hud.get_node("FeedbackLayer/RuntimePromptPanel")

func _outcome_panel(hud: Node) -> Control:
	return hud.get_node("FeedbackLayer/OutcomePanel")

func _runtime_prompt_label(hud: Node) -> Label:
	return hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

func _outcome_label(hud: Node) -> Label:
	return hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")

# acceptance: ACC:T19.1
# acceptance: ACC:T42.3
# acceptance: ACC:T42.10
# ACC:T53.1
# ACC:T55.3
func test_terminal_and_reward_runtime_events_should_use_transient_feedback_and_keep_legacy_panels_hidden() -> void:
	var hud = await _hud()
	var feedback_label := _feedback_label(hud)
	var runtime_prompt_panel := _runtime_prompt_panel(hud)
	var outcome_panel := _outcome_panel(hud)
	var runtime_prompt := _runtime_prompt_label(hud)
	var outcome_label := _outcome_label(hud)

	assert_bool(runtime_prompt_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(runtime_prompt.text.find("n/a") >= 0).is_true()
	assert_bool(outcome_label.text.find("n/a") >= 0).is_true()

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
	assert_bool(feedback_label.text.find("artifact+1") >= 0).is_true()
	assert_bool(feedback_label.text.find("gold+600") >= 0).is_true()
	assert_bool(feedback_label.text.find("tech+3") >= 0).is_true()
	assert_bool(runtime_prompt_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()

	_publish("core.run.state.transitioned", {
		"outcome": "Win",
		"day": 15,
		"castle_hp": 80
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=15") >= 0).is_true()
	assert_bool(runtime_prompt_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(runtime_prompt.text.find("n/a") >= 0).is_true()
	assert_bool(outcome_label.text.find("n/a") >= 0).is_true()

	_publish("core.run.state.transitioned", {
		"outcome": "Loss",
		"day": 8,
		"castle_hp": 0
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=8") >= 0).is_true()
	assert_bool(runtime_prompt_panel.visible).is_false()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(runtime_prompt.text.find("n/a") >= 0).is_true()
	assert_bool(outcome_label.text.find("n/a") >= 0).is_true()

# ACC:T53.3
# ACC:T53.7
func test_terminal_feedback_should_ignore_non_terminal_outcome_and_keep_legacy_labels_neutral() -> void:
	var hud = await _hud()
	var feedback_label := _feedback_label(hud)
	var runtime_prompt := _runtime_prompt_label(hud)
	var outcome_label := _outcome_label(hud)
	var prompt_before := String(runtime_prompt.text)
	var outcome_before := String(outcome_label.text)

	_publish("core.run.state.transitioned", {
		"outcome": "NONE",
		"day": 3,
		"castle_hp": 90
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_false()
	assert_str(runtime_prompt.text).is_equal(prompt_before)
	assert_str(outcome_label.text).is_equal(outcome_before)

	_publish("core.run.state.transitioned", {
		"outcome": "UNKNOWN",
		"day": 4,
		"castle_hp": 70
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_false()
	assert_str(runtime_prompt.text).is_equal(prompt_before)
	assert_str(outcome_label.text).is_equal(outcome_before)

	_publish("core.run.state.transitioned", {
		"outcome": "Win",
		"day": 10,
		"castle_hp": 66
	})
	await get_tree().process_frame
	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=10") >= 0).is_true()
	assert_str(runtime_prompt.text).is_equal(prompt_before)
	assert_str(outcome_label.text).is_equal(outcome_before)

	_publish("core.run.state.transitioned", {
		"outcome": "NONE",
		"day": 11,
		"castle_hp": 65
	})
	await get_tree().process_frame
	assert_str(runtime_prompt.text).is_equal(prompt_before)
	assert_str(outcome_label.text).is_equal(outcome_before)
