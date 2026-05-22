extends Node

var _screen: Control = null
var _wave_timer: Timer = null
var _translate: Callable
var _back_callback: Callable
var _bridge_provider: Callable
var _operation_controller: Node = null
var _feedback_controller: Node = null

var _daily_settlement_modal: PanelContainer = null
var _daily_settlement_title: Label = null
var _daily_settlement_summary: Label = null
var _daily_evidence_title: Label = null
var _daily_evidence_hp: Label = null
var _daily_evidence_kills: Label = null
var _daily_evidence_reward_summary: Label = null
var _daily_evidence_resources: Label = null
var _daily_evidence_defeat_reason: Label = null
var _daily_expand_context_btn: Button = null
var _daily_runtime_context_payload: Label = null
var _daily_reward_a: Button = null
var _daily_reward_b: Button = null
var _daily_reward_c: Button = null
var _daily_hint: Label = null
var _victory_outcome_modal: PanelContainer = null
var _victory_outcome_title: Label = null
var _victory_outcome_summary: Label = null
var _victory_outcome_hint: Label = null
var _victory_return_btn: Button = null
var _victory_restart_btn: Button = null
var _defeat_outcome_modal: PanelContainer = null
var _defeat_outcome_title: Label = null
var _defeat_outcome_summary: Label = null
var _defeat_outcome_hint: Label = null
var _defeat_return_btn: Button = null
var _defeat_restart_btn: Button = null
var _battle_settings_title: Label = null
var _battle_settings_return_btn: Button = null
var _battle_settings_main_menu_btn: Button = null

var _settlement_modal_open: bool = false
var _settlement_options: Array[String] = ["battlemap.reward.a", "battlemap.reward.b", "battlemap.reward.c"]
var _settlement_context_expanded: bool = false
var _settlement_context_snapshot: Dictionary = {}

func configure(screen: Control, refs: Dictionary) -> void:
	_screen = screen
	_wave_timer = refs["wave_timer"]
	_translate = refs["translate"]
	_back_callback = refs["back_callback"]
	_bridge_provider = refs["bridge_provider"]
	_operation_controller = refs["operation_controller"]
	_feedback_controller = refs["feedback_controller"]
	_daily_settlement_modal = refs["daily_settlement_modal"]
	_daily_settlement_title = refs["daily_settlement_title"]
	_daily_settlement_summary = refs["daily_settlement_summary"]
	_daily_evidence_title = refs["daily_evidence_title"]
	_daily_evidence_hp = refs["daily_evidence_hp"]
	_daily_evidence_kills = refs["daily_evidence_kills"]
	_daily_evidence_reward_summary = refs["daily_evidence_reward_summary"]
	_daily_evidence_resources = refs["daily_evidence_resources"]
	_daily_evidence_defeat_reason = refs["daily_evidence_defeat_reason"]
	_daily_expand_context_btn = refs["daily_expand_context_btn"]
	_daily_runtime_context_payload = refs["daily_runtime_context_payload"]
	_daily_reward_a = refs["daily_reward_a"]
	_daily_reward_b = refs["daily_reward_b"]
	_daily_reward_c = refs["daily_reward_c"]
	_daily_hint = refs["daily_hint"]
	_victory_outcome_modal = refs["victory_outcome_modal"]
	_victory_outcome_title = refs["victory_outcome_title"]
	_victory_outcome_summary = refs["victory_outcome_summary"]
	_victory_outcome_hint = refs["victory_outcome_hint"]
	_victory_return_btn = refs["victory_return_btn"]
	_victory_restart_btn = refs["victory_restart_btn"]
	_defeat_outcome_modal = refs["defeat_outcome_modal"]
	_defeat_outcome_title = refs["defeat_outcome_title"]
	_defeat_outcome_summary = refs["defeat_outcome_summary"]
	_defeat_outcome_hint = refs["defeat_outcome_hint"]
	_defeat_return_btn = refs["defeat_return_btn"]
	_defeat_restart_btn = refs["defeat_restart_btn"]
	_battle_settings_title = refs.get("battle_settings_title", null)
	_battle_settings_return_btn = refs.get("battle_settings_return_btn", null)
	_battle_settings_main_menu_btn = refs.get("battle_settings_main_menu_btn", null)

