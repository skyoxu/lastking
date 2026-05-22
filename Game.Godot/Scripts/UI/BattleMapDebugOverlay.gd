extends Control

const DEBUG_OVERLAY_ENV := "LASTKING_BATTLEMAP_DEBUG_OVERLAY"

@onready var _scene_label: Label = $Panel/VBox/SceneLabel
@onready var _hover_label: Label = $Panel/VBox/HoverLabel
@onready var _path_label: Label = $Panel/VBox/PathLabel
@onready var _tower_target_label: Label = $Panel/VBox/TowerTargetLabel
@onready var _tower_target_hp_label: Label = $Panel/VBox/TowerTargetHpLabel
@onready var _tower_shots_label: Label = $Panel/VBox/TowerShotsLabel
@onready var _tower_enemy_count_label: Label = $Panel/VBox/TowerEnemyCountLabel
@onready var _tower_nodes_label: Label = $Panel/VBox/TowerNodesLabel
@onready var _tower_details_label: Label = $Panel/VBox/TowerDetailsLabel

var _hovered_node: Node = null
var _manual_override_frames: int = 0
var _battle_map_session_active: bool = false
var _hidden_by_hotkey: bool = false
var _hotkey_pressed_last_frame: bool = false
var _debug_overlay_enabled: bool = false


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	visible = false
	_debug_overlay_enabled = _is_debug_overlay_enabled()
	set_process(_debug_overlay_enabled)
	if not _debug_overlay_enabled:
		_set_inactive_state()
		return
	_refresh_labels()


func _process(_delta: float) -> void:
	if not _debug_overlay_enabled:
		return
	var current_screen: Node = _current_screen()
	var battle_map_active: bool = current_screen != null and str(current_screen.name) == "BattleMapScreen"
	if not battle_map_active:
		_set_inactive_state()
		return

	if not _battle_map_session_active:
		_battle_map_session_active = true
		_hidden_by_hotkey = false
		visible = true

	var hotkey_pressed: bool = Input.is_key_pressed(KEY_F3)
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
	_refresh_tower_debug()


func DebugSetHoveredNode(node: Node) -> void:
	_hovered_node = node
	_manual_override_frames = 2
	_refresh_labels()


func _update_hover_from_mouse() -> void:
	var viewport: Viewport = get_viewport()
	if viewport == null:
		return
	var hovered: Control = viewport.gui_get_hovered_control()
	if hovered == null:
		return
	if hovered == self or hovered == $Panel or hovered == $Panel/VBox:
		return
	_hovered_node = hovered


func _refresh_labels() -> void:
	var current_screen: Node = _current_screen()
	var scene_name := "n/a"
	if current_screen != null:
		if str(current_screen.name) == "BattleMapScreen":
			scene_name = "BattleMapScreen"
		else:
			var scene_root := current_screen.get_parent()
			if scene_root != null and str(scene_root.name) == "ScreenRoot":
				scene_name = "BattleMapScreen"
			else:
				scene_name = str(current_screen.name)
	_scene_label.text = "Scene: %s" % scene_name
	if _hovered_node == null:
		_hover_label.text = "Element: n/a"
		_path_label.text = "Node: n/a"
	else:
		_hover_label.text = "Element: %s" % str(_hovered_node.name)
		_path_label.text = "Node: %s" % _relative_path_for(_hovered_node)

