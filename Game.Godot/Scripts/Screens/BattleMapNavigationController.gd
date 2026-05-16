extends Node

var _screen: Control = null
var _back_btn: Button = null

func configure(refs: Dictionary) -> void:
	_screen = refs["screen"]
	_back_btn = refs["back_btn"]

func connect_signals() -> void:
	if _back_btn != null and not _back_btn.pressed.is_connected(navigate_back_to_main_menu):
		_back_btn.pressed.connect(navigate_back_to_main_menu)

func navigate_back_to_main_menu() -> void:
	if _screen == null:
		return
	var hud := _resolve_main_node("RuntimeUi/HUD")
	_set_hud_active(hud, false)
	var local_hud := _screen.get_node_or_null("BattleHud")
	_set_hud_active(local_hud, false)
	var menu := _resolve_main_node("RuntimeUi/MainMenu")
	if menu != null and menu.has_method("ShowMenu"):
		menu.call("ShowMenu")
	var nav := _resolve_main_node("ScreenNavigator")
	if nav != null and nav.has_method("ClearCurrentScreen"):
		nav.call("ClearCurrentScreen")

func _resolve_main_node(relative_path: String) -> Node:
	if _screen == null:
		return null
	var current: Node = _screen
	while current != null:
		if String(current.name) == "Main":
			return current.get_node_or_null(relative_path)
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