func connect_signals() -> void:
	if _daily_reward_a != null:
		_daily_reward_a.pressed.connect(_on_settlement_reward_selected.bind(0))
	if _daily_reward_b != null:
		_daily_reward_b.pressed.connect(_on_settlement_reward_selected.bind(1))
	if _daily_reward_c != null:
		_daily_reward_c.pressed.connect(_on_settlement_reward_selected.bind(2))
	if _daily_expand_context_btn != null:
		_daily_expand_context_btn.pressed.connect(_on_settlement_context_toggle)
	if _victory_return_btn != null:
		_victory_return_btn.pressed.connect(_on_victory_return_to_main_menu)
	if _victory_restart_btn != null:
		_victory_restart_btn.pressed.connect(_on_victory_restart)
	if _defeat_return_btn != null:
		_defeat_return_btn.pressed.connect(_on_defeat_return_to_main_menu)
	if _defeat_restart_btn != null:
		_defeat_restart_btn.pressed.connect(_on_defeat_restart)

func cleanup() -> void:
	if _daily_reward_a != null and _daily_reward_a.pressed.is_connected(_on_settlement_reward_selected.bind(0)):
		_daily_reward_a.pressed.disconnect(_on_settlement_reward_selected.bind(0))
	if _daily_reward_b != null and _daily_reward_b.pressed.is_connected(_on_settlement_reward_selected.bind(1)):
		_daily_reward_b.pressed.disconnect(_on_settlement_reward_selected.bind(1))
	if _daily_reward_c != null and _daily_reward_c.pressed.is_connected(_on_settlement_reward_selected.bind(2)):
		_daily_reward_c.pressed.disconnect(_on_settlement_reward_selected.bind(2))
	if _daily_expand_context_btn != null and _daily_expand_context_btn.pressed.is_connected(_on_settlement_context_toggle):
		_daily_expand_context_btn.pressed.disconnect(_on_settlement_context_toggle)
	if _victory_return_btn != null and _victory_return_btn.pressed.is_connected(_on_victory_return_to_main_menu):
		_victory_return_btn.pressed.disconnect(_on_victory_return_to_main_menu)
	if _victory_restart_btn != null and _victory_restart_btn.pressed.is_connected(_on_victory_restart):
		_victory_restart_btn.pressed.disconnect(_on_victory_restart)
	if _defeat_return_btn != null and _defeat_return_btn.pressed.is_connected(_on_defeat_return_to_main_menu):
		_defeat_return_btn.pressed.disconnect(_on_defeat_return_to_main_menu)
	if _defeat_restart_btn != null and _defeat_restart_btn.pressed.is_connected(_on_defeat_restart):
		_defeat_restart_btn.pressed.disconnect(_on_defeat_restart)

func set_process_modes() -> void:
	if _daily_settlement_modal != null:
		_daily_settlement_modal.top_level = true
		_daily_settlement_modal.z_index = 520
		_daily_settlement_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var daily_backdrop: CanvasItem = _screen.get_node_or_null("DailySettlementModal/Backdrop")
	if daily_backdrop != null:
		daily_backdrop.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var daily_vbox: Control = _screen.get_node_or_null("DailySettlementModal/VBox")
	if daily_vbox != null:
		daily_vbox.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _daily_reward_a != null:
		_daily_reward_a.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _daily_reward_b != null:
		_daily_reward_b.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _daily_reward_c != null:
		_daily_reward_c.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _victory_outcome_modal != null:
		_victory_outcome_modal.top_level = true
		_victory_outcome_modal.z_index = 520
		_victory_outcome_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var victory_backdrop: CanvasItem = _screen.get_node_or_null("VictoryOutcomeModal/Backdrop")
	if victory_backdrop != null:
		victory_backdrop.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var victory_vbox: Control = _screen.get_node_or_null("VictoryOutcomeModal/VBox")
	if victory_vbox != null:
		victory_vbox.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _victory_return_btn != null:
		_victory_return_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _victory_restart_btn != null:
		_victory_restart_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _defeat_outcome_modal != null:
		_defeat_outcome_modal.top_level = true
		_defeat_outcome_modal.z_index = 520
		_defeat_outcome_modal.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var defeat_backdrop: CanvasItem = _screen.get_node_or_null("DefeatOutcomeModal/Backdrop")
	if defeat_backdrop != null:
		defeat_backdrop.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	var defeat_vbox: Control = _screen.get_node_or_null("DefeatOutcomeModal/VBox")
	if defeat_vbox != null:
		defeat_vbox.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _defeat_return_btn != null:
		_defeat_return_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED
	if _defeat_restart_btn != null:
		_defeat_restart_btn.process_mode = Node.PROCESS_MODE_WHEN_PAUSED

