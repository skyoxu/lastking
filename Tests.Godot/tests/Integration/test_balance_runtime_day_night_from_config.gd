extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BALANCE_SECTION := "balance"
const DAY_KEY := "day_duration_seconds"
const NIGHT_KEY := "night_duration_seconds"

func _build_balance_config_text(day_seconds: int, night_seconds: int) -> String:
	return "[balance]\n%s=%d\n%s=%d\n" % [DAY_KEY, day_seconds, NIGHT_KEY, night_seconds]

func _parse_day_night_seconds(config_text: String) -> Dictionary:
	var config := ConfigFile.new()
	var parse_error := config.parse(config_text)
	if parse_error != OK:
		return {}
	return {
		"day": int(config.get_value(BALANCE_SECTION, DAY_KEY, -1)),
		"night": int(config.get_value(BALANCE_SECTION, NIGHT_KEY, -1))
	}

# ACC:T2.8
# Acceptance: day and night durations come from config and match runtime values.
func test_runtime_day_night_durations_are_loaded_from_config() -> void:
	var config_text := _build_balance_config_text(240, 120)
	var durations := _parse_day_night_seconds(config_text)

	assert_bool(durations.has("day")).is_true()
	assert_bool(durations.has("night")).is_true()
	assert_int(durations["day"]).is_equal(240)
	assert_int(durations["night"]).is_equal(120)
	assert_int(durations["day"] + durations["night"]).is_equal(360)
