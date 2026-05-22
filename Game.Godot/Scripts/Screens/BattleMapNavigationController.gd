extends Node

var _screen: Control = null

func configure(refs: Dictionary) -> void:
	_screen = refs["screen"]

func connect_signals() -> void:
	pass

func navigate_back_to_main_menu() -> void:
	if _screen == null:
		return
	var main_root: Node = _resolve_main_root()
	if main_root == null:
		return
	var hud: Node = _resolve_main_node("RuntimeUi/HUD")
	_set_hud_active(hud, false)
	var local_hud: Node = _screen.get_node_or_null("BattleHud")
	_set_hud_active(local_hud, false)
	var menu: Node = _resolve_main_node("RuntimeUi/MainMenu")
	if menu is Control:
		(menu as Control).mouse_filter = Control.MOUSE_FILTER_STOP
	if menu != null and menu.has_method("ShowMenu"):
		menu.call("ShowMenu")
	var nav: Node = _resolve_main_node("ScreenNavigator")
	if nav != null and nav.has_method("ClearCurrentScreen"):
		nav.call("ClearCurrentScreen")

func _resolve_main_root() -> Node:
	if _screen == null:
		return null
	var current: Node = _screen
	while current != null:
		if str(current.name) == "Main":
			return current
		current = current.get_parent()
	return null

func _resolve_main_node(relative_path: String) -> Node:
	var main_root: Node = _resolve_main_root()
	if main_root == null:
		return null
	return main_root.get_node_or_null(relative_path)

func _set_hud_active(hud: Node, active: bool) -> void:
	if hud == null or not (hud is CanvasItem):
		return
	(hud as CanvasItem).visible = active
	if hud.has_node("TopBar"):
		var top_bar: Node = hud.get_node("TopBar")
		if top_bar is CanvasItem:
			(top_bar as CanvasItem).visible = active
	if hud.has_node("CombatHud/BottomBar"):
		var bottom_bar: Node = hud.get_node("CombatHud/BottomBar")
		if bottom_bar is CanvasItem:
			(bottom_bar as CanvasItem).visible = active
	if hud.has_node("FeedbackLayer"):
		var feedback_layer: Node = hud.get_node("FeedbackLayer")
		if feedback_layer is CanvasItem:
			(feedback_layer as CanvasItem).visible = active
	if hud is Control:
		(hud as Control).mouse_filter = Control.MOUSE_FILTER_PASS if active else Control.MOUSE_FILTER_IGNORE



