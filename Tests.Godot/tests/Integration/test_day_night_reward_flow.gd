extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"
const NightRewardRuntime = preload("res://Game.Godot/Scripts/Reward/NightRewardRuntime.gd")


func _build_harness() -> NightRewardRuntime:
    var harness = NightRewardRuntime.new()
    harness.pool_by_night_type = {
        "normal": ["swift", "focus", "thorns", "greed"],
        "elite": ["overclock", "barrier", "ferocity"],
        "boss": []
    }
    return harness


# ACC:T18.1
func test_night_reward_triggers_once_and_returns_three_choices() -> void:
    var harness = _build_harness()
    var outcome = harness.advance_phase("night", "normal")
    var choices: Array = outcome["choices"]

    assert_that(choices).is_equal(["swift", "focus", "thorns"])
    assert_that(choices.size()).is_equal(3)
    assert_that(harness.trigger_count).is_equal(1)


# ACC:T18.2
func test_night_reward_result_contains_expected_shape_for_runtime_checks() -> void:
    var harness = _build_harness()
    var outcome = harness.advance_phase("night", "elite")
    var choices: Array = outcome["choices"]

    assert_that(outcome["phase"]).is_equal("night")
    assert_that(outcome["night_type"]).is_equal("elite")
    assert_that(choices.size()).is_equal(3)
    assert_that(outcome["ui_presented"]).is_true()
    assert_that(outcome["fallback_gold"]).is_equal(0)
    assert_that(str(outcome.get("event_type", ""))).is_equal("core.lastking.reward.offered")


# ACC:T18.3
func test_reward_processing_does_not_block_day_night_progression() -> void:
    var harness = _build_harness()

    harness.advance_phase("day")
    harness.advance_phase("night", "normal")
    harness.advance_phase("day")
    harness.advance_phase("night", "elite")

    assert_that(harness.progression_steps).is_equal(["day", "night", "day", "night"])
    assert_that(harness.night_triggers.size()).is_equal(2)


# ACC:T18.4
func test_night_transition_uses_current_night_type_for_pool_selection() -> void:
    var harness = _build_harness()

    harness.advance_phase("day")
    var elite_night = harness.advance_phase("night", "elite")
    var choices: Array = elite_night["choices"]

    assert_that(harness.night_triggers.size()).is_equal(1)
    assert_that(elite_night["night_type"]).is_equal("elite")
    assert_that(choices).is_equal(["overclock", "barrier", "ferocity"])


# ACC:T18.9
func test_json_driven_pool_selection_and_fallback_when_pool_is_empty() -> void:
    var harness = NightRewardRuntime.new()
    harness.configure_from_json("{\"night_type_pools\":{\"normal\":[\"a\",\"b\",\"c\",\"d\"],\"boss\":[]}}")

    var normal_night = harness.advance_phase("night", "normal")
    var boss_night = harness.advance_phase("night", "boss")
    var normal_choices: Array = normal_night["choices"]
    var boss_choices: Array = boss_night["choices"]

    assert_that(normal_choices).is_equal(["a", "b", "c"])
    assert_that(normal_night["ui_presented"]).is_true()
    assert_that(boss_choices.size()).is_equal(0)
    assert_that(boss_night["ui_presented"]).is_false()
    assert_that(boss_night["fallback_gold"]).is_equal(100)


# ACC:T18.10
func test_reward_processing_is_refused_outside_night_phase() -> void:
    var harness = _build_harness()

    var day_outcome = harness.advance_phase("day", "normal")
    var dusk_outcome = harness.advance_phase("dusk", "elite")
    var day_choices: Array = day_outcome["choices"]
    var dusk_choices: Array = dusk_outcome["choices"]

    assert_that(harness.night_triggers.size()).is_equal(0)
    assert_that(day_choices.size()).is_equal(0)
    assert_that(dusk_choices.size()).is_equal(0)
    assert_that(day_outcome["ui_presented"]).is_false()
    assert_that(dusk_outcome["ui_presented"]).is_false()
    assert_that(day_outcome["fallback_gold"]).is_equal(0)
    assert_that(dusk_outcome["fallback_gold"]).is_equal(0)


# ACC:T18.12
func test_consecutive_nights_use_current_type_pool_each_time() -> void:
    var harness = _build_harness()

    var normal_night = harness.advance_phase("night", "normal")
    var elite_night = harness.advance_phase("night", "elite")
    var normal_choices: Array = normal_night["choices"]
    var elite_choices: Array = elite_night["choices"]

    assert_that(normal_choices).is_equal(["swift", "focus", "thorns"])
    assert_that(elite_choices).is_equal(["overclock", "barrier", "ferocity"])
    assert_that(harness.night_triggers.size()).is_equal(2)


# ACC:T18.14
func test_every_night_trigger_has_non_noop_result() -> void:
    var harness = _build_harness()
    var outcomes = [
        harness.advance_phase("night", "normal"),
        harness.advance_phase("night", "boss")
    ]

    for outcome in outcomes:
        var has_ui: bool = outcome["ui_presented"]
        var fallback_gold: int = outcome["fallback_gold"]
        assert_that(has_ui or fallback_gold > 0).is_true()
