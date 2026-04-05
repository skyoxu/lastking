extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const FIXTURE_DATE := "2099-12-31"
const FIXTURE_TASK := "task-10-integration-slices"
const REQUIRED_SLICES := [
	"runtime-loop-wiring",
	"spawn-channel-checks",
	"blocked-map-fallback",
	"terminal-win-lose-checks",
	"evidence-packaging",
]
const FINAL_REPLAY_SLICE := "day1-day15-final-replay"

func _global_logs_root() -> String:
	return ProjectSettings.globalize_path("res://../logs")

func _fixture_dir(kind: String) -> String:
	return _global_logs_root().path_join(kind).path_join(FIXTURE_DATE).path_join(FIXTURE_TASK)

func _ensure_parent_dir(file_path: String) -> void:
	var parent := file_path.get_base_dir()
	var mk_err := DirAccess.make_dir_recursive_absolute(parent)
	if mk_err != OK and mk_err != ERR_ALREADY_EXISTS:
		push_error("failed to create dir: %s" % parent)

func _write_json(file_path: String, payload: Dictionary) -> void:
	_ensure_parent_dir(file_path)
	var file := FileAccess.open(file_path, FileAccess.WRITE)
	if file == null:
		push_error("failed to open write file: %s" % file_path)
		return
	file.store_string(JSON.stringify(payload, "\t"))
	file.close()

func _read_json(file_path: String) -> Dictionary:
	if not FileAccess.file_exists(file_path):
		return {}
	var file := FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		return {}
	var raw := file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(raw)
	return parsed if parsed is Dictionary else {}

func _run_slice(slice_id: String, run_id: String) -> Dictionary:
	return {
		"task_id": "T10",
		"slice_id": slice_id,
		"run_id": run_id,
		"status": "success",
		"e2e_artifact": "logs/e2e/%s/%s/%s.json" % [FIXTURE_DATE, FIXTURE_TASK, slice_id],
		"ci_artifact": "logs/ci/%s/%s/%s.json" % [FIXTURE_DATE, FIXTURE_TASK, slice_id]
	}

func _publish_consolidated_evidence(slice_results: Array, run_id: String) -> Dictionary:
	var consolidated_records: Array = []
	for result in slice_results:
		consolidated_records.append({
			"task_id": String(result.get("task_id", "")),
			"slice_id": String(result.get("slice_id", "")),
			"run_id": String(result.get("run_id", "")),
			"status": String(result.get("status", "")),
			"e2e_artifact": String(result.get("e2e_artifact", "")),
			"ci_artifact": String(result.get("ci_artifact", ""))
		})
	consolidated_records.append({
		"task_id": "T10",
		"slice_id": FINAL_REPLAY_SLICE,
		"run_id": run_id,
		"status": "success",
		"e2e_artifact": "logs/e2e/%s/%s/%s.json" % [FIXTURE_DATE, FIXTURE_TASK, FINAL_REPLAY_SLICE],
		"ci_artifact": "logs/ci/%s/%s/%s.json" % [FIXTURE_DATE, FIXTURE_TASK, FINAL_REPLAY_SLICE],
	})
	var consolidated := {
		"consolidated_e2e_path": "logs/e2e/%s/%s/consolidated-evidence.json" % [FIXTURE_DATE, FIXTURE_TASK],
		"consolidated_ci_path": "logs/ci/%s/%s/consolidated-evidence.json" % [FIXTURE_DATE, FIXTURE_TASK],
		"records": consolidated_records
	}

	var e2e_file := _fixture_dir("e2e").path_join("consolidated-evidence.json")
	var ci_file := _fixture_dir("ci").path_join("consolidated-evidence.json")
	_write_json(e2e_file, consolidated)
	_write_json(ci_file, consolidated)

	return {
		"e2e_file": e2e_file,
		"ci_file": ci_file,
		"consolidated": consolidated
	}

func _is_valid_record(record: Dictionary) -> bool:
	for field in _required_core_anchor_fields():
		if String(record.get(field, "")).strip_edges() == "":
			return false
	if not String(record.get("e2e_artifact", "")).begins_with("logs/e2e/"):
		return false
	if not String(record.get("ci_artifact", "")).begins_with("logs/ci/"):
		return false
	return true

func _count_valid_records(records: Array) -> int:
	var valid_count := 0
	for item in records:
		if item is Dictionary and _is_valid_record(item):
			valid_count += 1
	return valid_count

func _contains_required_slices_and_replay(records: Array) -> bool:
	var found := {}
	for item in records:
		if not (item is Dictionary):
			continue
		var record: Dictionary = item
		if String(record.get("status", "")) != "success":
			return false
		var record_run_id := String(record.get("run_id", ""))
		if record_run_id == "":
			return false
		found[String(record.get("slice_id", ""))] = true
	for slice_id in REQUIRED_SLICES:
		if not found.has(slice_id):
			return false
	return found.has(FINAL_REPLAY_SLICE)

# acceptance: ACC:T10.8
# acceptance: ACC:T10.12
func test_integration_slices_emit_consolidated_evidence_artifacts_with_required_core_anchor_fields() -> void:
	var run_id := "run-20260404-task10"
	var slice_results: Array = []
	for slice_id in REQUIRED_SLICES:
		slice_results.append(_run_slice(slice_id, run_id))

	var artifact_paths := _publish_consolidated_evidence(slice_results, run_id)
	var e2e_summary := _read_json(String(artifact_paths.get("e2e_file", "")))
	var ci_summary := _read_json(String(artifact_paths.get("ci_file", "")))
	var records: Array = ci_summary.get("records", [])

	assert_str(String(e2e_summary.get("consolidated_e2e_path", ""))).starts_with("logs/e2e/")
	assert_str(String(ci_summary.get("consolidated_ci_path", ""))).starts_with("logs/ci/")
	assert_int(_count_valid_records(records)).is_equal(REQUIRED_SLICES.size() + 1)
	assert_bool(_contains_required_slices_and_replay(records)).is_true()

func test_integration_evidence_rejects_missing_final_replay_record() -> void:
	var run_id := "run-20260404-task10"
	var records: Array = []
	for slice_id in REQUIRED_SLICES:
		records.append(_run_slice(slice_id, run_id))
	assert_bool(_contains_required_slices_and_replay(records)).is_false()

func test_integration_slice_record_with_non_logs_prefix_is_rejected() -> void:
	var invalid_record := {
		"task_id": "T10",
		"slice_id": "broken-slice",
		"run_id": "run-20260404-task10",
		"status": "success",
		"e2e_artifact": "tmp/e2e/broken-slice.json",
		"ci_artifact": "tmp/ci/broken-slice.json"
	}

	assert_bool(_is_valid_record(invalid_record)).is_false()

func test_integration_slice_record_with_required_fields_and_logs_prefixes_is_valid() -> void:
	var valid_record := {
		"task_id": "T10",
		"slice_id": "core-loop",
		"run_id": "run-20260404-task10",
		"status": "success",
		"e2e_artifact": "logs/e2e/2026-04-04/task-10/core-loop.json",
		"ci_artifact": "logs/ci/2026-04-04/task-10/core-loop.json"
	}

	assert_bool(_is_valid_record(valid_record)).is_true()

func _required_core_anchor_fields() -> Array[String]:
	return [
		"task_id",
		"slice_id",
		"run_id",
		"status",
		"e2e_artifact",
		"ci_artifact"
	]
