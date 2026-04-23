extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REQUIRED_RUNS: PackedStringArray = ["windows_headless", "windows_playable"]
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

func _write_json_rel_path(relative_path: String, payload: Dictionary) -> void:
	var absolute_path := _repo_root_abs().path_join(relative_path).simplify_path()
	var parent := absolute_path.get_base_dir()
	var mk_err := DirAccess.make_dir_recursive_absolute(parent)
	assert_bool(mk_err == OK).is_true()
	var file := FileAccess.open(absolute_path, FileAccess.WRITE)
	assert_object(file).is_not_null()
	file.store_string(JSON.stringify(payload))
	file.flush()
	file.close()

func _read_json_file(absolute_path: String) -> Dictionary:
	if not FileAccess.file_exists(absolute_path):
		return {}
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(absolute_path))
	if parsed is Dictionary:
		return parsed
	return {}

func _sample_measurements() -> Dictionary:
	return {
		"windows_headless": {
			"baseline_fps": 60.2,
			"low_1_percent_fps": 45.6,
			"average_fps": 61.4,
		},
		"windows_playable": {
			"baseline_fps": 60.8,
			"low_1_percent_fps": 46.0,
			"average_fps": 62.1,
		},
	}

func _build_run_payload(measurement: Dictionary) -> Dictionary:
	return {
		"baseline_threshold_fps": 60,
		"low_1_percent_threshold_fps": 45,
		"average_threshold_fps": 60,
		"baseline_verdict": "pass" if float(measurement.get("baseline_fps", 0.0)) >= 60.0 else "fail",
		"low_1_percent_verdict": "pass" if float(measurement.get("low_1_percent_fps", 0.0)) >= 45.0 else "fail",
		"average_verdict": "pass" if float(measurement.get("average_fps", 0.0)) >= 60.0 else "fail",
	}

func _write_fixed_seed_artifacts(fixed_seed: int, measurements: Dictionary) -> Dictionary:
	var perf_rel_path := "logs/perf/task-30/perf-gate.json"
	var ci_rel_path := "logs/ci/task-30/perf-gate.json"
	if fixed_seed <= 0:
		return {
			"written": false,
			"reason": "seed_not_fixed",
			"perf": _read_json_file(_repo_root_abs().path_join(perf_rel_path).simplify_path()),
			"ci": _read_json_file(_repo_root_abs().path_join(ci_rel_path).simplify_path()),
		}

	var payload: Dictionary = {
		"seed": fixed_seed,
		"runs": {
			"windows_headless": _build_run_payload(measurements.get("windows_headless", {})),
			"windows_playable": _build_run_payload(measurements.get("windows_playable", {})),
		}
	}
	_write_json_rel_path(perf_rel_path, payload)
	_write_json_rel_path(ci_rel_path, payload)
	return {
		"written": true,
		"perf_path": perf_rel_path,
		"ci_path": ci_rel_path,
		"perf": payload,
		"ci": payload,
	}

func _validate_artifact_path(relative_path: String) -> Dictionary:
	var bridge := _new_bridge()
	return bridge.call("ValidatePerfGateArtifactPath", relative_path) as Dictionary

# acceptance: ACC:T30.8
func test_perf_gate_artifacts_include_headless_playable_runs_threshold_integers_and_verdicts() -> void:
	var result: Dictionary = _write_fixed_seed_artifacts(20260421, _sample_measurements())

	assert_bool(bool(result.get("written", false))).is_true()
	assert_str(String(result.get("perf_path", ""))).starts_with("logs/perf/")
	assert_str(String(result.get("ci_path", ""))).starts_with("logs/ci/")

	for pair in [{"key":"perf","path_key":"perf_path"}, {"key":"ci","path_key":"ci_path"}]:
		var rel_path: String = String(result.get(String(pair["path_key"]), ""))
		var validate_result: Dictionary = _validate_artifact_path(rel_path)
		assert_bool(bool(validate_result.get("valid", false))).is_true()
		var artifact: Dictionary = _read_json_file(_repo_root_abs().path_join(rel_path).simplify_path())
		var runs: Dictionary = artifact.get("runs", {})
		for run_name in REQUIRED_RUNS:
			assert_bool(runs.has(run_name)).is_true()