func SetSettlementOptionsForTest(options: Variant) -> void:
	_settlement_options.clear()
	if typeof(options) != TYPE_ARRAY:
		return
	for option in options:
		_settlement_options.append(str(option))

func get_settlement_options() -> Array[String]:
	return _settlement_options.duplicate()

func is_settlement_open() -> bool:
	return _settlement_modal_open

func is_terminal_outcome_modal_visible() -> bool:
	var victory_visible: bool = _victory_outcome_modal != null and _victory_outcome_modal.visible
	var defeat_visible: bool = _defeat_outcome_modal != null and _defeat_outcome_modal.visible
	return victory_visible or defeat_visible

func is_settlement_outcome(summary: Dictionary) -> bool:
	var outcome: String = str(summary.get("outcome", "")).to_lower()
	if outcome == "settlement":
		return true
	if outcome == "loss":
		return false
	return false

func is_victory_outcome(summary: Dictionary) -> bool:
	var outcome: String = str(summary.get("outcome", "")).to_lower()
	return outcome == "win"

func is_defeat_outcome(summary: Dictionary) -> bool:
	return not defeat_reason_for_summary(summary).is_empty()

func defeat_reason_for_summary(summary: Dictionary) -> String:
	var explicit_reason: String = str(summary.get("defeat_reason", "")).to_lower()
	if explicit_reason == "wall_breached" or explicit_reason == "castle_destroyed":
		return explicit_reason
	var outcome: String = str(summary.get("outcome", "")).to_lower()
	var castle_hp: int = int(summary.get("castle_hp", 0))
	var wall_hp: int = int(summary.get("wall_hp", 1))
	if outcome == "loss" and wall_hp <= 0:
		return "wall_breached"
	if outcome == "loss" and castle_hp <= 0:
		return "castle_destroyed"
	if wall_hp <= 0:
		return "wall_breached"
	if castle_hp <= 0:
		return "castle_destroyed"
	return ""

func open_outcome(summary: Dictionary) -> void:
	if is_defeat_outcome(summary):
		_open_defeat_modal(summary)
	elif is_victory_outcome(summary):
		_open_victory_modal(summary)
	elif is_settlement_outcome(summary):
		_open_settlement_modal(summary)

func sync_terminal_outcome_from_runtime(summary: Dictionary, wave_started: bool) -> void:
	if _settlement_modal_open or is_terminal_outcome_modal_visible():
		return
	if not wave_started:
		return
	if is_defeat_outcome(summary):
		_open_defeat_modal(summary)

func sync_terminal_outcome_from_bridge(wave_started: bool) -> void:
	sync_terminal_outcome_from_runtime(_try_bridge_summary(), wave_started)

func close_all() -> void:
	_close_settlement_modal()
	_close_victory_modal()
	_close_defeat_modal()

