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

func _spawn_cues(screen: Control) -> Array[float]:
	var a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")
	return [a.color.a, b.color.a]

# ACC:T65.2
func test_action_availability_blocks_out_of_order_exchange_and_keeps_cues_weak() -> void:
	var runtime := await _main_runtime()
	var hud: Node = runtime["main"].get_node("RuntimeUi/HUD")
	var exchange_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/ExchangeAction")
	assert_object(exchange_icon).is_not_null()
	assert_float((exchange_icon as CanvasItem).modulate.a).is_equal(0.5)

# ACC:T65.2
func test_action_cooldown_pulse_decays_back_to_weak_state() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = runtime["main"].get_node("RuntimeUi/HUD")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var wave_action := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/WaveAction")
	var cooldown_mask := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/WaveAction/CooldownMask")
	assert_object(wave_action).is_not_null()
	assert_object(cooldown_mask).is_not_null()

	wave_btn.emit_signal("pressed")
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
	var hud: Node = runtime["main"].get_node("RuntimeUi/HUD")
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]

	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	var build_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/BuildAction")
	var wave_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/WaveAction")
	var exchange_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/ExchangeAction")
	var cleanup_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/CleanupAction")
	var finish_icon := hud.get_node_or_null("CombatHud/BottomBar/VBox/Actions/FinishAction")

	assert_object(build_icon).is_not_null()
	assert_object(wave_icon).is_not_null()
	assert_object(exchange_icon).is_not_null()
	assert_object(cleanup_icon).is_not_null()
	assert_object(finish_icon).is_not_null()
	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(String(status.text).find("Battle finished") >= 0 or String(status.text).find("finished") >= 0).is_true()
	assert_bool((build_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((wave_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((exchange_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((cleanup_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool((finish_icon as CanvasItem).modulate.a >= 0.99).is_true()
	assert_bool(bridge.call("GetSummary") is Dictionary).is_true()


func test_action_cards_should_expose_runtime_status_labels_for_each_phase() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var hud: Node = runtime["main"].get_node("RuntimeUi/HUD")

	var build_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/BuildAction/Frame/Content/Status")
	var wave_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/WaveAction/Frame/Content/Status")
	var exchange_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/ExchangeAction/Frame/Content/Status")
	var cleanup_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/CleanupAction/Frame/Content/Status")
	var finish_status: Label = hud.get_node("CombatHud/BottomBar/VBox/Actions/FinishAction/Frame/Content/Status")

	assert_str(build_status.text).contains("Ready")
	assert_str(wave_status.text).contains("Ready")
	assert_str(exchange_status.text).contains("Locked")
	assert_str(cleanup_status.text).contains("Locked")
	assert_str(finish_status.text).contains("Locked")

	screen.get_node("Margin/VBox/Controls/WaveBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(wave_status.text).contains("Cooldown")
	assert_str(exchange_status.text).contains("Ready")
	assert_str(cleanup_status.text).contains("Ready")

	screen.get_node("Margin/VBox/Controls/ExchangeBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(exchange_status.text).contains("Done")
	assert_str(finish_status.text).contains("Ready")

	screen.get_node("Margin/VBox/Controls/CleanupBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(cleanup_status.text).contains("Done")

	screen.get_node("Margin/VBox/Controls/FinishBtn").emit_signal("pressed")
	await _await_frames(2)
	assert_str(finish_status.text).contains("Done")

