extends Control

@onready var _status: Label = $Margin/VBox/Status
@onready var _summary: Label = $Margin/VBox/Summary
@onready var _legend: Label = $Margin/VBox/Legend
@onready var _metrics_help: Label = $Margin/VBox/MetricsHelp
@onready var _build_btn: Button = $Margin/VBox/Controls/BuildBtn
@onready var _wave_btn: Button = $Margin/VBox/Controls/WaveBtn
@onready var _auto_wave_btn: Button = $Margin/VBox/Controls/AutoWaveBtn
@onready var _exchange_btn: Button = $Margin/VBox/Controls/ExchangeBtn
@onready var _cleanup_btn: Button = $Margin/VBox/Controls/CleanupBtn
@onready var _finish_btn: Button = $Margin/VBox/Controls/FinishBtn
@onready var _back_btn: Button = $Margin/VBox/Controls/BackBtn
@onready var _bridge: Node = $CombatExperienceRuntimeBridge
@onready var _wave_timer: Timer = $WaveTimer
@onready var _background: ColorRect = $Background
@onready var _enemy_spawn_a: ColorRect = $Background/EnemySpawnA
@onready var _enemy_spawn_b: ColorRect = $Background/EnemySpawnB
@onready var _daily_settlement_modal: PanelContainer = $DailySettlementModal
@onready var _daily_settlement_summary: Label = $DailySettlementModal/VBox/Summary
@onready var _daily_evidence_hp: Label = $DailySettlementModal/VBox/EvidencePanel/HpEvidence
@onready var _daily_evidence_kills: Label = $DailySettlementModal/VBox/EvidencePanel/KillEvidence
@onready var _daily_evidence_reward_summary: Label = $DailySettlementModal/VBox/EvidencePanel/RewardSummaryEvidence
@onready var _daily_evidence_resources: Label = $DailySettlementModal/VBox/EvidencePanel/ResourceEvidence
@onready var _daily_evidence_defeat_reason: Label = $DailySettlementModal/VBox/EvidencePanel/DefeatReasonEvidence
@onready var _daily_expand_context_btn: Button = $DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn
@onready var _daily_runtime_context_payload: Label = $DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload
@onready var _daily_reward_a: Button = $DailySettlementModal/VBox/Rewards/RewardA
@onready var _daily_reward_b: Button = $DailySettlementModal/VBox/Rewards/RewardB
@onready var _daily_reward_c: Button = $DailySettlementModal/VBox/Rewards/RewardC
@onready var _victory_outcome_modal: PanelContainer = $VictoryOutcomeModal
@onready var _victory_outcome_title: Label = $VictoryOutcomeModal/VBox/Title
@onready var _victory_outcome_summary: Label = $VictoryOutcomeModal/VBox/Summary
@onready var _victory_outcome_hint: Label = $VictoryOutcomeModal/VBox/Hint
@onready var _victory_return_btn: Button = $VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn
@onready var _victory_restart_btn: Button = $VictoryOutcomeModal/VBox/Actions/RestartBtn
@onready var _defeat_outcome_modal: PanelContainer = $DefeatOutcomeModal
@onready var _defeat_outcome_title: Label = $DefeatOutcomeModal/VBox/Title
@onready var _defeat_outcome_summary: Label = $DefeatOutcomeModal/VBox/Summary
@onready var _defeat_outcome_hint: Label = $DefeatOutcomeModal/VBox/Hint
@onready var _defeat_return_btn: Button = $DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn
@onready var _defeat_restart_btn: Button = $DefeatOutcomeModal/VBox/Actions/RestartBtn

var _wave_started := false
var _combat_resolved := false
var _cleaned := false
var _auto_wave := false
var _spawn_pulse_time_left := 0.0
var _enemy_tokens := {}
var _path_points: PackedVector2Array = PackedVector2Array(
	[Vector2(120, 120), Vector2(260, 120), Vector2(420, 240), Vector2(640, 240), Vector2(840, 340), Vector2(1080, 340)]
)
var _i18n: Variant = null
var _settlement_modal_open := false
var _settlement_options: Array[String] = ["Reward A", "Reward B", "Reward C"]
var _settlement_context_expanded := false
var _settlement_context_snapshot: Dictionary = {}

