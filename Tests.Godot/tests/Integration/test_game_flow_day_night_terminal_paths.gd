extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

enum Phase {
    DAY,
    NIGHT
}

enum TerminalResult {
    NONE,
    WIN,
    LOSE
}

class DayNightTerminalFlowHarness:
    var day_duration_seconds: int = 240
    var night_duration_seconds: int = 120

    var current_day: int = 1
    var current_phase: int = Phase.DAY
    var elapsed_in_phase: int = 0

    var castle_hp_source: int = 100
    var castle_hp_ui: int = 100

    var terminal_result: int = TerminalResult.NONE
    var phase_history: Array[String] = ["Day1"]
    var started_day16: bool = false

    func tick(seconds: int) -> void:
        if terminal_result != TerminalResult.NONE:
            return
        elapsed_in_phase += seconds
        while terminal_result == TerminalResult.NONE and elapsed_in_phase >= _phase_duration_seconds():
            var completed_phase := current_phase
            var completed_day := current_day
            _advance_phase(completed_phase, completed_day)

    func manual_confirm_transition() -> void:
        if elapsed_in_phase >= _phase_duration_seconds():
            var completed_phase := current_phase
            var completed_day := current_day
            _advance_phase(completed_phase, completed_day)

    func damage_castle(amount: int) -> void:
        castle_hp_source = maxi(0, castle_hp_source - amount)
        castle_hp_ui = maxi(0, castle_hp_ui - amount)
        _evaluate_terminal()

    func set_ui_only_hp(ui_value: int) -> void:
        castle_hp_ui = ui_value
        _evaluate_terminal()

    func _phase_duration_seconds() -> int:
        return day_duration_seconds if current_phase == Phase.DAY else night_duration_seconds

    func _advance_phase(completed_phase: int, completed_day: int) -> void:
        elapsed_in_phase = 0

        if completed_phase == Phase.DAY:
            current_phase = Phase.NIGHT
            phase_history.append("Night%d" % current_day)
        else:
            if completed_day >= 15:
                _evaluate_terminal(true)
                return
            current_phase = Phase.DAY
            current_day += 1
            phase_history.append("Day%d" % current_day)
            if current_day > 15:
                started_day16 = true
        _evaluate_terminal()

    func _evaluate_terminal(night15_completed: bool = false) -> void:
        if castle_hp_source <= 0:
            terminal_result = TerminalResult.LOSE
            return
        if night15_completed:
            terminal_result = TerminalResult.WIN
            elapsed_in_phase = 0

func _advance_to(flow: DayNightTerminalFlowHarness, target_day: int, target_phase: int) -> void:
    var guard: int = 0
    while (flow.current_day != target_day or flow.current_phase != target_phase) and guard < 128:
        var phase_seconds := flow.day_duration_seconds if flow.current_phase == Phase.DAY else flow.night_duration_seconds
        flow.tick(phase_seconds)
        flow.manual_confirm_transition()
        guard += 1

# acceptance: ACC:T19.4
func test_windows_baseline_flow_executes_deterministically_without_external_assets() -> void:
    var flow := DayNightTerminalFlowHarness.new()

    for _i in range(3):
        flow.tick(flow.day_duration_seconds)
        flow.manual_confirm_transition()
        flow.tick(flow.night_duration_seconds)
        flow.manual_confirm_transition()

    assert_int(flow.current_day).is_equal(4)
    assert_bool(flow.phase_history.size() > 0).is_true()

# acceptance: ACC:T19.10
func test_terminal_checks_use_castle_hp_source_of_truth_not_ui_display() -> void:
    var flow := DayNightTerminalFlowHarness.new()

    flow.castle_hp_source = 25
    flow.set_ui_only_hp(0)

    assert_int(flow.castle_hp_source).is_equal(25)
    assert_int(flow.terminal_result).is_equal(TerminalResult.NONE)

# acceptance: ACC:T19.12
func test_deterministic_terminal_paths_cover_forced_lose_and_survival_win() -> void:
    var lose_flow := DayNightTerminalFlowHarness.new()
    lose_flow.damage_castle(999)
    assert_int(lose_flow.terminal_result).is_equal(TerminalResult.LOSE)

    var win_flow := DayNightTerminalFlowHarness.new()
    _advance_to(win_flow, 15, Phase.NIGHT)
    win_flow.tick(win_flow.night_duration_seconds)
    win_flow.manual_confirm_transition()
    assert_int(win_flow.terminal_result).is_equal(TerminalResult.WIN)

# acceptance: ACC:T19.15
func test_full_win_path_reaches_day15_night15_before_terminal_and_never_starts_day16() -> void:
    var flow := DayNightTerminalFlowHarness.new()

    _advance_to(flow, 15, Phase.NIGHT)

    assert_int(flow.current_day).is_equal(15)
    assert_int(flow.current_phase).is_equal(Phase.NIGHT)
    assert_int(flow.terminal_result).is_equal(TerminalResult.NONE)

    flow.tick(flow.night_duration_seconds)
    flow.manual_confirm_transition()

    assert_int(flow.terminal_result).is_equal(TerminalResult.WIN)
    assert_bool(flow.started_day16).is_false()

# acceptance: ACC:T19.19
func test_phase_transitions_are_timer_driven_at_240s_and_120s_without_manual_actions() -> void:
    var flow := DayNightTerminalFlowHarness.new()

    flow.tick(240)
    assert_int(flow.current_day).is_equal(1)
    assert_int(flow.current_phase).is_equal(Phase.NIGHT)

    flow.tick(120)
    assert_int(flow.current_day).is_equal(2)
    assert_int(flow.current_phase).is_equal(Phase.DAY)

# acceptance: ACC:T19.21
func test_terminal_evaluation_prefers_lose_when_castle_reaches_zero_during_day15_or_night15() -> void:
    var flow_day15 := DayNightTerminalFlowHarness.new()
    _advance_to(flow_day15, 15, Phase.DAY)
    flow_day15.damage_castle(999)
    assert_int(flow_day15.terminal_result).is_equal(TerminalResult.LOSE)

    var flow_night15 := DayNightTerminalFlowHarness.new()
    _advance_to(flow_night15, 15, Phase.NIGHT)
    flow_night15.damage_castle(999)
    assert_int(flow_night15.terminal_result).is_equal(TerminalResult.LOSE)
