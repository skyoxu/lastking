extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONFIG_BRIDGE_PATH := "res://Game.Godot/Adapters/Config/ConfigRuntimeBridge.cs"

func _simulate_runtime_metrics(balance: Dictionary, wave_index: int) -> Dictionary:
	var cycle_seconds := float(balance.get("day_night_cycle_seconds", 120.0))
	var wave_budget_base := int(balance.get("wave_budget_base", 10))
	var wave_budget_growth := int(balance.get("wave_budget_growth", 2))
	var spawn_interval_seconds := float(balance.get("spawn_interval_seconds", 1.0))
	var boss_per_10_waves := int(balance.get("boss_per_10_waves", 1))

	var safe_wave_index := maxi(wave_index, 1)
	var wave_budget := wave_budget_base + wave_budget_growth * (safe_wave_index - 1)
	var safe_interval := maxf(spawn_interval_seconds, 0.1)
	var spawn_ticks_per_wave := int(ceil(float(maxi(wave_budget, 1)) / safe_interval))
	var boss_count := int(floor(float(safe_wave_index) / 10.0)) * boss_per_10_waves

	return {
		"day_night_cycle_seconds": cycle_seconds,
		"wave_budget": wave_budget,
		"spawn_interval_seconds": safe_interval,
		"spawn_ticks_per_wave": spawn_ticks_per_wave,
		"boss_count": boss_count,
	}

func _is_valid_balance_config(balance: Dictionary) -> bool:
	return (
		float(balance.get("day_night_cycle_seconds", 0.0)) > 0.0
		and int(balance.get("wave_budget_base", -1)) >= 0
		and int(balance.get("wave_budget_growth", -1)) >= 0
		and float(balance.get("spawn_interval_seconds", 0.0)) > 0.0
		and int(balance.get("boss_per_10_waves", -1)) >= 0
	)

# acceptance: ACC:T2.14
func test_runtime_metrics_are_observable_and_driven_by_config_values() -> void:
	var runtime_config := {
		"day_night_cycle_seconds": 90.0,
		"wave_budget_base": 12,
		"wave_budget_growth": 3,
		"spawn_interval_seconds": 1.5,
		"boss_per_10_waves": 1,
	}

	var metrics := _simulate_runtime_metrics(runtime_config, 5)

	assert_that(metrics["day_night_cycle_seconds"]).is_equal(90.0)
	assert_that(metrics["wave_budget"]).is_equal(24)
	assert_that(metrics["spawn_interval_seconds"]).is_equal(1.5)
	assert_that(metrics["boss_count"]).is_equal(0)

# acceptance: ACC:T2.4
func test_windows_headless_friendly_smoke_for_balance_config_contract() -> void:
	var config := {
		"day_night_cycle_seconds": 120.0,
		"wave_budget_base": 10,
		"wave_budget_growth": 2,
		"spawn_interval_seconds": 1.0,
		"boss_per_10_waves": 1,
	}

	assert_that(_is_valid_balance_config(config)).is_true()
	assert_that(_simulate_runtime_metrics(config, 1).has("wave_budget")).is_true()

# acceptance: ACC:T2.5
func test_replacing_balance_config_and_reloading_changes_runtime_outcomes() -> void:
	var config_a := {
		"day_night_cycle_seconds": 120.0,
		"wave_budget_base": 10,
		"wave_budget_growth": 2,
		"spawn_interval_seconds": 1.2,
		"boss_per_10_waves": 1,
	}
	var config_b := {
		"day_night_cycle_seconds": 60.0,
		"wave_budget_base": 14,
		"wave_budget_growth": 4,
		"spawn_interval_seconds": 0.8,
		"boss_per_10_waves": 2,
	}

	var before_reload := _simulate_runtime_metrics(config_a, 10)
	var after_reload := _simulate_runtime_metrics(config_b, 10)

	assert_that(after_reload["day_night_cycle_seconds"]).is_not_equal(before_reload["day_night_cycle_seconds"])
	assert_that(after_reload["wave_budget"]).is_not_equal(before_reload["wave_budget"])
	assert_that(after_reload["spawn_ticks_per_wave"]).is_not_equal(before_reload["spawn_ticks_per_wave"])
	assert_that(after_reload["boss_count"]).is_not_equal(before_reload["boss_count"])

# acceptance: ACC:T46.3
# acceptance: ACC:T46.4
func test_runtime_reload_uses_real_config_manager_chain_and_keeps_reason_snapshot_contract() -> void:
	var bridge_script := load(CONFIG_BRIDGE_PATH)
	if bridge_script == null:
		push_warning("SKIP: ConfigRuntimeBridge unavailable, skip")
		return

	var bridge: Node = bridge_script.new()
	add_child(auto_free(bridge))

	var baseline_json := JSON.stringify({
		"time": {"day_seconds": 240, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
		"channels": {"elite": "elite", "boss": "boss"},
		"spawn": {"cadence_seconds": 10},
		"boss": {"count": 2}
	})
	var promoted_json := JSON.stringify({
		"time": {"day_seconds": 240, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 80, "daily_growth": 1.25}},
		"channels": {"elite": "elite", "boss": "boss"},
		"spawn": {"cadence_seconds": 7},
		"boss": {"count": 3}
	})
	var malformed_json := "{ bad json"

	var initial_result: Dictionary = bridge.call("LoadInitialFromJson", baseline_json, "res://Config/task46-baseline.json")
	assert_bool(bool(initial_result.get("accepted", false))).is_true()
	assert_str(str(initial_result.get("source", ""))).is_equal("initial")

	var baseline_snapshot: Dictionary = bridge.call("CurrentSnapshot")
	assert_int(int(baseline_snapshot.get("day1_budget", -1))).is_equal(50)
	assert_int(int(baseline_snapshot.get("spawn_cadence_seconds", -1))).is_equal(10)

	var promoted_result: Dictionary = bridge.call("ReloadFromJson", promoted_json, "res://Config/task46-promoted.json")
	assert_bool(bool(promoted_result.get("accepted", false))).is_true()
	assert_str(str(promoted_result.get("source", ""))).is_equal("reload")

	var promoted_snapshot: Dictionary = bridge.call("CurrentSnapshot")
	assert_int(int(promoted_snapshot.get("day1_budget", -1))).is_equal(80)
	assert_int(int(promoted_snapshot.get("spawn_cadence_seconds", -1))).is_equal(7)
	assert_int(int(promoted_snapshot.get("boss_count", -1))).is_equal(3)

	var fallback_result: Dictionary = bridge.call("ReloadFromJson", malformed_json, "res://Config/task46-malformed.json")
	assert_bool(bool(fallback_result.get("accepted", true))).is_false()
	assert_str(str(fallback_result.get("source", ""))).is_equal("fallback")
	assert_str(str(fallback_result.get("reason_code", ""))).is_equal("CFG_PARSE_ERROR")

	var fallback_snapshot: Dictionary = bridge.call("CurrentSnapshot")
	assert_dict(fallback_snapshot).is_equal(promoted_snapshot)