func test_non_fixed_seed_run_is_refused_and_previous_artifacts_remain_unchanged() -> void:
	var first: Dictionary = _write_fixed_seed_artifacts(20260421, _sample_measurements())
	var perf_path := _repo_root_abs().path_join(String(first.get("perf_path", ""))).simplify_path()
	var ci_path := _repo_root_abs().path_join(String(first.get("ci_path", ""))).simplify_path()
	var perf_before_text := FileAccess.get_file_as_string(perf_path)
	var ci_before_text := FileAccess.get_file_as_string(ci_path)

	var rejected: Dictionary = _write_fixed_seed_artifacts(0, _sample_measurements())
	var perf_after_text := FileAccess.get_file_as_string(perf_path)
	var ci_after_text := FileAccess.get_file_as_string(ci_path)

	assert_bool(bool(rejected.get("written", true))).is_false()
	assert_str(String(rejected.get("reason", ""))).is_equal("seed_not_fixed")
	assert_that(perf_after_text).is_equal(perf_before_text)
	assert_that(ci_after_text).is_equal(ci_before_text)

func test_perf_gate_artifact_read_fails_when_required_threshold_field_is_missing() -> void:
	var broken_path := _repo_root_abs().path_join("logs/ci/task-30/perf-gate-broken-missing.json").simplify_path()
	var mk_err := DirAccess.make_dir_recursive_absolute(broken_path.get_base_dir())
	assert_bool(mk_err == OK).is_true()
	var file := FileAccess.open(broken_path, FileAccess.WRITE)
	assert_object(file).is_not_null()
	file.store_string(JSON.stringify({
		"seed": 20260421,
		"runs": {
			"windows_headless": {
				"baseline_threshold_fps": 60,
				"average_threshold_fps": 60,
				"baseline_verdict": "pass",
				"low_1_percent_verdict": "pass",
				"average_verdict": "pass"
			}
		}
	}))
	file.flush()
	file.close()

	var validate_result: Dictionary = _validate_artifact_path("logs/ci/task-30/perf-gate-broken-missing.json")
	assert_bool(bool(validate_result.get("valid", true))).is_false()

func test_perf_gate_artifact_is_invalid_when_windows_playable_run_is_missing() -> void:
	var broken_path := _repo_root_abs().path_join("logs/ci/task-30/perf-gate-broken-missing-playable.json").simplify_path()
	var mk_err := DirAccess.make_dir_recursive_absolute(broken_path.get_base_dir())
	assert_bool(mk_err == OK).is_true()
	var file := FileAccess.open(broken_path, FileAccess.WRITE)
	assert_object(file).is_not_null()
	file.store_string(JSON.stringify({
		"seed": 20260421,
		"runs": {
			"windows_headless": {
				"baseline_threshold_fps": 60,
				"low_1_percent_threshold_fps": 45,
				"average_threshold_fps": 60,
				"baseline_verdict": "pass",
				"low_1_percent_verdict": "pass",
				"average_verdict": "pass"
			}
		}
	}))
	file.flush()
	file.close()

	var validate_result: Dictionary = _validate_artifact_path("logs/ci/task-30/perf-gate-broken-missing-playable.json")
	assert_bool(bool(validate_result.get("valid", true))).is_false()

func test_perf_gate_artifact_path_validation_rejects_parent_traversal() -> void:
	var validate_result: Dictionary = _validate_artifact_path("../outside.json")

	assert_bool(bool(validate_result.get("valid", true))).is_false()

func test_perf_gate_artifact_path_validation_rejects_absolute_path_input() -> void:
	var absolute_path := _repo_root_abs().path_join("logs/ci/task-30/perf-gate.json").simplify_path()
	var validate_result: Dictionary = _validate_artifact_path(absolute_path)

	assert_bool(bool(validate_result.get("valid", true))).is_false()
