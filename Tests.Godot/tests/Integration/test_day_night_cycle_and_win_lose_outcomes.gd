extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DAY_LENGTH_TICKS: int = 3
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

# acceptance: ACC:T19.2
# deterministic end-to-end run validates cycle boundaries and terminal outcomes.
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
func test_hud_after_action_surfaces_should_render_for_win_loss_and_reset_to_neutral_without_outcome() -> void:
	var hud = await _hud()
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	assert_that(outcome_label.text).is_equal("Outcome: n/a")
	assert_that(prompt_label.text).is_equal("Prompt: n/a")

	_publish("core.lastking.castle.hp_changed", {"Day": 14, "PreviousHp": 100, "CurrentHp": 51})
	_publish("core.lastking.wave.spawned", {"day": 14, "count": 6})
	_publish("core.lastking.resources.changed", {
		"RunId": "run-i53-win",
		"DayNumber": 14,
		"Gold": 120,
		"Iron": 40,
		"PopulationCap": 25
	})
	_publish("core.run.state.transitioned", {"outcome": "win", "day": 14})
	await get_tree().process_frame

	assert_bool(outcome_label.text.find("Outcome: win day=14") >= 0).is_true()
	assert_bool(outcome_label.text.find("spawned=6") >= 0).is_true()
	assert_that(prompt_label.text).is_equal("Prompt: training: reinforce frontline")

	_publish("core.lastking.castle.hp_changed", {"Day": 15, "PreviousHp": 10, "CurrentHp": 0})
	_publish("core.lastking.wave.spawned", {"day": 15, "count": 8})
	_publish("core.lastking.resources.changed", {
		"RunId": "run-i53-loss",
		"DayNumber": 15,
		"Gold": 90,
		"Iron": 20,
		"PopulationCap": 18
	})
	_publish("core.run.state.transitioned", {"outcome": "loss", "day": 15})
	await get_tree().process_frame

	assert_bool(outcome_label.text.find("Outcome: loss day=15") >= 0).is_true()
	assert_bool(outcome_label.text.find("hp=0") >= 0).is_true()
	assert_that(prompt_label.text).is_equal("Prompt: training: reinforce frontline")

	_publish("core.run.state.transitioned", {"outcome": "NONE", "day": 1})
	await get_tree().process_frame
	assert_that(outcome_label.text).is_equal("Outcome: n/a")
	assert_that(prompt_label.text).is_equal("Prompt: n/a")
