extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# ACC:T9.1
# ACC:T9.4
# ACC:T9.10
# ACC:T9.12
# ACC:T9.13
# ACC:T24.15
func test_hud_scene_instantiates() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    assert_bool(scene.visible).is_true()
    var day_label: Label = scene.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = scene.get_node("TopBar/HBox/HealthLabel")
    assert_str(day_label.text).is_not_empty()
    assert_str(cycle_label.text).is_not_empty()
    assert_str(hp_label.text).is_equal("HP: 0")

# ACC:T9.16
# ACC:T9.18
func test_hud_scene_exposes_expected_labels() -> void:
    var scene := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(scene))
    await get_tree().process_frame
    var day_label = scene.get_node("TopBar/HBox/DayLabel")
    var cycle_label = scene.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label = scene.get_node("TopBar/HBox/HealthLabel")
    assert_object(day_label).is_not_null()
    assert_object(cycle_label).is_not_null()
    assert_object(hp_label).is_not_null()
    assert_bool(scene.has_node("TopBar/HBox/ScoreLabel")).is_false()
    var hbox: HBoxContainer = scene.get_node("TopBar/HBox")
    var label_count := 0
    for child in hbox.get_children():
        if child is Label:
            label_count += 1
    assert_int(label_count).is_equal(3)

