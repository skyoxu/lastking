extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node

func before() -> void:
    _bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    _bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(_bus))

func _hud() -> Node:
    var hud = preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
    add_child(auto_free(hud))
    await get_tree().process_frame
    return hud

func _bridge() -> Node:
    var bridge = preload("res://Game.Godot/Scripts/Combat/CastleBattleEventBridge.cs").new()
    add_child(auto_free(bridge))
    await get_tree().process_frame
    return bridge

func _barracks_bridge() -> Node:
    var bridge = preload("res://Game.Godot/Scripts/Building/BarracksTrainingQueueBridge.cs").new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", 240, 120, 3)
    await get_tree().process_frame
    return bridge

func _enemy_ai_probe() -> Node:
    var packed_scene: PackedScene = load("res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn")
    var probe: Node = packed_scene.instantiate()
    add_child(auto_free(probe))
    await get_tree().process_frame
    return probe

func _publish(type_name: String, payload: Dictionary) -> void:
    _bus.PublishSimple(type_name, "ut", JSON.stringify(payload))

func _remaining_seconds(label_text: String) -> float:
    var cleaned := label_text.replace("Cycle Remaining:", "").replace("s", "").strip_edges()
    return float(cleaned)

func _feedback_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/FeedbackLabel")

func _pressure_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/PressurePanel/VBox/PressureLabel")

func _camera_status_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/CameraControlOverlay/VBox/CameraStatusLabel")

func _error_dialog(hud: Node) -> PanelContainer:
    return hud.get_node("FeedbackLayer/ErrorDialog")

func _error_message_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/ErrorDialog/VBox/ErrorMessageLabel")

func _dismiss_button(hud: Node) -> Button:
    return hud.get_node("FeedbackLayer/ErrorDialog/VBox/DismissButton")

func _outcome_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel")

func _runtime_prompt_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel")

func _resource_summary_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/ResourcePanel/VBox/ResourceSummaryLabel")

func _build_summary_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/BuildPanel/VBox/BuildSummaryLabel")

func _progression_summary_label(hud: Node) -> Label:
    return hud.get_node("FeedbackLayer/ProgressionPanel/VBox/ProgressionSummaryLabel")

# ACC:T43.3
func test_hud_updates_day_cycle_and_castle_hp_when_runtime_publishes_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    _publish("core.lastking.day.started", {"day": 3, "from": "Night", "to": "Day", "tick": 11})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal("Day: 3")

    var cycle_before = _remaining_seconds(cycle_label.text)
    for i in range(15):
        await get_tree().process_frame
    var cycle_after = _remaining_seconds(cycle_label.text)
    assert_float(cycle_after).is_less_equal(cycle_before)
    assert_float(cycle_after).is_greater_equal(0.0)
    assert_float(cycle_before).is_less_equal(240.0)

    bridge.call("StartBattle", 50, "run-9", 3, "castle")
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(50)
    assert_str(hp_label.text).is_equal("HP: 50")

    bridge.call("ResolveCastleAttack", 8)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(42)
    assert_str(hp_label.text).is_equal("HP: 42")

# ACC:T43.3
# ACC:T48.4
# ACC:T50.4
func test_hud_renders_runtime_combat_outcome_and_feedback_messages() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)

    _publish("core.run.state.transitioned", {"outcome": "win", "day": 15})
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Victory!") >= 0).is_true()
    assert_bool(feedback_label.text.find("day=15") >= 0).is_true()

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "camera_locked"
    })
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Action blocked") >= 0).is_true()
    assert_bool(feedback_label.text.find("camera_locked") >= 0).is_true()

# ACC:T43.3
func test_hud_keeps_feedback_state_stable_when_unrelated_events_are_published() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var error_dialog := _error_dialog(hud)

    _publish("core.run.state.transitioned", {"outcome": "win", "day": 20})
    await get_tree().process_frame
    var outcome_text := feedback_label.text
    assert_bool(feedback_label.visible).is_true()
    assert_bool(error_dialog.visible).is_false()

    _publish("core.score.updated", {"value": 123})
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_str(feedback_label.text).is_equal(outcome_text)
    assert_bool(error_dialog.visible).is_false()

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "camera_locked"
    })
    await get_tree().process_frame
    var feedback_text := feedback_label.text
    assert_bool(feedback_label.visible).is_true()
    assert_bool(error_dialog.visible).is_false()

    _publish("core.score.updated", {"value": 777})
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_str(feedback_label.text).is_equal(feedback_text)
    assert_bool(error_dialog.visible).is_false()

