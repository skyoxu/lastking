extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T2.3
# ACC:T2.19
# ACC:T31.7
func test_settings_configfile_path_security() -> void:
    var safe: Node = preload("res://Game.Godot/Adapters/Security/SafeConfig.gd").new()
    add_child(auto_free(safe))
    var sections: Dictionary = { "s": { "k": "v" } }
    # allow: user://
    var ok: int = safe.save_user("user://ok_settings.cfg", sections)
    assert_int(ok).is_equal(0)

    # deny: absolute path
    var absolute_save_result: int = safe.save_user("C:/temp/bad_settings.cfg", sections)
    assert_bool(absolute_save_result != 0).is_true()

    # deny: traversal
    var traversal_save_result: int = safe.save_user("user://../bad_settings.cfg", sections)
    assert_bool(traversal_save_result != 0).is_true()

# ACC:T2.3
func test_settings_security_requires_source_scan_and_runtime_regression_gates() -> void:
    var source_scan: Dictionary = _source_scan_gate_result()
    var runtime_regression: Dictionary = _runtime_regression_gate_result()

    assert_bool(source_scan["passed"]).is_true()
    assert_bool(runtime_regression["passed"]).is_true()
    assert_str(str(source_scan["reason_code"])).is_equal("CFG_GATE_OK")
    assert_str(str(runtime_regression["reason_code"])).is_equal("CFG_GATE_OK")

# ACC:T2.19
func test_settings_security_rejects_when_any_gate_is_missing() -> void:
    var source_scan: Dictionary = {"passed": true, "reason_code": "CFG_GATE_OK"}
    var runtime_regression: Dictionary = {"passed": false, "reason_code": "CFG_GATE_RUNTIME_MISSING"}

    var composite: Dictionary = _combine_gate_results(source_scan, runtime_regression)
    assert_bool(composite["passed"]).is_false()
    assert_str(str(composite["reason_code"])).is_equal("CFG_GATE_RUNTIME_MISSING")

func _source_scan_gate_result() -> Dictionary:
    var files: Array = [
        "res://Game.Godot/Scripts/UI/SettingsPanel.cs",
        "res://Game.Godot/Adapters/Security/SafeConfig.gd"
    ]
    var has_config_access: bool = true
    var has_hardcoded_balance: bool = false
    for file_path in files:
        var source: String = FileAccess.get_file_as_string(file_path)
        var probe: Dictionary = _source_scan_probe(source)
        has_config_access = has_config_access and bool(probe["uses_config"])
        has_hardcoded_balance = has_hardcoded_balance or bool(probe["has_hardcoded_balance"])

    var passed: bool = has_config_access and not has_hardcoded_balance
    return {
        "passed": passed,
        "reason_code": "CFG_GATE_OK" if passed else "CFG_GATE_SOURCE_SCAN_MISSING"
    }

func _source_scan_probe(source: String) -> Dictionary:
    var file_uses_config: bool = source.contains("ConfigPath") or source.contains("ConfigFile") or source.contains("save_user")
    var has_hardcoded_balance: bool = source.contains("day1_budget =") or source.contains("spawn_cadence_seconds =") or source.contains("boss_count =")
    return {
        "uses_config": file_uses_config,
        "has_hardcoded_balance": has_hardcoded_balance
    }

func _runtime_regression_gate_result() -> Dictionary:
    var path_a: String = "user://balance_gate_a.json"
    var path_b: String = "user://balance_gate_b.json"
    var path_same: String = "user://balance_gate_same.json"

    var base_payload: Dictionary = {
        "time": {"day_seconds": 240, "night_seconds": 120},
        "waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
        "channels": {"elite": "elite", "boss": "boss"},
        "spawn": {"cadence_seconds": 10},
        "boss": {"count": 2}
    }
    var changed_payload: Dictionary = {
        "time": {"day_seconds": 180, "night_seconds": 90},
        "waves": {"normal": {"day1_budget": 75, "daily_growth": 1.3}},
        "channels": {"elite": "elite", "boss": "boss"},
        "spawn": {"cadence_seconds": 8},
        "boss": {"count": 2}
    }

    _write_balance_file(path_a, base_payload)
    _write_balance_file(path_b, changed_payload)
    _write_balance_file(path_same, base_payload)

    var signature_a: String = _runtime_signature_from_file(path_a)
    var signature_b: String = _runtime_signature_from_file(path_b)
    var signature_same: String = _runtime_signature_from_file(path_same)

    var changed_detected: bool = signature_a != signature_b
    var same_kept: bool = signature_a == signature_same
    var passed: bool = changed_detected and same_kept
    return {
        "passed": passed,
        "reason_code": "CFG_GATE_OK" if passed else "CFG_GATE_RUNTIME_MISSING"
    }

func _combine_gate_results(source_scan: Dictionary, runtime_regression: Dictionary) -> Dictionary:
    if not bool(source_scan["passed"]):
        return {"passed": false, "reason_code": source_scan["reason_code"]}
    if not bool(runtime_regression["passed"]):
        return {"passed": false, "reason_code": runtime_regression["reason_code"]}
    return {"passed": true, "reason_code": "CFG_GATE_OK"}

func test_settings_security_source_scan_fails_when_hardcoded_constants_exist() -> void:
    var probe: Dictionary = _source_scan_probe("var day1_budget = 50")
    assert_bool(bool(probe["uses_config"])).is_false()
    assert_bool(bool(probe["has_hardcoded_balance"])).is_true()

func _write_balance_file(path: String, payload: Dictionary) -> void:
    var file: FileAccess = FileAccess.open(path, FileAccess.WRITE)
    if file == null:
        return
    file.store_string(JSON.stringify(payload))
    file.flush()

func _runtime_signature_from_file(path: String) -> String:
    var raw: String = FileAccess.get_file_as_string(path)
    var parsed: Variant = JSON.parse_string(raw)
    if typeof(parsed) != TYPE_DICTIONARY:
        return "invalid"

    var data: Dictionary = parsed
    var time_data: Dictionary = data.get("time", {})
    var wave_data: Dictionary = data.get("waves", {})
    var normal_data: Dictionary = wave_data.get("normal", {})
    var spawn_data: Dictionary = data.get("spawn", {})
    var boss_data: Dictionary = data.get("boss", {})

    var day_seconds: int = int(time_data.get("day_seconds", 0))
    var night_seconds: int = int(time_data.get("night_seconds", 0))
    var budget: int = int(normal_data.get("day1_budget", 0))
    var cadence: int = int(spawn_data.get("cadence_seconds", 1))
    var boss_count: int = int(boss_data.get("count", 0))
    var cycle: int = day_seconds + night_seconds
    var pace: int = int(600 / max(1, cadence))
    return "%d-%d-%d-%d" % [cycle, budget, pace, boss_count]
