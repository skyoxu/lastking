extends Control

const BATTLEFIELD_SIZE := Vector2(1584.0, 624.0)
const SLOT_SIZE := Vector2(48.0, 48.0)
const BASE_SLOT_COLOR := Color(0.603922, 0.784314, 0.560784, 0.18)
const WARM_SLOT_COLOR := Color(0.905882, 0.65098, 0.278431, 0.45)
const COOL_SLOT_COLOR := Color(0.372549, 0.666667, 0.94902, 0.45)
const GREY_SLOT_COLOR := Color(0.552941, 0.552941, 0.552941, 0.32)
const RED_SLOT_COLOR := Color(0.862745, 0.286275, 0.286275, 0.38)
signal battlefield_slot_clicked(slot_id: String)
signal battlefield_slot_hovered(slot_id: String)
signal battlefield_slot_released(slot_id: String)
const REGION_DEFS := [
	{
		"name": "LeftOuterField",
		"width": 576.0,
		"buildable": true,
		"slot_root": "LeftOuterSlots",
		"columns": 12,
		"rows": 13,
		"color": Color(0.141176, 0.231373, 0.2, 1.0),
	},
	{
		"name": "LeftWall",
		"width": 48.0,
		"buildable": false,
		"color": Color(0.345098, 0.333333, 0.305882, 1.0),
	},
	{
		"name": "InnerCastleRegion",
		"width": 336.0,
		"buildable": true,
		"slot_root": "InnerCastleSlots",
		"columns": 7,
		"rows": 13,
		"color": Color(0.223529, 0.192157, 0.141176, 1.0),
	},
	{
		"name": "RightWall",
		"width": 48.0,
		"buildable": false,
		"color": Color(0.345098, 0.333333, 0.305882, 1.0),
	},
	{
		"name": "RightOuterField",
		"width": 576.0,
		"buildable": true,
		"slot_root": "RightOuterSlots",
		"columns": 12,
		"rows": 13,
		"color": Color(0.129412, 0.188235, 0.176471, 1.0),
	},
]

@onready var _viewport: Control = _require_control("BattlefieldViewport")
@onready var _map_base_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/MapBaseLayer")
@onready var _boundary_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/BoundaryLayer")
@onready var _slot_overlay_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer")
@onready var _local_feedback_layer: Control = _require_control("BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer")

var _slot_nodes: Dictionary = {}
var _placement_reason_bubble: Control = null
var _placement_reason_label: Label = null
var _wall_tile_texture: Texture2D = null
var _selection_range_ring: Control = null


func _ready() -> void:
	_resolve_reason_bubble()
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
	_wall_tile_texture = _create_wall_tile_texture()

	var offset_x: float = 0.0
	for region_def_variant in REGION_DEFS:
		var region_def: Dictionary = region_def_variant
		var region: ColorRect = ColorRect.new()
		region.name = str(region_def["name"])
		region.position = Vector2(offset_x, 0.0)
		region.size = Vector2(float(region_def["width"]), BATTLEFIELD_SIZE.y)
		region.color = region_def["color"]
		region.set_meta("buildable", region_def["buildable"] == true)
		_map_base_layer.add_child(region)
		if region_def["buildable"] != true:
			var boundary: ColorRect = ColorRect.new()
			boundary.name = "%sBoundary" % str(region_def["name"])
			boundary.position = region.position
			boundary.size = region.size
			boundary.color = Color(0.529412, 0.490196, 0.403922, 0.8)
			_boundary_layer.add_child(boundary)
			_add_wall_tiles(str(region_def["name"]), region.position, region.size)
		offset_x += float(region_def["width"])


func _rebuild_slots() -> void:
	for child in _slot_overlay_layer.get_children():
		child.queue_free()
	_slot_nodes.clear()

	var offset_x: float = 0.0
	for region_def_variant in REGION_DEFS:
		var region_def: Dictionary = region_def_variant
		if region_def["buildable"] == true:
			var slot_root: Control = Control.new()
			slot_root.name = str(region_def["slot_root"])
			slot_root.position = Vector2(offset_x, 0.0)
			slot_root.size = Vector2(float(region_def["width"]), BATTLEFIELD_SIZE.y)
			slot_root.modulate = Color(1.0, 1.0, 1.0, 0.95)
			slot_root.set_meta("buildable_region", true)
			_slot_overlay_layer.add_child(slot_root)
			_populate_slots(slot_root, int(region_def["columns"]), int(region_def["rows"]), str(region_def["name"]))
		offset_x += float(region_def["width"])


