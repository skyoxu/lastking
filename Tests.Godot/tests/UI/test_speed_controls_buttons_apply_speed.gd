extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _ensure_game_manager() -> Node:
	var existing := get_node_or_null("/root/GameManager")
	if existing != null:
		return existing
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	manager.name = "GameManager"
	get_tree().root.add_child(auto_free(manager))
	return manager


# acceptance: ACC:T23.1 ACC:T23.9 ACC:T23.11
func test_speed_buttons_apply_pause_one_x_and_two_x_immediately() -> void:
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame

	var pause_button: Button = hud.get_node("TopBar/HBox/SpeedControls/PauseButton")
	var one_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/OneXButton")
	var two_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/TwoXButton")
	var speed_state_label: Label = hud.get_node("TopBar/HBox/SpeedStateLabel")

	assert_bool(pause_button.disabled).is_false()
	assert_bool(one_x_button.disabled).is_false()
	assert_bool(two_x_button.disabled).is_false()

	pause_button.emit_signal("pressed")
	await get_tree().process_frame
	var paused: Dictionary = manager.call("GetSpeedState")
	assert_int(int(paused["scale_percent"])).is_equal(0)
	assert_bool(paused["is_paused"] == true).is_true()
	assert_float(Engine.time_scale).is_equal(0.0)
	assert_float(pause_button.modulate.a).is_equal(1.0)
	assert_bool(speed_state_label.text.find(":") >= 0).is_true()
	assert_bool(speed_state_label.text.find("0") >= 0 or speed_state_label.text.find("停") >= 0 or speed_state_label.text.find("Pause") >= 0).is_true()

	one_x_button.emit_signal("pressed")
	await get_tree().process_frame
	var one_x: Dictionary = manager.call("GetSpeedState")
	assert_int(int(one_x["scale_percent"])).is_equal(100)
	assert_bool(one_x["is_paused"] == true).is_false()
	assert_float(Engine.time_scale).is_equal(1.0)
	assert_float(one_x_button.modulate.a).is_equal(1.0)
	assert_bool(speed_state_label.text.find("1") >= 0).is_true()

	two_x_button.emit_signal("pressed")
	await get_tree().process_frame
	var two_x: Dictionary = manager.call("GetSpeedState")
	assert_int(int(two_x["scale_percent"])).is_equal(200)
	assert_bool(two_x["is_paused"] == true).is_false()
	assert_float(Engine.time_scale).is_equal(2.0)
	assert_float(two_x_button.modulate.a).is_equal(1.0)
	assert_bool(speed_state_label.text.find("2") >= 0).is_true()


# acceptance: ACC:T23.11 ACC:T23.24
func test_hud_remains_interactive_while_paused_and_can_resume_speed() -> void:
	var manager := _ensure_game_manager()
	manager.call("ResetRuntimeForTest")
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame

	var pause_button: Button = hud.get_node("TopBar/HBox/SpeedControls/PauseButton")
	var two_x_button: Button = hud.get_node("TopBar/HBox/SpeedControls/TwoXButton")

	pause_button.emit_signal("pressed")
	await get_tree().process_frame
	assert_bool(get_tree().paused).is_true()
	assert_int(int(hud.process_mode)).is_equal(int(Node.PROCESS_MODE_ALWAYS))

	two_x_button.emit_signal("pressed")
	await get_tree().process_frame
	var resumed: Dictionary = manager.call("GetSpeedState")
	assert_bool(get_tree().paused).is_false()
	assert_bool(resumed["is_paused"] == true).is_false()
	assert_int(int(resumed["scale_percent"])).is_equal(200)
	assert_float(Engine.time_scale).is_equal(2.0)
