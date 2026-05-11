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
	var pressure_label: Label = hud.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")
	var summary_label: Label = screen.get_node("Margin/VBox/Summary")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")

	assert_object(screen.get_node_or_null("CombatExperienceRuntimeBridge")).is_not_null()
	assert_bool(summary_label.text.length() > 0).is_true()
	assert_bool(status_label.text.length() > 0).is_true()
	assert_str(pressure_label.text).is_equal("Pressure: n/a")
	assert_str(outcome_label.text).is_equal("Outcome: n/a")
	assert_str(prompt_label.text).is_equal("Prompt: n/a")

	bridge.call("BuildPhase")
	bridge.call("TrainFriendlyUnitPhase")
	bridge.call("SpawnEnemyWavePhase")
	bridge.call("ResolveCombatExchangePhase")
	bridge.call("CleanupDeadUnitsPhase")
	bridge.call("PublishOutcomePhase")
	await _await_frames(2)

	assert_bool(summary_label.text.find("Castle HP:") >= 0).is_true()
	assert_bool(summary_label.text.find("Friendly Units:") >= 0).is_true()
	assert_bool(summary_label.text.find("Enemy Units Spawned:") >= 0).is_true()
	assert_bool(summary_label.text.find("Combat Exchanges:") >= 0).is_true()
	assert_bool(pressure_label.text.find("hp=") >= 0).is_true()
	assert_bool(pressure_label.text.find("stable") >= 0 or pressure_label.text.find("warning") >= 0 or pressure_label.text.find("danger") >= 0 or pressure_label.text.find("critical") >= 0).is_true()
	assert_bool(outcome_label.text.find("Outcome:") >= 0).is_true()
	assert_bool(prompt_label.text.find("Prompt:") >= 0).is_true()

	_publish("core.lastking.castle.hp_changed", {"Day": 9, "PreviousHp": 100, "CurrentHp": 42})
	await _await_frames(2)
	var hp_regex := RegEx.new()
	assert_int(hp_regex.compile("hp=([0-9]+)")).is_equal(OK)
	var hp_match := hp_regex.search(pressure_label.text)
	assert_object(hp_match).is_not_null()
	assert_str(hp_match.get_string(1)).is_equal("42")
	assert_bool(pressure_label.text.find("stable") >= 0 or pressure_label.text.find("warning") >= 0 or pressure_label.text.find("danger") >= 0 or pressure_label.text.find("critical") >= 0).is_true()

func test_bottom_bar_rejects_malformed_or_missing_required_displays() -> void:
	var hud := await _hud()
	var pressure_label: Label = hud.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	_publish("core.lastking.castle.hp_changed", {"Day": 9, "PreviousHp": 100, "CurrentHp": 55})
	await _await_frames(2)
	var before_pressure := pressure_label.text
	var before_prompt := prompt_label.text

	_publish("core.score.updated", {"value": 1})
	_publish("core.lastking.castle.hp_changed", {"Day": "bad", "CurrentHp": "bad"})
	await _await_frames(2)

	assert_str(pressure_label.text).is_equal(before_pressure)
	assert_str(prompt_label.text).is_equal(before_prompt)
