extends Node

const BUILDING_SLOT_IDS := {
	"MgTower": "InnerCastleRegionSlot_03_00",
	"Barracks": "RightOuterFieldSlot_00_00",
	"Residence": "InnerCastleRegionSlot_06_00",
}
const WALL_LEFT_X := 24.0
const WALL_RIGHT_X := 552.0
const WALL_CENTER_Y := 312.0
const ENEMY_LABEL_THEME := Color(1.0, 0.95, 0.82, 1.0)
const ENEMY_STATE_COLORS := {
	"advancing": Color(0.984314, 0.768627, 0.317647, 0.95),
	"attacking_wall": Color(0.937255, 0.32549, 0.32549, 0.95),
}

var _screen: Control = null
var _bridge_provider: Callable
var _battlefield_view: Node = null
var _map_marker_layer: Control = null
var _path_line: Line2D = null
var _root: Control = null
var _building_markers: Dictionary = {}
var _building_labels: Dictionary = {}
var _enemy_labels: Dictionary = {}
var _wall_markers: Array[ColorRect] = []
var _tower_range_ring: Control = null
var _tower_range_px: float = 0.0

func configure(refs: Dictionary) -> void:
	_screen = refs.get("screen", null)
	_bridge_provider = refs.get("bridge_provider", Callable())
	_battlefield_view = refs.get("battlefield_view", null)
	_map_marker_layer = refs.get("map_marker_layer", null)
	_path_line = refs.get("path_line", null)
	_ensure_root()
	_sync_path_visual()
	_sync_building_markers()
	_sync_wall_markers()
	_sync_tower_range()

func process_frame(_delta: float) -> void:
	if _root == null or _map_marker_layer == null:
		return
	_sync_building_markers()
	_sync_wall_markers()
	_sync_tower_range()
	_sync_enemy_labels()

func _ensure_root() -> void:
	if _map_marker_layer == null:
		return
	if _root != null and is_instance_valid(_root):
		return
	_root = Control.new()
	_root.name = "CombatDebugGeometryLayer"
	_root.set_anchors_preset(Control.PRESET_FULL_RECT)
	_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_root.z_index = 30
	_map_marker_layer.add_child(_root)

func _sync_path_visual() -> void:
	if _path_line == null:
		return
	_path_line.width = 4.0
	_path_line.default_color = Color(0.956863, 0.772549, 0.388235, 0.88)
	_path_line.modulate = Color(1.0, 1.0, 1.0, 0.88)

func _sync_building_markers() -> void:
	if _root == null:
		return
	for marker_name in BUILDING_SLOT_IDS.keys():
		var slot_id := str(BUILDING_SLOT_IDS[marker_name])
		var center := _resolve_slot_center(slot_id)
		var marker := _ensure_building_marker(marker_name)
		marker.position = center - (marker.size / 2.0)
		var label := _ensure_building_label(marker_name)
		label.text = "%s %s" % [marker_name, _format_vec2(center)]
		label.position = center + Vector2(16.0, -18.0)

func _sync_wall_markers() -> void:
	if _root == null:
		return
	if _wall_markers.is_empty():
		for wall_name in ["LeftWall", "RightWall"]:
			var rect := ColorRect.new()
			rect.name = "%sDebugMarker" % wall_name
			rect.size = Vector2(4.0, 624.0)
			rect.color = Color(0.905882, 0.870588, 0.65098, 0.6)
			rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
			rect.z_index = 29
			_root.add_child(rect)
			_wall_markers.append(rect)
	_wall_markers[0].position = Vector2(WALL_LEFT_X - 2.0, 0.0)
	_wall_markers[1].position = Vector2(WALL_RIGHT_X - 2.0, 0.0)

func _sync_tower_range() -> void:
	if _root == null:
		return
	if _tower_range_px <= 0.0:
		var bridge := _current_bridge()
		if bridge != null and bridge.has_method("GetTowerRangePx"):
			_tower_range_px = float(bridge.call("GetTowerRangePx"))
	if _tower_range_px <= 0.0:
		return
	if _tower_range_ring == null or not is_instance_valid(_tower_range_ring):
		_tower_range_ring = Control.new()
		_tower_range_ring.name = "MgTowerRangeRing"
		_tower_range_ring.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_tower_range_ring.z_index = 28
		_tower_range_ring.set_script(load("res://Game.Godot/Scripts/Screens/BattleMapCombatDebugRangeRing.gd"))
		_root.add_child(_tower_range_ring)
	var tower_center := _resolve_slot_center(str(BUILDING_SLOT_IDS["MgTower"]))
	_tower_range_ring.set("center_point", tower_center)
	_tower_range_ring.set("radius_px", _tower_range_px)
	_tower_range_ring.queue_redraw()

