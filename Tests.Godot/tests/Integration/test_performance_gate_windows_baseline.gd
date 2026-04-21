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

func _repo_root_abs() -> String:
	return ProjectSettings.globalize_path("res://../").simplify_path()

func _write_metrics(path_relative: String, payload: Dictionary) -> String:
	var absolute := _repo_root_abs().path_join(path_relative).simplify_path()
	var mk_err := DirAccess.make_dir_recursive_absolute(absolute.get_base_dir())
	assert_bool(mk_err == OK).is_true()
	var file := FileAccess.open(absolute, FileAccess.WRITE)
	assert_object(file).is_not_null()
	file.store_string(JSON.stringify(payload))
	file.flush()
	return absolute

func _read_metrics(path_absolute: String) -> Dictionary:
	if not FileAccess.file_exists(path_absolute):
		return {}
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path_absolute))
	if parsed is Dictionary:
		return parsed
	return {}

# acceptance: ACC:T30.2
func test_windows_baseline_gate_accepts_threshold_values() -> void:
	var metrics_path := _write_metrics("logs/ci/task-30/windows-baseline-metrics-pass.json", {
		"avg_fps": 60.0,
		"one_percent_low_fps": 45.0,
	})
	var metrics: Dictionary = _read_metrics(metrics_path)
	var bridge := _new_bridge()
	var result = bridge.call(
		"EvaluateWindowsBaseline",
		float(metrics.get("avg_fps", 0.0)),
		float(metrics.get("one_percent_low_fps", 0.0))
	) as Dictionary

	assert_bool(bool(result.get("passed", false))).is_true()

func test_windows_baseline_gate_rejects_metrics_below_targets() -> void:
	var metrics_path := _write_metrics("logs/ci/task-30/windows-baseline-metrics-fail.json", {
		"avg_fps": 59.9,
		"one_percent_low_fps": 44.9,
	})
	var metrics: Dictionary = _read_metrics(metrics_path)
	var bridge := _new_bridge()
	var result = bridge.call(
		"EvaluateWindowsBaseline",
		float(metrics.get("avg_fps", 0.0)),
		float(metrics.get("one_percent_low_fps", 0.0))
	) as Dictionary

	assert_bool(bool(result.get("passed", true))).is_false()

func test_windows_baseline_gate_rejects_when_metrics_file_missing_or_wrong_type() -> void:
	var bridge := _new_bridge()
	var missing_metrics := _read_metrics(_repo_root_abs().path_join("logs/ci/task-30/not-exists.json"))
	var missing_result = bridge.call(
		"EvaluateWindowsBaseline",
		float(missing_metrics.get("avg_fps", 0.0)),
		float(missing_metrics.get("one_percent_low_fps", 0.0))
	) as Dictionary
	assert_bool(bool(missing_result.get("passed", true))).is_false()

	var broken_path := _write_metrics("logs/ci/task-30/windows-baseline-metrics-type-error.json", {
		"avg_fps": "sixty",
		"one_percent_low_fps": 45.0
	})
	var broken_metrics: Dictionary = _read_metrics(broken_path)
	var broken_result = bridge.call(
		"EvaluateWindowsBaseline",
		float(broken_metrics.get("avg_fps", 0.0)),
		float(broken_metrics.get("one_percent_low_fps", 0.0))
	) as Dictionary
	assert_bool(bool(broken_result.get("passed", true))).is_false()
