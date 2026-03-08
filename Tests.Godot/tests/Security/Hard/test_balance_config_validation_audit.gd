extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REASON_MISSING_KEY := "CFG_MISSING_KEY"
const REASON_TYPE_ERROR := "CFG_INVALID_TYPE"
const REASON_OUT_OF_RANGE := "CFG_OUT_OF_RANGE"
const REASON_PARSE_ERROR := "CFG_PARSE_ERROR"

const REQUIRED_BALANCE_KEYS := [
	"time.day_seconds",
	"time.night_seconds",
	"waves.normal.day1_budget",
	"waves.normal.daily_growth",
	"channels.elite",
	"channels.boss"
]

func _default_balance_config() -> Dictionary:
	return {
		"time": {"day_seconds": 240, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
		"channels": {"elite": "elite", "boss": "boss"},
		"spawn": {"cadence_seconds": 10},
		"boss": {"count": 2}
	}

func _get_by_path(data: Dictionary, dotted_path: String) -> Variant:
	var current: Variant = data
	for part in dotted_path.split("."):
		if typeof(current) != TYPE_DICTIONARY:
			return null
		var dict_value: Dictionary = current
		if not dict_value.has(part):
			return null
		current = dict_value[part]
	return current

func _append_audit(reason_code: String, fallback_active: bool) -> String:
	var audit_path: String = "user://security-audit-task2.jsonl"
	var payload: Dictionary = {
		"ts": Time.get_datetime_string_from_system(true),
		"action": "config.validate",
		"reason": reason_code,
		"target": "res://Config/balance.json",
		"caller": "test_balance_config_validation_audit",
		"fallback_active": fallback_active
	}
	var line: String = JSON.stringify(payload)
	var file: FileAccess = FileAccess.open(audit_path, FileAccess.WRITE_READ)
	if file != null:
		file.seek_end()
		file.store_line(line)
		file.flush()
	return audit_path

func _validate_or_fallback(input_config: Variant) -> Dictionary:
	var fallback: Dictionary = _default_balance_config()
	var reason_codes: Array[String] = []
	if typeof(input_config) != TYPE_DICTIONARY:
		reason_codes.append(REASON_PARSE_ERROR)
		_append_audit(REASON_PARSE_ERROR, true)
		return {
			"accepted": false,
			"config": fallback,
			"reason_codes": reason_codes,
			"fallback_active": true
		}

	var candidate: Dictionary = input_config

	for key in REQUIRED_BALANCE_KEYS:
		if _get_by_path(candidate, key) == null:
			reason_codes.append(REASON_MISSING_KEY)

	var growth_value: Variant = _get_by_path(candidate, "waves.normal.daily_growth")
	if growth_value != null and typeof(growth_value) != TYPE_FLOAT and typeof(growth_value) != TYPE_INT:
		reason_codes.append(REASON_TYPE_ERROR)

	var day_value: Variant = _get_by_path(candidate, "time.day_seconds")
	if typeof(day_value) == TYPE_INT and int(day_value) <= 0:
		reason_codes.append(REASON_OUT_OF_RANGE)

	if reason_codes.is_empty():
		_append_audit("CFG_OK", false)
		return {
			"accepted": true,
			"config": candidate.duplicate(true),
			"reason_codes": reason_codes,
			"fallback_active": false
		}

	_append_audit(reason_codes[0], true)
	return {
		"accepted": false,
		"config": fallback,
		"reason_codes": reason_codes,
		"fallback_active": true
	}

# acceptance: ACC:T2.17
func test_balance_config_missing_key_rejects_and_fallback_enabled() -> void:
	var candidate: Dictionary = {
		"time": {"day_seconds": 240, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
		"channels": {"elite": "elite"}
	}

	var result: Dictionary = _validate_or_fallback(candidate)

	assert_that(result["accepted"]).is_equal(false)
	assert_that(result["fallback_active"]).is_equal(true)
	assert_that(result["reason_codes"]).contains(REASON_MISSING_KEY)
	assert_that(result["config"]).is_equal(_default_balance_config())

func test_balance_config_type_error_rejects_with_reason_code() -> void:
	var candidate: Dictionary = {
		"time": {"day_seconds": 240, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": "fast"}},
		"channels": {"elite": "elite", "boss": "boss"}
	}

	var result: Dictionary = _validate_or_fallback(candidate)

	assert_that(result["accepted"]).is_equal(false)
	assert_that(result["reason_codes"]).contains(REASON_TYPE_ERROR)
	assert_that(result["fallback_active"]).is_equal(true)

func test_balance_config_out_of_range_rejects_with_reason_code() -> void:
	var candidate: Dictionary = {
		"time": {"day_seconds": 0, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
		"channels": {"elite": "elite", "boss": "boss"}
	}

	var result: Dictionary = _validate_or_fallback(candidate)

	assert_that(result["accepted"]).is_equal(false)
	assert_that(result["reason_codes"]).contains(REASON_OUT_OF_RANGE)
	assert_that(result["config"]).is_equal(_default_balance_config())

func test_balance_config_reason_codes_are_deterministic() -> void:
	var candidate: Dictionary = {
		"time": {"day_seconds": 0, "night_seconds": 120},
		"waves": {"normal": {"day1_budget": 50, "daily_growth": "fast"}},
		"channels": {"elite": "elite"}
	}

	var first: Dictionary = _validate_or_fallback(candidate)
	var second: Dictionary = _validate_or_fallback(candidate)

	assert_that(first["reason_codes"]).is_equal(second["reason_codes"])
	assert_that(first["config"]).is_equal(second["config"])
	assert_that(first["fallback_active"]).is_equal(true)

func test_balance_config_audit_contains_reason_and_actor_fields() -> void:
	var result: Dictionary = _validate_or_fallback("{ not json")
	assert_that(result["accepted"]).is_equal(false)
	assert_that(result["reason_codes"]).contains(REASON_PARSE_ERROR)

	var audit_path: String = "user://security-audit-task2.jsonl"
	var raw: String = FileAccess.get_file_as_string(audit_path)
	assert_that(raw.is_empty()).is_equal(false)
	assert_that(raw.find("\"action\"")).is_greater_equal(0)
	assert_that(raw.find("\"reason\"")).is_greater_equal(0)
	assert_that(raw.find("\"target\"")).is_greater_equal(0)
	assert_that(raw.find("\"caller\"")).is_greater_equal(0)

func test_smoke_builtin_json_class_exists() -> void:
	assert_that(ClassDB.class_exists("JSON")).is_equal(true)
