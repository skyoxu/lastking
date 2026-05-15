extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))

func _await_frames(count: int) -> void:
	for i in range(count):
		await get_tree().process_frame

func _main_runtime() -> Dictionary:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)
	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)
	var screen: Control = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	return {
		"main": main,
		"screen": screen,
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
		"status": screen.get_node("Margin/VBox/Status"),
		"summary": screen.get_node("Margin/VBox/Summary"),
	}

# ACC:T65.4
func test_preserves_responsibility_boundaries_without_ambiguous_split() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]

	assert_str(str(screen.get_node("Background").get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(screen.get_node("WaveTimer").get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(screen.get_node("Margin").get_meta("ownership_container"))).is_equal("legacy_prototype")
	assert_bool(status.visible).is_false()
	assert_bool(status.text.length() > 0).is_true()
	assert_bool(summary.visible).is_false()

# ACC:T65.6
func test_runtime_flow_is_traceable_through_auditable_events() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await _await_frames(2)
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	screen.get_node("Margin/VBox/Controls/BuildBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/WaveBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/ExchangeBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/CleanupBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/FinishBtn").emit_signal("pressed")
	await _await_frames(4)

	assert_bool(outcome_label.text.find("Outcome:") >= 0).is_true()
	assert_bool(prompt_label.text.find("Prompt:") >= 0).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()
	assert_bool((screen.get_node("Margin/VBox/Status") as Label).visible).is_false()
	assert_bool(String(screen.get_node("Margin/VBox/Status").text).length() > 0).is_true()

# ACC:T65.7
func test_runtime_refresh_keeps_semantics_stable_until_later_transition() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var summary: Label = runtime["summary"]

	bridge.call("BuildPhase")
	bridge.call("TrainFriendlyUnitPhase")
	bridge.call("SpawnEnemyWavePhase")
	bridge.call("ResolveCombatExchangePhase")
	bridge.call("CleanupDeadUnitsPhase")
	bridge.call("PublishOutcomePhase")
	await _await_frames(2)
	var before := summary.text

	bridge.call("PublishOutcomePhase")
	await _await_frames(2)
	assert_str(summary.text).is_equal(before)
	assert_bool(summary.visible).is_false()
	assert_bool((screen.get_node("Margin/VBox/Status") as Label).visible).is_false()
	assert_bool(screen.get_node("Margin/VBox/Status").text.length() > 0).is_true()

# ACC:T65.8
func test_integration_validates_bottom_bar_state_and_action_readability() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]

	assert_bool(summary.visible).is_false()
	assert_bool(status.visible).is_false()
	assert_bool(status.text.length() > 0).is_true()

	screen.get_node("Margin/VBox/Controls/WaveBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_bool(status.visible).is_false()
	assert_bool(status.text.to_lower().find("wave") >= 0).is_true()
	assert_bool(summary.visible).is_false()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()

# ACC:T65.5
# ACC:T65.10
func test_stateful_bottom_bar_sequence_keeps_operation_surface_and_hud_readable() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]
	var hud := preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await _await_frames(2)
	var pressure_label: Label = hud.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")
	var outcome_label: Label = hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")
	var prompt_label: Label = hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

	screen.get_node("Margin/VBox/Controls/BuildBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/WaveBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/ExchangeBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/CleanupBtn").emit_signal("pressed")
	screen.get_node("Margin/VBox/Controls/FinishBtn").emit_signal("pressed")
	await _await_frames(3)

	assert_bool(status.visible).is_false()
	assert_bool(String(status.text).to_lower().find("finish") >= 0).is_true()
	assert_bool(summary.visible).is_false()
	assert_bool(String(pressure_label.text).find("hp=") >= 0 or String(pressure_label.text).find("stable") >= 0 or String(pressure_label.text).find("warning") >= 0 or String(pressure_label.text).find("danger") >= 0 or String(pressure_label.text).find("critical") >= 0).is_true()
	assert_bool(String(outcome_label.text).find("Outcome:") >= 0).is_true()
	assert_bool(String(prompt_label.text).find("Prompt:") >= 0).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()

