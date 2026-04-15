extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# RED-FIRST: this suite encodes acceptance behavior before production wiring is complete.
class _SignalCollector:
    extends RefCounted

    var events: Array = []

    func capture(payload: Dictionary) -> void:
        events.append(payload)


class _GameManagerProbe:
    extends Node

    signal phase_changed(payload)
    signal day_progressed(payload)

    var phase: StringName = &"DAY"
    var day_count: int = 1
    var phase_elapsed_seconds: float = 0.0
    var phase_remaining_seconds: float = 10.0
    var terminal: bool = false

    func advance_one_second() -> void:
        if terminal:
            return
        phase_elapsed_seconds += 1.0
        phase_remaining_seconds = max(0.0, phase_remaining_seconds - 1.0)

        if phase_remaining_seconds <= 0.0:
            if phase == &"DAY":
                phase = &"NIGHT"
            else:
                phase = &"DAY"
                day_count += 1
                emit_signal("day_progressed", {
                    "day_count": day_count,
                    "phase": String(phase),
                    "source": "day_progression",
                })

            phase_elapsed_seconds = 0.0
            phase_remaining_seconds = 10.0
            emit_signal("phase_changed", {
                "phase": String(phase),
                "elapsed_seconds": phase_elapsed_seconds,
                "remaining_seconds": phase_remaining_seconds,
            })

    func enter_win() -> void:
        terminal = true
        phase = &"WIN"
        emit_signal("phase_changed", {
            "phase": String(phase),
            "elapsed_seconds": phase_elapsed_seconds,
            "remaining_seconds": phase_remaining_seconds,
        })


# acceptance: ACC:T19.11
func test_day_progress_signal_payload_has_stable_ui_fields() -> void:
    var manager := _GameManagerProbe.new()
    var collector := _SignalCollector.new()
    manager.day_progressed.connect(collector.capture)

    manager.phase = &"NIGHT"
    manager.phase_remaining_seconds = 0.0
    manager.advance_one_second()

    assert_that(collector.events.size()).is_equal(1)
    var payload: Dictionary = collector.events[0]
    assert_that(payload.has("day_count")).is_true()
    assert_that(payload.has("phase")).is_true()


# acceptance: ACC:T19.14
func test_phase_signal_exposes_phase_and_time_window_for_observers() -> void:
    var manager := _GameManagerProbe.new()
    var collector := _SignalCollector.new()
    manager.phase_changed.connect(collector.capture)

    manager.phase = &"DAY"
    manager.phase_remaining_seconds = 0.0
    manager.advance_one_second()

    assert_that(collector.events.size()).is_equal(1)
    var payload: Dictionary = collector.events[0]
    assert_that(payload.has("phase")).is_true()
    assert_that(payload.has("elapsed_seconds")).is_true()
    assert_that(payload.has("remaining_seconds")).is_true()


# acceptance: ACC:T19.16
func test_terminal_win_state_blocks_future_phase_or_day_updates() -> void:
    var manager := _GameManagerProbe.new()
    var phase_collector := _SignalCollector.new()
    var day_collector := _SignalCollector.new()
    manager.phase_changed.connect(phase_collector.capture)
    manager.day_progressed.connect(day_collector.capture)

    manager.enter_win()
    var phase_events_before := phase_collector.events.size()
    var day_events_before := day_collector.events.size()

    for _i in range(12):
        manager.advance_one_second()

    assert_that(String(manager.phase)).is_equal("WIN")
    assert_that(phase_collector.events.size()).is_equal(phase_events_before)
    assert_that(day_collector.events.size()).is_equal(day_events_before)
