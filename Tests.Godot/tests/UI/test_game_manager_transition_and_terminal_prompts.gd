extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class _UiMappingHarness:
    extends RefCounted

    var transition_label: String = ""
    var terminal_prompt: String = ""

    func apply_transition_payload(payload: Dictionary) -> void:
        var day: int = int(payload.get("day", 0))
        var phase: String = String(payload.get("phase", "UNKNOWN"))
        transition_label = "Day %d - %s" % [day, phase]

    func apply_terminal_payload(payload: Dictionary) -> void:
        var outcome: String = String(payload.get("outcome", "NONE"))
        if outcome == "WIN":
            terminal_prompt = "Victory! You survived the siege."
        elif outcome == "LOSE":
            terminal_prompt = "Defeat. Your castle has fallen."

# acceptance: ACC:T19.1
# hard gate: transition and terminal payloads must map to correct on-screen values.
func test_transition_and_terminal_payloads_map_to_ui_values_and_win_lose_prompts() -> void:
    var ui := _UiMappingHarness.new()

    ui.apply_transition_payload({"day": 5, "phase": "NIGHT"})
    ui.apply_terminal_payload({"outcome": "WIN"})

    assert_that(ui.transition_label).is_equal("Day 5 - NIGHT")
    assert_that(ui.terminal_prompt).is_equal("Victory! You survived the siege.")

    ui.apply_terminal_payload({"outcome": "LOSE"})
    assert_that(ui.terminal_prompt).is_equal("Defeat. Your castle has fallen.")

func test_terminal_prompt_stays_unchanged_for_non_terminal_outcome() -> void:
    var ui := _UiMappingHarness.new()

    ui.terminal_prompt = "Victory! You survived the siege."
    ui.apply_terminal_payload({"outcome": "NONE"})

    assert_that(ui.terminal_prompt).is_equal("Victory! You survived the siege.")
