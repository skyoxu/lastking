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
	var menu := _screen.get_node_or_null("/root/Main/RuntimeUi/MainMenu")
	if menu != null and menu.has_method("ShowMenu"):
		menu.call("ShowMenu")
	var nav := _screen.get_node_or_null("/root/Main/ScreenNavigator")
	if nav != null and nav.has_method("ClearCurrentScreen"):
		nav.call("ClearCurrentScreen")
