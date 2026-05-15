extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func before() -> void:
	var __bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	__bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(__bus))

func _hud() -> Node:
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud

func _publish(type: String, payload: Dictionary) -> void:
	var bus := get_node_or_null("/root/EventBus")
	assert_object(bus).is_not_null()
	bus.call("PublishSimple", type, "ut", JSON.stringify(payload))

func test_t64_pressure_summary_should_remain_four_state_summary_after_wave_spawn_updates() -> void:
	var hud := await _hud()
	var pressure_label: Label = hud.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")

	_publish("core.lastking.castle.hp_changed", {"Day": 14, "PreviousHp": 100, "CurrentHp": 35})
	await get_tree().process_frame
	var pressure_after_hp := pressure_label.text
	assert_bool(pressure_after_hp.to_lower().find("danger") >= 0).is_true()

	_publish("core.lastking.wave.spawned", {"day": 14, "count": 6})
	await get_tree().process_frame
	assert_bool(pressure_label.text.to_lower().find("danger") >= 0).is_true()
	assert_bool(pressure_label.text.find("spawned=") < 0).is_true()
