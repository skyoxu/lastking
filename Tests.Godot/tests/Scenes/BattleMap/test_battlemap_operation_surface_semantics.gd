extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

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
		"presentation": screen.get_node("PresentationController"),
	}

# ACC:T65.3
# ACC:T65.9
func test_runtime_ui_semantics_mapping_for_empty_failure_completion() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	var summary_label: Label = runtime["summary"]
	var presentation_controller: Node = runtime["presentation"]
	var background: ColorRect = screen.get_node("Background")
	var finished_text := str(presentation_controller.call("translate", "battlemap.status.finished")).to_lower()

	assert_object(screen.get_node_or_null("CombatExperienceRuntimeBridge")).is_not_null()
	assert_object(screen.get_node_or_null("Background")).is_not_null()
	assert_object(screen.get_node_or_null("LegacyPrototypeRoot/VBox/Status")).is_not_null()
	assert_object(screen.get_node_or_null("LegacyPrototypeRoot/VBox/Summary")).is_not_null()
	assert_bool(screen.visible).is_true()
	assert_bool(background.visible).is_true()
	assert_bool(status_label.visible).is_false()
	assert_bool(String(status_label.text).length() > 0).is_true()
	assert_bool(summary_label.visible).is_false()

	_request_hud_action(screen, "build")
	_request_hud_action(screen, "wave")
	_request_hud_action(screen, "exchange")
	_request_hud_action(screen, "cleanup")
	_request_hud_action(screen, "finish")
	await _await_frames(2)
	var runtime_summary: Dictionary = bridge.call("GetSummary")
	assert_bool(summary_label.visible).is_false()
	assert_bool(runtime_summary.get("mg_tower_built", false) == false).is_true()
	assert_int(int(runtime_summary.get("combat_exchanges", 0))).is_greater_equal(1)
	assert_bool(status_label.text.to_lower().find(finished_text) >= 0).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()
	assert_bool(bridge.has_method("BuildPhase")).is_true()
	assert_bool(bridge.has_method("PublishOutcomePhase")).is_true()

func test_runtime_bridge_illegal_sequence_does_not_advance_to_completion() -> void:
	var runtime := await _main_runtime()
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	var summary_label: Label = runtime["summary"]

	var before_status := status_label.text
	var before_summary := summary_label.text
	var before_state: Dictionary = bridge.call("GetSummary")

	bridge.call("ResolveCombatExchangePhase")
	await _await_frames(2)

	var after_state: Dictionary = bridge.call("GetSummary")
	assert_str(status_label.text).is_equal(before_status)
	assert_str(summary_label.text).is_equal(before_summary)
	assert_int(int(after_state.get("combat_exchanges", -1))).is_equal(int(before_state.get("combat_exchanges", -1)))
	assert_int(int(after_state.get("enemy_units_spawned", -1))).is_equal(int(before_state.get("enemy_units_spawned", -1)))

