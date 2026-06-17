extends Node

const DAY_DURATION_SECONDS: float = 240.0
const NIGHT_DURATION_SECONDS: float = 120.0

var _bridge_provider: Callable
var _presentation_controller: Node = null
var _outcome_controller: Node = null
var _feedback_controller: Node = null
var _operation_controller: Node = null
var _screen: Control = null
var _hud: Node = null
var _day_night_loop: Node = null
var _last_summary_signature: String = ""
var _last_status_text: String = ""
var _last_resources_text: String = ""
var _last_enemies_text: String = ""
var _last_day: int = -1
var _last_is_day: bool = true
var _last_remaining_bucket: int = -1

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
	if _presentation_controller != null and _presentation_controller.has_method("sync_locale_and_texts"):
		_presentation_controller.call("sync_locale_and_texts")
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager != null and manager.has_method("SetOneX"):
		manager.call("SetOneX")
	var bridge: Node = _current_bridge()
	if bridge != null and bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
	if _operation_controller != null and _operation_controller.has_method("reset_flags"):
		_operation_controller.call("reset_flags")
	if _operation_controller != null and _operation_controller.has_method("apply_runtime_spawn_config"):
		_operation_controller.call("apply_runtime_spawn_config")
	if _day_night_loop != null:
		_day_night_loop.set("PauseLoop", false)
	if _outcome_controller != null and _outcome_controller.has_method("close_all"):
		_outcome_controller.call("close_all")
	if _feedback_controller != null and _feedback_controller.has_method("clear_spawn_pulse"):
		_feedback_controller.call("clear_spawn_pulse")
	_last_summary_signature = ""
	_last_status_text = ""
	_last_resources_text = ""
	_last_enemies_text = ""
	_last_day = -1
	_last_is_day = true
	_last_remaining_bucket = -1
	if _feedback_controller != null and _feedback_controller.has_method("render_loaded_summary"):
		_feedback_controller.call("render_loaded_summary", _translate_or_fallback("battlemap.status.loaded"))
	_sync_battle_hud_runtime_state()

func process_runtime_frame(delta: float) -> void:
	if _presentation_controller != null and _presentation_controller.has_method("sync_locale_and_texts"):
		_presentation_controller.call("sync_locale_and_texts")
	var bridge: Node = _current_bridge()
	var probe_wave_frame: bool = _operation_controller != null and _operation_controller.has_method("consume_post_wave_probe_tick") and _operation_controller.call("consume_post_wave_probe_tick") == true
	if bridge != null and bridge.has_method("AdvanceSimulation"):
		bridge.call("AdvanceSimulation", delta)
	if _day_night_loop != null and _day_night_loop.has_method("SimulateProcessStep"):
		var paused: bool = _is_runtime_paused()
		_day_night_loop.set("PauseLoop", paused)
		if not paused:
			_day_night_loop.call("SimulateProcessStep", delta)
	_sync_battle_hud_runtime_state()
	if _outcome_controller != null and _outcome_controller.has_method("sync_terminal_outcome_from_bridge"):
		var wave_started: bool = _operation_controller != null and _operation_controller.has_method("is_wave_started") and _operation_controller.call("is_wave_started") == true
		_outcome_controller.call("sync_terminal_outcome_from_bridge", wave_started)
	if _feedback_controller != null and _feedback_controller.has_method("process_frame"):
		_feedback_controller.call("process_frame", delta)

func _sync_battle_hud_runtime_state() -> void:
	if _hud == null:
		_hud = _resolve_local_hud()
	if _hud == null:
		return
	var bridge: Node = _current_bridge()
	var summary_dict: Dictionary = {}
	if bridge != null and bridge.has_method("GetSummary"):
		var summary: Variant = bridge.call("GetSummary")
		if summary is Dictionary:
			summary_dict = summary as Dictionary
	if not summary_dict.is_empty():
		var status_text: String = _resolve_runtime_status_text()
		if _hud.has_method("SetHealth"):
			_hud.call("SetHealth", int(summary_dict.get("castle_hp", 100)))
		if _hud.has_method("SetBattleWallHp"):
			_hud.call("SetBattleWallHp", int(summary_dict.get("wall_hp", 100)))
		_sync_direct_hud_runtime_labels(summary_dict)
		var signature: String = JSON.stringify(summary_dict)
		if _feedback_controller != null and _feedback_controller.has_method("render_summary") and (signature != _last_summary_signature or status_text != _last_status_text):
			_feedback_controller.call("render_summary", summary_dict, status_text)
			_last_summary_signature = signature
			_last_status_text = status_text
	if _day_night_loop == null:
		return
	var current_day: int = int(_day_night_loop.get("CurrentDay"))
	var is_day: bool = _day_night_loop.get("IsDayPhase") == true
	var elapsed: float = float(_day_night_loop.get("CurrentPhaseElapsedSeconds"))
	var duration: float = DAY_DURATION_SECONDS if is_day else NIGHT_DURATION_SECONDS
	var remaining: float = max(0.0, duration - elapsed)
	var remaining_bucket: int = int(round(remaining * 10.0))
	if _hud.has_method("SyncRuntimePhase") and (current_day != _last_day or is_day != _last_is_day or remaining_bucket != _last_remaining_bucket):
		_hud.call("SyncRuntimePhase", current_day, is_day, remaining)
		_last_day = current_day
		_last_is_day = is_day
		_last_remaining_bucket = remaining_bucket

func _sync_direct_hud_runtime_labels(summary: Dictionary) -> void:
	var resources_label: Label = _hud.get_node_or_null("TopBar/HBox/ResourcesLabel") as Label
	var resources_text := "%s: %s %d | %s %d | %s %d" % [
		_translate_or_fallback("hud.resources"),
		_translate_or_fallback("battlemap.resource.gold"),
		int(summary.get("resource_gold", 0)),
		_translate_or_fallback("battlemap.resource.iron"),
		int(summary.get("resource_iron", 0)),
		_translate_or_fallback("battlemap.resource.population"),
		int(summary.get("resource_population_cap", 0)),
	]
	if resources_label != null:
		if resources_text != _last_resources_text:
			resources_label.text = resources_text
			_last_resources_text = resources_text
	var enemies_label: Label = _hud.get_node_or_null("TopBar/HBox/EnemiesLabel") as Label
	var enemies_text := "%s: %d" % [
		_translate_or_fallback("hud.enemies"),
		int(summary.get("enemy_units_spawned", 0)),
	]
	if enemies_label != null:
		if enemies_text != _last_enemies_text:
			enemies_label.text = enemies_text
			_last_enemies_text = enemies_text

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

func _translate_or_fallback(key: String) -> String:
	if _presentation_controller != null and _presentation_controller.has_method("translate"):
		return str(_presentation_controller.call("translate", key))
	return key

func _resolve_runtime_status_text() -> String:
	if _screen != null:
		var status_label: Label = _screen.get_node_or_null("LegacyPrototypeRoot/VBox/Status") as Label
		if status_label != null:
			var current := String(status_label.text).strip_edges()
			if not current.is_empty():
				return current
	return _translate_or_fallback("battlemap.status.loaded")
