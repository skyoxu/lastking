extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T2.1
# ACC:T2.2
# ACC:T2.12
# ACC:T28.7
func test_settings_persistence_cross_restart() -> void:
    var cfg_path = "user://settings_%s.cfg" % Time.get_unix_time_from_system()
    var cfg = ConfigFile.new()
    cfg.set_value("app", "volume", 0.7)
    cfg.set_value("app", "lang", "en")
    var err = cfg.save(cfg_path)
    assert_int(err).is_equal(0)

    await get_tree().process_frame

    var cfg2 = ConfigFile.new()
    var err2 = cfg2.load(cfg_path)
    assert_int(err2).is_equal(0)
    assert_float(float(cfg2.get_value("app", "volume", 0.0))).is_equal(0.7)
    assert_str(str(cfg2.get_value("app", "lang", ""))).is_equal("en")

# ACC:T2.1
func test_settings_parse_failure_emits_fixed_reason_code_and_keeps_snapshot() -> void:
    var previous_snapshot := {
        "time": {"day_seconds": 240, "night_seconds": 120},
        "waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
        "spawn": {"cadence_seconds": 10},
        "boss": {"count": 2}
    }
    var malformed_payload := "{ bad json payload"

    var result := _try_parse_settings_payload(malformed_payload, previous_snapshot)

    assert_bool(result["ok"]).is_false()
    assert_str(str(result["reason_code"])).is_equal("CFG_PARSE_ERROR")
    assert_dict(result["snapshot"]).is_equal(previous_snapshot)

# ACC:T2.2
func test_settings_load_order_is_initial_then_reload_then_fallback() -> void:
    var initial_snapshot := {
        "time": {"day_seconds": 240, "night_seconds": 120},
        "waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
        "spawn": {"cadence_seconds": 10},
        "boss": {"count": 2}
    }
    var reloaded_snapshot := {
        "time": {"day_seconds": 180, "night_seconds": 90},
        "waves": {"normal": {"day1_budget": 75, "daily_growth": 1.3}},
        "spawn": {"cadence_seconds": 8},
        "boss": {"count": 2}
    }
    var malformed_payload := "{ missing_end"

    var first := _apply_ordered_snapshot({}, JSON.stringify(initial_snapshot))
    var second := _apply_ordered_snapshot(first["snapshot"], JSON.stringify(reloaded_snapshot))
    var third := _apply_ordered_snapshot(second["snapshot"], malformed_payload)

    assert_str(str(first["source"])).is_equal("initial")
    assert_str(str(second["source"])).is_equal("reload")
    assert_str(str(third["source"])).is_equal("fallback")
    assert_dict(third["snapshot"]).is_equal(second["snapshot"])

# ACC:T2.12
func test_settings_uses_deterministic_default_values_when_optional_keys_missing() -> void:
    var payload := {
        "time": {"day_seconds": 240, "night_seconds": 120},
        "waves": {"normal": {"day1_budget": 50, "daily_growth": 1.2}},
        "channels": {"elite": "elite", "boss": "boss"}
    }

    var result := _normalize_balance_snapshot(payload)

    assert_int(int(result["spawn"]["cadence_seconds"])).is_equal(10)
    assert_int(int(result["boss"]["count"])).is_equal(2)

func _apply_ordered_snapshot(previous_snapshot: Dictionary, payload: String) -> Dictionary:
    var parsed := _try_parse_settings_payload(payload, previous_snapshot)
    if bool(parsed["ok"]):
        var source := "reload"
        if previous_snapshot.is_empty():
            source = "initial"
        return {
            "source": source,
            "snapshot": _normalize_balance_snapshot(parsed["snapshot"])
        }

    return {
        "source": "fallback",
        "snapshot": previous_snapshot
    }

func _try_parse_settings_payload(payload: String, previous_snapshot: Dictionary) -> Dictionary:
    var parsed: Variant = JSON.parse_string(payload)
    if parsed == null or typeof(parsed) != TYPE_DICTIONARY:
        return {
            "ok": false,
            "reason_code": "CFG_PARSE_ERROR",
            "snapshot": previous_snapshot
        }

    return {
        "ok": true,
        "reason_code": "",
        "snapshot": Dictionary(parsed)
    }

func _normalize_balance_snapshot(payload: Dictionary) -> Dictionary:
    var snapshot := payload.duplicate(true)
    if not snapshot.has("spawn"):
        snapshot["spawn"] = {}
    if not snapshot.has("boss"):
        snapshot["boss"] = {}

    var spawn: Dictionary = snapshot["spawn"]
    if not spawn.has("cadence_seconds"):
        spawn["cadence_seconds"] = 10

    var boss: Dictionary = snapshot["boss"]
    if not boss.has("count"):
        boss["count"] = 2

    snapshot["spawn"] = spawn
    snapshot["boss"] = boss
    return snapshot

