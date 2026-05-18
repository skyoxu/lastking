extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DAY_LENGTH_TICKS: int = 3
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

enum Phase {
	DAY,
	NIGHT
}

enum Outcome {
	NONE,
	WIN,
	LOSE
}

class FakeDayNightWinLoseFlow:
	var tick_count: int = 0
	var phase: int = Phase.DAY
	var reached_goal: bool = false
	var reached_failure: bool = false
	var outcome: int = Outcome.NONE

	func advance_tick() -> void:
		tick_count += 1
		if tick_count >= DAY_LENGTH_TICKS:
			phase = Phase.NIGHT

	func resolve_outcome() -> void:
		if reached_goal:
			outcome = Outcome.WIN
		elif reached_failure:
			outcome = Outcome.LOSE
		else:
			outcome = Outcome.NONE

func _is_neutral_legacy_label(label: Label) -> bool:
	return String(label.text).find("n/a") >= 0

# acceptance: ACC:T19.2
func test_cycle_boundaries_and_terminal_outcomes_in_one_run() -> void:
	var sut := FakeDayNightWinLoseFlow.new()

	sut.advance_tick()
	sut.advance_tick()
	sut.advance_tick()
	assert_that(sut.phase).is_equal(Phase.NIGHT)

	sut.reached_goal = true
	sut.reached_failure = true
	sut.resolve_outcome()
	assert_that(sut.outcome).is_equal(Outcome.WIN)

func test_outcome_remains_none_when_no_terminal_flags_are_set() -> void:
	var sut := FakeDayNightWinLoseFlow.new()

	sut.resolve_outcome()

	assert_that(sut.outcome).is_equal(Outcome.NONE)

# ACC:T53.1
# ACC:T53.3
# ACC:T53.7
func test_hud_terminal_transitions_should_use_feedback_label_and_keep_legacy_after_action_panels_neutral() -> void:
	var hud = await _hud()
	var feedback_label: Label = hud.get_node("FeedbackLayer/FeedbackLabel")
	var outcome_panel: Control = hud.get_node("FeedbackLayer/OutcomePanel")
	var prompt_panel: Control = hud.get_node("FeedbackLayer/RuntimePromptPanel")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	assert_bool(_is_neutral_legacy_label(outcome_label)).is_true()
	assert_bool(_is_neutral_legacy_label(prompt_label)).is_true()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()

	_publish("core.lastking.castle.hp_changed", {"Day": 14, "PreviousHp": 100, "CurrentHp": 51})
	_publish("core.lastking.wave.spawned", {"day": 14, "count": 6})
	_publish("core.run.state.transitioned", {"outcome": "win", "day": 14})
	await get_tree().process_frame

	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=14") >= 0).is_true()
	assert_bool(_is_neutral_legacy_label(outcome_label)).is_true()
	assert_bool(_is_neutral_legacy_label(prompt_label)).is_true()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()

	_publish("core.lastking.castle.hp_changed", {"Day": 15, "PreviousHp": 10, "CurrentHp": 0})
	_publish("core.lastking.wave.spawned", {"day": 15, "count": 8})
	_publish("core.run.state.transitioned", {"outcome": "loss", "day": 15})
	await get_tree().process_frame

	assert_bool(feedback_label.visible).is_true()
	assert_bool(feedback_label.text.find("day=15") >= 0).is_true()
	assert_bool(_is_neutral_legacy_label(outcome_label)).is_true()
	assert_bool(_is_neutral_legacy_label(prompt_label)).is_true()
	assert_bool(outcome_panel.visible).is_false()
	assert_bool(prompt_panel.visible).is_false()

	_publish("core.run.state.transitioned", {"outcome": "NONE", "day": 1})
	await get_tree().process_frame
	assert_bool(_is_neutral_legacy_label(outcome_label)).is_true()
	assert_bool(_is_neutral_legacy_label(prompt_label)).is_true()
