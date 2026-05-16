extends Control

@onready var _scene_label: Label = $Panel/VBox/SceneLabel
@onready var _hover_label: Label = $Panel/VBox/HoverLabel
@onready var _path_label: Label = $Panel/VBox/PathLabel

var _hovered_node: Node = null
var _manual_override_frames := 0
var _battle_map_session_active := false
var _hidden_by_hotkey := false
var _hotkey_pressed_last_frame := false


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	visible = false
	_refresh_labels()


func _process(_delta: float) -> void:
	var current_screen := _current_screen()
	var battle_map_active := current_screen != null and String(current_screen.name) == "BattleMapScreen"
	if not battle_map_active:
		_set_inactive_state()
		return

	if not _battle_map_session_active:
		_battle_map_session_active = true
		_hidden_by_hotkey = false
		visible = true

	var hotkey_pressed := Input.is_key_pressed(KEY_F3)
	if hotkey_pressed and not _hotkey_pressed_last_frame:
		_hidden_by_hotkey = not _hidden_by_hotkey
	_hotkey_pressed_last_frame = hotkey_pressed
	visible = not _hidden_by_hotkey

	if not visible:
		_refresh_labels()
		return

	if _manual_override_frames > 0:
		_manual_override_frames -= 1
	else:
		_update_hover_from_mouse()
	_refresh_labels()


func DebugSetHoveredNode(node: Node) -> void:
	_hovered_node = node
	_manual_override_frames = 2
	_refresh_labels()


func _update_hover_from_mouse() -> void:
	var viewport := get_viewport()
	if viewport == null:
		return
	var hovered := viewport.gui_get_hovered_control()
	if hovered == null:
		return
	if hovered == self or hovered == $Panel or hovered == $Panel/VBox:
		return
	_hovered_node = hovered


func _refresh_labels() -> void:
	var current_screen := _current_screen()
	_scene_label.text = "Scene: %s" % (String(current_screen.name) if current_screen != null else "n/a")
	if _hovered_node == null:
		_hover_label.text = "Element: n/a"
		_path_label.text = "Node: n/a"
		return
	_hover_label.text = "Element: %s" % String(_hovered_node.name)
	_path_label.text = "Node: %s" % _relative_path_for(_hovered_node)


func _current_screen() -> Node:
	var main := _main_root()
	if main != null:
		var screen_root := main.get_node_or_null("RuntimeUi/ScreenRoot")
		if screen_root != null and screen_root.get_child_count() > 0:
			return screen_root.get_child(0)
	var direct_screen := _battle_map_root()
	if direct_screen != null:
		return direct_screen
	return null


func _relative_path_for(node: Node) -> String:
	var main := _main_root()
	if main == null:
		var battle_map := _battle_map_root()
		if battle_map != null:
			return String(battle_map.get_path_to(node))
		return String(node.get_path())
	return String(main.get_path_to(node))


func _main_root() -> Node:
	var current: Node = self
	while current != null:
		if String(current.name) == "Main":
			return current
		current = current.get_parent()
	return null


func _battle_map_root() -> Node:
	var current: Node = self
	while current != null:
		if String(current.name) == "BattleMapScreen":
			return current
		current = current.get_parent()
	return null


func _set_inactive_state() -> void:
	_battle_map_session_active = false
	_hidden_by_hotkey = false
	_hotkey_pressed_last_frame = false
	_hovered_node = null
	visible = false
	_refresh_labels()
