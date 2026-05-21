extends Node

signal legality_changed(cells: Array)
signal overlay_rendered()

const LEGALITY_VALID_INNER := "valid_inner"
const LEGALITY_VALID_OUTER := "valid_outer"
const LEGALITY_OUTSIDE_VALID := "outside_valid"
const LEGALITY_FIXED_INVALID := "fixed_invalid"
const LEGALITY_TEMP_INVALID := "temp_invalid"
const LEGALITY_WALL := "wall"

var _slot_visuals: Dictionary = {}
var _placement_context_active: bool = true

func set_placement_context_active(active: bool) -> void:
	if _placement_context_active and not active:
		_slot_visuals.clear()
	_placement_context_active = active

func apply_legality(cells: Array[Vector2i]) -> void:
	emit_signal("legality_changed", cells.duplicate())
	emit_signal("overlay_rendered")

func apply_legality_overlay(legality_by_slot: Dictionary) -> void:
	if not _placement_context_active:
		return
	for slot_id_variant in legality_by_slot.keys():
		var slot_id: String = str(slot_id_variant)
		var legality: String = str(legality_by_slot[slot_id_variant])
		_slot_visuals[slot_id] = render_outcome(slot_id, legality)

func read_slot_visual(slot_id: String) -> Dictionary:
	return (
		_slot_visuals.get(slot_id, _visual(false, "none", "none", "none", "")) as Dictionary
	).duplicate(true)

func render_outcome(_slot_id: String, legality: String) -> Dictionary:
	match legality:
		LEGALITY_VALID_INNER:
			return _visual(true, "warm", "none", "none", "")
		LEGALITY_VALID_OUTER:
			return _visual(true, "warm", "none", "none", "")
		LEGALITY_OUTSIDE_VALID:
			return _visual(false, "none", "none", "none", "")
		LEGALITY_FIXED_INVALID:
			return _visual(false, "red", "none", "red", "")
		LEGALITY_TEMP_INVALID:
			return _visual(false, "none", "none", "red", "")
		LEGALITY_WALL:
			return _visual(false, "red", "blocker", "red", "")
		_:
			return _visual(false, "none", "none", "none", "")

func _visual(valid_overlay: bool, overlay_tint: String, marker: String, frame: String, reason_text: String) -> Dictionary:
	return {
		"valid_overlay": valid_overlay,
		"overlay_tint": overlay_tint,
		"marker": marker,
		"frame": frame,
		"reason_text": reason_text,
		"overlay_state": _overlay_state(valid_overlay, marker, frame)
	}

func _overlay_state(valid_overlay: bool, marker: String, frame: String) -> String:
	if valid_overlay:
		return "overlay_legal"
	if marker == "none" and frame == "none":
		return "overlay_hidden"
	return "overlay_illegal"