# ACC:T43.2
# ACC:T43.3
# ACC:T48.7
func test_hud_feedback_surfaces_stay_hidden_without_relevant_feedback_events() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var error_dialog := _error_dialog(hud)

    assert_bool(feedback_label.visible).is_false()
    assert_bool(error_dialog.visible).is_false()

# ACC:T43.5
func test_hud_maps_blocked_path_fallback_outcome_to_declared_feedback_surface() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var error_dialog := _error_dialog(hud)

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "target_path_blocked_fallback",
        "MessageKey": "ui.combat.target_path_blocked_fallback",
        "Details": "fallback_attack_executed"
    })
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("fallback_attack_executed") >= 0).is_true()
    var fallback_text := feedback_label.text

    _publish("core.score.updated", {"value": 123})
    _publish("core.lastking.castle.hp_changed", {"Day": 4, "PreviousHp": 100, "CurrentHp": 96})
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_str(feedback_label.text).is_equal(fallback_text)
    assert_bool(error_dialog.visible).is_false()

# ACC:T43.2
# ACC:T43.3
# ACC:T51.2
# ACC:T51.11
# ACC:T53.2
func test_hud_combat_pressure_and_camera_overlay_exist_and_update_from_runtime_events() -> void:
    var hud = await _hud()
    var pressure_label := _pressure_label(hud)
    var camera_label := _camera_status_label(hud)

    assert_bool(hud.has_node("CombatHud")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/PressurePanel")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/CameraControlOverlay")).is_true()
    assert_str(pressure_label.text).is_equal("Pressure: n/a")
    assert_str(camera_label.text).is_equal("Camera: idle")

    _publish("core.lastking.castle.hp_changed", {"Day": 7, "PreviousHp": 100, "CurrentHp": 42})
    await get_tree().process_frame
    assert_bool(pressure_label.text.find("Pressure: high") >= 0).is_true()
    assert_bool(pressure_label.text.find("hp=42") >= 0).is_true()

    _publish("core.lastking.wave.spawned", {"day": 7, "count": 5})
    await get_tree().process_frame
    assert_str(pressure_label.text).is_equal("Pressure: day=7 spawned=5")

    _publish("core.lastking.camera.scrolled", {"dx": 6, "dy": -2})
    await get_tree().process_frame
    assert_str(camera_label.text).is_equal("Camera: dx=6 dy=-2")

# ACC:T43.5
# ACC:T48.8
# ACC:T50.7
# ACC:T51.5
# ACC:T51.7
# ACC:T51.10
func test_hud_surfaces_render_targeting_and_blocked_pathing_feedback_from_runtime_decision_chain() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var pressure_label := _pressure_label(hud)
    var camera_label := _camera_status_label(hud)
    var probe := await _enemy_ai_probe()

    var candidates := [
        {
            "id": "hero_blocked",
            "class": "unit",
            "reachable": false,
            "blocked": true,
            "path_points": 0,
            "distance": 1,
            "blocks_route_to_higher_priority": false
        },
        {
            "id": "barricade_1",
            "class": "blocking_structure",
            "reachable": false,
            "blocked": true,
            "path_points": 0,
            "distance": 1,
            "blocks_route_to_higher_priority": true
        }
    ]
    var decision: Dictionary = probe.call("SelectTarget", candidates)
    assert_bool(bool(decision.get("is_fallback_attack", false))).is_true()
    assert_str(str(decision.get("attack_event_target_id", ""))).is_equal("barricade_1")

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "target_path_blocked_fallback",
        "MessageKey": "ui.combat.target_path_blocked_fallback",
        "Details": "target=barricade_1"
    })
    _publish("core.lastking.wave.spawned", {"day": 8, "count": 3})
    _publish("core.lastking.camera.scrolled", {"dx": 4, "dy": -1})
    await get_tree().process_frame

    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("target=barricade_1") >= 0).is_true()
    assert_bool(pressure_label.text.find("spawned=3") >= 0).is_true()
    assert_str(camera_label.text).is_equal("Camera: dx=4 dy=-1")