func _ready() -> void:
	_i18n = load("res://Game.Godot/Scripts/Localization/LocalizationManager.gd").new()
	_i18n.configure_locale_resource("en-US", "res://Game.Godot/Localization/en-US.json")
	_i18n.configure_locale_resource("zh-CN", "res://Game.Godot/Localization/zh-CN.json")
	_i18n.switch_locale(_normalize_locale(str(TranslationServer.get_locale())))
	_apply_static_texts()
	_daily_settlement_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_daily_reward_a.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_daily_reward_b.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_daily_reward_c.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_victory_outcome_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_victory_return_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_victory_restart_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_defeat_outcome_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_defeat_return_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	_defeat_restart_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED

	# Task 56 ownership markers: battlefield presentation, runtime bridge, legacy prototype.
	_background.set_meta("ownership_container", "battlefield_presentation")
	_bridge.set_meta("ownership_container", "runtime_bridge")
	_wave_timer.set_meta("ownership_container", "runtime_bridge")
	$Margin.set_meta("ownership_container", "legacy_prototype")
	$Margin.set_meta("migration_only", true)

	_build_btn.pressed.connect(_on_build)
	_wave_btn.pressed.connect(_on_wave)
	_auto_wave_btn.pressed.connect(_on_auto_wave)
	_exchange_btn.pressed.connect(_on_exchange)
	_cleanup_btn.pressed.connect(_on_cleanup)
	_finish_btn.pressed.connect(_on_finish)
	_back_btn.pressed.connect(_on_back)
	_daily_reward_a.pressed.connect(_on_settlement_reward_selected.bind(0))
	_daily_reward_b.pressed.connect(_on_settlement_reward_selected.bind(1))
	_daily_reward_c.pressed.connect(_on_settlement_reward_selected.bind(2))
	_daily_expand_context_btn.pressed.connect(_on_settlement_context_toggle)
	_victory_return_btn.pressed.connect(_on_victory_return_to_main_menu)
	_victory_restart_btn.pressed.connect(_on_victory_restart)
	_defeat_return_btn.pressed.connect(_on_defeat_return_to_main_menu)
	_defeat_restart_btn.pressed.connect(_on_defeat_restart)
	_wave_timer.timeout.connect(_on_wave_timer_timeout)

	_try_bridge_reset()
	_close_settlement_modal()
	_close_victory_modal()
	_close_defeat_modal()
	_apply_spawn_cues()
	_render(_try_bridge_summary(), _t("battlemap.status.loaded"))

func _process(delta: float) -> void:
	var locale := _normalize_locale(str(TranslationServer.get_locale()))
	if locale != _i18n.current_locale():
		_i18n.switch_locale(locale)
		_apply_static_texts()
	if _bridge.has_method("AdvanceSimulation"):
		_bridge.call("AdvanceSimulation", delta)
	_sync_terminal_outcome_from_runtime()
	_update_spawn_cues(delta)
	_render_actor_tokens()

func _on_build() -> void:
	if _bridge.has_method("BuildPhase"):
		_bridge.call("BuildPhase")
	if _bridge.has_method("TrainFriendlyUnitPhase"):
		_bridge.call("TrainFriendlyUnitPhase")
	var summary := _try_bridge_summary()
	var friendly := int(summary.get("friendly_units_deployed", 0))
	_render(summary, "%s (%s=%d)" % [_t("battlemap.status.build_ready"), _t("battlemap.summary.friendly_units"), friendly])
	_sync_terminal_outcome_from_runtime(summary)

func _on_wave() -> void:
	if _settlement_modal_open:
		_status.text = "Settlement modal is open; resolve reward first."
		return
	if _is_terminal_outcome_modal_visible():
		_status.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		return
	_wave_started = true
	_combat_resolved = false
	_cleaned = false
	_spawn_pulse_time_left = 0.35
	_apply_spawn_cues()
	var summary := _call_or_fallback("SpawnEnemyWavePhase")
	_render(summary, _t("battlemap.status.wave_spawned"))
	_sync_terminal_outcome_from_runtime(summary)

func _on_auto_wave() -> void:
	_auto_wave = not _auto_wave
	_auto_wave_btn.text = _t("battlemap.btn.auto_stop") if _auto_wave else _t("battlemap.btn.auto_toggle")
	if _auto_wave:
		_wave_timer.start()
		_status.text = _t("battlemap.status.auto_on")
	else:
		_wave_timer.stop()
		_status.text = _t("battlemap.status.auto_off")

func _on_wave_timer_timeout() -> void:
	if _auto_wave and int(_try_bridge_summary().get("castle_hp", 0)) > 0:
		_on_wave()

