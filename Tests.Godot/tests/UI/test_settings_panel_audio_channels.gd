extends 'res://addons/gdUnit4/src/GdUnitTestSuite.gd'

class FakeSettingsStore:
    var music_volume: float = -6.0
    var sfx_volume: float = -10.0

    func _init(initial_music: float = -6.0, initial_sfx: float = -10.0) -> void:
        music_volume = initial_music
        sfx_volume = initial_sfx


class FakeAudioRuntime:
    var music_volume: float = 0.0
    var sfx_volume: float = 0.0
    var interruptions := {'music': 0, 'sfx': 0}
    var runtime_errors: Array[String] = []
    var playback_active := {'music': true, 'sfx': true}

    func apply_music_volume(volume: float) -> void:
        music_volume = volume

    func apply_sfx_volume(volume: float) -> void:
        sfx_volume = volume


class SettingsPanelAudioHarness:
    var _store: FakeSettingsStore
    var _audio: FakeAudioRuntime
    var music_slider_value: float = 0.0
    var sfx_slider_value: float = 0.0

    func _init(store: FakeSettingsStore, audio: FakeAudioRuntime) -> void:
        _store = store
        _audio = audio
        music_slider_value = _store.music_volume
        sfx_slider_value = _store.sfx_volume
        _audio.apply_music_volume(music_slider_value)
        _audio.apply_sfx_volume(sfx_slider_value)

    func set_music_slider(value: float) -> void:
        music_slider_value = value
        _store.music_volume = value
        _audio.apply_music_volume(value)

    func set_sfx_slider(value: float) -> void:
        sfx_slider_value = value
        _store.sfx_volume = value
        _audio.apply_sfx_volume(value)

    func apply_rapid_changes(channel: String, values: Array) -> void:
        for value in values:
            var numeric_value := float(value)
            if channel == 'music':
                set_music_slider(numeric_value)
            elif channel == 'sfx':
                set_sfx_slider(numeric_value)
            else:
                _audio.runtime_errors.append('unknown channel: ' + channel)


func test_launch_time_controls_are_separate_and_values_persist_across_sessions() -> void:
    var shared_store := FakeSettingsStore.new(-20.0, -12.0)
    var first_audio := FakeAudioRuntime.new()
    var first_panel := SettingsPanelAudioHarness.new(shared_store, first_audio)

    first_panel.set_music_slider(-3.0)
    first_panel.set_sfx_slider(-15.0)

    var second_audio := FakeAudioRuntime.new()
    var second_panel := SettingsPanelAudioHarness.new(shared_store, second_audio)

    assert_that(second_panel.music_slider_value).is_equal(-3.0)
    assert_that(second_panel.sfx_slider_value).is_equal(-15.0)


func test_startup_initializes_sliders_and_audible_output_to_last_saved_values() -> void:
    var store := FakeSettingsStore.new(-8.0, -18.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    assert_that(panel.music_slider_value).is_equal(-8.0)
    assert_that(panel.sfx_slider_value).is_equal(-18.0)
    assert_that(audio.music_volume).is_equal(-8.0)
    assert_that(audio.sfx_volume).is_equal(-18.0)


func test_music_and_sfx_keep_independent_stored_values() -> void:
    var store := FakeSettingsStore.new(-9.0, -11.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    panel.set_music_slider(-2.5)
    assert_that(store.music_volume).is_equal(-2.5)
    assert_that(store.sfx_volume).is_equal(-11.0)

    panel.set_sfx_slider(-7.5)
    assert_that(store.music_volume).is_equal(-2.5)
    assert_that(store.sfx_volume).is_equal(-7.5)


func test_runtime_music_change_updates_only_music_channel_immediately() -> void:
    var store := FakeSettingsStore.new(-9.0, -14.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    panel.set_music_slider(-9.0)
    panel.set_sfx_slider(-14.0)
    panel.set_music_slider(-4.0)

    assert_that(audio.music_volume).is_equal(-4.0)
    assert_that(audio.sfx_volume).is_equal(-14.0)


func test_concurrent_playback_keeps_channels_independently_controllable() -> void:
    var store := FakeSettingsStore.new(-6.0, -12.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    panel.set_music_slider(-6.0)
    panel.set_sfx_slider(-12.0)
    panel.set_music_slider(-2.0)

    assert_that(audio.playback_active['music']).is_equal(true)
    assert_that(audio.playback_active['sfx']).is_equal(true)
    assert_that(audio.music_volume).is_equal(-2.0)
    assert_that(audio.sfx_volume).is_equal(-12.0)

    panel.set_sfx_slider(-8.0)

    assert_that(audio.music_volume).is_equal(-2.0)
    assert_that(audio.sfx_volume).is_equal(-8.0)


func test_rapid_slider_changes_do_not_interrupt_playback_or_raise_runtime_errors() -> void:
    var store := FakeSettingsStore.new(-5.0, -9.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    panel.set_music_slider(-5.0)
    panel.set_sfx_slider(-9.0)
    panel.apply_rapid_changes('music', [-4.0, -3.5, -6.0, -2.0, -5.0])
    panel.apply_rapid_changes('sfx', [-8.5, -7.0, -10.0, -6.5, -9.0])

    assert_that(audio.runtime_errors.size()).is_equal(0)
    assert_that(audio.interruptions['music']).is_equal(0)
    assert_that(audio.interruptions['sfx']).is_equal(0)
    assert_that(audio.playback_active['music']).is_equal(true)
    assert_that(audio.playback_active['sfx']).is_equal(true)


func test_ui_control_adjustment_changes_only_corresponding_audible_channel() -> void:
    var store := FakeSettingsStore.new(-10.0, -13.0)
    var audio := FakeAudioRuntime.new()
    var panel := SettingsPanelAudioHarness.new(store, audio)

    panel.set_sfx_slider(-13.0)
    panel.set_music_slider(-1.0)
    assert_that(audio.music_volume).is_equal(-1.0)
    assert_that(audio.sfx_volume).is_equal(-13.0)

    panel.set_sfx_slider(-7.0)
    assert_that(audio.music_volume).is_equal(-1.0)
    assert_that(audio.sfx_volume).is_equal(-7.0)
