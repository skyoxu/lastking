extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const RuntimeScript = preload("res://Game.Godot/Scripts/Building/BuildingUpgradeRepairRuntime.gd")


func _new_runtime(start_level: int = 1, start_hp: int = 40, start_gold: int = 2500):
	var runtime = RuntimeScript.new()
	add_child(auto_free(runtime))
	runtime.set_snapshot(start_level, start_hp, start_gold)
	return runtime


# acceptance: ACC:T15.2
func test_task15_flow_refuses_cross_operation_start_and_keeps_upgrade_active() -> void:
	var runtime = _new_runtime(1, 40, 2500)

	var upgrade_started: bool = runtime.start_upgrade()
	var repair_started_during_upgrade: bool = runtime.start_repair()

	assert_bool(upgrade_started).is_true()
	assert_bool(repair_started_during_upgrade).is_false()
	assert_str(runtime.state).is_equal("Upgrading")


# acceptance: ACC:T15.10
func test_repair_is_incremental_and_not_an_instant_full_heal() -> void:
	var runtime = _new_runtime(1, 35, 2500)

	var repair_started: bool = runtime.start_repair()

	assert_bool(repair_started).is_true()
	assert_bool(runtime.hp < runtime.max_hp).is_true()
	assert_str(runtime.state).is_equal("Repairing")


# acceptance: ACC:T15.11
func test_upgrade_and_repair_are_mutually_exclusive_while_active() -> void:
	var runtime = _new_runtime(1, 35, 2500)

	var repair_started: bool = runtime.start_repair()
	var upgrade_started_during_repair: bool = runtime.start_upgrade()

	assert_bool(repair_started).is_true()
	assert_bool(upgrade_started_during_repair).is_false()
	assert_str(runtime.state).is_equal("Repairing")


# acceptance: ACC:T15.13
func test_completion_effects_appear_only_after_queued_progress_updates() -> void:
	var runtime = _new_runtime(1, 35, 2500)

	var repair_started: bool = runtime.start_repair()

	assert_bool(repair_started).is_true()
	assert_int(runtime.completion_events.size()).is_equal(0)

	runtime.queue_progress_step()
	assert_int(runtime.completion_events.size()).is_equal(0)

	await get_tree().process_frame
	assert_int(runtime.completion_events.size()).is_equal(1)


# acceptance: ACC:T15.16
func test_additional_start_requests_are_rejected_without_parallel_repair_flows() -> void:
	var runtime = _new_runtime(1, 35, 2500)

	var first_start: bool = runtime.start_repair()
	var second_start: bool = runtime.start_repair()

	assert_bool(first_start).is_true()
	assert_bool(second_start).is_false()
	assert_str(runtime.state).is_equal("Repairing")
	assert_int(runtime.count_timeline_entries("repair:start")).is_equal(1)


# acceptance: ACC:T15.18
func test_repair_uses_multiple_integer_safe_capped_steps_for_hp_and_gold() -> void:
	var runtime = _new_runtime(1, 10, 3000)
	var hp_before: int = runtime.hp
	var gold_before: int = runtime.gold

	var repair_started: bool = runtime.start_repair()
	assert_bool(repair_started).is_true()

	runtime.queue_progress_step()
	await get_tree().process_frame
	var hp_after_step_one: int = runtime.hp
	var gold_after_step_one: int = runtime.gold

	runtime.queue_progress_step()
	await get_tree().process_frame
	var hp_after_step_two: int = runtime.hp
	var gold_after_step_two: int = runtime.gold

	assert_bool(hp_after_step_one > hp_before).is_true()
	assert_bool(hp_after_step_one < runtime.max_hp).is_true()
	assert_bool(hp_after_step_two >= hp_after_step_one).is_true()

	var step_one_spent: int = gold_before - gold_after_step_one
	var step_two_spent: int = gold_after_step_one - gold_after_step_two
	var expected_total_cost: int = int(runtime.build_cost / 2)
	var remaining_after_step_one: int = expected_total_cost - step_one_spent

	assert_bool(step_two_spent <= remaining_after_step_one).is_true()
	assert_int(runtime.total_repair_gold_deducted).is_equal(expected_total_cost)


# acceptance: ACC:T15.22
func test_progress_execution_requires_queue_or_coroutine_driven_async_evidence() -> void:
	var runtime = _new_runtime(1, 35, 2500)

	var repair_started: bool = runtime.start_repair()

	assert_bool(repair_started).is_true()
	assert_int(runtime.completion_events.size()).is_equal(0)

	runtime.queue_progress_step()
	assert_int(runtime.completion_events.size()).is_equal(0)

	await get_tree().process_frame
	assert_int(runtime.completion_events.size()).is_equal(1)