func _on_exchange() -> void:
	if _settlement_modal_open:
		_status.text = "Settlement modal is open; resolve reward first."
		return
	if _is_terminal_outcome_modal_visible():
		_status.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		return
	if not _wave_started:
		_status.text = _t("battlemap.status.require_wave")
		return
	_combat_resolved = true
	var summary := _call_or_fallback("ResolveCombatExchangePhase")
	_render(summary, _t("battlemap.status.exchange_done"))
	_sync_terminal_outcome_from_runtime(summary)

func _on_cleanup() -> void:
	if _settlement_modal_open:
		_status.text = "Settlement modal is open; resolve reward first."
		return
	if _is_terminal_outcome_modal_visible():
		_status.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		return
	if not _combat_resolved:
		_status.text = _t("battlemap.status.require_exchange")
		return
	_cleaned = true
	var summary := _call_or_fallback("CleanupDeadUnitsPhase")
	_render(summary, _t("battlemap.status.cleanup_done"))
	_sync_terminal_outcome_from_runtime(summary)

func _on_finish() -> void:
	if _settlement_modal_open:
		_status.text = "Settlement modal is open; resolve reward first."
		return
	if _is_terminal_outcome_modal_visible():
		_status.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		return
	if not _cleaned:
		_status.text = _t("battlemap.status.require_cleanup")
		return
	# Ensure local spawn cues decay to weak state once the loop is completed.
	_spawn_pulse_time_left = 0.0
	_apply_spawn_cues()
	var outcome := _call_or_fallback("PublishOutcomePhase")
	_render(outcome, _t("battlemap.status.finished"))
	if _is_defeat_outcome(outcome):
		_open_defeat_modal(outcome)
	elif _is_victory_outcome(outcome):
		_open_victory_modal(outcome)
	elif _is_settlement_outcome(outcome):
		_open_settlement_modal(outcome)

func _on_back() -> void:
	var menu := get_node_or_null("/root/Main/RuntimeUi/MainMenu")
	if menu != null and menu.has_method("ShowMenu"):
		menu.ShowMenu()
	var nav := get_node_or_null("/root/Main/ScreenNavigator")
	if nav != null and nav.has_method("ClearCurrentScreen"):
		nav.ClearCurrentScreen()

func _render(result: Dictionary, status_text: String) -> void:
	var friendly := int(result.get("friendly_units_deployed", 0))
	var enemies := int(result.get("enemy_units_spawned", 0))
	var exchanges := int(result.get("combat_exchanges", 0))
	var retired := int(result.get("dead_units_retired", 0))
	var active := int(result.get("active_combat_nodes_after_cleanup", 0))
	var castle_hp := int(result.get("castle_hp", 0))
	_status.text = status_text
	_summary.text = "%s\n" % _t("battlemap.summary.title") \
		+ "%s: %d\n" % [_t("battlemap.summary.castle_hp"), castle_hp] \
		+ "%s: %d\n" % [_t("battlemap.summary.friendly_units"), friendly] \
		+ "%s: %d\n" % [_t("battlemap.summary.enemy_units_spawned"), enemies] \
		+ "%s: %d\n" % [_t("battlemap.summary.combat_exchanges"), exchanges] \
		+ "%s: %d\n" % [_t("battlemap.summary.dead_units_retired"), retired] \
		+ "%s: %d" % [_t("battlemap.summary.active_nodes"), active]

func _render_actor_tokens() -> void:
	if not _bridge.has_method("GetActorSnapshots"):
		return
	var snapshots: Array = _bridge.call("GetActorSnapshots")
	var alive_names := {}
	for item in snapshots:
		var d := item as Dictionary
		if d == null:
			continue
		if not bool(d.get("is_moving_enemy", false)):
			continue
		var actor_name := String(d.get("name", ""))
		if actor_name.is_empty():
			continue
		if not bool(d.get("active", false)):
			continue
		alive_names[actor_name] = true
		var token: ColorRect = _enemy_tokens.get(actor_name, null)
		if token == null:
			token = ColorRect.new()
			token.color = Color(0.95, 0.35, 0.35, 1.0)
			token.custom_minimum_size = Vector2(14, 14)
			token.size = Vector2(14, 14)
			_background.add_child(token)
			_enemy_tokens[actor_name] = token
		token.position = _sample_path(float(d.get("path_progress", 0.0)))

	for key in _enemy_tokens.keys():
		if alive_names.has(key):
			continue
		var stale: ColorRect = _enemy_tokens[key]
		if stale != null and is_instance_valid(stale):
			stale.queue_free()
		_enemy_tokens.erase(key)


