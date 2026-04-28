extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T41.11
func test_main_menu_scene_instantiates() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    assert_bool(scene.visible).is_true()
    assert_object(scene.get_node_or_null("BootStatusPanel")).is_not_null()
    assert_object(scene.get_node_or_null("BootStatusPanel/VBox/BootStatusLabel")).is_not_null()
    assert_object(scene.get_node_or_null("BootStatusPanel/VBox/ExportStatusLabel")).is_not_null()
    var continue_gate := scene.get_node_or_null("ContinueGateDialog")
    assert_object(continue_gate).is_not_null()
    assert_bool(bool(continue_gate.visible)).is_false()

