extends Node

var _screen: Control = null
var _hud: Node = null
var _operation_controller: Node = null
var _navigation_controller: Node = null
var _uses_global_hud := false

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
	_uses_global_hud = _is_global_hud(_hud)
	_set_hud_active(_hud, true)
	var local_hud := _screen.get_node_or_null("BattleHud")
	if local_hud != null and local_hud != _hud:
		_set_hud_active(local_hud, false)

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
	var global_hud := _resolve_runtime_ui_node("HUD")
	if global_hud != null:
		return global_hud
	return _screen.get_node_or_null("BattleHud")

func _is_global_hud(candidate: Node) -> bool:
	if candidate == null:
		return false
	return String(candidate.get_path()).find("/RuntimeUi/HUD") >= 0

func _resolve_runtime_ui_node(relative_path: String) -> Node:
	if _screen == null:
		return null
	var current: Node = _screen
	while current != null:
		if String(current.name) == "Main":
			return current.get_node_or_null("RuntimeUi/%s" % relative_path)
		current = current.get_parent()
	return null

func _set_hud_active(hud: Node, active: bool) -> void:
	if hud == null or not (hud is CanvasItem):
		return
	(hud as CanvasItem).visible = active
	if hud.has_node("TopBar"):
		var top_bar := hud.get_node("TopBar")
		if top_bar is CanvasItem:
			(top_bar as CanvasItem).visible = active
	if hud.has_node("CombatHud/BottomBar"):
		var bottom_bar := hud.get_node("CombatHud/BottomBar")
		if bottom_bar is CanvasItem:
			(bottom_bar as CanvasItem).visible = active
	if hud.has_node("FeedbackLayer"):
		var feedback_layer := hud.get_node("FeedbackLayer")
		if feedback_layer is CanvasItem:
			(feedback_layer as CanvasItem).visible = active
	if hud is Control:
		(hud as Control).mouse_filter = Control.MOUSE_FILTER_PASS if active else Control.MOUSE_FILTER_IGNORE
