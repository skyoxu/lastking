extends Node

var _screen: Control = null
var _hud: Node = null
var _operation_controller: Node = null
var _navigation_controller: Node = null

func configure(refs: Dictionary) -> void:
	_screen = refs.get("screen", null)
	_operation_controller = refs.get("operation_controller", null)
	_navigation_controller = refs.get("navigation_controller", null)
	_hud = refs.get("hud", null)

func connect_signals() -> void:
	if _hud == null:
		_hud = _resolve_hud()
	if _hud == null:
		return
	var callable := Callable(self, "_on_battle_action_requested")
	if not _hud.is_connected("BattleActionRequested", callable):
		_hud.connect("BattleActionRequested", callable)

func _on_battle_action_requested(action_code: String) -> void:
	route_action(action_code)

func route_action(action_code: String) -> void:
	match action_code:
		"build":
			if _operation_controller != null:
				_operation_controller.call("on_build")
		"wave":
			if _operation_controller != null:
				_operation_controller.call("on_wave")
		"exchange":
			if _operation_controller != null:
				_operation_controller.call("on_exchange")
		"cleanup":
			if _operation_controller != null:
				_operation_controller.call("on_cleanup")
		"finish":
			if _operation_controller != null:
				_operation_controller.call("on_finish")
		"back":
			if _navigation_controller != null:
				_navigation_controller.call("navigate_back_to_main_menu")

func _resolve_hud() -> Node:
	if _screen == null:
		return null
	return _screen.get_node_or_null("/root/Main/RuntimeUi/HUD")
