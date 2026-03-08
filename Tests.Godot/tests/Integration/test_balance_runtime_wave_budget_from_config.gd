extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const _SECTION_BALANCE := "balance"
const _KEY_DAY1_BUDGET := "wave_budget_day1"
const _KEY_DAILY_GROWTH := "wave_budget_daily_growth"

func _parse_wave_budget_from_config(config: ConfigFile) -> Dictionary:
	var day1_budget: int = int(config.get_value(_SECTION_BALANCE, _KEY_DAY1_BUDGET, 50))
	var daily_growth: float = float(config.get_value(_SECTION_BALANCE, _KEY_DAILY_GROWTH, 1.2))
	return {
		"day1": day1_budget,
		"daily_growth": daily_growth
	}

# acceptance: ACC:T2.9
func test_wave_budget_runtime_values_are_loaded_from_config() -> void:
	var config := ConfigFile.new()
	config.set_value(_SECTION_BALANCE, _KEY_DAY1_BUDGET, 50)
	config.set_value(_SECTION_BALANCE, _KEY_DAILY_GROWTH, 1.2)

	var runtime_budget := _parse_wave_budget_from_config(config)

	assert(runtime_budget.has("day1"))
	assert(runtime_budget.has("daily_growth"))
	assert(runtime_budget["day1"] == 50)
	assert(is_equal_approx(runtime_budget["daily_growth"], 1.2))