# ACC:T43.3
func test_pressure_and_camera_surfaces_do_not_drift_on_unrelated_or_incomplete_events() -> void:
    var hud = await _hud()
    var pressure_label := _pressure_label(hud)
    var camera_label := _camera_status_label(hud)

    _publish("core.lastking.wave.spawned", {"day": 9, "count": 4})
    _publish("core.lastking.camera.scrolled", {"dx": 5, "dy": 2})
    await get_tree().process_frame
    var pressure_before := pressure_label.text
    var camera_before := camera_label.text

    _publish("core.score.updated", {"value": 404})
    _publish("core.lastking.camera.scrolled", {"dx": 11})
    _publish("core.lastking.wave.spawned", {"day": 9})
    await get_tree().process_frame

    assert_str(pressure_label.text).is_equal(pressure_before)
    assert_str(camera_label.text).is_equal(camera_before)

# ACC:T43.2
# ACC:T43.3
func test_hud_owned_surfaces_keep_identity_and_text_when_malformed_payloads_arrive() -> void:
    var hud = await _hud()
    var pressure_label := _pressure_label(hud)
    var camera_label := _camera_status_label(hud)
    var pressure_panel: PanelContainer = hud.get_node("FeedbackLayer/PressurePanel")
    var camera_overlay: PanelContainer = hud.get_node("FeedbackLayer/CameraControlOverlay")

    _publish("core.lastking.wave.spawned", {"day": 10, "count": 2})
    _publish("core.lastking.camera.scrolled", {"dx": 3, "dy": -1})
    await get_tree().process_frame

    var pressure_before := pressure_label.text
    var camera_before := camera_label.text
    var pressure_panel_path := str(pressure_panel.get_path())
    var camera_overlay_path := str(camera_overlay.get_path())

    _publish("core.lastking.wave.spawned", {"day": 10})
    _publish("core.lastking.wave.spawned", {"day": "bad", "count": "bad"})
    _publish("core.lastking.camera.scrolled", {"dx": "bad", "dy": 2})
    _publish("core.lastking.camera.scrolled", {"dx": 5, "dy": "bad"})
    await get_tree().process_frame

    assert_bool(hud.has_node("FeedbackLayer/PressurePanel")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/CameraControlOverlay")).is_true()
    assert_str(str(hud.get_node("FeedbackLayer/PressurePanel").get_path())).is_equal(pressure_panel_path)
    assert_str(str(hud.get_node("FeedbackLayer/CameraControlOverlay").get_path())).is_equal(camera_overlay_path)
    assert_str(pressure_label.text).is_equal(pressure_before)
    assert_str(camera_label.text).is_equal(camera_before)

# ACC:T43.3
func test_hud_failure_events_show_feedback_without_mutating_non_target_runtime_surfaces() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var pressure_label := _pressure_label(hud)
    var camera_label := _camera_status_label(hud)
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    _publish("core.lastking.day.started", {"day": 12, "from": "Night", "to": "Day", "tick": 2})
    _publish("core.lastking.castle.hp_changed", {"Day": 12, "PreviousHp": 100, "CurrentHp": 77})
    _publish("core.lastking.wave.spawned", {"day": 12, "count": 4})
    _publish("core.lastking.camera.scrolled", {"dx": 8, "dy": -4})
    await get_tree().process_frame

    var day_before := day_label.text
    var hp_before := hp_label.text
    var pressure_before := pressure_label.text
    var camera_before := camera_label.text

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "target_path_blocked_fallback",
        "MessageKey": "ui.combat.target_path_blocked_fallback",
        "Details": "route_obstructed"
    })
    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "chapter_locked"
    })
    _publish("core.lastking.ui_feedback.raised", {
        "Code": "invalid_payload",
        "MessageKey": "ui.error.unknown",
        "Details": "malformed_event"
    })
    await get_tree().process_frame

    var error_dialog := _error_dialog(hud)
    assert_bool(feedback_label.visible or error_dialog.visible).is_true()
    if feedback_label.visible:
        assert_bool(feedback_label.text.find("Victory!") == -1).is_true()
    assert_str(day_label.text).is_equal(day_before)
    assert_str(hp_label.text).is_equal(hp_before)
    assert_str(pressure_label.text).is_equal(pressure_before)
    assert_str(camera_label.text).is_equal(camera_before)

