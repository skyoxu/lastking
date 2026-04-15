extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"
const RewardPanelRuntime = preload("res://Game.Godot/Scripts/Reward/RewardPanelRuntime.gd")

# ACC:T18.15
func test_reward_panel_shows_exactly_three_choices_from_active_night_pool() -> void:
    var sut := RewardPanelRuntime.new()
    var active_pool: Array[String] = ["atk_up", "hp_up", "shield", "crit_up"]
    var elite_pool: Array[String] = ["elite_blessing", "elite_armor"]

    var outcome := sut.present_choices(active_pool, elite_pool)
    var choices: Array = outcome["choices"]

    assert_that(choices.size()).is_equal(3)

    var all_from_active_pool := true
    for choice in choices:
        if not active_pool.has(choice):
            all_from_active_pool = false
            break

    assert_that(all_from_active_pool).is_true()

# ACC:T18.5
func test_reward_panel_refuses_cross_pool_mixing_within_single_trigger() -> void:
    var sut := RewardPanelRuntime.new()
    var normal_night_pool: Array[String] = ["coin_boost", "draw_up", "speed_up", "guard_up"]
    var boss_night_pool: Array[String] = ["boss_relic", "boss_aura"]

    var outcome := sut.present_choices(normal_night_pool, boss_night_pool)
    var choices: Array = outcome["choices"]

    var foreign_choice_count := 0
    for choice in choices:
        if boss_night_pool.has(choice):
            foreign_choice_count += 1

    assert_that(foreign_choice_count).is_equal(0)