func _open_settlement_modal(summary: Dictionary) -> void:
	if _daily_settlement_modal == null:
		return
	_settlement_modal_open = true
	_settlement_context_expanded = false
	_settlement_context_snapshot = summary.duplicate(true)
	if _defeat_outcome_modal != null:
		_defeat_outcome_modal.visible = false
	if _victory_outcome_modal != null:
		_victory_outcome_modal.visible = false
	var castle_hp: int = int(summary.get("castle_hp", 100))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	var rewards: Array[String] = _settlement_options
	if rewards.size() != 3:
		_settlement_modal_open = false
		if _daily_settlement_modal != null:
			_daily_settlement_modal.visible = false
		if _screen != null and _screen.get_tree() != null:
			_screen.get_tree().paused = false
		_render_status_only(_t("battlemap.status.invalid_settlement_rewards"))
		return
	_daily_settlement_modal.visible = true
	_screen.get_tree().paused = true
	_daily_settlement_title.text = _t("battlemap.daily_settlement.title")
	_daily_evidence_title.text = _t("battlemap.daily_settlement.evidence_title")
	_daily_hint.text = _t("battlemap.daily_settlement.hint")
	var reward_a_text: String = _display_reward_text(rewards[0])
	var reward_b_text: String = _display_reward_text(rewards[1])
	var reward_c_text: String = _display_reward_text(rewards[2])
	var reward_summary: String = "%s,%s,%s" % [reward_a_text, reward_b_text, reward_c_text]
	_daily_settlement_summary.text = "%s=%d | %s=%d | %s=%s | %s=%d | %s=%d %s=%d %s=%d" % [
		_t("battlemap.daily_settlement.final_hp"),
		castle_hp,
		_t("battlemap.daily_settlement.rewards_count"),
		rewards.size(),
		_t("battlemap.daily_settlement.reward_summary"),
		reward_summary,
		_t("battlemap.daily_settlement.kills"),
		kills,
		_t("battlemap.resource.gold"),
		gold,
		_t("battlemap.resource.iron"),
		iron,
		_t("battlemap.resource.population"),
		pop_cap,
	]
	var has_castle_hp: bool = summary.has("castle_hp") and typeof(summary.get("castle_hp", null)) != TYPE_NIL
	_daily_evidence_hp.visible = has_castle_hp
	if has_castle_hp:
		_daily_evidence_hp.text = "%s: %d" % [_t("battlemap.daily_settlement.final_hp"), castle_hp]
	var has_kills: bool = summary.has("dead_units_retired") and typeof(summary.get("dead_units_retired", null)) != TYPE_NIL
	_daily_evidence_kills.visible = has_kills
	if has_kills:
		_daily_evidence_kills.text = "%s: %d" % [_t("battlemap.daily_settlement.kills"), kills]
	var has_summary_reward_key: bool = summary.has("reward_summary") and typeof(summary.get("reward_summary", null)) != TYPE_NIL
	var summary_reward_text: String = str(summary.get("reward_summary", "")).strip_edges()
	var has_reward_summary: bool = has_summary_reward_key and not summary_reward_text.is_empty()
	_daily_evidence_reward_summary.visible = has_reward_summary
	if has_reward_summary:
		_daily_evidence_reward_summary.text = "%s: %s" % [_t("battlemap.daily_settlement.reward_summary"), summary_reward_text]
	var has_resource_gold: bool = summary.has("resource_gold") and typeof(summary.get("resource_gold", null)) != TYPE_NIL
	var has_resource_iron: bool = summary.has("resource_iron") and typeof(summary.get("resource_iron", null)) != TYPE_NIL
	var has_resource_pop: bool = summary.has("resource_population_cap") and typeof(summary.get("resource_population_cap", null)) != TYPE_NIL
	var has_resources: bool = has_resource_gold and has_resource_iron and has_resource_pop
	_daily_evidence_resources.visible = has_resources
	if has_resources:
		_daily_evidence_resources.text = "%s: %s=%d, %s=%d, %s=%d" % [
			_t("battlemap.daily_settlement.resources"),
			_t("battlemap.resource.gold"),
			gold,
			_t("battlemap.resource.iron"),
			iron,
			_t("battlemap.resource.population"),
			pop_cap,
		]
	var defeat_reason_raw: Variant = summary.get("defeat_reason", null)
	var defeat_reason: String = str(defeat_reason_raw if defeat_reason_raw != null else "").strip_edges()
	_daily_evidence_defeat_reason.visible = summary.has("defeat_reason") and not defeat_reason.is_empty()
	_daily_evidence_defeat_reason.text = "%s: %s" % [_t("battlemap.daily_settlement.defeat_reason"), defeat_reason]
	_daily_runtime_context_payload.visible = false
	_daily_runtime_context_payload.text = JSON.stringify(_settlement_context_snapshot)
	_daily_expand_context_btn.text = _t("battlemap.daily_settlement.show_runtime_context")
	_daily_reward_a.text = reward_a_text
	_daily_reward_b.text = reward_b_text
	_daily_reward_c.text = reward_c_text

