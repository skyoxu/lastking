extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"
const RewardSelectionRuntime = preload("res://Game.Godot/Scripts/Reward/RewardSelectionRuntime.gd")

# acceptance: ACC:T18.13
func test_selecting_one_choice_applies_exactly_one_effect() -> void:
	var sut := RewardSelectionRuntime.new()
	sut.set_choices("heal_20", "gain_30_gold", "max_hp_plus_5")

	sut.select_choice(0)

	assert_int(sut.applied_effects.size()).is_equal(1)
	assert_str(sut.applied_effects[0]).is_equal("heal_20")

func test_selecting_one_choice_does_not_apply_non_selected_effects() -> void:
	var sut := RewardSelectionRuntime.new()
	sut.set_choices("heal_20", "gain_30_gold", "max_hp_plus_5")

	sut.select_choice(1)

	assert_bool(sut.applied_effects.has("gain_30_gold")).is_true()
	assert_bool(sut.applied_effects.has("heal_20")).is_false()
	assert_bool(sut.applied_effects.has("max_hp_plus_5")).is_false()