func _update_spawn_cues(delta: float) -> void:
	if _spawn_pulse_time_left > 0.0:
		_spawn_pulse_time_left = maxf(0.0, _spawn_pulse_time_left - delta)
	_apply_spawn_cues()


func _apply_spawn_cues() -> void:
	var pulse_active := _spawn_pulse_time_left > 0.0
	var weak_color := Color(0.847059, 0.286275, 0.286275, 0.55)
	var pulse_color := Color(0.996078, 0.505882, 0.505882, 0.95)
	var spawn_color := pulse_color if pulse_active else weak_color
	_enemy_spawn_a.color = spawn_color
	_enemy_spawn_b.color = spawn_color

func _sample_path(progress: float) -> Vector2:
	var p := clampf(progress, 0.0, 1.0)
	var rev: PackedVector2Array = PackedVector2Array()
	for i in range(_path_points.size() - 1, -1, -1):
		rev.append(_path_points[i])
	var seg_count := rev.size() - 1
	if seg_count <= 0:
		return Vector2.ZERO
	var scaled := p * float(seg_count)
	var idx := mini(int(floor(scaled)), seg_count - 1)
	var local_t := scaled - float(idx)
	return rev[idx].lerp(rev[idx + 1], local_t)

func _normalize_locale(locale: String) -> String:
	var v := locale.strip_edges().to_lower()
	if v == "zh":
		return "zh-CN"
	if v == "en":
		return "en-US"
	if v.begins_with("zh"):
		return "zh-CN"
	return "en-US"

func _t(key: String) -> String:
	var translated: String = str(_i18n.translate(key))
	return translated if translated != key else key

func _try_bridge_reset() -> void:
	if _bridge.has_method("ResetForInteractiveRun"):
		_bridge.call("ResetForInteractiveRun")

func _try_bridge_summary() -> Dictionary:
	if _bridge.has_method("GetSummary"):
		var result = _bridge.call("GetSummary")
		if result is Dictionary:
			return result
	return _call_or_fallback("")

func _call_or_fallback(method_name: String) -> Dictionary:
	if not method_name.is_empty() and _bridge.has_method(method_name):
		var result = _bridge.call(method_name)
		if result is Dictionary:
			return result
	if _bridge.has_method("RunCompleteCombatExperienceForTest"):
		var full = _bridge.call("RunCompleteCombatExperienceForTest")
		if full is Dictionary:
			return full
	return {}

func _apply_static_texts() -> void:
	$Margin/VBox/Title.text = _t("battlemap.title")
	_build_btn.text = _t("battlemap.btn.build")
	_wave_btn.text = _t("battlemap.btn.wave")
	if not _auto_wave:
		_auto_wave_btn.text = _t("battlemap.btn.auto_toggle")
	_exchange_btn.text = _t("battlemap.btn.exchange")
	_cleanup_btn.text = _t("battlemap.btn.cleanup")
	_finish_btn.text = _t("battlemap.btn.finish")
	_back_btn.text = _t("battlemap.btn.back")
	_legend.text = _t("battlemap.legend")
	_metrics_help.text = _t("battlemap.metrics_help")

func register_overlay_controller(path: NodePath, controller: Node) -> void:
	var node_name := String(path.get_concatenated_names()).replace("/", "_")
	var existing := get_node_or_null(NodePath(node_name))
	if existing != null and existing != controller:
		remove_child(existing)
		existing.queue_free()
	if controller.get_parent() != null and controller.get_parent() != self:
		controller.get_parent().remove_child(controller)
	controller.name = node_name
	add_child(controller)

func SetSettlementOptionsForTest(options: Array) -> void:
	_settlement_options.clear()
	for option in options:
		_settlement_options.append(String(option))

func _is_settlement_outcome(summary: Dictionary) -> bool:
	var outcome := String(summary.get("outcome", "")).to_lower()
	if outcome == "settlement":
		return true
	if outcome == "loss":
		return false
	return false

func _is_victory_outcome(summary: Dictionary) -> bool:
	var outcome := String(summary.get("outcome", "")).to_lower()
	return outcome == "win"

func _is_defeat_outcome(summary: Dictionary) -> bool:
	return not _defeat_reason_for_summary(summary).is_empty()

