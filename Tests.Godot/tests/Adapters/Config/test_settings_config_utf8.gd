extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T2.18
# ACC:T2.19
func test_configfile_utf8_roundtrip() -> void:
    var path: String = "user://settings_%s.cfg" % Time.get_unix_time_from_system()
    var cfg := ConfigFile.new()
    var note := "UTF8 note: cafe, nao, uber"
    cfg.set_value("time", "day_seconds", 240)
    cfg.set_value("time", "night_seconds", 120)
    cfg.set_value("waves", "day1_budget", 50)
    cfg.set_value("waves", "daily_growth", 1.2)
    cfg.set_value("spawn", "cadence_seconds", 10)
    cfg.set_value("boss", "count", 2)
    cfg.set_value("app", "volume", 0.66)
    cfg.set_value("app", "lang", "zh")
    cfg.set_value("app", "note", note)
    var err: int = cfg.save(path)
    assert_int(err).is_equal(0)
    await get_tree().process_frame

    var cfg2 := ConfigFile.new()
    var err2: int = cfg2.load(path)
    assert_int(err2).is_equal(0)
    assert_float(float(cfg2.get_value("app", "volume", 0.0))).is_equal(0.66)
    assert_str(str(cfg2.get_value("app", "lang", ""))).is_equal("zh")
    assert_str(str(cfg2.get_value("app", "note", ""))).is_equal(note)
    assert_int(int(cfg2.get_value("time", "day_seconds", 0))).is_equal(240)
    assert_int(int(cfg2.get_value("time", "night_seconds", 0))).is_equal(120)
    assert_int(int(cfg2.get_value("waves", "day1_budget", 0))).is_equal(50)
    assert_float(float(cfg2.get_value("waves", "daily_growth", 0.0))).is_equal(1.2)
    assert_int(int(cfg2.get_value("spawn", "cadence_seconds", 0))).is_equal(10)
    assert_int(int(cfg2.get_value("boss", "count", 0))).is_equal(2)
