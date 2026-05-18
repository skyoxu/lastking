extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
	_bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
	_bus.name = "EventBus"
	get_tree().get_root().add_child(auto_free(_bus))

func _await_frames(count: int) -> void:
	for i in range(count):
		await get_tree().process_frame
func _request_hud_action(screen: Control, action_code: String) -> void:
	var hud: Node = screen.get_node("BattleHud")
	hud.call("RequestBattleAction", action_code)


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
		"status": screen.get_node("LegacyPrototypeRoot/VBox/Status"),
		"summary": screen.get_node("LegacyPrototypeRoot/VBox/Summary"),
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
	assert_str(str(screen.get_node("LegacyPrototypeRoot").get_meta("ownership_container"))).is_equal("legacy_prototype")
	assert_bool(status.visible).is_false()
	assert_bool(status.text.length() > 0).is_true()
	assert_bool(summary.visible).is_false()

# ACC:T65.6
func test_runtime_flow_is_traceable_through_auditable_events() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var battle_status: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/SummaryLabel")
	var battle_summary: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel")

	_request_hud_action(screen, "build")
	_request_hud_action(screen, "wave")
	_request_hud_action(screen, "exchange")
	_request_hud_action(screen, "cleanup")
	_request_hud_action(screen, "finish")
	await _await_frames(4)

	assert_bool(String(battle_status.text).length() > 0).is_true()
	assert_bool(String(battle_status.text).to_lower().find("finish") >= 0 or String(battle_status.text).to_lower().find("battle") >= 0).is_true()
	assert_bool(String(battle_summary.text).length() > 0).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()
	assert_bool((screen.get_node("LegacyPrototypeRoot/VBox/Status") as Label).visible).is_false()
	assert_bool(String(screen.get_node("LegacyPrototypeRoot/VBox/Status").text).length() > 0).is_true()

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
	assert_bool((screen.get_node("LegacyPrototypeRoot/VBox/Status") as Label).visible).is_false()
	assert_bool(screen.get_node("LegacyPrototypeRoot/VBox/Status").text.length() > 0).is_true()

# ACC:T65.8
func test_integration_validates_bottom_bar_state_and_action_readability() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]
	var battle_status: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/SummaryLabel")
	var battle_summary: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel")

	assert_bool(summary.visible).is_false()
	assert_bool(status.visible).is_false()
	assert_bool(status.text.length() > 0).is_true()

	_request_hud_action(screen, "wave")
	await _await_frames(2)
	assert_bool(status.visible).is_false()
	assert_bool(status.text.to_lower().find("wave") >= 0).is_true()
	assert_bool(summary.visible).is_false()
	assert_bool(String(battle_status.text).to_lower().find("wave") >= 0).is_true()
	assert_bool(String(battle_summary.text).find("HP=") >= 0 or String(battle_summary.text).find("Castle") >= 0).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()

# ACC:T65.5
# ACC:T65.10
func test_stateful_bottom_bar_sequence_keeps_operation_surface_and_hud_readable() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]
	var battle_status: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/SummaryLabel")
	var battle_summary: Label = screen.get_node("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel")
	var phase_label: Label = screen.get_node("BattleHud/TopBar/HBox/PhaseLabel")

	_request_hud_action(screen, "build")
	_request_hud_action(screen, "wave")
	_request_hud_action(screen, "exchange")
	_request_hud_action(screen, "cleanup")
	_request_hud_action(screen, "finish")
	await _await_frames(3)

	assert_bool(status.visible).is_false()
	assert_bool(String(status.text).to_lower().find("finish") >= 0).is_true()
	assert_bool(summary.visible).is_false()
	assert_bool(String(battle_status.text).length() > 0).is_true()
	assert_bool(String(battle_summary.text).length() > 0).is_true()
	assert_bool(String(phase_label.text).length() > 0).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()

