extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _simulate_core_loop(config: Dictionary, ticks: int) -> Array:
	var balance := int(config.get("initial_balance", 0))
	var income_per_tick := int(config.get("income_per_tick", 0))
	var upkeep_per_tick := int(config.get("upkeep_per_tick", 0))
	var clamp_min := int(config.get("clamp_min", -2147483648))
	var outputs: Array = []

	for _i in range(ticks):
		balance += income_per_tick - upkeep_per_tick
		if balance < clamp_min:
			balance = clamp_min
		outputs.append(balance)

	return outputs


# acceptance: ACC:T2.6
func test_balance_simulation_is_deterministic_when_config_is_unchanged() -> void:
	var config := {
		"initial_balance": 100,
		"income_per_tick": 13,
		"upkeep_per_tick": 6,
		"clamp_min": 0,
	}

	var run_a := _simulate_core_loop(config, 10)
	var run_b := _simulate_core_loop(config, 10)

	assert_that(run_a).is_equal(run_b)
	assert_that(run_a.size()).is_equal(10)
	assert_that(run_a[0]).is_equal(107)
	assert_that(run_a[9]).is_equal(170)


func test_balance_simulation_respects_minimum_clamp() -> void:
	var config := {
		"initial_balance": 2,
		"income_per_tick": 0,
		"upkeep_per_tick": 5,
		"clamp_min": 0,
	}

	var outputs := _simulate_core_loop(config, 3)

	assert_that(outputs).is_equal([0, 0, 0])
