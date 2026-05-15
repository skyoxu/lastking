extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))

func _await_frames(count: int) -> void:
	for i in range(count):
		await get_tree().process_frame

func _hud() -> Node:
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud

func _screen() -> Control:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	return screen

func _publish(type_name: String, payload: Dictionary) -> void:
	_bus.PublishSimple(type_name, "ut", JSON.stringify(payload))

# ACC:T65.1
func test_bottom_bar_shows_combat_current_max_and_numeric_morale_placeholder_when_operation_surface_visible() -> void:
	var hud := await _hud()
	var screen := await _screen()
	var bottom_bar := hud.get_node_or_null("CombatHud/BottomBar")
	var counts_label := hud.get_node_or_null("CombatHud/BottomBar/VBox/CombatCountsLabel")
	var morale_label := hud.get_node_or_null("CombatHud/BottomBar/VBox/MoraleLabel")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")

	assert_object(bottom_bar).is_not_null()
	assert_object(counts_label).is_not_null()
	assert_object(morale_label).is_not_null()
	assert_object(screen.get_node_or_null("CombatExperienceRuntimeBridge")).is_not_null()
	assert_bool((bottom_bar as Control).visible).is_true()
	assert_bool(String((counts_label as Label).text).find("/") >= 0).is_true()
	assert_bool(String((morale_label as Label).text).find("Morale:") >= 0).is_true()

	bridge.call("BuildPhase")
	bridge.call("TrainFriendlyUnitPhase")
	bridge.call("SpawnEnemyWavePhase")
	bridge.call("ResolveCombatExchangePhase")
	bridge.call("CleanupDeadUnitsPhase")
	bridge.call("PublishOutcomePhase")
	await _await_frames(2)

	var counts_text := String((counts_label as Label).text)
	var morale_text := String((morale_label as Label).text)
	var counts_regex := RegEx.new()
	assert_int(counts_regex.compile("([0-9]+)/([0-9]+)")).is_equal(OK)
	assert_object(counts_regex.search(counts_text)).is_not_null()
	var morale_regex := RegEx.new()
	assert_int(morale_regex.compile("Morale: ([0-9]+)")).is_equal(OK)
	assert_object(morale_regex.search(morale_text)).is_not_null()

func test_bottom_bar_rejects_malformed_or_missing_required_displays() -> void:
	var hud := await _hud()
	var counts_label := hud.get_node_or_null("CombatHud/BottomBar/VBox/CombatCountsLabel")
	var morale_label := hud.get_node_or_null("CombatHud/BottomBar/VBox/MoraleLabel")

	assert_object(counts_label).is_not_null()
	assert_object(morale_label).is_not_null()
	_publish("core.lastking.castle.hp_changed", {"Day": 9, "PreviousHp": 100, "CurrentHp": 55})
	await _await_frames(2)
	var before_counts := String((counts_label as Label).text)
	var before_morale := String((morale_label as Label).text)

	_publish("core.score.updated", {"value": 1})
	_publish("core.lastking.castle.hp_changed", {"Day": "bad", "CurrentHp": "bad"})
	await _await_frames(2)

	assert_str(String((counts_label as Label).text)).is_equal(before_counts)
	assert_str(String((morale_label as Label).text)).is_equal(before_morale)
