extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class BuggyAudioManagerRuntime:
    extends Node

    var music_player: AudioStreamPlayer
    var sfx_player: AudioStreamPlayer
    var music_volume_db: float = -6.0
    var sfx_volume_db: float = -6.0
    var runtime_errors: Array[String] = []
    var _music_playing: bool = false
    var _sfx_playing: bool = false

    func _init() -> void:
        music_player = AudioStreamPlayer.new()
        sfx_player = AudioStreamPlayer.new()
        add_child(music_player)
        add_child(sfx_player)
        music_player.volume_db = music_volume_db
        sfx_player.volume_db = sfx_volume_db

    func start_music_playback() -> void:
        _music_playing = true

    func play_sfx_once() -> void:
        _sfx_playing = true

    func set_music_volume_db(value: float) -> void:
        music_volume_db = value
        music_player.volume_db = value

    func set_sfx_volume_db(value: float) -> void:
        sfx_volume_db = value
        sfx_player.volume_db = value

    func is_music_playing() -> bool:
        return _music_playing

    func is_sfx_playing() -> bool:
        return _sfx_playing


func _new_manager() -> BuggyAudioManagerRuntime:
    return BuggyAudioManagerRuntime.new()


func _seed_independent_levels(manager: BuggyAudioManagerRuntime, music_db: float, sfx_db: float) -> void:
    manager.music_volume_db = music_db
    manager.sfx_volume_db = sfx_db
    manager.music_player.volume_db = music_db
    manager.sfx_player.volume_db = sfx_db


func test_changing_music_stored_value_must_not_mutate_sfx_stored_value() -> void:
    var manager := _new_manager()
    _seed_independent_levels(manager, -8.0, -14.0)

    manager.set_music_volume_db(-3.0)

    assert_that(manager.music_volume_db).is_equal(-3.0)
    assert_that(manager.sfx_volume_db).is_equal(-14.0)


func test_runtime_music_change_updates_only_music_loudness_without_restart() -> void:
    var manager := _new_manager()
    _seed_independent_levels(manager, -10.0, -18.0)

    manager.set_music_volume_db(-4.0)

    assert_that(manager.music_player.volume_db).is_equal(-4.0)
    assert_that(manager.sfx_player.volume_db).is_equal(-18.0)


func test_dedicated_runtime_component_does_not_disable_the_other_playback_path() -> void:
    var manager := _new_manager()

    manager.start_music_playback()
    manager.play_sfx_once()

    assert_that(manager.is_music_playing()).is_true()
    assert_that(manager.is_sfx_playing()).is_true()


func test_per_channel_volume_change_targets_only_the_matching_player_node() -> void:
    var manager := _new_manager()
    _seed_independent_levels(manager, -9.0, -21.0)

    manager.set_sfx_volume_db(-6.0)

    assert_that(manager.sfx_player.volume_db).is_equal(-6.0)
    assert_that(manager.music_player.volume_db).is_equal(-9.0)


func test_concurrent_playback_music_and_sfx_loudness_remain_independent_during_changes() -> void:
    var manager := _new_manager()
    _seed_independent_levels(manager, -11.0, -16.0)

    manager.set_music_volume_db(-2.0)
    assert_that(manager.music_player.volume_db).is_equal(-2.0)
    assert_that(manager.sfx_player.volume_db).is_equal(-16.0)

    manager.set_sfx_volume_db(-7.0)
    assert_that(manager.sfx_player.volume_db).is_equal(-7.0)
    assert_that(manager.music_player.volume_db).is_equal(-2.0)


func test_rapid_runtime_volume_updates_keep_playback_alive_and_error_free() -> void:
    var manager := _new_manager()
    manager.start_music_playback()
    manager.play_sfx_once()

    var music_updates: Array[float] = [-18.0, -12.0, -8.0, -5.0, -3.0, -9.0]
    var sfx_updates: Array[float] = [-20.0, -15.0, -10.0, -6.0, -4.0, -11.0]

    for i in range(music_updates.size()):
        manager.set_music_volume_db(music_updates[i])
        manager.set_sfx_volume_db(sfx_updates[i])

    assert_that(manager.runtime_errors.size()).is_equal(0)
    assert_that(manager.is_music_playing()).is_true()
    assert_that(manager.is_sfx_playing()).is_true()
