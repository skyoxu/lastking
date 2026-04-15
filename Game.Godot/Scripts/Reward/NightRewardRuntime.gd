extends RefCounted
class_name NightRewardRuntime

const EVENT_LASTKING_REWARD_OFFERED := "core.lastking.reward.offered"

var pool_by_night_type: Dictionary = {}
var trigger_count: int = 0
var night_triggers: Array = []
var progression_steps: Array = []
var fixed_gold_fallback: int = 100

func configure_from_json(json_text: String) -> void:
	var parsed = JSON.parse_string(json_text)
	if typeof(parsed) == TYPE_DICTIONARY:
		var pools: Variant = parsed.get("night_type_pools", {})
		if typeof(pools) == TYPE_DICTIONARY:
			pool_by_night_type = pools.duplicate(true)

func advance_phase(phase: String, night_type: String = "normal") -> Dictionary:
	progression_steps.append(phase)
	var outcome = {
		"phase": phase,
		"night_type": night_type,
		"choices": [],
		"ui_presented": false,
		"fallback_gold": 0,
		"event_type": "",
		"event_payload": {},
	}

	if phase != "night":
		return outcome

	trigger_count += 1

	var raw_pool = pool_by_night_type.get(night_type, [])
	var pool: Array = []
	if typeof(raw_pool) == TYPE_ARRAY:
		pool = raw_pool

	if pool.is_empty():
		outcome["fallback_gold"] = fixed_gold_fallback
	else:
		var choice_count = mini(3, pool.size())
		for i in range(choice_count):
			outcome["choices"].append(pool[i])
		outcome["ui_presented"] = true
		outcome["event_type"] = EVENT_LASTKING_REWARD_OFFERED
		outcome["event_payload"] = {
			"night_type": night_type,
			"options": outcome["choices"],
		}

	night_triggers.append(outcome.duplicate(true))
	return outcome
