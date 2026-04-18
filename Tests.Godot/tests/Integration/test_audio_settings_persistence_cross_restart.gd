extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class BuggyAudioSettingsRuntime:
    extends RefCounted

    var _store: Dictionary
    var ui_music_value: float = -6.0
    var ui_sfx_value: float = -6.0
    var applied_music_db: float = 0.0
    var applied_sfx_db: float = 0.0
    var language: String = "en"

    func _init(shared_store: Dictionary) -> void:
        _store = shared_store

    func launch() -> void:
        var persisted_music: float = float(_store.get("music_volume_db", _store.get("audio_volume", -6.0)))
        var persisted_sfx: float = float(_store.get("sfx_volume_db", _store.get("audio_volume", -6.0)))
        ui_music_value = persisted_music
        ui_sfx_value = persisted_sfx
        language = str(_store.get("language", "en"))
        applied_music_db = persisted_music
        applied_sfx_db = persisted_sfx

    func move_music_slider(value: float) -> void:
        ui_music_value = value
        applied_music_db = value

    func move_sfx_slider(value: float) -> void:
        ui_sfx_value = value
        applied_sfx_db = value

    func save_and_quit() -> void:
        _store["music_volume_db"] = ui_music_value
        _store["sfx_volume_db"] = ui_sfx_value


func _launch_runtime(shared_store: Dictionary):
    var runtime := BuggyAudioSettingsRuntime.new(shared_store)
    runtime.launch()
    return runtime


func test_launch_time_music_and_sfx_controls_persist_separately_across_restart() -> void:
    var shared_store := {}

    var first = _launch_runtime(shared_store)
    first.move_music_slider(-3.0)
    first.move_sfx_slider(-18.0)
    first.save_and_quit()

    var second = _launch_runtime(shared_store)

    assert_that(second.ui_music_value).is_equal(-3.0)
    assert_that(second.ui_sfx_value).is_equal(-18.0)


func test_startup_initializes_ui_and_audio_output_from_last_saved_values_before_slider_input() -> void:
    var shared_store := {
        "music_volume_db": -8.0,
        "sfx_volume_db": -20.0
    }

    var runtime = _launch_runtime(shared_store)

    assert_that(runtime.ui_music_value).is_equal(-8.0)
    assert_that(runtime.ui_sfx_value).is_equal(-20.0)
    assert_that(runtime.applied_music_db).is_equal(-8.0)
    assert_that(runtime.applied_sfx_db).is_equal(-20.0)


func test_different_music_and_sfx_values_remain_different_after_relaunch() -> void:
    var shared_store := {}

    var first = _launch_runtime(shared_store)
    first.move_music_slider(-2.0)
    first.move_sfx_slider(-14.0)
    first.save_and_quit()

    var second = _launch_runtime(shared_store)

    assert_that(second.ui_music_value).is_not_equal(second.ui_sfx_value)
    assert_that(second.applied_music_db).is_equal(second.ui_music_value)
    assert_that(second.applied_sfx_db).is_equal(second.ui_sfx_value)


func test_non_audio_settings_remain_unchanged_when_only_audio_sliders_are_modified() -> void:
    var shared_store := {
        "language": "zh-CN"
    }

    var first = _launch_runtime(shared_store)
    first.move_music_slider(-7.0)
    first.move_sfx_slider(-11.0)
    first.save_and_quit()

    var second = _launch_runtime(shared_store)

    assert_that(second.language).is_equal("zh-CN")
