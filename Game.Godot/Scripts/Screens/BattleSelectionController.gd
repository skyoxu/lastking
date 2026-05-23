extends Node

const DEFAULT_OVERLAY_CONTROLLER_NAME := "UI_PlacementOverlayController"
const BATTLEFIELD_VIEW_PATH := "Background"
const CATEGORY_ECONOMY := "economy"
const CATEGORY_DEFENSE := "defense"
const CATEGORY_UNIT := "unit"

const CHANNEL_PRIORITY := {
	"linked_unit": 10,
	"defense_range": 20,
	"unit_range": 20,
	"building_outline": 30,
	"economy_glow": 40,
}

var _screen: Control = null
var _formal_selection_data_provider: Node = null
var _overlay_slot_visuals: Dictionary = {}
var _selection_slot_visuals: Dictionary = {}
var _selection_context_active: bool = true
var _hover_overlay_context: Dictionary = {}

func configure(screen: Control, refs: Dictionary = {}) -> void:
	_screen = screen
	var provider: Variant = refs.get("formal_selection_data_provider", null)
	if provider is Node:
		_formal_selection_data_provider = provider

func register_overlay_controller(path: NodePath, controller: Node) -> void:
	if _screen == null:
		return
	var node_name := str(path.get_concatenated_names()).replace("/", "_")
	var existing: Node = _screen.get_node_or_null(NodePath(node_name))
	if existing != null and existing != controller:
		_screen.remove_child(existing)
		existing.queue_free()
	if controller.get_parent() != null and controller.get_parent() != _screen:
		controller.get_parent().remove_child(controller)
	controller.name = node_name
	_screen.add_child(controller)

func get_overlay_controller() -> Node:
	if _screen == null:
		return null
	return _screen.get_node_or_null(NodePath(DEFAULT_OVERLAY_CONTROLLER_NAME))

func get_battlefield_view() -> Node:
	if _screen == null:
		return null
	return _screen.get_node_or_null(NodePath(BATTLEFIELD_VIEW_PATH))

func apply_legality_overlay(legality_by_slot: Dictionary) -> void:
	var controller: Node = get_overlay_controller()
	if controller != null and controller.has_method("apply_legality_overlay"):
		controller.call("apply_legality_overlay", legality_by_slot)
	for slot_id_variant in legality_by_slot.keys():
		var slot_id := str(slot_id_variant)
		_overlay_slot_visuals[slot_id] = _read_overlay_slot_visual(slot_id)
	_sync_runtime_visuals()

func apply_hover_overlay_context(legality_by_slot: Dictionary, payload: Dictionary) -> void:
	_hover_overlay_context = payload.duplicate(true)
	apply_legality_overlay(legality_by_slot)

func read_slot_visual(slot_id: String) -> Dictionary:
	var selection_visual: Variant = _selection_slot_visuals.get(slot_id, null)
	var overlay_visual: Variant = _overlay_slot_visuals.get(slot_id, null)
	if selection_visual is Dictionary and overlay_visual is Dictionary:
		return _merge_slot_visuals(
			(selection_visual as Dictionary).duplicate(true),
			(overlay_visual as Dictionary).duplicate(true)
		)
	if selection_visual is Dictionary:
		return (selection_visual as Dictionary).duplicate(true)
	if overlay_visual is Dictionary:
		return (overlay_visual as Dictionary).duplicate(true)
	return _hidden_visual()

func _merge_slot_visuals(selection_visual: Dictionary, overlay_visual: Dictionary) -> Dictionary:
	var overlay_state := str(overlay_visual.get("overlay_state", "overlay_hidden"))
	if overlay_state == "overlay_hidden":
		return selection_visual
	var merged := selection_visual.duplicate(true)
	merged["overlay_state"] = overlay_state
	merged["overlay_tint"] = str(overlay_visual.get("overlay_tint", merged.get("overlay_tint", "none")))
	merged["marker"] = str(overlay_visual.get("marker", merged.get("marker", "none")))
	merged["frame"] = str(overlay_visual.get("frame", merged.get("frame", "none")))
	merged["range_clipped"] = overlay_visual.get("range_clipped", false) == true or merged.get("range_clipped", false) == true
	var overlay_reason := str(overlay_visual.get("reason_text", ""))
	if not overlay_reason.is_empty():
		merged["reason_text"] = overlay_reason
	return merged