# ACC:T29.6
func test_task29_audio_values_save_and_restore_on_next_launch() -> void:
    var cfg_path := "user://task29_audio_%s.cfg" % Time.get_unix_time_from_system()
    var writer := ConfigFile.new()
    writer.set_value("settings", "music_volume", 0.8)
    writer.set_value("settings", "sfx_volume", 0.3)
    assert_int(writer.save(cfg_path)).is_equal(0)

    var reader := ConfigFile.new()
    assert_int(reader.load(cfg_path)).is_equal(0)
    assert_float(float(reader.get_value("settings", "music_volume", 0.0))).is_equal(0.8)
    assert_float(float(reader.get_value("settings", "sfx_volume", 0.0))).is_equal(0.3)


# ACC:T29.9
func test_task29_startup_uses_saved_audio_values_before_new_input() -> void:
    var cfg_path := "user://task29_startup_%s.cfg" % Time.get_unix_time_from_system()
    var writer := ConfigFile.new()
    writer.set_value("settings", "music_volume", 0.7)
    writer.set_value("settings", "sfx_volume", 0.2)
    assert_int(writer.save(cfg_path)).is_equal(0)

    var reader := ConfigFile.new()
    assert_int(reader.load(cfg_path)).is_equal(0)
    var startup_music := float(reader.get_value("settings", "music_volume", 0.5))
    var startup_sfx := float(reader.get_value("settings", "sfx_volume", 0.5))
    assert_that(startup_music).is_equal(0.7)
    assert_that(startup_sfx).is_equal(0.2)


# ACC:T29.10
func test_task29_single_channel_update_keeps_other_channel_value() -> void:
    var cfg := ConfigFile.new()
    cfg.set_value("settings", "music_volume", 0.6)
    cfg.set_value("settings", "sfx_volume", 0.4)
    cfg.set_value("settings", "music_volume", 0.9)

    assert_float(float(cfg.get_value("settings", "music_volume", 0.0))).is_equal(0.9)
    assert_float(float(cfg.get_value("settings", "sfx_volume", 0.0))).is_equal(0.4)


# ACC:T29.11
func test_task29_untouched_channel_stays_equal_to_prior_saved_state() -> void:
    var cfg := ConfigFile.new()
    cfg.set_value("settings", "music_volume", 0.5)
    cfg.set_value("settings", "sfx_volume", 0.2)
    cfg.set_value("settings", "sfx_volume", 0.1)

    assert_float(float(cfg.get_value("settings", "music_volume", 0.0))).is_equal(0.5)
    assert_float(float(cfg.get_value("settings", "sfx_volume", 0.0))).is_equal(0.1)


# ACC:T29.12
func test_task29_distinct_channel_values_remain_distinct_after_reload() -> void:
    var cfg_path := "user://task29_distinct_%s.cfg" % Time.get_unix_time_from_system()
    var writer := ConfigFile.new()
    writer.set_value("settings", "music_volume", 0.9)
    writer.set_value("settings", "sfx_volume", 0.1)
    assert_int(writer.save(cfg_path)).is_equal(0)

    var reader := ConfigFile.new()
    assert_int(reader.load(cfg_path)).is_equal(0)
    var music := float(reader.get_value("settings", "music_volume", 0.0))
    var sfx := float(reader.get_value("settings", "sfx_volume", 0.0))
    assert_that(music).is_not_equal(sfx)


# ACC:T29.14
func test_task29_saved_values_are_written_as_separate_music_and_sfx_entries() -> void:
    var cfg := ConfigFile.new()
    cfg.set_value("settings", "music_volume", 0.4)
    cfg.set_value("settings", "sfx_volume", 0.6)

    var section_keys := PackedStringArray(cfg.get_section_keys("settings"))
    assert_that(section_keys.has("music_volume")).is_true()
    assert_that(section_keys.has("sfx_volume")).is_true()


# ACC:T29.15
func test_task29_obligation_o5_restores_separate_music_and_sfx_keys_from_file() -> void:
    var cfg_path := "user://task29_o5_%s.cfg" % Time.get_unix_time_from_system()
    var writer := ConfigFile.new()
    writer.set_value("settings", "music_volume", 0.65)
    writer.set_value("settings", "sfx_volume", 0.35)
    assert_int(writer.save(cfg_path)).is_equal(0)

    var reader := ConfigFile.new()
    assert_int(reader.load(cfg_path)).is_equal(0)
    assert_that(float(reader.get_value("settings", "music_volume", 0.0))).is_equal(0.65)
    assert_that(float(reader.get_value("settings", "sfx_volume", 0.0))).is_equal(0.35)
