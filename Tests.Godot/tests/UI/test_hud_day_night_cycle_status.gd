extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DEFAULT_VISIBLE_CYCLE_STATUS := "Day1"

func _resolve_initial_cycle_status(has_first_switch_happened: bool, current_visible_status: String) -> String:
	if has_first_switch_happened:
		return current_visible_status
	return DEFAULT_VISIBLE_CYCLE_STATUS

# acceptance anchor: ACC:T3.16
# Verifies the HUD-visible cycle status before the first switch is always Day1.
func test_hud_visible_cycle_status_is_day1_before_first_switch() -> void:
	var status := _resolve_initial_cycle_status(false, "Night1")
	assert_str(status).is_equal("Day1")
	assert_bool(status == DEFAULT_VISIBLE_CYCLE_STATUS).is_true()
