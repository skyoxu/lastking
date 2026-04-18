extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CFG_PATH := "user://settings.cfg"

func _clear_config() -> void:
    var dir := DirAccess.open("user://")
    if dir and dir.file_exists("settings.cfg"):
        dir.remove("settings.cfg")

# ACC:T29.4
func test_settings_save_and_load_via_configfile() -> void:
    _clear_config()
    var packed = load("res://Game.Godot/Scenes/UI/SettingsPanel.tscn")
    if packed == null:
        push_warning("SKIP settings panel test: SettingsPanel.tscn not found")
        return
    var panel = packed.instantiate()
    add_child(auto_free(panel))
    await get_tree().process_frame

    # set values
    var music_slider = panel.get_node("VBox/VolRow/VolSlider")
    var sfx_slider = panel.get_node("VBox/SfxRow/SfxSlider")
    music_slider.value = 0.7
    sfx_slider.value = 0.3
    var gfx = panel.get_node("VBox/GraphicsRow/GraphicsOpt")
    if gfx.get_item_count() == 0:
        gfx.add_item("low"); gfx.add_item("medium"); gfx.add_item("high")
    gfx.select(2) # high
    var lang = panel.get_node("VBox/LangRow/LangOpt")
    if lang.get_item_count() == 0:
        lang.add_item("en"); lang.add_item("zh"); lang.add_item("ja")
    lang.select(1) # zh

    # save (to ConfigFile)
    panel.get_node("VBox/Buttons/SaveBtn").emit_signal("pressed")
    await get_tree().process_frame

    # reset in-memory selections
    music_slider.value = 0.0
    sfx_slider.value = 0.0
    gfx.select(0)
    lang.select(0)

    # load (from ConfigFile)
    panel.get_node("VBox/Buttons/LoadBtn").emit_signal("pressed")
    await get_tree().process_frame
    assert_float(float(music_slider.value)).is_equal(0.7)
    assert_float(float(sfx_slider.value)).is_equal(0.3)
    panel.queue_free()
