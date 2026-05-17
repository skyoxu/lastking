extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for i in range(count):
		await get_tree().process_frame

func _await_until_hidden(node: Control, max_frames: int) -> bool:
	for i in range(max_frames):
		if not node.visible:
			return true
		await get_tree().process_frame
	return not node.visible

func _advance_hud_cooldown(hud: Node, frames: int, delta := 1.0 / 60.0) -> void:
	for i in range(frames):
		hud.call("AdvanceUiFrameForTest", delta)
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

func _spawn_cues(screen: Control) -> Array[float]:
	var a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")
	return [a.color.a, b.color.a]

# ACC:T65.2
func test_action_availability_blocks_out_of_order_exchange_and_keeps_cues_weak() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = screen.get_node("BattleHud")
	var exchange_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/ExchangeAction")
	assert_object(exchange_icon).is_not_null()
	assert_float((exchange_icon as CanvasItem).modulate.a).is_equal(0.5)

# ACC:T65.2
func test_action_cooldown_pulse_decays_back_to_weak_state() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = screen.get_node("BattleHud")
	var wave_action := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var cooldown_mask := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction/CooldownMask")
	assert_object(wave_action).is_not_null()
	assert_object(cooldown_mask).is_not_null()

	_request_hud_action(screen, "wave")
	await _await_frames(2)
	assert_bool((cooldown_mask as Control).visible).is_true()
	assert_bool((wave_action as CanvasItem).modulate.a < 1.0).is_true()

	await get_tree().create_timer(2.2).timeout
	await _await_frames(2)
	assert_bool(await _await_until_hidden(cooldown_mask as Control, 60)).is_true()
	assert_bool((wave_action as CanvasItem).modulate.a >= 0.99).is_true()

# ACC:T65.2
func test_action_sequence_complete_flow_keeps_summary_machine_resolvable() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var hud: Node = screen.get_node("BattleHud")
	var status: Label = runtime["status"]

	var build_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction")
	var wave_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var exchange_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/ExchangeAction")
	var cleanup_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/CleanupAction")
	var finish_icon := hud.get_node_or_null("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction")

	assert_object(build_icon).is_not_null()
	assert_object(wave_icon).is_not_null()
	assert_object(exchange_icon).is_not_null()
	assert_object(cleanup_icon).is_not_null()
	assert_object(finish_icon).is_not_null()
	_request_hud_action(screen, "build")
	_request_hud_action(screen, "wave")
	_request_hud_action(screen, "exchange")
	_request_hud_action(screen, "cleanup")
	_request_hud_action(screen, "finish")
	await _await_frames(2)

	assert_bool(String(status.text).find("Battle finished") >= 0 or String(status.text).find("finished") >= 0).is_true()
	assert_bool((build_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((wave_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((exchange_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((cleanup_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((finish_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()

func test_blocked_battle_actions_should_not_leave_raw_english_runtime_prompts() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = screen.get_node("BattleHud")
	var settings_button: Button = hud.get_node("TopBar/HBox/SettingsButton")
	var local_prompt: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")

	settings_button.emit_signal("pressed")
	await _await_frames(1)
	_request_hud_action(screen, "wave")
	await _await_frames(2)

	assert_bool(String(local_prompt.text).find("Resolve settlement reward") < 0).is_true()
	assert_bool(String(local_prompt.text).find("Close terminal outcome") < 0).is_true()
	assert_bool(String(local_prompt.text).find("Terminal outcome is open") < 0).is_true()


func test_action_cards_should_expose_runtime_status_labels_for_each_phase() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = screen.get_node("BattleHud")

	var build_action := hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction") as Button
	var wave_action := hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction") as Button
	var exchange_action := hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/ExchangeAction") as Button
	var cleanup_action := hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/CleanupAction") as Button
	var finish_action := hud.get_node("CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction") as Button

	assert_bool(build_action.disabled).is_false()
	assert_bool(wave_action.disabled).is_false()
	assert_bool(exchange_action.disabled).is_true()
	assert_bool(cleanup_action.disabled).is_true()
	assert_bool(finish_action.disabled).is_true()
	assert_float(exchange_action.modulate.a).is_equal(0.5)
	assert_float(cleanup_action.modulate.a).is_equal(0.5)
	assert_float(finish_action.modulate.a).is_equal(0.5)

	_request_hud_action(screen, "wave")
	await _await_frames(2)
	assert_bool(wave_action.disabled).is_false()
	assert_bool(exchange_action.disabled).is_false()
	assert_bool(cleanup_action.disabled).is_false()
	assert_bool(finish_action.disabled).is_true()
	assert_bool(wave_action.modulate.a < 1.0).is_true()
	assert_float(exchange_action.modulate.a).is_equal(1.0)
	assert_float(cleanup_action.modulate.a).is_equal(1.0)

	_request_hud_action(screen, "exchange")
	await _await_frames(2)
	assert_bool(exchange_action.disabled).is_false()
	assert_bool(finish_action.disabled).is_false()
	assert_float(finish_action.modulate.a).is_equal(1.0)

	_request_hud_action(screen, "cleanup")
	await _await_frames(2)
	assert_bool(cleanup_action.disabled).is_false()
	assert_float(cleanup_action.modulate.a).is_equal(1.0)

	_request_hud_action(screen, "finish")
	await _await_frames(2)
	assert_bool(finish_action.disabled).is_false()
	assert_float(finish_action.modulate.a).is_equal(1.0)

