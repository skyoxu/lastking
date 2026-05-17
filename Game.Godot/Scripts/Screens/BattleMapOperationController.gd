extends Node

const SPAWN_PULSE_DURATION_SEC := 4.0

var _bridge: Node = null
var _bridge_provider: Callable
var _wave_timer: Timer = null
var _status_label: Label = null
var _translate: Callable
var _feedback_controller: Node = null
var _outcome_controller: Node = null

var _wave_started: bool = false
var _combat_resolved: bool = false
var _cleaned: bool = false
var _auto_wave: bool = false
var _outcome_published: bool = false

func configure(refs: Dictionary) -> void:
	_bridge = refs["bridge"]
	_bridge_provider = refs["bridge_provider"]
	_wave_timer = refs["wave_timer"]
	_status_label = refs["status"]
	_translate = refs["translate"]
	_feedback_controller = refs["feedback_controller"]
	_outcome_controller = refs["outcome_controller"]

func connect_signals() -> void:
	if _wave_timer != null and not _wave_timer.timeout.is_connected(_on_wave_timer_timeout_signal):
		_wave_timer.timeout.connect(_on_wave_timer_timeout_signal)

func is_wave_started() -> bool:
	return _wave_started

func is_combat_resolved() -> bool:
	return _combat_resolved

func is_cleanup_completed() -> bool:
	return _cleaned

func is_auto_wave() -> bool:
	return _auto_wave

func get_hud_phase_statuses() -> Dictionary:
	var exchange_ready: bool = _wave_started
	var cleanup_ready: bool = _wave_started
	var finish_ready: bool = _combat_resolved or _cleaned
	return {
		"build_status": "Ready",
		"wave_status": "Ready",
		"exchange_status": "Done" if _combat_resolved else ("Ready" if exchange_ready else "Locked"),
		"cleanup_status": "Done" if _cleaned else ("Ready" if cleanup_ready else "Locked"),
		"finish_status": "Done" if _outcome_published else ("Ready" if finish_ready else "Locked"),
		"build_available": true,
		"wave_available": true,
		"exchange_available": exchange_ready,
		"cleanup_available": cleanup_ready,
		"finish_available": finish_ready or _outcome_published,
	}

func reset_flags() -> void:
	_wave_started = false
	_combat_resolved = false
	_cleaned = false
	_auto_wave = false
	_outcome_published = false
	if _wave_timer != null:
		_wave_timer.stop()

func on_build() -> void:
	var bridge: Node = _current_bridge()
	if bridge.has_method("BuildPhase"):
		bridge.call("BuildPhase")
	if bridge.has_method("TrainFriendlyUnitPhase"):
		bridge.call("TrainFriendlyUnitPhase")
	var summary: Dictionary = _try_bridge_summary()
	var friendly: int = int(summary.get("friendly_units_deployed", 0))
	_render(summary, "%s (%s=%d)" % [_t("battlemap.status.build_ready"), _t("battlemap.summary.friendly_units"), friendly])
	_sync(summary)

func on_wave() -> void:
	if _is_settlement_open():
		_status_label.text = "Settlement modal is open; resolve reward first."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Resolve settlement reward before continuing.")
		return
	if _is_terminal_visible():
		_status_label.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Close terminal outcome before starting a new wave.")
		return
	_wave_started = true
	_combat_resolved = false
	_cleaned = false
	_outcome_published = false
	if _feedback_controller != null:
		_feedback_controller.call("mark_spawn_pulse", SPAWN_PULSE_DURATION_SEC)
	var summary: Dictionary = _call_or_fallback("SpawnEnemyWavePhase")
	_render(summary, _t("battlemap.status.wave_spawned"))
	_sync(summary)

func on_auto_wave() -> void:
	_auto_wave = not _auto_wave
	if _auto_wave:
		_wave_timer.start()
		_status_label.text = _t("battlemap.status.auto_on")
	else:
		_wave_timer.stop()
		_status_label.text = _t("battlemap.status.auto_off")