func _populate_slots(slot_root: Control, columns: int, rows: int, region_name: String) -> void:
	for row in range(rows):
		for column in range(columns):
			var slot: ColorRect = ColorRect.new()
			slot.name = "%sSlot_%02d_%02d" % [region_name, column, row]
			slot.position = Vector2(column * SLOT_SIZE.x, row * SLOT_SIZE.y)
			slot.size = SLOT_SIZE
			slot.set_meta("buildable", true)
			slot.set_meta("slot_available", true)
			slot.set_meta("region_name", region_name)
			slot.mouse_filter = Control.MOUSE_FILTER_PASS
			var outline := ColorRect.new()
			outline.name = "Outline"
			outline.visible = false
			outline.position = Vector2.ZERO
			outline.size = SLOT_SIZE
			outline.mouse_filter = Control.MOUSE_FILTER_IGNORE
			slot.add_child(outline)
			var reason_label := Label.new()
			reason_label.name = "ReasonLabel"
			reason_label.visible = false
			reason_label.position = Vector2(3.0, 2.0)
			reason_label.size = Vector2(SLOT_SIZE.x - 6.0, SLOT_SIZE.y - 4.0)
			reason_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			reason_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
			reason_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			reason_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
			reason_label.add_theme_font_size_override("font_size", 9)
			slot.add_child(reason_label)
			slot.gui_input.connect(_on_slot_gui_input.bind(str(slot.name)))
			_apply_slot_visual(slot, _hidden_visual())
			slot_root.add_child(slot)
			_slot_nodes[str(slot.name)] = slot


func apply_slot_visual(slot_id: String, visual: Dictionary) -> void:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return
	_apply_slot_visual(slot, visual)

func read_slot_visual(slot_id: String) -> Dictionary:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return _hidden_visual()
	return {
		"overlay_state": str(slot.get_meta("overlay_state", "overlay_hidden")),
		"overlay_tint": str(slot.get_meta("overlay_tint", "none")),
		"marker": str(slot.get_meta("marker", "none")),
		"frame": str(slot.get_meta("frame", "none")),
		"reason_text": str(slot.get_meta("reason_text", "")),
		"feedback_channel": str(slot.get_meta("feedback_channel", "none")),
		"selection_owner": str(slot.get_meta("selection_owner", "")),
		"selection_category": str(slot.get_meta("selection_category", "")),
		"outline_tint": str(slot.get_meta("outline_tint", "none")),
		"range_clipped": slot.get_meta("range_clipped", false) == true,
	}

func get_all_slot_ids() -> Array[String]:
	var slot_ids: Array[String] = []
	for slot_id_variant in _slot_nodes.keys():
		slot_ids.append(str(slot_id_variant))
	slot_ids.sort()
	return slot_ids

func get_slot_region_kind(slot_id: String) -> String:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return ""
	var region_name: String = str(slot.get_meta("region_name", ""))
	if region_name == "InnerCastleRegion":
		return "inner_castle"
	if region_name == "LeftOuterField" or region_name == "RightOuterField":
		return "outer_field"
	return "wall"

func is_slot_available(slot_id: String) -> bool:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return false
	return slot.get_meta("slot_available", false) == true

func set_slot_available(slot_id: String, available: bool) -> void:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return
	slot.set_meta("slot_available", available)

func get_slot_position(slot_id: String) -> Vector2:
	var slot: ColorRect = _slot_nodes.get(slot_id, null) as ColorRect
	if slot == null:
		return Vector2.ZERO
	var parent_control := slot.get_parent() as Control
	if parent_control == null:
		return slot.position
	return parent_control.position + slot.position

func get_slot_id_at_screen_position(screen_position: Vector2) -> String:
	for slot_id_variant in _slot_nodes.keys():
		var slot: ColorRect = _slot_nodes.get(slot_id_variant, null) as ColorRect
		if slot == null:
			continue
		if slot.get_global_rect().has_point(screen_position):
			return str(slot_id_variant)
	return ""


func clear_all_slot_visuals() -> void:
	_hide_reason_bubble()
	for slot_variant in _slot_nodes.values():
		var slot: ColorRect = slot_variant as ColorRect
		if slot != null:
			_apply_slot_visual(slot, _hidden_visual())


