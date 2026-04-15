extends RefCounted
class_name RewardSelectionRuntime

var _choices: Array[String] = []
var applied_effects: Array[String] = []

func set_choices(choice_a: String, choice_b: String, choice_c: String) -> void:
	_choices = [choice_a, choice_b, choice_c]
	applied_effects.clear()

func select_choice(index: int) -> void:
	if index < 0 or index >= _choices.size():
		return
	applied_effects.append(_choices[index])
