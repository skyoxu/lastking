extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DAY_LENGTH_TICKS: int = 3

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