# ACC:T51.7
func test_hud_feedback_only_events_do_not_create_pressure_differentiation_without_core_pressure_payload() -> void:
    var hud = await _hud()
    var pressure_label := _pressure_label(hud)
    assert_str(pressure_label.text).is_equal("Pressure: n/a")

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "target_path_blocked_fallback",
        "MessageKey": "ui.combat.target_path_blocked_fallback",
        "Details": "elite_hint_only"
    })
    _publish("core.lastking.ui_feedback.raised", {
        "Code": "target_path_blocked_fallback",
        "MessageKey": "ui.combat.target_path_blocked_fallback",
        "Details": "boss_hint_only"
    })
    await get_tree().process_frame

    assert_str(pressure_label.text).is_equal("Pressure: n/a")

    _publish("core.lastking.wave.spawned", {"day": 11, "count": 6})
    await get_tree().process_frame
    assert_str(pressure_label.text).is_equal("Pressure: day=11 spawned=6")

# ACC:T9.2
# ACC:T9.5
# ACC:T9.6
# ACC:T9.8
# ACC:T9.9
# ACC:T7.1
# ACC:T7.9
# ACC:T7.15
# ACC:T42.1
# ACC:T42.2
# ACC:T42.3
# ACC:T42.4
# ACC:T42.6
func test_hud_acceptance_anchor_binding_for_t9_refs() -> void:
    var hud = await _hud()
    var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")
    assert_bool(hud.has_node("FeedbackLayer")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/FeedbackLabel")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/ErrorDialog")).is_true()
    assert_bool(hud.has_node("FeedbackLayer/ErrorDialog/VBox/DismissButton")).is_true()

    _publish("core.lastking.day.started", {"day": 20, "from": "Night", "to": "Day", "tick": 1})
    _publish("core.lastking.castle.hp_changed", {"Day": 15, "PreviousHp": 100, "CurrentHp": 66})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal("Day: 15")
    assert_str(hp_label.text).is_equal("HP: 66")

    hud.call("SetCycleRemainingSeconds", 88.0)
    await get_tree().process_frame
    var day_before = day_label.text
    var cycle_before = cycle_label.text
    var hp_before = hp_label.text

    _publish("core.score.updated", {"value": 999})
    await get_tree().process_frame
    assert_str(day_label.text).is_equal(day_before)
    assert_str(cycle_label.text).is_equal(cycle_before)
    assert_str(hp_label.text).is_equal(hp_before)

# ACC:T9.14
# ACC:T9.17
# ACC:T9.20
# ACC:T7.12
func test_hud_tracks_latest_castle_hp_across_runtime_published_events_and_ignores_non_castle_events() -> void:
    var hud = await _hud()
    var bridge = await _bridge()
    var hp_label: Label = hud.get_node("TopBar/HBox/HealthLabel")

    bridge.call("StartBattle", 50, "run-9", 2, "castle")
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(50)
    assert_str(hp_label.text).is_equal("HP: 50")

    bridge.call("ResolveCastleAttack", 3)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(47)
    assert_str(hp_label.text).is_equal("HP: 47")

    bridge.call("ResolveCastleAttack", 8)
    await get_tree().process_frame
    assert_int(int(bridge.GetCurrentHp())).is_equal(39)
    assert_str(hp_label.text).is_equal("HP: 39")

    _publish("core.score.updated", {"value": 99})
    await get_tree().process_frame
    assert_str(hp_label.text).is_equal("HP: 39")

