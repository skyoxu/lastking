extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func test_settings_panel_scene_instantiates() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/SettingsPanel.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    assert_bool(scene.is_inside_tree()).is_true()
    var save_panel = scene.get_node("VBox/SavePanel")
    var run_summary_panel = scene.get_node("VBox/RunSummaryPanel")
    assert_object(save_panel).is_not_null()
    assert_object(run_summary_panel).is_not_null()
    var autosave_path_label = scene.get_node("VBox/SavePanel/VBox/AutosavePathLabel")
    var summary_locale_label = scene.get_node("VBox/RunSummaryPanel/VBox/SummaryLocaleLabel")
    assert_str(str(autosave_path_label.text)).contains("user://autosave.save")
    assert_str(str(summary_locale_label.text)).contains("Locale:")
