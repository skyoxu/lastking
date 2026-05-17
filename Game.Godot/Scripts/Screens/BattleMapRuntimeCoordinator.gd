extends Node

const DAY_DURATION_SECONDS := 240.0
const NIGHT_DURATION_SECONDS := 120.0

var _bridge_provider: Callable
var _presentation_controller: Node = null
var _outcome_controller: Node = null
var _feedback_controller: Node = null
var _operation_controller: Node = null
var _screen: Control = null
var _hud: Node = null
var _day_night_loop: Node = null

func configure(refs: Dictionary) -> void:
	_bridge_provider = refs["bridge_provider"]
	_presentation_controller = refs["presentation_controller"]
	_outcome_controller = refs["outcome_controller"]
	_feedback_controller = refs["feedback_controller"]
	_operation_controller = refs["operation_controller"]
	_screen = refs.get("screen", null)
	_hud = refs.get("hud", null)
	_day_night_loop = refs.get("day_night_loop", null)

func initialize_runtime() -> void:
	_presentation_controller.call("sync_locale_and_texts")
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager != null and manager.has_method("SetOneX"):
		manager.call("SetOneX")
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
	if _operation_controller != null and _operation_controller.has_method("reset_flags"):
		_operation_controller.call("reset_flags")
	if _day_night_loop != null:
		_day_night_loop.set("PauseLoop", false)
	_outcome_controller.call("close_all")
	_feedback_controller.call("clear_spawn_pulse")
	_feedback_controller.call("render_loaded_summary", _presentation_controller.call("translate", "battlemap.status.loaded"))
	_sync_battle_hud_runtime_state()

func process_runtime_frame(delta: float) -> void:
	_presentation_controller.call("sync_locale_and_texts")
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("AdvanceSimulation"):
		bridge.call("AdvanceSimulation", delta)
	if _day_night_loop != null and _day_night_loop.has_method("SimulateProcessStep"):
		var paused: bool = _is_runtime_paused()
		_day_night_loop.set("PauseLoop", paused)
		if not paused:
			_day_night_loop.call("SimulateProcessStep", delta)
	_sync_battle_hud_runtime_state()
	_outcome_controller.call("sync_terminal_outcome_from_bridge", _operation_controller.call("is_wave_started"))
	_feedback_controller.call("process_frame", delta)

func _sync_battle_hud_runtime_state() -> void:
	if _hud == null:
		_hud = _resolve_local_hud()
	if _hud == null:
		return
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("GetSummary") and _hud.has_method("SetHealth"):
		var summary: Variant = bridge.call("GetSummary")
		if summary is Dictionary:
			_hud.call("SetHealth", int((summary as Dictionary).get("castle_hp", 100)))
	if _day_night_loop == null:
		return
	var current_day: int = int(_day_night_loop.get("CurrentDay"))
	var is_day: bool = _day_night_loop.get("IsDayPhase") == true
	var elapsed: float = float(_day_night_loop.get("CurrentPhaseElapsedSeconds"))
	var duration: float = DAY_DURATION_SECONDS if is_day else NIGHT_DURATION_SECONDS
	var remaining: float = max(0.0, duration - elapsed)
	if _hud.has_method("SyncRuntimePhase"):
		_hud.call("SyncRuntimePhase", current_day, is_day, remaining)

func _is_runtime_paused() -> bool:
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager == null or not manager.has_method("GetSpeedState"):
		return false
	var state: Variant = manager.call("GetSpeedState")
	if state is Dictionary:
		return (state as Dictionary).get("is_paused", false) == true
	return false

func _resolve_local_hud() -> Node:
	if _screen == null:
		return null
	return _screen.get_node_or_null("BattleHud")

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null