func _refresh_tower_debug() -> void:
	var screen := _current_screen()
	if screen == null or str(screen.name) != "BattleMapScreen":
		_tower_target_label.text = "TowerTarget: n/a"
		_tower_target_hp_label.text = "TargetHp: n/a"
		_tower_shots_label.text = "Shots: 0"
		_tower_enemy_count_label.text = "ActiveEnemies: 0"
		_tower_nodes_label.text = "TowerNodes: n/a"
		_tower_details_label.text = "TowerDetails: n/a"
		return
	var bridge: Node = screen.get_node_or_null("CombatExperienceRuntimeBridge")
	if bridge == null or not bridge.has_method("GetTowerCombatDebugSnapshot"):
		_tower_target_label.text = "TowerTarget: n/a"
		_tower_target_hp_label.text = "TargetHp: n/a"
		_tower_shots_label.text = "Shots: 0"
		_tower_enemy_count_label.text = "ActiveEnemies: 0"
		_tower_nodes_label.text = "TowerNodes: n/a"
		_tower_details_label.text = "TowerDetails: n/a"
		return
	var snapshot: Variant = bridge.call("GetTowerCombatDebugSnapshot")
	if snapshot is Dictionary:
		var data := snapshot as Dictionary
		_tower_target_label.text = "TowerTarget: %s" % str(data.get("target_name", "n/a"))
		var hp_value := int(data.get("target_hp", -1))
		_tower_target_hp_label.text = "TargetHp: %s" % ("n/a" if hp_value < 0 else str(hp_value))
		_tower_shots_label.text = "Shots: %d" % int(data.get("projectiles_created", 0))
		_tower_enemy_count_label.text = "ActiveEnemies: %d" % int(data.get("active_enemy_count", 0))
		var tower_nodes: Variant = data.get("tower_nodes", [])
		var labels: Array[String] = []
		if tower_nodes is Array:
			for item in tower_nodes:
				labels.append(str(item))
		_tower_nodes_label.text = "TowerNodes: %s" % (", ".join(labels) if not labels.is_empty() else "n/a")
		var tower_targets: Variant = data.get("tower_targets", [])
		var detail_lines: Array[String] = []
		if tower_targets is Array:
			for item in tower_targets:
				var entry := item as Dictionary
				if entry == null:
					continue
				var tower_name := str(entry.get("tower_name", "tower"))
				var target_name := str(entry.get("target_name", "n/a"))
				var cooldown_seconds := float(entry.get("cooldown_seconds", 0.0))
				var range_px := float(entry.get("range_px", 0.0))
				var attack_damage := int(entry.get("attack_damage", 0))
				var attack_interval_seconds := float(entry.get("attack_interval_seconds", 0.0))
				var targeting_mode := str(entry.get("targeting_mode", "n/a"))
				detail_lines.append(
					"%s -> %s (cd %.2fs | rng %.0f | dmg %d | int %.2fs | mode %s)"
					% [tower_name, target_name, cooldown_seconds, range_px, attack_damage, attack_interval_seconds, targeting_mode]
				)
		_tower_details_label.text = "TowerDetails: %s" % ("\n".join(detail_lines) if not detail_lines.is_empty() else "n/a")


func _current_screen() -> Node:
	var main: Node = _main_root()
	if main != null:
		var screen_root: Node = main.get_node_or_null("RuntimeUi/ScreenRoot")
		if screen_root != null:
			for child in screen_root.get_children():
				if child is Node and str(child.name) == "BattleMapScreen":
					return child
			if screen_root.get_child_count() > 0:
				return screen_root.get_child(0)
	var direct_screen: Node = _battle_map_root()
	if direct_screen != null:
		return direct_screen
	return null


func _relative_path_for(node: Node) -> String:
	var main: Node = _main_root()
	if main == null:
		var battle_map: Node = _battle_map_root()
		if battle_map != null:
			return str(battle_map.get_path_to(node))
		return str(node.get_path())
	return str(main.get_path_to(node))


func _main_root() -> Node:
	var current: Node = self
	while current != null:
		if str(current.name) == "Main":
			return current
		current = current.get_parent()
	return null


func _battle_map_root() -> Node:
	var current: Node = self
	while current != null:
		if str(current.name) == "BattleMapScreen":
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


func _is_debug_overlay_enabled() -> bool:
	if not OS.has_environment(DEBUG_OVERLAY_ENV):
		return true
	var raw: String = OS.get_environment(DEBUG_OVERLAY_ENV).strip_edges().to_lower()
	return not (raw == "0" or raw == "false" or raw == "no" or raw == "off")