func _close_settlement_modal() -> void:
	_settlement_modal_open = false
	_settlement_context_expanded = false
	_settlement_context_snapshot = {}
	if _daily_settlement_modal != null:
		_daily_settlement_modal.visible = false
	if _daily_runtime_context_payload != null:
		_daily_runtime_context_payload.visible = false
	if _daily_expand_context_btn != null:
		_daily_expand_context_btn.text = _t("battlemap.daily_settlement.show_runtime_context")
	if _screen != null and _screen.get_tree() != null:
		_screen.get_tree().paused = false

func _on_settlement_context_toggle() -> void:
	if not _settlement_modal_open:
		return
	_settlement_context_expanded = not _settlement_context_expanded
	_daily_runtime_context_payload.visible = _settlement_context_expanded
	_daily_expand_context_btn.text = _t("battlemap.daily_settlement.hide_runtime_context") if _settlement_context_expanded else _t("battlemap.daily_settlement.show_runtime_context")
	if _settlement_context_expanded:
		_daily_runtime_context_payload.text = JSON.stringify(_settlement_context_snapshot)

func _open_victory_modal(summary: Dictionary) -> void:
	if _victory_outcome_modal == null:
		return
	_settlement_modal_open = false
	if _daily_settlement_modal != null:
		_daily_settlement_modal.visible = false
	if _defeat_outcome_modal != null:
		_defeat_outcome_modal.visible = false
	_victory_outcome_modal.visible = true
	if _screen != null and _screen.get_tree() != null:
		_screen.get_tree().paused = true
	var castle_hp: int = int(summary.get("castle_hp", 100))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	_victory_outcome_title.text = _t("battlemap.outcome.victory.title")
	_victory_outcome_summary.text = "%s=%d | %s=%d | %s=%d %s=%d %s=%d" % [
		_t("battlemap.daily_settlement.final_hp"),
		castle_hp,
		_t("battlemap.daily_settlement.kills"),
		kills,
		_t("battlemap.resource.gold"),
		gold,
		_t("battlemap.resource.iron"),
		iron,
		_t("battlemap.resource.population"),
		pop_cap,
	]
	_victory_outcome_hint.text = _t("battlemap.outcome.hint")
	_victory_return_btn.text = _t("battlemap.return_main_menu")
	_victory_restart_btn.text = _t("battlemap.restart")

func _close_victory_modal() -> void:
	if _victory_outcome_modal != null:
		_victory_outcome_modal.visible = false
	if _screen != null and _screen.get_tree() != null:
		_screen.get_tree().paused = false

func _open_defeat_modal(summary: Dictionary) -> void:
	if _defeat_outcome_modal == null:
		return
	_settlement_modal_open = false
	if _daily_settlement_modal != null:
		_daily_settlement_modal.visible = false
	if _victory_outcome_modal != null:
		_victory_outcome_modal.visible = false
	_defeat_outcome_modal.visible = true
	if _screen != null and _screen.get_tree() != null:
		_screen.get_tree().paused = true
	var wall_hp: int = int(summary.get("wall_hp", 0))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	var defeat_copy: String = _t("battlemap.outcome.defeat.wall_breached")
	_defeat_outcome_title.text = _t("battlemap.outcome.defeat.title")
	_defeat_outcome_summary.text = "%s %s=%d/100 | %s=%d | %s=%d %s=%d %s=%d" % [
		defeat_copy,
		_t("battlemap.summary.wall_hp"),
		wall_hp,
		_t("battlemap.daily_settlement.kills"),
		kills,
		_t("battlemap.resource.gold"),
		gold,
		_t("battlemap.resource.iron"),
		iron,
		_t("battlemap.resource.population"),
		pop_cap,
	]
	_defeat_outcome_hint.text = _t("battlemap.outcome.hint")
	_defeat_return_btn.text = _t("battlemap.return_main_menu")
	_defeat_restart_btn.text = _t("battlemap.restart")

