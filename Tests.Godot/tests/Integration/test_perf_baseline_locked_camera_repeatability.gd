extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Performance/Task30PerformanceGateBridge.cs"
const VARIANCE_WINDOW_PERCENT := 1.0

func _new_bridge() -> Node:
	var script = load(BRIDGE_PATH)
	assert_object(script).is_not_null()
	assert_bool(script.has_method("new")).is_true()
	assert_bool(script.can_instantiate()).is_true()
	var bridge = script.new()
	assert_object(bridge).is_not_null()
	add_child(auto_free(bridge))
	return bridge

func _capture_profile(camera_locked: bool, scripted_session: bool, avg_fps: float, low_1_percent_fps: float) -> Dictionary:
	return {
		"camera_locked": camera_locked,
		"scripted_session": scripted_session,
		"avg_fps": avg_fps,
		"low_1_percent_fps": low_1_percent_fps,
	}

func _evaluate_repeatability(run_a: Dictionary, run_b: Dictionary) -> Dictionary:
	var bridge := _new_bridge()
	return bridge.call(
		"EvaluateBaselineVariance",
		bool(run_a.get("camera_locked", false)) and bool(run_b.get("camera_locked", false)),
		bool(run_a.get("scripted_session", false)) and bool(run_b.get("scripted_session", false)),
		VARIANCE_WINDOW_PERCENT,
		float(run_a.get("low_1_percent_fps", 0.0)),
		float(run_a.get("avg_fps", 0.0)),
		float(run_b.get("low_1_percent_fps", 0.0)),
		float(run_b.get("avg_fps", 0.0))
	) as Dictionary

# acceptance: ACC:T30.4
func test_locked_camera_repeated_runs_stay_within_declared_variance_window() -> void:
	var first_run: Dictionary = _capture_profile(true, true, 60.0, 45.0)
	var second_run: Dictionary = _capture_profile(true, true, 59.5, 44.7)
	var result: Dictionary = _evaluate_repeatability(first_run, second_run)

	assert_bool(bool(result.get("eligible", false))).is_true()
	assert_bool(bool(result.get("within_variance", false))).is_true()

func test_unlocked_camera_run_is_rejected_for_baseline_repeatability() -> void:
	var locked_run: Dictionary = _capture_profile(true, true, 60.0, 45.0)
	var unlocked_run: Dictionary = _capture_profile(false, true, 60.1, 44.9)
	var result: Dictionary = _evaluate_repeatability(locked_run, unlocked_run)

	assert_bool(bool(result.get("eligible", true))).is_false()
	assert_bool(bool(result.get("within_variance", true))).is_false()