func _apply_slot_visual(slot: ColorRect, visual: Dictionary) -> void:
	var overlay_state: String = str(visual.get("overlay_state", "overlay_hidden"))
	var overlay_tint: String = str(visual.get("overlay_tint", "none"))
	var marker: String = str(visual.get("marker", "none"))
	var frame: String = str(visual.get("frame", "none"))
	var reason_text: String = str(visual.get("reason_text", ""))
	var feedback_channel: String = str(visual.get("feedback_channel", "none"))
	var selection_owner: String = str(visual.get("selection_owner", ""))
	var selection_category: String = str(visual.get("selection_category", ""))
	var outline_tint: String = str(visual.get("outline_tint", "none"))
	var range_clipped: bool = visual.get("range_clipped", false) == true

	match overlay_state:
		"overlay_legal":
			if overlay_tint == "warm":
				slot.color = WARM_SLOT_COLOR
			elif overlay_tint == "cool":
				slot.color = COOL_SLOT_COLOR
			else:
				slot.color = BASE_SLOT_COLOR
		"overlay_illegal":
			if overlay_tint == "grey":
				slot.color = GREY_SLOT_COLOR
			else:
				slot.color = RED_SLOT_COLOR
		_:
			slot.color = BASE_SLOT_COLOR

	var outline := slot.get_node_or_null("Outline") as ColorRect
	if outline != null:
		outline.visible = frame != "none" or outline_tint != "none"
		outline.color = _outline_color_for_tint(outline_tint if outline_tint != "none" else overlay_tint, overlay_state)

	var reason_label := slot.get_node_or_null("ReasonLabel") as Label
	if reason_label != null:
		reason_label.visible = false
		reason_label.text = ""

	if _placement_reason_bubble != null and _placement_reason_bubble.visible:
		var bubble_slot_id := str(_placement_reason_bubble.get_meta("slot_id", ""))
		if bubble_slot_id == str(slot.name):
			_hide_reason_bubble()

	slot.set_meta("overlay_state", overlay_state)
	slot.set_meta("overlay_tint", overlay_tint)
	slot.set_meta("marker", marker)
	slot.set_meta("frame", frame)
	slot.set_meta("reason_text", reason_text)
	slot.set_meta("feedback_channel", feedback_channel)
	slot.set_meta("selection_owner", selection_owner)
	slot.set_meta("selection_category", selection_category)
	slot.set_meta("outline_tint", outline_tint)
	slot.set_meta("range_clipped", range_clipped)

func _outline_color_for_tint(tint: String, overlay_state: String) -> Color:
	match tint:
		"warm":
			return Color(0.972549, 0.788235, 0.360784, 0.9)
		"cool":
			return Color(0.447059, 0.733333, 1.0, 0.9)
		"grey":
			return Color(0.694118, 0.694118, 0.694118, 0.85)
		"red":
			return Color(0.941176, 0.423529, 0.372549, 0.95)
		_:
			if overlay_state == "overlay_illegal":
				return Color(0.941176, 0.423529, 0.372549, 0.95)
			if overlay_state == "overlay_legal":
				return Color(0.866667, 0.843137, 0.627451, 0.78)
			return Color(1.0, 1.0, 1.0, 0.0)


func _hidden_visual() -> Dictionary:
	return {
		"overlay_state": "overlay_hidden",
		"overlay_tint": "none",
		"marker": "none",
		"frame": "none",
		"reason_text": "",
		"feedback_channel": "none",
		"selection_owner": "",
		"selection_category": "",
		"outline_tint": "none",
		"range_clipped": false,
	}


func _on_slot_gui_input(event: InputEvent, slot_id: String) -> void:
	if event is InputEventMouseMotion:
		emit_signal("battlefield_slot_hovered", slot_id)
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			emit_signal("battlefield_slot_clicked", slot_id)

func show_selection_range_ring(center_point: Vector2, radius_px: float, tint: String = "cool") -> void:
	if radius_px <= 0.0:
		hide_selection_range_ring()
		return
	_ensure_selection_range_ring()
	if _selection_range_ring == null:
		return
	_selection_range_ring.set("center_point", center_point)
	_selection_range_ring.set("radius_px", radius_px)
	_selection_range_ring.modulate = _range_ring_color_for_tint(tint)
	_selection_range_ring.visible = true
	_selection_range_ring.queue_redraw()

func hide_selection_range_ring() -> void:
	if _selection_range_ring != null and is_instance_valid(_selection_range_ring):
		_selection_range_ring.visible = false

