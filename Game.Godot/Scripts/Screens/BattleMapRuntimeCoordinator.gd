extends Node

var _bridge_provider: Callable
var _presentation_controller: Node = null
var _outcome_controller: Node = null
var _feedback_controller: Node = null
var _operation_controller: Node = null

func configure(refs: Dictionary) -> void:
	_bridge_provider = refs["bridge_provider"]
	_presentation_controller = refs["presentation_controller"]
	_outcome_controller = refs["outcome_controller"]
	_feedback_controller = refs["feedback_controller"]
	_operation_controller = refs["operation_controller"]

func initialize_runtime() -> void:
	_presentation_controller.call("sync_locale_and_texts")
	var bridge := _current_bridge()
	if bridge != null and bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
	_outcome_controller.call("close_all")
	_feedback_controller.call("clear_spawn_pulse")
	_feedback_controller.call("render_loaded_summary", _presentation_controller.call("translate", "battlemap.status.loaded"))

func process_runtime_frame(delta: float) -> void:
	_presentation_controller.call("sync_locale_and_texts")
	var bridge := _current_bridge()
	if bridge != null and bridge.has_method("AdvanceSimulation"):
		bridge.call("AdvanceSimulation", delta)
	_outcome_controller.call("sync_terminal_outcome_from_bridge", _operation_controller.call("is_wave_started"))
	_feedback_controller.call("process_frame", delta)

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided = _bridge_provider.call()
		if provided is Node:
			return provided
	return null
