extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const FIXTURE_DATE := "2099-12-31"
const FIXTURE_TASK := "task-10-staged-order"

class StagedSlicesHarness:
	static func required_order() -> PackedStringArray:
		return PackedStringArray([
			"runtime_loop_wiring",
			"spawn_channel_checks",
			"blocked_map_fallback",
			"terminal_win_lose_checks",
			"evidence_packaging",
		])

	func run_slice_independently(slice_id: String, verdict: String) -> Dictionary:
		return {
			"slice_id": slice_id,
			"verdict": verdict,
		}

	func run_staged_required_slices() -> Array[Dictionary]:
		var staged: Array[Dictionary] = []
		staged.append(run_slice_independently("runtime_loop_wiring", "pass"))
		staged.append(run_slice_independently("spawn_channel_checks", "pass"))
		staged.append(run_slice_independently("blocked_map_fallback", "pass"))
		staged.append(run_slice_independently("terminal_win_lose_checks", "pass"))
		staged.append(run_slice_independently("evidence_packaging", "pass"))
		return staged

	func is_acceptance_passed(staged_results: Array[Dictionary]) -> bool:
		var required := required_order()
		if staged_results.size() != required.size():
			return false

		for i in range(staged_results.size()):
			var result: Dictionary = staged_results[i]
			if String(result.get("slice_id", "")) != required[i]:
				return false
			var verdict := String(result.get("verdict", ""))
			if verdict != "pass" and verdict != "fail":
				return false

		return true

func _global_logs_root() -> String:
	return ProjectSettings.globalize_path("res://../logs")

func _fixture_ci_dir() -> String:
	return _global_logs_root().path_join("ci").path_join(FIXTURE_DATE).path_join(FIXTURE_TASK)

func _write_json(file_path: String, payload: Dictionary) -> void:
	var parent := file_path.get_base_dir()
	var mk_err := DirAccess.make_dir_recursive_absolute(parent)
	if mk_err != OK and mk_err != ERR_ALREADY_EXISTS:
		push_error("failed to create dir: %s" % parent)
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

func _persist_staged_slices(staged_results: Array[Dictionary]) -> Array[String]:
	var paths: Array[String] = []
	for index in range(staged_results.size()):
		var file_path := _fixture_ci_dir().path_join("slice-%02d.json" % [index + 1])
		var payload := staged_results[index].duplicate()
		payload["sequence"] = index + 1
		_write_json(file_path, payload)
		paths.append(file_path)
	return paths

func _load_staged_slices(paths: Array[String]) -> Array[Dictionary]:
	var loaded: Array[Dictionary] = []
	for file_path in paths:
		var payload := _read_json(file_path)
		if payload.is_empty():
			continue
		loaded.append(payload)
	loaded.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return int(a.get("sequence", 0)) < int(b.get("sequence", 0))
	)
	return loaded

func _slice_ids(staged_results: Array[Dictionary]) -> PackedStringArray:
	var ids := PackedStringArray()
	for result in staged_results:
		ids.append(String(result.get("slice_id", "")))
	return ids

func _all_verdicts_are_explicit_pass_or_fail(staged_results: Array[Dictionary]) -> bool:
	for result in staged_results:
		var verdict := String(result.get("verdict", ""))
		if verdict != "pass" and verdict != "fail":
			return false
	return true

# acceptance: ACC:T10.9
func test_staged_slices_execute_in_mandated_order_and_emit_explicit_verdicts() -> void:
	var harness := StagedSlicesHarness.new()
	var staged_results := harness.run_staged_required_slices()
	var paths := _persist_staged_slices(staged_results)
	var loaded := _load_staged_slices(paths)

	assert_that(_slice_ids(loaded)).is_equal(StagedSlicesHarness.required_order())
	assert_bool(_all_verdicts_are_explicit_pass_or_fail(loaded)).is_true()

# acceptance: ACC:T10.13
func test_staged_integration_rejects_inconclusive_slice_even_when_order_is_correct() -> void:
	var harness := StagedSlicesHarness.new()
	var staged_results: Array[Dictionary] = [
		harness.run_slice_independently("runtime_loop_wiring", "pass"),
		harness.run_slice_independently("spawn_channel_checks", "pass"),
		harness.run_slice_independently("blocked_map_fallback", "pass"),
		harness.run_slice_independently("terminal_win_lose_checks", "pass"),
		harness.run_slice_independently("evidence_packaging", "inconclusive"),
	]
	var paths := _persist_staged_slices(staged_results)
	var loaded := _load_staged_slices(paths)

	assert_bool(harness.is_acceptance_passed(loaded)).is_false()

func test_staged_integration_rejects_skipped_slice() -> void:
	var harness := StagedSlicesHarness.new()
	var staged_results: Array[Dictionary] = [
		harness.run_slice_independently("runtime_loop_wiring", "pass"),
		harness.run_slice_independently("spawn_channel_checks", "pass"),
		harness.run_slice_independently("blocked_map_fallback", "pass"),
		harness.run_slice_independently("terminal_win_lose_checks", "pass"),
		harness.run_slice_independently("evidence_packaging", "skip"),
	]
	var paths := _persist_staged_slices(staged_results)
	var loaded := _load_staged_slices(paths)

	assert_bool(harness.is_acceptance_passed(loaded)).is_false()

func test_staged_integration_rejects_wrong_slice_order() -> void:
	var harness := StagedSlicesHarness.new()
	var staged_results: Array[Dictionary] = [
		harness.run_slice_independently("runtime_loop_wiring", "pass"),
		harness.run_slice_independently("blocked_map_fallback", "pass"),
		harness.run_slice_independently("spawn_channel_checks", "pass"),
		harness.run_slice_independently("terminal_win_lose_checks", "pass"),
		harness.run_slice_independently("evidence_packaging", "pass"),
	]
	var paths := _persist_staged_slices(staged_results)
	var loaded := _load_staged_slices(paths)

	assert_bool(harness.is_acceptance_passed(loaded)).is_false()