# ACC:T9.21
func test_cycle_remaining_is_monotonic_and_bounded_within_each_phase() -> void:
    var hud = await _hud()
    var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")

    _publish("core.lastking.day.started", {"day": 6, "from": "Night", "to": "Day", "tick": 31})
    await get_tree().process_frame
    var day_first = _remaining_seconds(cycle_label.text)
    for i in range(20):
        await get_tree().process_frame
    var day_second = _remaining_seconds(cycle_label.text)
    assert_float(day_first).is_less(240.1)
    assert_float(day_first).is_greater_equal(0.0)
    assert_float(day_second).is_less_equal(day_first)
    assert_float(day_second).is_greater_equal(0.0)

    _publish("core.lastking.night.started", {"day": 6, "from": "Day", "to": "Night", "tick": 32})
    await get_tree().process_frame
    var night_first = _remaining_seconds(cycle_label.text)
    for i in range(20):
        await get_tree().process_frame
    var night_second = _remaining_seconds(cycle_label.text)
    assert_float(night_first).is_less(120.1)
    assert_float(night_first).is_greater_equal(0.0)
    assert_float(night_second).is_less_equal(night_first)
    assert_float(night_second).is_greater_equal(0.0)

# ACC:T24.8
# ACC:T24.19
# ACC:T24.20
func test_hud_feedback_runtime_priority_and_dedup_stability_for_task24_events() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var error_dialog := _error_dialog(hud)
    var error_label := _error_message_label(hud)
    var dismiss_btn := _dismiss_button(hud)

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "tile_occupied",
        "MessageKey": "ui.invalid_action.tile_occupied",
        "Severity": "warning",
        "Details": "tile=(2,3)"
    })
    await get_tree().process_frame
    var first_text := feedback_label.text
    assert_bool(feedback_label.visible).is_true()
    assert_bool(first_text.find("Invalid action") >= 0).is_true()
    assert_bool(first_text.find("tile_occupied") == -1).is_true()

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "tile_occupied",
        "MessageKey": "ui.invalid_action.tile_occupied",
        "Severity": "warning",
        "Details": "tile=(2,3)"
    })
    await get_tree().process_frame
    assert_str(feedback_label.text).is_equal(first_text)

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Severity": "warning",
        "Details": "chapter_locked"
    })
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Action blocked") >= 0).is_true()
    assert_bool(feedback_label.text.find("chapter_locked") >= 0).is_true()

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "missing_required_field",
        "MessageKey": "ui.migration_failure.missing_required_field",
        "Severity": "error",
        "Details": "slot=slot_a"
    })
    await get_tree().process_frame
    assert_bool(error_dialog.visible).is_true()
    assert_bool(feedback_label.visible).is_false()
    assert_bool(error_label.text.find("Migration failed") >= 0).is_true()
    assert_bool(error_label.text.find("slot=slot_a") >= 0).is_true()

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Severity": "warning",
        "Details": "chapter_locked"
    })
    await get_tree().process_frame
    assert_bool(error_dialog.visible).is_true()

    dismiss_btn.emit_signal("pressed")
    await get_tree().process_frame
    assert_bool(error_dialog.visible).is_false()

# ACC:T42.2
# ACC:T42.3
# ACC:T42.4
# ACC:T53.1
# ACC:T53.3
# ACC:T53.7
func test_hud_outcome_and_runtime_prompt_surfaces_update_from_runtime_events() -> void:
    var hud = await _hud()
    var outcome_label := _outcome_label(hud)
    var prompt_label := _runtime_prompt_label(hud)

    assert_str(outcome_label.text).is_equal("Outcome: n/a")
    assert_str(prompt_label.text).is_equal("Prompt: n/a")

    _publish("core.run.state.transitioned", {"outcome": "win", "day": 15})
    await get_tree().process_frame
    assert_str(outcome_label.text).is_equal("Outcome: win day=15")

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "chapter_locked"
    })
    await get_tree().process_frame
    assert_bool(prompt_label.text.find("Prompt: Action blocked.") >= 0).is_true()
    assert_bool(prompt_label.text.find("chapter_locked") >= 0).is_true()

