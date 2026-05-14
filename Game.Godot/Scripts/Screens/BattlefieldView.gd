extends Control

const BATTLEFIELD_SIZE := Vector2(1440.0, 600.0)
const SLOT_SIZE := Vector2(50.0, 50.0)
const REGION_DEFS := [
	{
		"name": "LeftOuterField",
		"width": 500.0,
		"buildable": true,
		"slot_root": "LeftOuterSlots",
		"columns": 10,
		"rows": 12,
		"color": Color(0.141176, 0.231373, 0.2, 1.0),
	},
	{
		"name": "LeftWall",
		"width": 20.0,
		"buildable": false,
		"color": Color(0.345098, 0.333333, 0.305882, 1.0),
	},
	{
		"name": "InnerCastleRegion",
		"width": 400.0,
		"buildable": true,
		"slot_root": "InnerCastleSlots",
		"columns": 8,
		"rows": 12,
		"color": Color(0.223529, 0.192157, 0.141176, 1.0),
	},
	{
		"name": "RightWall",
		"width": 20.0,
		"buildable": false,
		"color": Color(0.345098, 0.333333, 0.305882, 1.0),
	},
	{
		"name": "RightOuterField",
		"width": 500.0,
		"buildable": true,
		"slot_root": "RightOuterSlots",
		"columns": 10,
		"rows": 12,
		"color": Color(0.129412, 0.188235, 0.176471, 1.0),
	},
]

@onready var _viewport: Control = _require_control("BattlefieldViewport")
@onready var _root: Control = _require_control("BattlefieldViewport/BattlefieldRoot")
@onready var _map_base_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/MapBaseLayer")
@onready var _boundary_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/BoundaryLayer")
@onready var _slot_overlay_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer")


func _ready() -> void:
	ensure_layout()


func ensure_layout() -> void:
	_viewport.custom_minimum_size = BATTLEFIELD_SIZE
	_rebuild_regions()
	_rebuild_slots()


func _rebuild_regions() -> void:
	for child in _map_base_layer.get_children():
		child.queue_free()
	for child in _boundary_layer.get_children():
		child.queue_free()

	var offset_x := 0.0
	for region_def_variant in REGION_DEFS:
		var region_def: Dictionary = region_def_variant
		var region := ColorRect.new()
		region.name = String(region_def["name"])
		region.position = Vector2(offset_x, 0.0)
		region.size = Vector2(float(region_def["width"]), BATTLEFIELD_SIZE.y)
		region.color = region_def["color"]
		region.set_meta("buildable", bool(region_def["buildable"]))
		_map_base_layer.add_child(region)
		if not bool(region_def["buildable"]):
			var boundary := ColorRect.new()
			boundary.name = "%sBoundary" % String(region_def["name"])
			boundary.position = region.position
			boundary.size = region.size
			boundary.color = Color(0.529412, 0.490196, 0.403922, 0.8)
			_boundary_layer.add_child(boundary)
		offset_x += float(region_def["width"])


func _rebuild_slots() -> void:
	for child in _slot_overlay_layer.get_children():
		child.queue_free()

	var offset_x := 0.0
	for region_def_variant in REGION_DEFS:
		var region_def: Dictionary = region_def_variant
		if bool(region_def["buildable"]):
			var slot_root := Control.new()
			slot_root.name = String(region_def["slot_root"])
			slot_root.position = Vector2(offset_x, 0.0)
			slot_root.size = Vector2(float(region_def["width"]), BATTLEFIELD_SIZE.y)
			slot_root.modulate = Color(1.0, 1.0, 1.0, 0.95)
			slot_root.set_meta("buildable_region", true)
			_slot_overlay_layer.add_child(slot_root)
			_populate_slots(slot_root, int(region_def["columns"]), int(region_def["rows"]), String(region_def["name"]))
		offset_x += float(region_def["width"])


func _populate_slots(slot_root: Control, columns: int, rows: int, region_name: String) -> void:
	for row in range(rows):
		for column in range(columns):
			var slot := ColorRect.new()
			slot.name = "%sSlot_%02d_%02d" % [region_name, column, row]
			slot.position = Vector2(column * SLOT_SIZE.x, row * SLOT_SIZE.y)
			slot.size = SLOT_SIZE
			slot.color = Color(0.603922, 0.784314, 0.560784, 0.18)
			slot.set_meta("buildable", true)
			slot.set_meta("slot_available", true)
			slot_root.add_child(slot)


func _require_control(node_path: NodePath) -> Control:
	var node := get_node(node_path) as Control
	if node == null:
		push_error("BattlefieldView missing required control at %s" % String(node_path))
	return node