func on_wave_timer_timeout() -> void:
	if _auto_wave and int(_try_bridge_summary().get("castle_hp", 0)) > 0:
		on_wave()

func on_exchange() -> void:
	if _is_settlement_open():
		_status_label.text = "Settlement modal is open; resolve reward first."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Resolve settlement reward before combat exchange.")
		return
	if _is_terminal_visible():
		_status_label.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Outcome modal blocks combat exchange.")
		return
	if not _wave_started:
		_status_label.text = _t("battlemap.status.require_wave")
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Spawn a wave before resolving combat.")
		return
	_combat_resolved = true
	var summary: Dictionary = _call_or_fallback("ResolveCombatExchangePhase")
	_render(summary, _t("battlemap.status.exchange_done"))
	_sync(summary)

func on_cleanup() -> void:
	if _is_settlement_open():
		_status_label.text = "Settlement modal is open; resolve reward first."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Resolve settlement reward before cleanup.")
		return
	if _is_terminal_visible():
		_status_label.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Outcome modal blocks cleanup.")
		return
	if not _combat_resolved:
		_status_label.text = _t("battlemap.status.require_exchange")
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Resolve combat before cleanup.")
		return
	_cleaned = true
	var summary: Dictionary = _call_or_fallback("CleanupDeadUnitsPhase")
	_render(summary, _t("battlemap.status.cleanup_done"))
	_sync(summary)

func on_finish() -> void:
	if _is_settlement_open():
		_status_label.text = "Settlement modal is open; resolve reward first."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Resolve settlement reward before finishing battle.")
		return
	if _is_terminal_visible():
		_status_label.text = "Terminal outcome is open; choose Restart or Return to Main Menu."
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Terminal outcome already open.")
		return
	if not _cleaned:
		_status_label.text = _t("battlemap.status.require_cleanup")
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", "Cleanup dead units before finishing battle.")
		return
	if _feedback_controller != null:
		_feedback_controller.call("clear_spawn_pulse")
	_outcome_published = true
	var outcome: Dictionary = _call_or_fallback("PublishOutcomePhase")
	_render(outcome, _t("battlemap.status.finished"))
	if _outcome_controller != null:
		_outcome_controller.call("open_outcome", outcome)

func _on_wave_timer_timeout_signal() -> void:
	on_wave_timer_timeout()

func _render(summary: Dictionary, status_text: String) -> void:
	if _feedback_controller != null:
		_feedback_controller.call("render_summary", summary, status_text)

func _sync(summary: Dictionary) -> void:
	if _outcome_controller != null:
		_outcome_controller.call("sync_terminal_outcome_from_runtime", summary, _wave_started)

func _is_settlement_open() -> bool:
	return _outcome_controller.call("is_settlement_open") == true if _outcome_controller != null else false

func _is_terminal_visible() -> bool:
	return _outcome_controller.call("is_terminal_outcome_modal_visible") == true if _outcome_controller != null else false

func _try_bridge_summary() -> Dictionary:
	var bridge: Node = _current_bridge()
	if bridge.has_method("GetSummary"):
		var result: Variant = bridge.call("GetSummary")
		if result is Dictionary:
			return result
	return _call_or_fallback("")

func _call_or_fallback(method_name: String) -> Dictionary:
	var bridge: Node = _current_bridge()
	if not method_name.is_empty() and bridge.has_method(method_name):
		var result: Variant = bridge.call(method_name)
		if result is Dictionary:
			return result
	if bridge.has_method("RunCompleteCombatExperienceForTest"):
		var full: Variant = bridge.call("RunCompleteCombatExperienceForTest")
		if full is Dictionary:
			return full
	return {}

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return _bridge

func _t(key: String) -> String:
	if _translate.is_valid():
		return str(_translate.call(key))
	return key


