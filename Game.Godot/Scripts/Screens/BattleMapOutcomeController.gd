extends Node

var _screen: Control = null
var _wave_timer: Timer = null
var _translate: Callable
var _back_callback: Callable
var _bridge_provider: Callable
var _operation_controller: Node = null
var _feedback_controller: Node = null

var _daily_settlement_modal: PanelContainer = null
var _daily_settlement_summary: Label = null
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

var _settlement_modal_open: bool = false
var _settlement_options: Array[String] = ["Reward A", "Reward B", "Reward C"]
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
	_daily_settlement_summary = refs["daily_settlement_summary"]
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

func connect_signals() -> void:
	_daily_reward_a.pressed.connect(_on_settlement_reward_selected.bind(0))
	_daily_reward_b.pressed.connect(_on_settlement_reward_selected.bind(1))
	_daily_reward_c.pressed.connect(_on_settlement_reward_selected.bind(2))
	_daily_expand_context_btn.pressed.connect(_on_settlement_context_toggle)
	_victory_return_btn.pressed.connect(_on_victory_return_to_main_menu)
	_victory_restart_btn.pressed.connect(_on_victory_restart)
	_defeat_return_btn.pressed.connect(_on_defeat_return_to_main_menu)
	_defeat_restart_btn.pressed.connect(_on_defeat_restart)

func set_process_modes() -> void:
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
	return _victory_outcome_modal.visible or _defeat_outcome_modal.visible

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
	_settlement_modal_open = true
	_settlement_context_expanded = false
	_settlement_context_snapshot = summary.duplicate(true)
	_defeat_outcome_modal.visible = false
	_victory_outcome_modal.visible = false
	var hp: int = int(summary.get("castle_hp", 0))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	var rewards: Array[String] = _settlement_options
	if rewards.size() != 3:
		_settlement_modal_open = false
		_daily_settlement_modal.visible = false
		_screen.get_tree().paused = false
		_render_status_only("Settlement rewards invalid; expected exactly 3 options.")
		return
	_daily_settlement_modal.visible = true
	_screen.get_tree().paused = true
	var reward_summary: String = "%s,%s,%s" % [rewards[0], rewards[1], rewards[2]]
	_daily_settlement_summary.text = "HP=%d | rewards=%d | reward_summary=%s | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [hp, rewards.size(), reward_summary, kills, gold, iron, pop_cap]
	var has_hp: bool = summary.has("castle_hp") and typeof(summary.get("castle_hp", null)) != TYPE_NIL
	_daily_evidence_hp.visible = has_hp
	if has_hp:
		_daily_evidence_hp.text = "Final HP: %d" % hp
	var has_kills: bool = summary.has("dead_units_retired") and typeof(summary.get("dead_units_retired", null)) != TYPE_NIL
	_daily_evidence_kills.visible = has_kills
	if has_kills:
		_daily_evidence_kills.text = "Kills: %d" % kills
	var has_summary_reward_key: bool = summary.has("reward_summary") and typeof(summary.get("reward_summary", null)) != TYPE_NIL
	var summary_reward_text: String = str(summary.get("reward_summary", "")).strip_edges()
	var has_reward_summary: bool = has_summary_reward_key and not summary_reward_text.is_empty()
	_daily_evidence_reward_summary.visible = has_reward_summary
	if has_reward_summary:
		_daily_evidence_reward_summary.text = "Reward Summary: %s" % summary_reward_text
	var has_resource_gold: bool = summary.has("resource_gold") and typeof(summary.get("resource_gold", null)) != TYPE_NIL
	var has_resource_iron: bool = summary.has("resource_iron") and typeof(summary.get("resource_iron", null)) != TYPE_NIL
	var has_resource_pop: bool = summary.has("resource_population_cap") and typeof(summary.get("resource_population_cap", null)) != TYPE_NIL
	var has_resources: bool = has_resource_gold and has_resource_iron and has_resource_pop
	_daily_evidence_resources.visible = has_resources
	if has_resources:
		_daily_evidence_resources.text = "Resources: gold=%d, iron=%d, pop=%d" % [gold, iron, pop_cap]
	var defeat_reason_raw: Variant = summary.get("defeat_reason", null)
	var defeat_reason: String = str(defeat_reason_raw if defeat_reason_raw != null else "").strip_edges()
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
	_screen.get_tree().paused = false

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
	_screen.get_tree().paused = true
	var hp: int = int(summary.get("castle_hp", 0))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	_victory_outcome_title.text = "Victory"
	_victory_outcome_summary.text = "HP=%d | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [hp, kills, gold, iron, pop_cap]
	_victory_outcome_hint.text = "Live battle cannot be resumed from this outcome state."

func _close_victory_modal() -> void:
	_victory_outcome_modal.visible = false
	if _screen.get_tree() != null:
		_screen.get_tree().paused = false

func _open_defeat_modal(summary: Dictionary) -> void:
	_settlement_modal_open = false
	_daily_settlement_modal.visible = false
	_victory_outcome_modal.visible = false
	_defeat_outcome_modal.visible = true
	_screen.get_tree().paused = true
	var hp: int = int(summary.get("castle_hp", 0))
	var wall_hp := int(summary.get("wall_hp", 0))
	var kills: int = int(summary.get("dead_units_retired", 0))
	var gold: int = int(summary.get("resource_gold", 0))
	var iron: int = int(summary.get("resource_iron", 0))
	var pop_cap: int = int(summary.get("resource_population_cap", 0))
	var defeat_copy: String = "Wall breached. The run ended."
	_defeat_outcome_title.text = "Defeat"
	_defeat_outcome_summary.text = "%s Castle HP=%d | Wall HP=%d | kills=%d | resources(gold=%d,iron=%d,pop=%d)" % [defeat_copy, hp, wall_hp, kills, gold, iron, pop_cap]
	_defeat_outcome_hint.text = "Live battle cannot be resumed from this outcome state."

func _close_defeat_modal() -> void:
	_defeat_outcome_modal.visible = false
	if _screen.get_tree() != null:
		_screen.get_tree().paused = false

func _on_settlement_reward_selected(index: int) -> void:
	if not _settlement_modal_open:
		return
	if index < 0 or index >= _settlement_options.size():
		return
	_render_status_only("Settlement resolved with %s" % _settlement_options[index])
	_close_settlement_modal()

func _on_victory_return_to_main_menu() -> void:
	if not _victory_outcome_modal.visible:
		return
	_close_victory_modal()
	_render_status_only("Victory outcome resolved with Return to Main Menu.")
	_reset_runtime_state()
	if _back_callback.is_valid():
		_back_callback.call()

func _on_victory_restart() -> void:
	if not _victory_outcome_modal.visible:
		return
	_close_victory_modal()
	_render_status_only("Victory outcome resolved with Restart.")
	_restart_runtime_state()

func _on_defeat_return_to_main_menu() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_close_defeat_modal()
	_render_status_only("Defeat outcome resolved with Return to Main Menu.")
	_reset_runtime_state()
	if _back_callback.is_valid():
		_back_callback.call()

func _on_defeat_restart() -> void:
	if not _defeat_outcome_modal.visible:
		return
	_close_defeat_modal()
	_render_status_only("Defeat outcome resolved with Restart.")
	_restart_runtime_state()

func _reset_runtime_state() -> void:
	if _operation_controller != null:
		_operation_controller.call("reset_flags")

func _restart_runtime_state() -> void:
	_reset_runtime_state()
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
	if _feedback_controller != null:
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

