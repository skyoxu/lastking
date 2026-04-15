extends RefCounted
class_name RewardPanelRuntime

var presented_choices: Array = []
var panel_visible: bool = false
var fixed_fallback_gold: int = 100
var total_gold: int = 0
var fallback_trigger_ids: Array[String] = []
var _last_choices: Array = []

func present_choices(active_pool: Array, _other_pool: Array = []) -> Dictionary:
	var choices: Array = []
	if active_pool.size() >= 3:
		choices = [active_pool[0], active_pool[1], active_pool[2]]
	elif active_pool.size() > 0:
		choices = active_pool.duplicate()

	presented_choices = choices.duplicate()
	panel_visible = not choices.is_empty()
	_last_choices = presented_choices.duplicate()
	return {"choices": presented_choices.duplicate()}

func present_nightly_choices(generated_choices: Array) -> void:
	presented_choices.clear()
	for choice in generated_choices:
		var kind := str(choice.get("kind", ""))
		if kind == "resource" or kind == "unit" or kind == "bonus":
			presented_choices.append(choice)
	panel_visible = not presented_choices.is_empty()
	_last_choices = presented_choices.duplicate()

func trigger_reward(trigger_id: String, pool: Array[String]) -> Dictionary:
	var outcome = {
		"trigger_id": trigger_id,
		"choices": [],
		"panel_visible": false,
		"fallback_gold": 0,
	}

	if pool.is_empty():
		total_gold += fixed_fallback_gold
		fallback_trigger_ids.append(trigger_id)
		panel_visible = false
		_last_choices = []
		outcome["fallback_gold"] = fixed_fallback_gold
		return outcome

	var choice_count := mini(3, pool.size())
	for i in range(choice_count):
		outcome["choices"].append(pool[i])
	panel_visible = true
	_last_choices = outcome["choices"].duplicate()
	outcome["panel_visible"] = true
	return outcome

func try_select_choice(index: int) -> bool:
	if not panel_visible:
		return false
	if index < 0 or index >= _last_choices.size():
		return false
	return true