func _sync_enemy_labels() -> void:
	var bridge := _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		_clear_enemy_labels()
		return
	var snapshots: Array = bridge.call("GetActorSnapshots")
	var active_enemy_names: Dictionary = {}
	for item in snapshots:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		if snapshot.get("is_moving_enemy", false) != true:
			continue
		if snapshot.get("active", false) != true:
			continue
		var actor_name := str(snapshot.get("name", ""))
		if actor_name.is_empty():
			continue
		active_enemy_names[actor_name] = true
		var label := _ensure_enemy_label(actor_name)
		var world_x := float(snapshot.get("world_x", 0.0))
		var world_y := float(snapshot.get("world_y", 0.0))
		var state := str(snapshot.get("state", "advancing"))
		var hp := int(snapshot.get("hp", 0))
		label.text = "%s  hp:%d  %s  %s" % [actor_name, hp, state, _format_vec2(Vector2(world_x, world_y))]
		label.position = Vector2(world_x + 12.0, world_y + 10.0)
		label.modulate = ENEMY_STATE_COLORS.get(state, ENEMY_LABEL_THEME)
	for actor_name in _enemy_labels.keys():
		if active_enemy_names.has(actor_name):
			continue
		var stale: Label = _enemy_labels[actor_name] as Label
		if stale != null and is_instance_valid(stale):
			stale.queue_free()
		_enemy_labels.erase(actor_name)

func _clear_enemy_labels() -> void:
	for actor_name in _enemy_labels.keys():
		var label: Label = _enemy_labels[actor_name] as Label
		if label != null and is_instance_valid(label):
			label.queue_free()
	_enemy_labels.clear()

func _ensure_building_marker(marker_name: String) -> ColorRect:
	var marker := _building_markers.get(marker_name, null) as ColorRect
	if marker != null and is_instance_valid(marker):
		return marker
	marker = ColorRect.new()
	marker.name = "%sDebugMarker" % marker_name
	marker.size = Vector2(12.0, 12.0)
	marker.color = Color(0.968627, 0.882353, 0.4, 0.95) if marker_name == "MgTower" else Color(0.447059, 0.819608, 0.964706, 0.92)
	marker.mouse_filter = Control.MOUSE_FILTER_IGNORE
	marker.z_index = 30
	_root.add_child(marker)
	_building_markers[marker_name] = marker
	return marker

func _ensure_building_label(marker_name: String) -> Label:
	var label := _building_labels.get(marker_name, null) as Label
	if label != null and is_instance_valid(label):
		return label
	label = Label.new()
	label.name = "%sDebugLabel" % marker_name
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.z_index = 31
	label.add_theme_color_override("font_color", Color(0.980392, 0.956863, 0.858824, 1.0))
	label.add_theme_font_size_override("font_size", 14)
	_root.add_child(label)
	_building_labels[marker_name] = label
	return label

func _ensure_enemy_label(actor_name: String) -> Label:
	var label := _enemy_labels.get(actor_name, null) as Label
	if label != null and is_instance_valid(label):
		return label
	label = Label.new()
	label.name = "%sDebugEnemyLabel" % actor_name
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.z_index = 31
	label.add_theme_color_override("font_color", ENEMY_LABEL_THEME)
	label.add_theme_font_size_override("font_size", 13)
	_root.add_child(label)
	_enemy_labels[actor_name] = label
	return label

func _resolve_slot_center(slot_id: String) -> Vector2:
	if _battlefield_view != null and _battlefield_view.has_method("get_slot_position"):
		var pos: Variant = _battlefield_view.call("get_slot_position", slot_id)
		if pos is Vector2:
			return (pos as Vector2) + Vector2(24.0, 24.0)
	return Vector2.ZERO

func _format_vec2(value: Vector2) -> String:
	return "(%.0f, %.0f)" % [value.x, value.y]

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null

func cleanup() -> void:
	_clear_enemy_labels()
	for marker_name in _building_markers.keys():
		var marker := _building_markers[marker_name] as CanvasItem
		if marker != null and is_instance_valid(marker):
			marker.queue_free()
	_building_markers.clear()
	for label_name in _building_labels.keys():
		var label := _building_labels[label_name] as CanvasItem
		if label != null and is_instance_valid(label):
			label.queue_free()
	_building_labels.clear()
	for marker in _wall_markers:
		if marker != null and is_instance_valid(marker):
			marker.queue_free()
	_wall_markers.clear()
	if _tower_range_ring != null and is_instance_valid(_tower_range_ring):
		_tower_range_ring.queue_free()
	_tower_range_ring = null
	if _root != null and is_instance_valid(_root):
		_root.queue_free()
	_root = null