func _close_defeat_modal() -> void:
	if _defeat_outcome_modal != null:
		_defeat_outcome_modal.visible = false
	if _screen != null and _screen.get_tree() != null:
		_screen.get_tree().paused = false

func _on_settlement_reward_selected(index: int) -> void:
	if not _settlement_modal_open:
		return
	if index < 0 or index >= _settlement_options.size():
		return
	_render_status_only("%s: %s" % [_t("battlemap.status.settlement_resolved"), _display_reward_text(_settlement_options[index])])
	_close_settlement_modal()

func _on_victory_return_to_main_menu() -> void:
	if not _victory_outcome_modal.visible:
		return
	_close_victory_modal()
	_render_status_only(_t("battlemap.status.victory_return_to_main_menu"))
	_reset_runtime_state()
	if _back_callback.is_valid():
		_back_callback.call()

func _on_victory_restart() -> void:
	if not _victory_outcome_modal.visible:
		return
	_close_victory_modal()
	_render_status_only(_t("battlemap.status.victory_restart"))
	_restart_runtime_state()

func _on_defeat_return_to_main_menu() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_close_defeat_modal()
	_render_status_only(_t("battlemap.status.defeat_return_to_main_menu"))
	_reset_runtime_state()
	if _back_callback.is_valid():
		_back_callback.call()

func _on_defeat_restart() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_close_defeat_modal()
	_render_status_only(_t("battlemap.status.defeat_restart"))
	_restart_runtime_state()

func _reset_runtime_state() -> void:
	if _operation_controller != null:
		_operation_controller.call("reset_flags")

func _restart_runtime_state() -> void:
	_reset_runtime_state()
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
	var build_placement_controller: Node = _screen.get_node_or_null("BuildPlacementController")
	if build_placement_controller != null and build_placement_controller.has_method("reset_runtime_state"):
		build_placement_controller.call("reset_runtime_state")
	var selection_controller: Node = _screen.get_node_or_null("SelectionController")
	if selection_controller != null and selection_controller.has_method("clear_building_selection"):
		selection_controller.call("clear_building_selection")
	if _feedback_controller != null:
		if _feedback_controller.has_method("reset_runtime_state"):
			_feedback_controller.call("reset_runtime_state")
		_feedback_controller.call("clear_spawn_pulse")
		_feedback_controller.call("render_summary", _try_bridge_summary(), _t("battlemap.status.loaded"))

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null

func _try_bridge_summary() -> Dictionary:
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("GetSummary"):
		var result: Variant = bridge.call("GetSummary")
		if result is Dictionary:
			return result
	if bridge != null and bridge.has_method("RunCompleteCombatExperienceForTest"):
		var full: Variant = bridge.call("RunCompleteCombatExperienceForTest")
		if full is Dictionary:
			return full
	return {}

func _t(key: String) -> String:
	if _translate.is_valid():
		return str(_translate.call(key))
	return key

func _render_status_only(status_text: String) -> void:
	if _feedback_controller != null:
		_feedback_controller.call("render_status_only", status_text)

func _display_reward_text(raw_value: String) -> String:
	var key: String = raw_value.strip_edges()
	match key:
		"Reward A":
			key = "battlemap.reward.a"
		"Reward B":
			key = "battlemap.reward.b"
		"Reward C":
			key = "battlemap.reward.c"
	if key.begins_with("battlemap."):
		return _t(key)
	return key