func _ensure_selection_range_ring() -> void:
	if _selection_range_ring != null and is_instance_valid(_selection_range_ring):
		return
	if _local_feedback_layer == null:
		return
	_selection_range_ring = Control.new()
	_selection_range_ring.name = "SelectionRangeRing"
	_selection_range_ring.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_selection_range_ring.z_index = 27
	_selection_range_ring.set_script(load("res://Game.Godot/Scripts/Screens/BattleMapCombatDebugRangeRing.gd"))
	_local_feedback_layer.add_child(_selection_range_ring)

func _range_ring_color_for_tint(tint: String) -> Color:
	match tint:
		"warm":
			return Color(0.980392, 0.764706, 0.356863, 0.92)
		_:
			return Color(0.447059, 0.733333, 1.0, 0.92)


func _require_control(node_path: NodePath) -> Control:
	var node: Control = get_node(node_path) as Control
	if node == null:
		push_error("BattlefieldView missing required control at %s" % str(node_path))
	return node


func _resolve_reason_bubble() -> void:
	if _placement_reason_bubble != null and _placement_reason_label != null:
		return
	_placement_reason_bubble = _local_feedback_layer.get_node_or_null("PlacementReasonBubble")
	_placement_reason_label = _local_feedback_layer.get_node_or_null("PlacementReasonBubble/BubbleLabel")

func _add_wall_tiles(region_name: String, region_position: Vector2, region_size: Vector2) -> void:
	if region_name != "LeftWall" and region_name != "RightWall":
		return
	var root := Control.new()
	root.name = "%sTiles" % region_name
	root.position = region_position
	root.size = region_size
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_boundary_layer.add_child(root)
	var rows: int = int(region_size.y / SLOT_SIZE.y)
	for row in range(rows):
		var tile := TextureRect.new()
		tile.name = "%sTile_%02d" % [region_name, row]
		tile.position = Vector2(0.0, row * SLOT_SIZE.y)
		tile.size = SLOT_SIZE
		tile.mouse_filter = Control.MOUSE_FILTER_IGNORE
		tile.stretch_mode = TextureRect.STRETCH_SCALE
		tile.texture = _wall_tile_texture
		root.add_child(tile)

func _create_wall_tile_texture() -> Texture2D:
	var image := Image.create(int(SLOT_SIZE.x), int(SLOT_SIZE.y), false, Image.FORMAT_RGBA8)
	image.fill(Color(0.384314, 0.337255, 0.278431, 1.0))
	for y in range(int(SLOT_SIZE.y)):
		for x in range(int(SLOT_SIZE.x)):
			var px: Color = image.get_pixel(x, y)
			if y % 12 == 0 or y % 12 == 1:
				px = Color(0.756863, 0.686275, 0.576471, 1.0)
			elif x == 0 or x == int(SLOT_SIZE.x) - 1 or y == int(SLOT_SIZE.y) - 1:
				px = Color(0.188235, 0.160784, 0.137255, 1.0)
			elif x % 24 == 0 or x % 24 == 1:
				px = Color(0.52549, 0.454902, 0.380392, 1.0)
			elif ((x + y) % 11) == 0:
				px = Color(0.847059, 0.760784, 0.639216, 1.0)
			image.set_pixel(x, y, px)
	return ImageTexture.create_from_image(image)

func _show_reason_bubble(slot: ColorRect, reason_text: String) -> void:
	_resolve_reason_bubble()
	if _placement_reason_bubble == null or _placement_reason_label == null:
		return
	_placement_reason_label.text = reason_text
	_placement_reason_bubble.visible = true
	_placement_reason_bubble.set_meta("slot_id", str(slot.name))
	var bubble_size: Vector2 = _placement_reason_bubble.size
	if bubble_size == Vector2.ZERO:
		bubble_size = _placement_reason_bubble.custom_minimum_size
	var slot_top_left: Vector2 = get_slot_position(str(slot.name))
	var default_position: Vector2 = slot_top_left + Vector2(SLOT_SIZE.x + 12.0, 0.0)
	var max_x: float = max(0.0, BATTLEFIELD_SIZE.x - max(bubble_size.x, _placement_reason_bubble.custom_minimum_size.x) - 8.0)
	var max_y: float = max(0.0, BATTLEFIELD_SIZE.y - max(bubble_size.y, 56.0) - 8.0)
	_placement_reason_bubble.position = Vector2(min(default_position.x, max_x), min(slot_top_left.y, max_y))

func _hide_reason_bubble() -> void:
	_resolve_reason_bubble()
	if _placement_reason_bubble == null or _placement_reason_label == null:
		return
	_placement_reason_bubble.visible = false
	_placement_reason_bubble.remove_meta("slot_id")
	_placement_reason_label.text = ""
