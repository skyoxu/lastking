extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Performance/Task30PerformanceGateBridge.cs"

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	assert_object(script).is_not_null()
	assert_bool(script.has_method("new")).is_true()
	assert_bool(script.can_instantiate()).is_true()
	var bridge = script.new()
	assert_object(bridge).is_not_null()
	add_child(auto_free(bridge))
	return bridge

func _evaluate_perf_gate_verdict(report: Dictionary) -> String:
	var headless: Dictionary = report.get("fixed_seed_headless", {})
	var playable: Dictionary = report.get("playable_gate", {})
	var bridge := _new_bridge()
	var result = bridge.call(
		"EvaluateFixedSeedGate",
		String(report.get("platform", "")),
		String(report.get("seed_mode", "")),
		float(headless.get("fps_1pct_low", 0.0)),
		float(headless.get("fps_avg", 0.0)),
		float(playable.get("fps_1pct_low", 0.0)),
		float(playable.get("fps_avg", 0.0))
	) as Dictionary
	return String(result.get("verdict", "FAIL"))

# acceptance: ACC:T30.9
func test_perf_gate_passes_when_fixed_seed_headless_and_playable_runs_hit_floor_values() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 45.0,
			"fps_avg": 60.0
		},
		"playable_gate": {
			"fps_1pct_low": 45.0,
			"fps_avg": 60.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("PASS")

func test_perf_gate_fails_when_playable_gate_average_fps_is_below_60() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 58.2,
			"fps_avg": 73.4
		},
		"playable_gate": {
			"fps_1pct_low": 48.0,
			"fps_avg": 59.9
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")

func test_perf_gate_fails_when_headless_average_fps_is_below_60() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 50.0,
			"fps_avg": 59.8
		},
		"playable_gate": {
			"fps_1pct_low": 48.0,
			"fps_avg": 62.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")

func test_perf_gate_fails_when_headless_one_percent_low_is_below_45() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 44.9,
			"fps_avg": 61.0
		},
		"playable_gate": {
			"fps_1pct_low": 47.0,
			"fps_avg": 62.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")

func test_perf_gate_fails_when_playable_one_percent_low_is_below_45() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 46.0,
			"fps_avg": 61.0
		},
		"playable_gate": {
			"fps_1pct_low": 44.8,
			"fps_avg": 61.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")

func test_perf_gate_fails_when_platform_is_not_windows() -> void:
	var report: Dictionary = {
		"platform": "linux",
		"seed_mode": "fixed",
		"fixed_seed_headless": {
			"fps_1pct_low": 48.0,
			"fps_avg": 62.0
		},
		"playable_gate": {
			"fps_1pct_low": 48.0,
			"fps_avg": 62.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")

func test_perf_gate_fails_when_seed_mode_is_not_fixed() -> void:
	var report: Dictionary = {
		"platform": "windows",
		"seed_mode": "random",
		"fixed_seed_headless": {
			"fps_1pct_low": 48.0,
			"fps_avg": 62.0
		},
		"playable_gate": {
			"fps_1pct_low": 48.0,
			"fps_avg": 62.0
		}
	}

	var verdict: String = _evaluate_perf_gate_verdict(report)

	assert_that(verdict).is_equal("FAIL")
