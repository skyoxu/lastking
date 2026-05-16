extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func test_main_scene_should_keep_battle_only_hud_and_debug_overlay_hidden_before_entering_battlemap() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame
	await get_tree().process_frame

	var hud := main.get_node_or_null("RuntimeUi/HUD") as Control
	var bottom_bar := main.get_node_or_null("RuntimeUi/HUD/CombatHud/BottomBar") as Control
	var feedback_layer := main.get_node_or_null("RuntimeUi/HUD/FeedbackLayer") as Control
	var main_menu := main.get_node_or_null("RuntimeUi/MainMenu") as Control
	var play_button := main.get_node_or_null("RuntimeUi/MainMenu/VBox/BtnPlay") as Button

	assert_object(hud).is_not_null()
	assert_object(bottom_bar).is_not_null()
	assert_object(feedback_layer).is_not_null()
	assert_object(main_menu).is_not_null()
	assert_object(play_button).is_not_null()

	assert_bool(hud.visible).is_false()
	assert_bool(bottom_bar.visible).is_false()
	assert_bool(feedback_layer.visible).is_false()
	assert_object(main.get_node_or_null("RuntimeUi/Overlays/DebugInspectorOverlay")).is_null()
	assert_bool(main_menu.visible).is_true()
	assert_bool(play_button.disabled).is_false()