# ACC:T65.4
func test_runtime_bridge_preserves_hud_navigator_runtime_ownership_boundaries() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var background: Node = screen.get_node("Background")
	var margin: Node = screen.get_node("LegacyPrototypeRoot")

	assert_str(str(background.get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(bridge.get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(margin.get_meta("ownership_container"))).is_equal("legacy_prototype")

# ACC:T65.9
func test_new_visible_node_paths_require_focused_scene_test_coverage() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var background := screen.get_node_or_null("Background")
	var title := screen.get_node_or_null("LegacyPrototypeRoot/VBox/Title")
	var wave_btn := screen.get_node_or_null("BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var operation_controller := screen.get_node_or_null("OperationController")
	var feedback_controller := screen.get_node_or_null("FeedbackController")
	var outcome_controller := screen.get_node_or_null("OutcomeController")
	var selection_controller := screen.get_node_or_null("SelectionController")

	assert_object(bridge).is_not_null()
	assert_object(background).is_not_null()
	assert_object(title).is_not_null()
	assert_object(wave_btn).is_not_null()
	assert_object(operation_controller).is_not_null()
	assert_object(feedback_controller).is_not_null()
	assert_object(outcome_controller).is_not_null()
	assert_object(selection_controller).is_not_null()
	assert_bool(background.visible).is_true()
	assert_bool(String((title as Label).text).length() > 0).is_true()
	assert_bool(bridge.has_method("BuildPhase")).is_true()
	assert_bool(bridge.has_method("GetSummary")).is_true()

# ACC:T63.5
# ACC:T63.6
# ACC:T63.7
# ACC:T63.8
func test_t63_local_feedback_surface_nodes_should_exist_under_battlefield_root_with_transient_defaults() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var local_feedback_layer := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer")
	var hit_flash_overlay := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/HitFlashOverlay")
	var wall_pressure_overlay := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallPressureOverlay")
	var local_prompt_panel := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel")
	var local_prompt_label := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")

	assert_object(local_feedback_layer).is_not_null()
	assert_object(hit_flash_overlay).is_not_null()
	assert_object(wall_pressure_overlay).is_not_null()
	assert_object(local_prompt_panel).is_not_null()
	assert_object(local_prompt_label).is_not_null()
	assert_bool((local_feedback_layer as CanvasItem).visible).is_true()
	assert_bool((hit_flash_overlay as Control).visible).is_false()
	assert_bool((wall_pressure_overlay as Control).visible).is_false()
	assert_bool((local_prompt_panel as Control).visible).is_false()
	assert_str(String((local_prompt_label as Label).text)).is_empty()

# ACC:T63.1
# ACC:T63.2
func test_t63_local_feedback_surfaces_should_light_up_inside_battlefield_for_wave_exchange_and_wall_pressure() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var hit_flash_overlay: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/HitFlashOverlay")
	var wall_pressure_overlay: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallPressureOverlay")
	var local_prompt_panel: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel")
	var local_prompt_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")
	var presentation_controller: Node = runtime["presentation"]
	var wave_prompt_text := str(presentation_controller.call("translate", "battlemap.prompt.wave_entered")).to_lower()
	var reinforce_prompt_text := str(presentation_controller.call("translate", "battlemap.prompt.reinforce_frontline")).to_lower()

	_request_hud_action(screen, "wave")
	await _await_frames(2)
	assert_bool(local_prompt_panel.visible).is_true()
	assert_bool(String(local_prompt_label.text).to_lower().find(wave_prompt_text) >= 0).is_true()

	_request_hud_action(screen, "exchange")
	await _await_frames(2)
	assert_bool(hit_flash_overlay.visible).is_true()

	bridge.call("ConfigureDurabilityForTest", 45, 10)
	screen.get_node("FeedbackController").call("render_summary", bridge.call("GetSummary"), "Wall under pressure")
	await _await_frames(2)
	assert_bool(wall_pressure_overlay.visible).is_true()
	assert_bool(local_prompt_panel.visible).is_true()
	assert_bool(String(local_prompt_label.text).to_lower().find(reinforce_prompt_text) >= 0).is_true()


# ACC:T66.5
func test_t66_feedback_ownership_should_keep_runtime_bridge_as_state_authority() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var summary_label: Label = runtime["summary"]
	var legend_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Legend")
	var metrics_help_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/MetricsHelp")
	var status_label: Label = runtime["status"]

	var before_summary: Dictionary = bridge.call("GetSummary")
	var before_status := String(status_label.text)
	summary_label.text = "LEGACY_TEXT_ONLY_OVERRIDE"
	legend_label.text = "LEGACY_LEGEND_ONLY_OVERRIDE"
	metrics_help_label.text = "LEGACY_METRICS_ONLY_OVERRIDE"
	await _await_frames(1)
	var after_legacy_override: Dictionary = bridge.call("GetSummary")
	assert_that(after_legacy_override).is_equal(before_summary)
	assert_str(String(status_label.text)).is_equal(before_status)

	_request_hud_action(screen, "wave")
	await _await_frames(1)
	var after_wave: Dictionary = bridge.call("GetSummary")
	assert_int(int(after_wave.get("enemy_units_spawned", 0))).is_greater_equal(int(before_summary.get("enemy_units_spawned", 0)) + 2)


# ACC:T66.7
# ACC:T66.8
# ACC:T66.9
func test_t66_state_transition_semantics_should_stay_bridge_driven_after_legacy_text_override() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	var summary_label: Label = runtime["summary"]
	var presentation_controller: Node = runtime["presentation"]
	var loaded_text := str(presentation_controller.call("translate", "battlemap.status.loaded")).to_lower()
	var require_cleanup_text := str(presentation_controller.call("translate", "battlemap.status.require_cleanup")).to_lower()
	var finished_text := str(presentation_controller.call("translate", "battlemap.status.finished")).to_lower()

	var empty_snapshot: Dictionary = bridge.call("GetSummary")
	var empty_status := String(status_label.text)
	assert_int(int(empty_snapshot.get("enemy_units_spawned", 0))).is_equal(0)
	assert_bool(empty_status.to_lower().find(loaded_text) >= 0).is_true()
	assert_bool(empty_status.to_lower().find(finished_text) < 0).is_true()

	summary_label.text = "LEGACY_OVERRIDE_SHOULD_NOT_DRIVE_STATE"
	await _await_frames(1)
	assert_that(bridge.call("GetSummary")).is_equal(empty_snapshot)
	assert_str(String(status_label.text)).is_equal(empty_status)

	# failure semantics: out-of-order finish should not enter completion.
	_request_hud_action(screen, "finish")
	await _await_frames(1)
	var failure_status := String(status_label.text)
	assert_bool(failure_status.to_lower().find(require_cleanup_text) >= 0).is_true()
	assert_bool(failure_status.to_lower().find(finished_text) < 0).is_true()

	# completion semantics: bridge-driven valid sequence should enter completion deterministically.
	_request_hud_action(screen, "wave")
	await _await_frames(1)
	_request_hud_action(screen, "exchange")
	await _await_frames(1)
	_request_hud_action(screen, "cleanup")
	await _await_frames(1)
	_request_hud_action(screen, "finish")
	await _await_frames(1)

	var completion_status := String(status_label.text)
	var completion_snapshot: Dictionary = bridge.call("GetSummary")
	assert_bool(completion_status.to_lower().find(finished_text) >= 0).is_true()
	assert_int(int(completion_snapshot.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(completion_snapshot.get("combat_exchanges", 0))).is_greater_equal(1)


# ACC:T67.9
# ACC:T70.9
func test_t67_daily_settlement_modal_node_paths_should_exist_and_default_hidden() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var modal := screen.get_node_or_null("DailySettlementModal")
	var rewards := screen.get_node_or_null("DailySettlementModal/VBox/Rewards")
	var reward_a := screen.get_node_or_null("DailySettlementModal/VBox/Rewards/RewardA")
	var reward_b := screen.get_node_or_null("DailySettlementModal/VBox/Rewards/RewardB")
	var reward_c := screen.get_node_or_null("DailySettlementModal/VBox/Rewards/RewardC")

	assert_object(modal).is_not_null()
	assert_object(rewards).is_not_null()
	assert_object(reward_a).is_not_null()
	assert_object(reward_b).is_not_null()
	assert_object(reward_c).is_not_null()
	assert_bool((modal as Control).visible).is_false()
	# T70.9: focused scene path must become visible/interactive after settlement trigger.
	var bridge: Node = runtime["bridge"]
	var status_label: Label = runtime["status"]
	assert_bool(bridge.has_method("ForceOutcomeForTest")).is_true()
	bridge.call("ForceOutcomeForTest", "settlement", 42)
	_request_hud_action(screen, "wave")
	await _await_frames(1)
	_request_hud_action(screen, "exchange")
	await _await_frames(1)
	_request_hud_action(screen, "cleanup")
	await _await_frames(1)
	_request_hud_action(screen, "finish")
	await _await_frames(2)
	assert_bool((modal as Control).visible).is_true()
	assert_bool((reward_a as Button).disabled).is_false()
	var summary_before_select: Dictionary = bridge.call("GetSummary")
	assert_str(String(summary_before_select.get("outcome", ""))).is_equal("settlement")
	(reward_a as Button).emit_signal("pressed")
	await _await_frames(1)
	assert_bool((modal as Control).visible).is_false()
	assert_bool(get_tree().paused).is_false()
	var settlement_resolved_text := str((runtime["presentation"] as Node).call("translate", "battlemap.status.settlement_resolved")).to_lower()
	assert_bool(String(status_label.text).to_lower().find(settlement_resolved_text) >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before_select)


# ACC:T68.1
# ACC:T68.4
# ACC:T68.6
# ACC:T68.9
func test_t68_victory_outcome_modal_node_paths_should_exist_and_default_hidden() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var modal := screen.get_node_or_null("VictoryOutcomeModal")
	var summary := screen.get_node_or_null("VictoryOutcomeModal/VBox/Summary")
	var hint := screen.get_node_or_null("VictoryOutcomeModal/VBox/Hint")
	var actions := screen.get_node_or_null("VictoryOutcomeModal/VBox/Actions")
	var return_btn := screen.get_node_or_null("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn")
	var restart_btn := screen.get_node_or_null("VictoryOutcomeModal/VBox/Actions/RestartBtn")

	assert_object(modal).is_not_null()
	assert_object(summary).is_not_null()
	assert_object(hint).is_not_null()
	assert_object(actions).is_not_null()
	assert_object(return_btn).is_not_null()
	assert_object(restart_btn).is_not_null()
	assert_bool((modal as Control).visible).is_false()
	assert_int((actions as VBoxContainer).get_child_count()).is_equal(2)


func test_battle_map_should_hide_player_castle_marker_and_place_enemy_spawns_on_both_sides() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var viewport: Control = screen.get_node("Background/BattlefieldViewport")
	var player_castle := screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/PlayerCastle")
	var spawn_a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var spawn_b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")

	assert_object(player_castle).is_null()
	assert_float(spawn_a.position.x).is_equal(0.0)
	assert_float(spawn_a.position.y).is_equal(0.0)
	assert_float(spawn_a.size.x).is_equal(48.0)
	assert_float(spawn_a.size.y).is_equal(viewport.size.y)
	assert_float(spawn_b.position.x).is_equal(viewport.size.x - 48.0)
	assert_float(spawn_b.position.y).is_equal(0.0)
	assert_float(spawn_b.size.x).is_equal(48.0)
	assert_float(spawn_b.size.y).is_equal(viewport.size.y)


func test_battle_map_debug_inspector_should_exist_and_report_scene_and_hovered_node() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var inspector := screen.get_node_or_null("DebugInspectorOverlay")
	var scene_label := screen.get_node_or_null("DebugInspectorOverlay/Panel/VBox/SceneLabel")
	var hover_label := screen.get_node_or_null("DebugInspectorOverlay/Panel/VBox/HoverLabel")
	var path_label := screen.get_node_or_null("DebugInspectorOverlay/Panel/VBox/PathLabel")
	var wave_btn: Button = screen.get_node("BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction")
	var hud_finish: CanvasItem = screen.get_node("BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction")

	assert_object(inspector).is_not_null()
	assert_object(scene_label).is_not_null()
	assert_object(hover_label).is_not_null()
	assert_object(path_label).is_not_null()
	assert_bool((inspector as Control).visible).is_true()
	assert_bool(String((scene_label as Label).text).find("BattleMapScreen") >= 0).is_true()
	assert_bool(inspector.has_method("DebugSetHoveredNode")).is_true()

	inspector.call("DebugSetHoveredNode", wave_btn)
	await _await_frames(1)
	assert_bool(String((hover_label as Label).text).find("WaveAction") >= 0).is_true()
	assert_bool(String((path_label as Label).text).find("BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction") >= 0).is_true()

	inspector.call("DebugSetHoveredNode", hud_finish)
	await _await_frames(1)
	assert_bool(String((hover_label as Label).text).find("FinishAction") >= 0).is_true()
	assert_bool(String((path_label as Label).text).find("BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/FinishAction") >= 0).is_true()


func test_battle_map_should_hide_legacy_summary_legend_and_metrics_help_from_player_view() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var summary_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Summary")
	var legend_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Legend")
	var metrics_help_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/MetricsHelp")

	assert_bool(summary_label.visible).is_false()
	assert_bool(legend_label.visible).is_false()
	assert_bool(metrics_help_label.visible).is_false()


func test_battle_map_should_hide_legacy_status_from_player_view() -> void:
	var runtime := await _main_runtime()
	var screen: Control = runtime["screen"]
	var status_label: Label = screen.get_node("LegacyPrototypeRoot/VBox/Status")

	assert_bool(status_label.visible).is_false()

