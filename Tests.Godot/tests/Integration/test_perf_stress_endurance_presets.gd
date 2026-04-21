extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REQUIRED_SCENARIOS: PackedStringArray = ["worst_case_wave_mix", "ui_heavy"]
const REQUIRED_METRICS: PackedStringArray = ["memory_growth", "stutter_spikes"]

# acceptance: ACC:T30.7
# Red-first: current launch report stub lacks a required stutter trace in a UI-heavy case.
func test_launch_preset_report_covers_worst_case_and_ui_heavy_with_verdict_or_explicit_failure_trace() -> void:
	var report: Dictionary = _build_current_launch_report_stub()
	var issues: Array[String] = _validate_launch_report(report)
	assert_that(issues).is_empty()

func test_ingest_refuses_incomplete_metric_and_keeps_existing_report_unchanged() -> void:
	var existing: Dictionary = _build_valid_launch_report()
	var snapshot: Dictionary = existing.duplicate(true)

	var invalid_candidate: Dictionary = {
		"ui_heavy": {
			"memory_growth": {"verdict": "pass"},
			"stutter_spikes": {"value_ms": 47.2}
		}
	}

	var result: Dictionary = _ingest_report(existing, invalid_candidate)

	assert_that(result["accepted"]).is_false()
	assert_that(result["report"]).is_equal(snapshot)
	assert_that(result["issues"].size()).is_greater(0)

func test_validate_launch_report_fails_when_worst_case_wave_mix_scenario_is_missing() -> void:
	var report := _build_valid_launch_report()
	report.erase("worst_case_wave_mix")

	var issues: Array[String] = _validate_launch_report(report)

	assert_that(issues).contains("Missing scenario coverage: worst_case_wave_mix")

func test_validate_launch_report_fails_when_ui_heavy_scenario_is_missing() -> void:
	var report := _build_valid_launch_report()
	report.erase("ui_heavy")

	var issues: Array[String] = _validate_launch_report(report)

	assert_that(issues).contains("Missing scenario coverage: ui_heavy")

func test_validate_launch_report_fails_when_memory_growth_metric_is_missing() -> void:
	var report := _build_valid_launch_report()
	var worst_case: Dictionary = report.get("worst_case_wave_mix", {}).duplicate(true)
	worst_case.erase("memory_growth")
	report["worst_case_wave_mix"] = worst_case

	var issues: Array[String] = _validate_launch_report(report)

	assert_that(issues).contains("Missing metric memory_growth in scenario worst_case_wave_mix")

func test_validate_launch_report_fails_when_stutter_spikes_metric_is_missing() -> void:
	var report := _build_valid_launch_report()
	var ui_heavy: Dictionary = report.get("ui_heavy", {}).duplicate(true)
	ui_heavy.erase("stutter_spikes")
	report["ui_heavy"] = ui_heavy

	var issues: Array[String] = _validate_launch_report(report)

	assert_that(issues).contains("Missing metric stutter_spikes in scenario ui_heavy")

func _validate_launch_report(report: Dictionary) -> Array[String]:
	var issues: Array[String] = []

	for scenario in REQUIRED_SCENARIOS:
		if not report.has(scenario):
			issues.append("Missing scenario coverage: %s" % scenario)
			continue

		var scenario_payload: Dictionary = report[scenario]
		for metric in REQUIRED_METRICS:
			if not scenario_payload.has(metric):
				issues.append("Missing metric %s in scenario %s" % [metric, scenario])
				continue

			var metric_payload: Dictionary = scenario_payload[metric]
			if not _has_verdict_or_explicit_failure(metric_payload):
				issues.append("Metric %s in scenario %s needs verdict or fail_reason+trace" % [metric, scenario])

	return issues

func _has_verdict_or_explicit_failure(metric_payload: Dictionary) -> bool:
	var verdict: String = String(metric_payload.get("verdict", "")).strip_edges()
	if verdict != "":
		return true

	var fail_reason: String = String(metric_payload.get("fail_reason", "")).strip_edges()
	if fail_reason == "":
		return false

	var trace: Variant = metric_payload.get("trace", [])
	if trace is Array:
		return trace.size() > 0
	if trace is String:
		return String(trace).strip_edges() != ""
	return false

func _ingest_report(existing_report: Dictionary, candidate_patch: Dictionary) -> Dictionary:
	var merged: Dictionary = existing_report.duplicate(true)
	for scenario in candidate_patch.keys():
		merged[scenario] = candidate_patch[scenario]

	var issues: Array[String] = _validate_launch_report(merged)
	if issues.is_empty():
		return {
			"accepted": true,
			"report": merged,
			"issues": issues
		}

	return {
		"accepted": false,
		"report": existing_report.duplicate(true),
		"issues": issues
	}

func _build_current_launch_report_stub() -> Dictionary:
	return {
		"worst_case_wave_mix": {
			"memory_growth": {
				"verdict": "pass",
				"value_mb": 180.0,
				"threshold_mb": 256.0
			},
			"stutter_spikes": {
				"verdict": "pass",
				"p99_ms": 16.0,
				"threshold_ms": 22.0
			}
		},
		"ui_heavy": {
			"memory_growth": {
				"fail_reason": "Growth exceeded during animated panel spam",
				"trace": ["frame:3102", "frame:3103"]
			},
			"stutter_spikes": {
				"fail_reason": "Spike exceeded while opening layered menus",
				"trace": ["frame:3110", "frame:3111"]
			}
		}
	}

func _build_valid_launch_report() -> Dictionary:
	return {
		"worst_case_wave_mix": {
			"memory_growth": {
				"verdict": "pass"
			},
			"stutter_spikes": {
				"verdict": "pass"
			}
		},
		"ui_heavy": {
			"memory_growth": {
				"fail_reason": "Known leak under capture mode",
				"trace": ["frame:4201"]
			},
			"stutter_spikes": {
				"fail_reason": "Transient shader compile hitch",
				"trace": ["frame:4202", "frame:4203"]
			}
		}
	}