# ACC:T53.7
func test_hud_after_action_surfaces_clear_stale_summary_and_prompt_for_new_non_terminal_state() -> void:
    var hud = await _hud()
    var outcome_label := _outcome_label(hud)
    var prompt_label := _runtime_prompt_label(hud)

    _publish("core.run.state.transitioned", {"outcome": "win", "day": 15})
    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "chapter_locked"
    })
    await get_tree().process_frame
    assert_str(outcome_label.text).is_equal("Outcome: win day=15")
    assert_bool(prompt_label.text.find("chapter_locked") >= 0).is_true()

    _publish("core.run.state.transitioned", {"outcome": "NONE", "day": 1})
    _publish("core.score.updated", {"value": 1})
    await get_tree().process_frame

    assert_str(outcome_label.text).is_equal("Outcome: n/a")
    assert_str(prompt_label.text).is_equal("Prompt: n/a")

# ACC:T44.1
# ACC:T44.2
# ACC:T44.3
# ACC:T44.4
# ACC:T49.5
# ACC:T53.5
func test_hud_barracks_deployment_outcome_feedback_tracks_success_and_failure_with_active_presence() -> void:
    var hud = await _hud()
    var feedback_label := _feedback_label(hud)
    var barracks = await _barracks_bridge()

    barracks.call("EnqueueUpfront", "spearman", 1, 20, 5)
    var success_done: Dictionary = barracks.call("Tick", 1)
    assert_array(success_done.get("completed_units", [])).is_equal(["spearman"])
    assert_array(success_done.get("failed_deployments", [])).is_empty()
    assert_int(int(barracks.call("GetActiveBattleUnitCountForTest"))).is_equal(1)

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_win",
        "MessageKey": "ui.run.win.day15",
        "Details": "barracks_deploy_success active_units=1"
    })
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Victory!") >= 0).is_true()
    assert_bool(feedback_label.text.find("active_units=1") >= 0).is_true()

    var active_before_failed := int(barracks.call("GetActiveBattleUnitCountForTest"))
    barracks.call("EnqueueUpfront", "spearman", 1, 20, 5)
    barracks.call("SetNextDeploymentOwnershipInvalidForTest", true)
    var failed_done: Dictionary = barracks.call("Tick", 1)
    assert_array(failed_done.get("completed_units", [])).is_empty()
    assert_array(failed_done.get("failed_deployments", [])).is_equal(["spearman"])
    assert_int(int(barracks.call("GetActiveBattleUnitCountForTest"))).is_equal(active_before_failed)

    _publish("core.lastking.ui_feedback.raised", {
        "Code": "run_continue_blocked",
        "MessageKey": "ui.blocked_action.run_continue_blocked",
        "Details": "barracks_deploy_failed active_units=%d failed=spearman" % active_before_failed
    })
    await get_tree().process_frame
    assert_bool(feedback_label.visible).is_true()
    assert_bool(feedback_label.text.find("Action blocked") >= 0).is_true()
    assert_bool(feedback_label.text.find("failed=spearman") >= 0).is_true()

# ACC:T53.2
# ACC:T53.4
# ACC:T53.8
func test_hud_economy_build_and_progression_surfaces_update_from_domain_events() -> void:
    var hud = await _hud()
    var resource_label := _resource_summary_label(hud)
    var build_label := _build_summary_label(hud)
    var progression_label := _progression_summary_label(hud)

    _publish("core.lastking.resources.changed", {
        "RunId": "run-44",
        "DayNumber": 9,
        "Gold": 120,
        "Iron": 44,
        "PopulationCap": 26
    })
    await get_tree().process_frame
    assert_str(resource_label.text).is_equal("Resources: gold=120 iron=44 pop=26")

    _publish("core.lastking.tax.collected", {
        "RunId": "run-44",
        "DayNumber": 9,
        "ResidenceId": "res-1",
        "GoldDelta": 15,
        "TotalGold": 135
    })
    await get_tree().process_frame
    assert_bool(build_label.text.find("Build: tax=15 total_gold=135") >= 0).is_true()
    assert_bool(build_label.text.find("residence=res-1") >= 0).is_true()

    _publish("core.lastking.tech.applied", {
        "RunId": "run-44",
        "TechId": "tech_rate_i",
        "StatKey": "attack_speed",
        "PreviousValue": 100,
        "CurrentValue": 110
    })
    await get_tree().process_frame
    assert_bool(progression_label.text.find("Progression: tech=tech_rate_i:attack_speed 100->110") >= 0).is_true()

    _publish("core.lastking.reward.offered", {
        "option_a": "gold+100",
        "option_b": "tech+1",
        "option_c": "unit+tank"
    })
    await get_tree().process_frame
    assert_bool(progression_label.text.find("reward=") >= 0).is_true()

