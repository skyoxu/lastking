extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"


func _ensure_game_manager() -> Node:
	var existing := get_node_or_null("/root/GameManager")
	if existing != null:
		return existing
	var manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	manager.name = "GameManager"
	get_tree().root.add_child(auto_free(manager))
	return manager


# acceptance: ACC:T23.21 ACC:T23.22 ACC:T23.23
func test_game_manager_singleton_applies_engine_time_scale_modes() -> void:
	var manager := _ensure_game_manager()
	await get_tree().process_frame

	manager.call("SetPause")
	assert_float(Engine.time_scale).is_equal(0.0)
	manager.call("SetOneX")
	assert_float(Engine.time_scale).is_equal(1.0)
	manager.call("SetTwoX")
	assert_float(Engine.time_scale).is_equal(2.0)
	assert_that(get_node_or_null("/root/GameManager")).is_not_null()


# acceptance: ACC:T23.21
func test_game_manager_singleton_rejects_duplicate_runtime_instance() -> void:
	var primary := _ensure_game_manager()
	await get_tree().process_frame
	assert_that(primary).is_not_null()

	var duplicate_manager := preload("res://Game.Godot/Scripts/Runtime/GameManager.cs").new()
	duplicate_manager.name = "GameManagerDuplicate"
	get_tree().root.add_child(auto_free(duplicate_manager))
	await get_tree().process_frame

	assert_that(get_node_or_null("/root/GameManager")).is_same(primary)
	assert_bool(is_instance_valid(duplicate_manager)).is_false()
