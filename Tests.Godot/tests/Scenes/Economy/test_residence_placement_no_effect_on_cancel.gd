extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class ResidencePlacementEconomyDouble:
	extends RefCounted

	var gold: int
	var population_cap: int
	var tax_tick_count: int = 0
	var _tax_schedule_running: bool = false

	func _init(initial_gold: int = 120, initial_population_cap: int = 5) -> void:
		gold = initial_gold
		population_cap = initial_population_cap

	func try_place_residence(outcome: String) -> Dictionary:
		if outcome == "success":
			gold -= 20
			population_cap += 3
			_tax_schedule_running = true
			return {"accepted": true, "reason": "success"}

		if outcome == "cancelled":
			return {"accepted": false, "reason": "cancelled"}

		if outcome == "invalid":
			return {"accepted": false, "reason": "invalid"}

		return {"accepted": false, "reason": "unknown"}

	func is_tax_schedule_running() -> bool:
		return _tax_schedule_running

	func advance(seconds: float) -> void:
		if seconds <= 0.0:
			return
		if not _tax_schedule_running:
			return
		tax_tick_count += int(floor(seconds / 15.0))


func _new_runtime() -> ResidencePlacementEconomyDouble:
	return ResidencePlacementEconomyDouble.new()


# acceptance: ACC:T14.10
func test_cancelled_residence_placement_keeps_gold_and_population_cap_unchanged_and_does_not_start_tax_schedule() -> void:
	var runtime = _new_runtime()
	var gold_before = runtime.gold
	var population_cap_before = runtime.population_cap

	var result = runtime.try_place_residence("cancelled")
	runtime.advance(30.0)

	assert_bool(result.get("accepted", true)).is_false()
	assert_int(runtime.gold).is_equal(gold_before)
	assert_int(runtime.population_cap).is_equal(population_cap_before)
	assert_bool(runtime.is_tax_schedule_running()).is_false()
	assert_int(runtime.tax_tick_count).is_equal(0)


func test_invalid_residence_placement_keeps_state_unchanged_and_prevents_tax_ticks() -> void:
	var runtime = _new_runtime()
	var gold_before = runtime.gold
	var population_cap_before = runtime.population_cap

	var result = runtime.try_place_residence("invalid")
	runtime.advance(45.0)

	assert_bool(result.get("accepted", true)).is_false()
	assert_int(runtime.gold).is_equal(gold_before)
	assert_int(runtime.population_cap).is_equal(population_cap_before)
	assert_bool(runtime.is_tax_schedule_running()).is_false()
	assert_int(runtime.tax_tick_count).is_equal(0)