func set_placement_context_active(active: bool) -> void:
	_selection_context_active = active
	var controller: Node = get_overlay_controller()
	if controller != null and controller.has_method("set_placement_context_active"):
		controller.call("set_placement_context_active", active)
	if not active:
		_overlay_slot_visuals.clear()
		_selection_slot_visuals.clear()
		_hover_overlay_context.clear()
	_sync_runtime_visuals()

func apply_building_selection(snapshot: Dictionary) -> void:
	if not _selection_context_active:
		return
	_selection_slot_visuals = _build_selection_slot_visuals(snapshot)
	_push_selection_range_ring(snapshot)
	_sync_runtime_visuals()

func clear_building_selection() -> void:
	_selection_slot_visuals.clear()
	_hover_overlay_context.clear()
	_push_selection_range_ring({})
	_sync_runtime_visuals()

func select_formal_building_slot(slot_id: String) -> void:
	if not _selection_context_active:
		return
	if _formal_selection_data_provider != null and _formal_selection_data_provider.has_method("get_formal_selection_snapshot"):
		var snapshot_variant = _formal_selection_data_provider.call("get_formal_selection_snapshot", slot_id)
		if snapshot_variant is Dictionary and not (snapshot_variant as Dictionary).is_empty():
			apply_building_selection((snapshot_variant as Dictionary).duplicate(true))
			return
	clear_building_selection()

func _push_selection_range_ring(snapshot: Dictionary) -> void:
	var battlefield_view: Node = get_battlefield_view()
	if battlefield_view == null:
		return
	if snapshot.is_empty():
		if battlefield_view.has_method("hide_selection_range_ring"):
			battlefield_view.call("hide_selection_range_ring")
		return
	if not battlefield_view.has_method("show_selection_range_ring"):
		return
	var building_slots := _string_array(snapshot.get("building_slots", []))
	if building_slots.is_empty():
		battlefield_view.call("hide_selection_range_ring")
		return
	var slot_id := str(building_slots[0])
	var center := _slot_center(slot_id)
	if center.is_zero_approx():
		battlefield_view.call("hide_selection_range_ring")
		return
	var radius_px := float(snapshot.get("range_px", 0.0))
	if radius_px <= 0.0:
		battlefield_view.call("hide_selection_range_ring")
		return
	var category := str(snapshot.get("category", ""))
	var tint := "cool"
	if category == CATEGORY_ECONOMY:
		tint = "warm"
	battlefield_view.call("show_selection_range_ring", center, radius_px, tint)

func _slot_center(slot_id: String) -> Vector2:
	var battlefield_view: Node = get_battlefield_view()
	if battlefield_view != null and battlefield_view.has_method("get_slot_position"):
		var pos: Variant = battlefield_view.call("get_slot_position", slot_id)
		if pos is Vector2:
			return (pos as Vector2) + Vector2(24.0, 24.0)
	return Vector2.ZERO

