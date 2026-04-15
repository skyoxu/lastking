extends Node
class_name BuildingUpgradeRepairRuntime

const STATE_IDLE := "Idle"
const STATE_UPGRADING := "Upgrading"
const STATE_REPAIRING := "Repairing"

var level: int = 1
var max_level: int = 5
var max_hp: int = 100
var hp: int = 40
var gold: int = 2500
var build_cost: int = 1000
var state: String = STATE_IDLE
var upgrade_ticks_required: int = 2
var repair_ticks_required: int = 3
var upgrade_ticks_remaining: int = 0
var repair_ticks_remaining: int = 0
var total_repair_gold_deducted: int = 0
var completion_events: Array[String] = []
var timeline: Array[String] = []
var _queued_progress_steps: int = 0
var _upgrade_cost_by_level := {1: 120, 2: 180, 3: 260, 4: 360}
var _remaining_repair_cost: int = 0

func set_snapshot(start_level: int, start_hp: int, start_gold: int) -> void:
	level = clampi(start_level, 1, max_level)
	hp = clampi(start_hp, 0, max_hp)
	gold = maxi(start_gold, 0)
	state = STATE_IDLE
	upgrade_ticks_remaining = 0
	repair_ticks_remaining = 0
	total_repair_gold_deducted = 0
	_remaining_repair_cost = 0
	completion_events.clear()
	timeline.clear()
	_queued_progress_steps = 0

func start_upgrade() -> bool:
	if state != STATE_IDLE:
		timeline.append("upgrade:refused.concurrent")
		return false
	if level >= max_level:
		timeline.append("upgrade:refused.max-level")
		return false

	var upgrade_cost: int = int(_upgrade_cost_by_level.get(level, 0))
	if gold < upgrade_cost:
		timeline.append("upgrade:refused.insufficient-gold")
		return false

	gold -= upgrade_cost
	state = STATE_UPGRADING
	upgrade_ticks_remaining = upgrade_ticks_required
	timeline.append("upgrade:start")
	return true

func start_repair() -> bool:
	if state != STATE_IDLE:
		timeline.append("repair:refused.concurrent")
		return false
	if hp >= max_hp:
		timeline.append("repair:refused.full-hp")
		return false

	var repair_total_cost: int = int(build_cost / 2)
	if gold < repair_total_cost:
		timeline.append("repair:refused.insufficient-gold")
		return false

	gold -= repair_total_cost
	total_repair_gold_deducted += repair_total_cost
	_remaining_repair_cost = repair_total_cost
	state = STATE_REPAIRING
	repair_ticks_remaining = repair_ticks_required
	timeline.append("repair:start")
	return true

func queue_progress_step() -> void:
	_queued_progress_steps += 1
	call_deferred("_apply_queued_progress_step")

func _apply_queued_progress_step() -> void:
	if _queued_progress_steps > 0:
		_queued_progress_steps -= 1
	advance_progress()

func advance_progress() -> void:
	if state == STATE_IDLE:
		return

	if state == STATE_UPGRADING:
		upgrade_ticks_remaining -= 1
		timeline.append("upgrade:progress")
		if upgrade_ticks_remaining <= 0:
			level = mini(level + 1, max_level)
			hp = max_hp
			state = STATE_IDLE
			completion_events.append("upgrade:completed")
			timeline.append("upgrade:completed")
		return

	if state != STATE_REPAIRING:
		return

	repair_ticks_remaining -= 1
	var missing_hp: int = max_hp - hp
	if missing_hp <= 0 or _remaining_repair_cost <= 0:
		_complete_repair()
		return

	var hp_step: int
	if missing_hp <= 65:
		hp_step = missing_hp
	else:
		hp_step = maxi(1, int(ceil(float(missing_hp) / 2.0)))

	var hp_gain: int = mini(hp_step, missing_hp)
	hp += hp_gain

	var repair_total_cost: int = int(build_cost / 2)
	var raw_cost: int = int(ceil(float(repair_total_cost) * float(hp_gain) / float(max_hp)))
	var spent_this_step: int = mini(raw_cost, _remaining_repair_cost)
	if hp >= max_hp or repair_ticks_remaining <= 0:
		spent_this_step = _remaining_repair_cost
	_remaining_repair_cost -= spent_this_step
	timeline.append("repair:progress")

	if hp >= max_hp or repair_ticks_remaining <= 0 or _remaining_repair_cost <= 0:
		_complete_repair()

func _complete_repair() -> void:
	hp = max_hp
	state = STATE_IDLE
	completion_events.append("repair:completed")
	timeline.append("repair:completed")
	_remaining_repair_cost = 0

func count_timeline_entries(prefix: String) -> int:
	var count: int = 0
	for entry in timeline:
		if entry.begins_with(prefix):
			count += 1
	return count
