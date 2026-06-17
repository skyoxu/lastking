extends Node

const SPAWN_PULSE_DURATION_SEC: float = 4.0

var _bridge: Node = null
var _bridge_provider: Callable
var _wave_timer: Timer = null
var _translate: Callable
var _feedback_controller: Node = null
var _outcome_controller: Node = null

var _wave_started: bool = false
var _combat_resolved: bool = false
var _cleaned: bool = false
var _auto_wave: bool = false
var _outcome_published: bool = false
var _post_wave_probe_frames: int = 0

func configure(refs: Dictionary) -> void:
	_bridge = refs["bridge"]
	_bridge_provider = refs["bridge_provider"]
	_wave_timer = refs["wave_timer"]
	_translate = refs["translate"]
	_feedback_controller = refs["feedback_controller"]
	_outcome_controller = refs["outcome_controller"]

func connect_signals() -> void:
	if _wave_timer != null and not _wave_timer.timeout.is_connected(_on_wave_timer_timeout_signal):
		_wave_timer.timeout.connect(_on_wave_timer_timeout_signal)

func cleanup() -> void:
	if _wave_timer != null:
		if _wave_timer.timeout.is_connected(_on_wave_timer_timeout_signal):
			_wave_timer.timeout.disconnect(_on_wave_timer_timeout_signal)
		_wave_timer.stop()

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

func apply_runtime_spawn_config() -> void:
	var bridge: Node = _current_bridge()
	if bridge == null:
		return
	if _wave_timer != null and bridge.has_method("GetSpawnCadenceSeconds"):
		var cadence_seconds: int = max(1, int(bridge.call("GetSpawnCadenceSeconds")))
		_wave_timer.wait_time = float(cadence_seconds)
	if bridge.has_method("IsAutoSpawnEnabled"):
		_auto_wave = bridge.call("IsAutoSpawnEnabled") == true
		if _wave_timer != null:
			if _auto_wave:
				_wave_timer.start()
			else:
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
		_render_status_only(_t("battlemap.status.settlement_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.resolve_settlement_before_continue"))
		return
	if _is_terminal_visible():
		_render_status_only(_t("battlemap.status.terminal_outcome_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.close_terminal_before_wave"))
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
	if _post_wave_probe_enabled():
		_post_wave_probe_frames = 5

func consume_post_wave_probe_tick() -> bool:
	if _post_wave_probe_frames <= 0:
		return false
	_post_wave_probe_frames -= 1
	return true

func _post_wave_probe_enabled() -> bool:
	if not OS.has_environment("LASTKING_BATTLEMAP_ENABLE_POST_WAVE_PROBE"):
		return false
	var raw := OS.get_environment("LASTKING_BATTLEMAP_ENABLE_POST_WAVE_PROBE").strip_edges().to_lower()
	return raw == "1" or raw == "true" or raw == "yes"

func on_auto_wave() -> void:
	_auto_wave = not _auto_wave
	if _auto_wave:
		_wave_timer.start()
		_render_status_only(_t("battlemap.status.auto_on"))
	else:
		_wave_timer.stop()
		_render_status_only(_t("battlemap.status.auto_off"))

func on_wave_timer_timeout() -> void:
	if _auto_wave and int(_try_bridge_summary().get("castle_hp", 0)) > 0:
		on_wave()

func on_exchange() -> void:
	if _is_settlement_open():
		_render_status_only(_t("battlemap.status.settlement_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.resolve_settlement_before_exchange"))
		return
	if _is_terminal_visible():
		_render_status_only(_t("battlemap.status.terminal_outcome_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.outcome_blocks_exchange"))
		return
	if not _wave_started:
		_render_status_only(_t("battlemap.status.require_wave"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.spawn_wave_before_exchange"))
		return
	_combat_resolved = true
	var summary: Dictionary = _call_or_fallback("ResolveCombatExchangePhase")
	_render(summary, _t("battlemap.status.exchange_done"))
	_sync(summary)

func on_cleanup() -> void:
	if _is_settlement_open():
		_render_status_only(_t("battlemap.status.settlement_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.resolve_settlement_before_cleanup"))
		return
	if _is_terminal_visible():
		_render_status_only(_t("battlemap.status.terminal_outcome_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.outcome_blocks_cleanup"))
		return
	if not _combat_resolved:
		_render_status_only(_t("battlemap.status.require_exchange"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.resolve_exchange_before_cleanup"))
		return
	_cleaned = true
	var summary: Dictionary = _call_or_fallback("CleanupDeadUnitsPhase")
	_render(summary, _t("battlemap.status.cleanup_done"))
	_sync(summary)

func on_finish() -> void:
	if _is_settlement_open():
		_render_status_only(_t("battlemap.status.settlement_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.resolve_settlement_before_finish"))
		return
	if _is_terminal_visible():
		_render_status_only(_t("battlemap.status.terminal_outcome_open"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.terminal_already_open"))
		return
	if not _cleaned:
		_render_status_only(_t("battlemap.status.require_cleanup"))
		if _feedback_controller != null:
			_feedback_controller.call("show_local_prompt", _t("battlemap.prompt.cleanup_before_finish"))
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

func _render_status_only(status_text: String) -> void:
	if _feedback_controller != null:
		_feedback_controller.call("render_status_only", status_text)

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