func _build_selection_slot_visuals(snapshot: Dictionary) -> Dictionary:
	var slot_visuals: Dictionary = {}
	var selection_id := str(snapshot.get("selection_id", ""))
	var category := str(snapshot.get("category", ""))
	var building_slots := _string_array(snapshot.get("building_slots", []))
	var hidden_slots := _string_array(snapshot.get("hidden_slots", []))
	var range_slots := _string_array(snapshot.get("range_slots", []))
	var blocked_range_slots := _string_array(snapshot.get("blocked_range_slots", []))
	var linked_unit_slots := _string_array(snapshot.get("linked_unit_slots", []))

	for slot_id in hidden_slots:
		slot_visuals[slot_id] = _hidden_visual()

	for slot_id in building_slots:
		var building_channel: String = "building_outline"
		var building_tint: String = "cool"
		var building_frame: String = "cool"
		if category == CATEGORY_ECONOMY:
			building_channel = "economy_glow"
			building_tint = "warm"
			building_frame = "warm"
		_assign_selection_visual(slot_visuals, slot_id, _selection_visual(
			building_tint,
			"none",
			building_frame,
			building_channel,
			selection_id,
			category,
			false
		))

	var range_channel: String = ""
	if category == CATEGORY_UNIT:
		range_channel = "unit_range"
	if not range_channel.is_empty():
		for slot_id in range_slots:
			_assign_selection_visual(slot_visuals, slot_id, _selection_visual(
				"cool",
				"none",
				"none",
				range_channel,
				selection_id,
				category,
				false
			))
		for slot_id in blocked_range_slots:
			_assign_selection_visual(slot_visuals, slot_id, _selection_visual(
				"red",
				"blocker",
				"red",
				range_channel,
				selection_id,
				category,
				true
			))

	for slot_id in linked_unit_slots:
		_assign_selection_visual(slot_visuals, slot_id, _selection_visual(
			"cool",
			"none",
			"none",
			"linked_unit",
			selection_id,
			category,
			false
		))

	return slot_visuals

func _assign_selection_visual(slot_visuals: Dictionary, slot_id: String, visual: Dictionary) -> void:
	var new_channel := str(visual.get("feedback_channel", "none"))
	var new_priority := int(CHANNEL_PRIORITY.get(new_channel, 0))
	var existing: Variant = slot_visuals.get(slot_id, null)
	if existing is Dictionary:
		var existing_channel := str((existing as Dictionary).get("feedback_channel", "none"))
		var existing_priority := int(CHANNEL_PRIORITY.get(existing_channel, 0))
		if existing_priority > new_priority:
			return
	slot_visuals[slot_id] = visual

func _selection_visual(
	overlay_tint: String,
	marker: String,
	frame: String,
	feedback_channel: String,
	selection_owner: String,
	selection_category: String,
	range_clipped: bool
) -> Dictionary:
	var visual: Dictionary = _hidden_visual()
	visual["overlay_state"] = "overlay_legal"
	visual["overlay_tint"] = overlay_tint
	visual["marker"] = marker
	visual["frame"] = frame
	visual["feedback_channel"] = feedback_channel
	visual["selection_owner"] = selection_owner
	visual["selection_category"] = selection_category
	visual["outline_tint"] = "cool"
	visual["range_clipped"] = range_clipped
	if marker != "none" or frame != "none":
		visual["overlay_state"] = "overlay_illegal"
	return visual

func _sync_runtime_visuals() -> void:
	var battlefield_view: Node = get_battlefield_view()
	if battlefield_view == null or not battlefield_view.has_method("apply_slot_visual"):
		return
	if battlefield_view.has_method("clear_all_slot_visuals"):
		battlefield_view.call("clear_all_slot_visuals")
	var slot_ids: Dictionary = {}
	for slot_id_variant in _overlay_slot_visuals.keys():
		slot_ids[str(slot_id_variant)] = true
	for slot_id_variant in _selection_slot_visuals.keys():
		slot_ids[str(slot_id_variant)] = true
	for slot_id_variant in slot_ids.keys():
		var slot_id := str(slot_id_variant)
		var visual := read_slot_visual(slot_id)
		if _should_apply_hover_reason(slot_id, visual):
			visual["reason_text"] = str(_hover_overlay_context.get("reason_text", ""))
			visual["frame"] = "red"
			visual["outline_tint"] = "red"
		battlefield_view.call("apply_slot_visual", slot_id, visual)

func _should_apply_hover_reason(_slot_id: String, _visual: Dictionary) -> bool:
	return false

func _read_overlay_slot_visual(slot_id: String) -> Dictionary:
	var controller: Node = get_overlay_controller()
	if controller != null and controller.has_method("read_slot_visual"):
		var result = controller.call("read_slot_visual", slot_id)
		if result is Dictionary:
			return (result as Dictionary).duplicate(true)
	return _hidden_visual()

func _string_array(values: Variant) -> Array[String]:
	var result: Array[String] = []
	if values is Array:
		for value in values:
			result.append(str(value))
	return result

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
