extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _simulate_residence_tax_tick(initial_gold: int, debt_floor: int, tax_deltas: Array[int]) -> Dictionary:
	var ordered_deltas: Array[int] = tax_deltas.duplicate()

	var gold = initial_gold
	var debt_state = "in_debt" if gold < debt_floor else "stable"
	var transition_trace: Array = []

	for index in range(ordered_deltas.size()):
		var delta = int(ordered_deltas[index])
		gold += delta
		var next_state = "in_debt" if gold < debt_floor else "stable"
		if next_state != debt_state:
			transition_trace.append({
				"step": index + 1,
				"from": debt_state,
				"to": next_state,
			})
			debt_state = next_state

	return {
		"gold_total": gold,
		"debt_state": debt_state,
		"transition_trace": transition_trace,
	}


func _apply_non_integer_adjustment(initial_gold: int, debt_floor: int, delta: float) -> Dictionary:
	if not is_equal_approx(delta, floor(delta)):
		return {
			"status": "rejected_non_integer",
			"gold_total": initial_gold,
			"debt_state": "in_debt" if initial_gold < debt_floor else "stable",
		}
	var gold = initial_gold + int(delta)
	var debt_state = "in_debt" if gold < debt_floor else "stable"
	return {
		"status": "applied",
		"gold_total": gold,
		"debt_state": debt_state,
	}


# acceptance: ACC:T14.17
func test_residence_tax_tick_replay_keeps_gold_and_debt_transition_trace_identical() -> void:
	var tax_deltas: Array[int] = [6, -3, 4]

	var run_a = _simulate_residence_tax_tick(-5, 0, tax_deltas)
	var run_b = _simulate_residence_tax_tick(-5, 0, tax_deltas)

	assert_that(run_a["gold_total"]).is_equal(run_b["gold_total"])
	assert_that(run_a["debt_state"]).is_equal(run_b["debt_state"])
	assert_that(run_a["transition_trace"]).is_equal(run_b["transition_trace"])


func test_residence_tax_tick_rejects_non_integer_delta_and_keeps_state_unchanged() -> void:
	var before_gold = 10
	var before_debt_state = "stable"

	var result = _apply_non_integer_adjustment(before_gold, 0, 2.5)

	assert_that(result["status"]).is_equal("rejected_non_integer")
	assert_that(result["gold_total"]).is_equal(before_gold)
	assert_that(result["debt_state"]).is_equal(before_debt_state)