# ACC:T53.4
func test_hud_after_action_labels_are_deterministic_for_identical_terminal_inputs() -> void:
    var hud = await _hud()
    var outcome_label := _outcome_label(hud)
    var prompt_label := _runtime_prompt_label(hud)

    var hp_payload := {"Day": 9, "PreviousHp": 100, "CurrentHp": 42}
    var wave_payload := {"day": 9, "count": 6}
    var resources_payload := {
        "RunId": "run-53-deterministic",
        "DayNumber": 9,
        "Gold": 120,
        "Iron": 44,
        "PopulationCap": 26
    }
    var outcome_payload := {"outcome": "win", "day": 9}

    _publish("core.lastking.castle.hp_changed", hp_payload)
    _publish("core.lastking.wave.spawned", wave_payload)
    _publish("core.lastking.resources.changed", resources_payload)
    _publish("core.run.state.transitioned", outcome_payload)
    await get_tree().process_frame
    var first_outcome := outcome_label.text
    var first_prompt := prompt_label.text

    _publish("core.run.state.transitioned", {"outcome": "NONE", "day": 1})
    await get_tree().process_frame
    assert_str(outcome_label.text).is_equal("Outcome: n/a")
    assert_str(prompt_label.text).is_equal("Prompt: n/a")

    _publish("core.lastking.castle.hp_changed", hp_payload)
    _publish("core.lastking.wave.spawned", wave_payload)
    _publish("core.lastking.resources.changed", resources_payload)
    _publish("core.run.state.transitioned", outcome_payload)
    await get_tree().process_frame

    assert_str(outcome_label.text).is_equal(first_outcome)
    assert_str(prompt_label.text).is_equal(first_prompt)

# ACC:T53.1
# ACC:T53.3
func test_hud_after_action_prompt_should_offer_actionable_guidance_from_contract_inputs() -> void:
    var hud = await _hud()
    var prompt_label := _runtime_prompt_label(hud)

    _publish("core.lastking.castle.hp_changed", {"Day": 9, "PreviousHp": 100, "CurrentHp": 42})
    _publish("core.lastking.wave.spawned", {"day": 9, "count": 6})
    _publish("core.lastking.resources.changed", {
        "RunId": "run-53-a",
        "DayNumber": 9,
        "Gold": 120,
        "Iron": 44,
        "PopulationCap": 26
    })
    _publish("core.run.state.transitioned", {"outcome": "win", "day": 9})
    await get_tree().process_frame
    assert_str(prompt_label.text).contains("reinforce frontline")

    _publish("core.lastking.castle.hp_changed", {"Day": 10, "PreviousHp": 100, "CurrentHp": 90})
    _publish("core.lastking.wave.spawned", {"day": 10, "count": 2})
    _publish("core.lastking.resources.changed", {
        "RunId": "run-53-b",
        "DayNumber": 10,
        "Gold": 100,
        "Iron": 35,
        "PopulationCap": 24
    })
    _publish("core.run.state.transitioned", {"outcome": "win", "day": 10})
    await get_tree().process_frame
    assert_str(prompt_label.text).contains("increase income")

    _publish("core.lastking.castle.hp_changed", {"Day": 11, "PreviousHp": 100, "CurrentHp": 90})
    _publish("core.lastking.wave.spawned", {"day": 11, "count": 2})
    _publish("core.lastking.resources.changed", {
        "RunId": "run-53-c",
        "DayNumber": 11,
        "Gold": 180,
        "Iron": 60,
        "PopulationCap": 30
    })
    _publish("core.run.state.transitioned", {"outcome": "win", "day": 11})
    await get_tree().process_frame
    assert_str(prompt_label.text).contains("expand defenses")
