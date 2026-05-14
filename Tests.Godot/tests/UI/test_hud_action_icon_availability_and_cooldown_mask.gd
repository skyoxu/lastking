extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

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

func _spawn_cues(screen: Control) -> Array[float]:
	var a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")
	return [a.color.a, b.color.a]

# ACC:T65.2
func test_action_availability_blocks_out_of_order_exchange_and_keeps_cues_weak() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")

	var before_summary: Dictionary = bridge.call("GetSummary")
	var before_cues := _spawn_cues(screen)

	exchange_btn.emit_signal("pressed")
	await _await_frames(2)

	var after_summary: Dictionary = bridge.call("GetSummary")
	assert_bool(String(status.text).length() > 0).is_true()
	assert_int(int(after_summary.get("enemy_units_spawned", -1))).is_equal(int(before_summary.get("enemy_units_spawned", -1)))
	assert_int(int(after_summary.get("combat_exchanges", -1))).is_equal(int(before_summary.get("combat_exchanges", -1)))
	assert_float(_spawn_cues(screen)[0]).is_equal(before_cues[0])
	assert_float(_spawn_cues(screen)[1]).is_equal(before_cues[1])

# ACC:T65.2
func test_action_cooldown_pulse_decays_back_to_weak_state() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")

	var before_summary: Dictionary = bridge.call("GetSummary")
	wave_btn.emit_signal("pressed")
	await _await_frames(1)

	var pulse := _spawn_cues(screen)
	var after_summary: Dictionary = bridge.call("GetSummary")
	assert_bool(String(status.text).length() > 0).is_true()
	assert_int(int(after_summary.get("enemy_units_spawned", -1))).is_equal(int(before_summary.get("enemy_units_spawned", -1)) + 2)
	assert_bool(pulse[0] > 0.9 and pulse[1] > 0.9).is_true()

	await _await_frames(60)
	var weak := _spawn_cues(screen)
	assert_bool(weak[0] <= 0.6 and weak[1] <= 0.6).is_true()

# ACC:T65.2
func test_action_sequence_complete_flow_keeps_summary_machine_resolvable() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status: Label = runtime["status"]
	var summary: Label = runtime["summary"]

	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var summary_after: Dictionary = bridge.call("GetSummary")
	assert_bool(String(status.text).find("Battle finished") >= 0 or String(status.text).find("finished") >= 0).is_true()
	assert_bool(String(summary.text).find("BattleMap Runtime") >= 0).is_true()
	assert_bool(String(summary.text).find("Castle HP:") >= 0).is_true()
	assert_bool(String(summary.text).find("Friendly Units:") >= 0).is_true()
	assert_bool(String(summary.text).find("Enemy Units Spawned:") >= 0).is_true()
	assert_bool(String(summary.text).find("Combat Exchanges:") >= 0).is_true()
	assert_int(int(summary_after.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(summary_after.get("combat_exchanges", 0))).is_greater_equal(1)
