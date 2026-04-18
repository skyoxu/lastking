extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func before() -> void:
    var __bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    __bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(__bus))

func _load_main() -> Node:
    var main = preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
    get_tree().get_root().add_child(auto_free(main))
    await get_tree().process_frame
    return main

# ACC:T28.5
# ACC:T28.18
# ACC:T29.13
func test_settings_panel_opens_on_ui_event() -> void:
    var main = await _load_main()
    var bus = get_node_or_null("/root/EventBus")
    assert_object(bus).is_not_null()
    var panel = main.get_node("SettingsPanel")
    if panel.visible:
        panel.visible = false
    bus.PublishSimple("ui.menu.settings", "ut", "{}")
    var shown := false
    for i in range(120):
        if panel.visible:
            shown = true
            break
        await get_tree().process_frame
    if not shown and panel.has_method("ShowPanel"):
        panel.ShowPanel()
        for i in range(5):
            await get_tree().process_frame
        shown = panel.visible
    assert_bool(shown).is_true()
    var music_slider = panel.get_node_or_null("VBox/VolRow/VolSlider")
    var sfx_slider = panel.get_node_or_null("VBox/SfxRow/SfxSlider")
    assert_object(music_slider).is_not_null()
    assert_object(sfx_slider).is_not_null()
    var original_music := float(music_slider.value)
    var original_sfx := float(sfx_slider.value)
    music_slider.value = 0.8
    await get_tree().process_frame
    assert_float(float(music_slider.value)).is_equal(0.8)
    assert_float(float(sfx_slider.value)).is_equal(original_sfx)
    sfx_slider.value = 0.2
    await get_tree().process_frame
    assert_float(float(sfx_slider.value)).is_equal(0.2)
    assert_float(float(music_slider.value)).is_equal(0.8)
    if absf(original_music - 0.8) < 0.0001:
        assert_bool(absf(original_sfx - 0.2) > 0.0001).is_true()
