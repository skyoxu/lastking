extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


const MAIN_SCENE_PATH := "res://Game.Godot/Scenes/Main.tscn"


# ACC:T55.2
# ACC:T55.6
func test_main_scene_should_mount_runtime_ui_under_canvas_layer() -> void:
	var main := preload(MAIN_SCENE_PATH).instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame

	var hud := main.get_node_or_null("RuntimeUi/HUD")
	var main_menu := main.get_node_or_null("RuntimeUi/MainMenu")
	var settings_panel := main.get_node_or_null("RuntimeUi/SettingsPanel")
	var overlays := main.get_node_or_null("RuntimeUi/Overlays")
	var screen_root := main.get_node_or_null("RuntimeUi/ScreenRoot")

	assert_object(main.get_node_or_null("RuntimeUi")).is_not_null()
	assert_object(hud).is_not_null()
	assert_object(main_menu).is_not_null()
	assert_object(settings_panel).is_not_null()
	assert_object(overlays).is_not_null()
	assert_object(screen_root).is_not_null()
