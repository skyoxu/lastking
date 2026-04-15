extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"
const RewardPanelRuntime = preload("res://Game.Godot/Scripts/Reward/RewardPanelRuntime.gd")


# ACC:T18.8
func test_fallback_grants_fixed_gold_once_and_hides_reward_panel_when_pool_empty() -> void:
    var sut := RewardPanelRuntime.new()

    var outcome := sut.trigger_reward("night-12-boss", [])
    var choices: Array = outcome["choices"]

    assert_that(outcome["panel_visible"]).is_false()
    assert_that(choices.size()).is_equal(0)
    assert_that(int(outcome["fallback_gold"])).is_equal(100)
    assert_that(sut.fallback_trigger_ids.size()).is_equal(1)


func test_selection_is_refused_and_gold_delta_stays_fixed_after_empty_pool_fallback() -> void:
    var sut := RewardPanelRuntime.new()
    var before_gold := sut.total_gold

    sut.trigger_reward("night-13-boss", [])
    var selected := sut.try_select_choice(0)
    var delta := sut.total_gold - before_gold

    assert_that(selected).is_false()
    assert_that(sut.panel_visible).is_false()
    assert_that(delta).is_equal(100)