func _defeat_reason_for_summary(summary: Dictionary) -> String:
	var explicit_reason := String(summary.get("defeat_reason", "")).to_lower()
	if explicit_reason == "wall_breached" or explicit_reason == "castle_destroyed":
		return explicit_reason
	var outcome := String(summary.get("outcome", "")).to_lower()
	var castle_hp := int(summary.get("castle_hp", 0))
	var wall_hp := int(summary.get("wall_hp", 1))
	if outcome == "loss" and wall_hp <= 0:
		return "wall_breached"
	if outcome == "loss" and castle_hp <= 0:
		return "castle_destroyed"
	if wall_hp <= 0:
		return "wall_breached"
	if castle_hp <= 0:
		return "castle_destroyed"
	return ""

func _open_settlement_modal(summary: Dictionary) -> void:
	_settlement_modal_open = true
	_settlement_context_expanded = false
	_settlement_context_snapshot = summary.duplicate(true)
	_defeat_outcome_modal.visible = false
	_victory_outcome_modal.visible = false
	var hp := int(summary.get("castle_hp", 0))
	var kills := int(summary.get("dead_units_retired", 0))
	var gold := int(summary.get("resource_gold", 0))
	var iron := int(summary.get("resource_iron", 0))
	var pop_cap := int(summary.get("resource_population_cap", 0))
	var rewards: Array[String] = _settlement_options
	if rewards.size() != 3:
		_settlement_modal_open = false
		_daily_settlement_modal.visible = false
		get_tree().paused = false
		_status.text = "Settlement rewards invalid; expected exactly 3 options."
		return
	_daily_settlement_modal.visible = true
	get_tree().paused = true
	var reward_summary := "%s,%s,%s" % [rewards[0], rewards[1], rewards[2]]
	_daily_settlement_summary.text = "HP=%d | rewards=%d | reward_summary=%s | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [hp, rewards.size(), reward_summary, kills, gold, iron, pop_cap]
	var has_hp := summary.has("castle_hp") and typeof(summary.get("castle_hp", null)) != TYPE_NIL
	_daily_evidence_hp.visible = has_hp
	if has_hp:
		_daily_evidence_hp.text = "Final HP: %d" % hp
	var has_kills := summary.has("dead_units_retired") and typeof(summary.get("dead_units_retired", null)) != TYPE_NIL
	_daily_evidence_kills.visible = has_kills
	if has_kills:
		_daily_evidence_kills.text = "Kills: %d" % kills
	var has_summary_reward_key := summary.has("reward_summary") and typeof(summary.get("reward_summary", null)) != TYPE_NIL
	var summary_reward_text := String(summary.get("reward_summary", "")).strip_edges()
	var has_reward_summary := has_summary_reward_key and not summary_reward_text.is_empty()
	_daily_evidence_reward_summary.visible = has_reward_summary
	if has_reward_summary:
		_daily_evidence_reward_summary.text = "Reward Summary: %s" % summary_reward_text
	var has_resource_gold := summary.has("resource_gold") and typeof(summary.get("resource_gold", null)) != TYPE_NIL
	var has_resource_iron := summary.has("resource_iron") and typeof(summary.get("resource_iron", null)) != TYPE_NIL
	var has_resource_pop := summary.has("resource_population_cap") and typeof(summary.get("resource_population_cap", null)) != TYPE_NIL
	var has_resources := has_resource_gold and has_resource_iron and has_resource_pop
	_daily_evidence_resources.visible = has_resources
	if has_resources:
		_daily_evidence_resources.text = "Resources: gold=%d, iron=%d, pop=%d" % [gold, iron, pop_cap]
	var defeat_reason_raw = summary.get("defeat_reason", null)
	var defeat_reason := String(defeat_reason_raw if defeat_reason_raw != null else "").strip_edges()
	_daily_evidence_defeat_reason.visible = summary.has("defeat_reason") and not defeat_reason.is_empty()
	_daily_evidence_defeat_reason.text = "Defeat Reason: %s" % defeat_reason
	_daily_runtime_context_payload.visible = false
	_daily_runtime_context_payload.text = JSON.stringify(_settlement_context_snapshot)
	_daily_expand_context_btn.text = "Show Runtime Context"
	_daily_reward_a.text = rewards[0]
	_daily_reward_b.text = rewards[1]
	_daily_reward_c.text = rewards[2]

func _close_settlement_modal() -> void:
	_settlement_modal_open = false
	_settlement_context_expanded = false
	_settlement_context_snapshot = {}
	_daily_settlement_modal.visible = false
	_daily_runtime_context_payload.visible = false
	_daily_expand_context_btn.text = "Show Runtime Context"
	get_tree().paused = false

