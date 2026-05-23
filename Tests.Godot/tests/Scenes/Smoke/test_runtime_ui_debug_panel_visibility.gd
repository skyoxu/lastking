extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node
var _event_types: Array[String] = []


func before() -> void:
	_bus = get_tree().get_root().get_node_or_null("EventBus")
	if _bus == null:
		_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
		_bus.name = "EventBus"
		get_tree().get_root().add_child(auto_free(_bus))
	if not _bus.is_connected("DomainEventEmitted", Callable(self, "_on_evt")):
		_bus.connect("DomainEventEmitted", Callable(self, "_on_evt"))


func _on_evt(type, _source, _data_json, _id, _spec, _ct, _ts) -> void:
	_event_types.append(str(type))


func test_main_scene_should_hide_debug_runtime_panel_and_keep_menu_actions_clickable() -> void:
	_event_types.clear()
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await get_tree().process_frame

	var debug_panel := main.get_node_or_null("RuntimeUi/VBox") as Control
	var menu := main.get_node_or_null("RuntimeUi/MainMenu") as Control
	assert_object(debug_panel).is_not_null()
	assert_object(menu).is_not_null()
	assert_bool(debug_panel.visible).is_false()

	var play_button := menu.get_node_or_null("VBox/BtnPlay") as Button
	var settings_button := menu.get_node_or_null("VBox/BtnSettings") as Button
	assert_object(play_button).is_not_null()
	assert_object(settings_button).is_not_null()

	play_button.emit_signal("pressed")
	await get_tree().process_frame
	settings_button.emit_signal("pressed")
	await get_tree().process_frame

	assert_bool(_event_types.has("ui.menu.start")).is_true()
	assert_bool(_event_types.has("ui.menu.settings")).is_true()
