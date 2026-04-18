extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CFG_PATH := "user://settings.cfg"

func before() -> void:
    _clear_config()


func _clear_config() -> void:
    var dir := DirAccess.open("user://")
    if dir and dir.file_exists("settings.cfg"):
        dir.remove("settings.cfg")


func _load_main() -> Node:
    var main = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    get_tree().get_root().add_child(auto_free(main))
    await get_tree().process_frame
    return main


func _settings_panel(main: Node) -> Node:
    return main.get_node("SettingsPanel")


func _music_slider(panel: Node) -> HSlider:
    return panel.get_node("VBox/VolRow/VolSlider")


func _sfx_slider(panel: Node) -> HSlider:
    return panel.get_node("VBox/SfxRow/SfxSlider")


func _save_button(panel: Node) -> Button:
    return panel.get_node("VBox/Buttons/SaveBtn")


func _load_button(panel: Node) -> Button:
    return panel.get_node("VBox/Buttons/LoadBtn")


func _music_player(main: Node) -> AudioStreamPlayer:
    return main.get_node("AudioManager/MusicPlayer")


func _sfx_player(main: Node) -> AudioStreamPlayer:
    return main.get_node("AudioManager/SfxPlayer")


func _assert_close(actual: float, expected: float, eps: float = 0.01) -> void:
    assert_bool(absf(actual - expected) <= eps).is_true()


# ACC:T29.1
# ACC:T29.10
# ACC:T29.11
# ACC:T29.12
# ACC:T29.14
# ACC:T29.15
func test_real_scene_persists_audio_values_and_restores_them_on_next_launch() -> void:
    var main = await _load_main()
    var panel = _settings_panel(main)
    var music_slider = _music_slider(panel)
    var sfx_slider = _sfx_slider(panel)

    music_slider.value = 0.8
    sfx_slider.value = 0.3
    _save_button(panel).emit_signal("pressed")
    await get_tree().process_frame

    var cfg = ConfigFile.new()
    assert_int(cfg.load(CFG_PATH)).is_equal(0)
    assert_float(float(cfg.get_value("settings", "music_volume", 0.0))).is_equal(0.8)
    assert_float(float(cfg.get_value("settings", "sfx_volume", 0.0))).is_equal(0.3)

    main.queue_free()
    await get_tree().process_frame

    var relaunched = await _load_main()
    var relaunched_music_player = _music_player(relaunched)
    var relaunched_sfx_player = _sfx_player(relaunched)
    _assert_close(float(relaunched_music_player.volume_db), float(linear_to_db(0.8)))
    _assert_close(float(relaunched_sfx_player.volume_db), float(linear_to_db(0.3)))

    var relaunched_panel = _settings_panel(relaunched)
    _load_button(relaunched_panel).emit_signal("pressed")
    await get_tree().process_frame
    assert_float(float(_music_slider(relaunched_panel).value)).is_equal(0.8)
    assert_float(float(_sfx_slider(relaunched_panel).value)).is_equal(0.3)

# ACC:T29.6
# ACC:T29.9
func test_real_scene_saved_values_are_applied_before_new_input() -> void:
    var cfg = ConfigFile.new()
    cfg.set_value("settings", "music_volume", 0.7)
    cfg.set_value("settings", "sfx_volume", 0.2)
    assert_int(cfg.save(CFG_PATH)).is_equal(0)

    var main = await _load_main()
    var music_player = _music_player(main)
    var sfx_player = _sfx_player(main)
    _assert_close(float(music_player.volume_db), float(linear_to_db(0.7)))
    _assert_close(float(sfx_player.volume_db), float(linear_to_db(0.2)))

    var panel = _settings_panel(main)
    _load_button(panel).emit_signal("pressed")
    await get_tree().process_frame
    assert_float(float(_music_slider(panel).value)).is_equal(0.7)
    assert_float(float(_sfx_slider(panel).value)).is_equal(0.2)


func test_real_scene_exposes_two_audio_channels_and_runtime_changes_stay_independent() -> void:
    var main = await _load_main()
    var panel = _settings_panel(main)
    if panel.has_method("ShowPanel"):
        panel.ShowPanel()
    await get_tree().process_frame

    var music_slider = _music_slider(panel)
    var sfx_slider = _sfx_slider(panel)
    assert_object(music_slider).is_not_null()
    assert_object(sfx_slider).is_not_null()

    var music_player = _music_player(main)
    var sfx_player = _sfx_player(main)
    assert_object(music_player).is_not_null()
    assert_object(sfx_player).is_not_null()
    assert_bool(music_player != sfx_player).is_true()

    music_slider.value = 0.6
    sfx_slider.value = 0.4
    await get_tree().process_frame
    _assert_close(float(music_player.volume_db), float(linear_to_db(0.6)))
    _assert_close(float(sfx_player.volume_db), float(linear_to_db(0.4)))

    music_slider.value = 0.9
    await get_tree().process_frame
    _assert_close(float(music_player.volume_db), float(linear_to_db(0.9)))
    _assert_close(float(sfx_player.volume_db), float(linear_to_db(0.4)))

    sfx_slider.value = 0.2
    await get_tree().process_frame
    _assert_close(float(music_player.volume_db), float(linear_to_db(0.9)))
    _assert_close(float(sfx_player.volume_db), float(linear_to_db(0.2)))


# ACC:T29.2
# ACC:T29.3
func test_real_scene_audio_manager_and_players_exist_on_windows_baseline() -> void:
    var main = await _load_main()
    var music_player = _music_player(main)
    var sfx_player = _sfx_player(main)
    assert_object(music_player).is_not_null()
    assert_object(sfx_player).is_not_null()
    assert_bool(music_player != sfx_player).is_true()


# ACC:T29.4
# ACC:T29.5
# ACC:T29.7
# ACC:T29.8
# ACC:T29.13
func test_real_scene_music_and_sfx_updates_are_runtime_independent() -> void:
    var main = await _load_main()
    var panel = _settings_panel(main)
    if panel.has_method("ShowPanel"):
        panel.ShowPanel()
    await get_tree().process_frame

    var music_slider = _music_slider(panel)
    var sfx_slider = _sfx_slider(panel)
    var music_player = _music_player(main)
    var sfx_player = _sfx_player(main)

    music_slider.value = 0.5
    sfx_slider.value = 0.4
    await get_tree().process_frame

    var music_updates := [0.6, 0.7, 0.3, 0.9, 0.5]
    var sfx_updates := [0.2, 0.8, 0.1, 0.6, 0.4]
    for idx in range(music_updates.size()):
        music_slider.value = float(music_updates[idx])
        await get_tree().process_frame
        _assert_close(float(music_player.volume_db), float(linear_to_db(float(music_updates[idx]))))
        _assert_close(float(sfx_player.volume_db), float(linear_to_db(float(sfx_slider.value))))

        sfx_slider.value = float(sfx_updates[idx])
        await get_tree().process_frame
        _assert_close(float(music_player.volume_db), float(linear_to_db(float(music_slider.value))))
        _assert_close(float(sfx_player.volume_db), float(linear_to_db(float(sfx_updates[idx]))))

    assert_bool(main.is_inside_tree()).is_true()