func _on_settlement_context_toggle() -> void:
	if not _settlement_modal_open:
		return
	_settlement_context_expanded = not _settlement_context_expanded
	_daily_runtime_context_payload.visible = _settlement_context_expanded
	_daily_expand_context_btn.text = "Hide Runtime Context" if _settlement_context_expanded else "Show Runtime Context"
	if _settlement_context_expanded:
		_daily_runtime_context_payload.text = JSON.stringify(_settlement_context_snapshot)

func _open_victory_modal(summary: Dictionary) -> void:
	_settlement_modal_open = false
	_daily_settlement_modal.visible = false
	_defeat_outcome_modal.visible = false
	_victory_outcome_modal.visible = true
	get_tree().paused = true
	var hp := int(summary.get("castle_hp", 0))
	var kills := int(summary.get("dead_units_retired", 0))
	var gold := int(summary.get("resource_gold", 0))
	var iron := int(summary.get("resource_iron", 0))
	var pop_cap := int(summary.get("resource_population_cap", 0))
	_victory_outcome_title.text = "Victory"
	_victory_outcome_summary.text = "HP=%d | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [hp, kills, gold, iron, pop_cap]
	_victory_outcome_hint.text = "Live battle cannot be resumed from this outcome state."

func _close_victory_modal() -> void:
	_victory_outcome_modal.visible = false
	if get_tree() != null:
		get_tree().paused = false

func _open_defeat_modal(summary: Dictionary) -> void:
	_settlement_modal_open = false
	_daily_settlement_modal.visible = false
	_victory_outcome_modal.visible = false
	_defeat_outcome_modal.visible = true
	get_tree().paused = true
	var hp := int(summary.get("castle_hp", 0))
	var wall_hp := int(summary.get("wall_hp", 0))
	var kills := int(summary.get("dead_units_retired", 0))
	var gold := int(summary.get("resource_gold", 0))
	var iron := int(summary.get("resource_iron", 0))
	var pop_cap := int(summary.get("resource_population_cap", 0))
	var defeat_copy := "Wall breached. The run ended."
	_defeat_outcome_title.text = "Defeat"
	_defeat_outcome_summary.text = "%s Castle HP=%d | Wall HP=%d | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [defeat_copy, hp, wall_hp, kills, gold, iron, pop_cap]
	_defeat_outcome_hint.text = "Live battle cannot be resumed from this outcome state."

func _close_defeat_modal() -> void:
	_defeat_outcome_modal.visible = false
	if get_tree() != null:
		get_tree().paused = false

func _is_terminal_outcome_modal_visible() -> bool:
	return _victory_outcome_modal.visible or _defeat_outcome_modal.visible

func _on_settlement_reward_selected(index: int) -> void:
	if not _settlement_modal_open:
		return
	if index < 0 or index >= _settlement_options.size():
		return
	_status.text = "Settlement resolved with %s" % _settlement_options[index]
	_close_settlement_modal()

func _on_victory_return_to_main_menu() -> void:
	if not _victory_outcome_modal.visible:
		return
	_wave_started = false
	_combat_resolved = false
	_cleaned = false
	_close_victory_modal()
	_status.text = "Victory outcome resolved with Return to Main Menu."
	_on_back()

func _on_victory_restart() -> void:
	if not _victory_outcome_modal.visible:
		return
	_close_victory_modal()
	_status.text = "Victory outcome resolved with Restart."
	_restart_from_terminal_outcome()

func _on_defeat_return_to_main_menu() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_wave_started = false
	_combat_resolved = false
	_cleaned = false
	_close_defeat_modal()
	_status.text = "Defeat outcome resolved with Return to Main Menu."
	_on_back()

func _on_defeat_restart() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_close_defeat_modal()
	_status.text = "Defeat outcome resolved with Restart."
	_restart_from_terminal_outcome()

func _restart_from_terminal_outcome() -> void:
	_wave_started = false
	_combat_resolved = false
	_cleaned = false
	_auto_wave = false
	_wave_timer.stop()
	_auto_wave_btn.text = _t("battlemap.btn.auto_toggle")
	_try_bridge_reset()
	_apply_spawn_cues()
	_render(_try_bridge_summary(), _t("battlemap.status.loaded"))

func _sync_terminal_outcome_from_runtime(summary: Dictionary = {}) -> void:
	if _settlement_modal_open or _is_terminal_outcome_modal_visible():
		return
	if not _wave_started:
		return
	var runtime_summary := summary
	if runtime_summary.is_empty():
		runtime_summary = _try_bridge_summary()
	if _is_defeat_outcome(runtime_summary):
		_open_defeat_modal(runtime_summary)
