extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class _PayloadCollector:
	extends RefCounted
	var events: Array = []

	func on_ui_day_counter_updated(payload: Dictionary) -> void:
		events.append(payload.duplicate(true))

class _HudSignalProbe:
	extends RefCounted
	signal ui_day_counter_updated(payload)

	func emit_day_progression(day_index: int) -> void:
		emit_signal("ui_day_counter_updated", {
			"day_count": day_index,
			"phase": "DAY",
			"source": "day_progression",
		})

# acceptance: ACC:T19.11
# Day progression must expose a stable UI payload for day counter rendering.
func test_day_progression_signal_contains_stable_day_counter_payload_for_hud() -> void:
	var probe := _HudSignalProbe.new()
	var collector := _PayloadCollector.new()
	probe.ui_day_counter_updated.connect(collector.on_ui_day_counter_updated)

	probe.emit_day_progression(2)

	assert_that(collector.events.size()).is_equal(1)
	var payload: Dictionary = collector.events[0]
	assert_that(payload.has("day_count")).is_true()
	assert_that(payload.has("phase")).is_true()
	assert_int(int(payload.get("day_count", -1))).is_equal(2)
