extends Node

const REGION_INNER_CASTLE := "inner_castle"
const REGION_OUTER_FIELD := "outer_field"
const REGION_WALL := "wall"
const LEGALITY_VALID_INNER := "valid_inner"
const LEGALITY_VALID_OUTER := "valid_outer"
const LEGALITY_FIXED_INVALID := "fixed_invalid"

var _selection_controller: Node = null
var _feedback_controller: Node = null
var _selection_data_provider: Node = null
var _bridge_provider: Callable
var _battlefield_view: Node = null
var _translate: Callable
var _drag_preview: Control = null
var _drag_preview_frame: ColorRect = null
var _drag_preview_sprite: TextureRect = null
var _drag_tooltip: Control = null
var _drag_tooltip_label: Label = null
var _screen: Control = null
var _battle_hud: Node = null
var _drag_preview_texture_path: String = ""

const BUILD_PREVIEW_TEXTURES := {
	"tower_alpha": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png",
	"tower_beta": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png",
	"barracks_alpha": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_barracks.png",
	"farm_alpha": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_residence.png",
}

var _active_selection_id: String = ""
var _occupied_slots: Dictionary = {}
var _drag_active: bool = false
var _hovered_slot_id: String = ""
var _pointer_position: Vector2 = Vector2.ZERO
var _pending_cancel_after_place: bool = false
var _post_release_probe_id: int = 0

func configure(refs: Dictionary) -> void:
	_selection_controller = refs.get("selection_controller", null)
	_feedback_controller = refs.get("feedback_controller", null)
	_selection_data_provider = refs.get("selection_data_provider", null)
	_bridge_provider = refs.get("bridge_provider", Callable())
	_battlefield_view = refs.get("battlefield_view", null)
	_translate = refs.get("translate", Callable())
	_screen = refs.get("screen", null)
	_battle_hud = _screen.get_node_or_null("BattleHud") if _screen != null else null
	_resolve_drag_preview()
	_sync_occupied_slots_from_bridge()
	_update_drag_preview()

func handle_action(action_code: String) -> bool:
	match action_code:
		"build":
			cancel_active_placement()
			_push_build_mode_to_hud(false)
			_show_prompt("battlemap.prompt.choose_building_to_place", "Choose a building to place.")
			return true
		"select_tower":
			return _begin_placement("tower_alpha", false)
		"select_tower_beta":
			return _begin_placement("tower_beta", false)
		"select_residence":
			return _begin_placement("farm_alpha", false)
		"select_barracks":
			return _begin_placement("barracks_alpha", false)
		"cancel_build":
			cancel_active_placement()
			return true
		_:
			return false

func handle_battlefield_slot_clicked(slot_id: String) -> bool:
	if _active_selection_id.is_empty():
		return false
	if _drag_active:
		return false
	return handle_battlefield_slot_released(slot_id)

func begin_drag_building(selection_id: String) -> bool:
	return _begin_placement(selection_id, true)

func handle_battlefield_slot_hovered(slot_id: String) -> bool:
	if not _drag_active or _active_selection_id.is_empty():
		return false
	_hovered_slot_id = slot_id
	_apply_current_legality_overlay()
	_update_drag_preview()
	return true

func handle_battlefield_slot_released(slot_id: String) -> bool:
	if _active_selection_id.is_empty():
		return false
	print("[BuildPlacement] release slot=%s selection=%s drag=%s" % [slot_id, _active_selection_id, str(_drag_active)])
	var evaluation := _evaluate_slot_legality(_active_selection_id, slot_id)
	var legality: String = str(evaluation.get("legality", ""))
	if legality == LEGALITY_VALID_INNER or legality == LEGALITY_VALID_OUTER:
		var bridge: Node = _current_bridge()
		var placement_result: Dictionary = {}
		if bridge != null and bridge.has_method("PlaceBuildingAtSlot"):
			print("[BuildPlacement] calling PlaceBuildingAtSlot")
			var result: Variant = bridge.call("PlaceBuildingAtSlot", _active_selection_id, slot_id)
			print("[BuildPlacement] PlaceBuildingAtSlot returned type=%s" % [str(typeof(result))])
			if result is Dictionary:
				placement_result = result
				if placement_result.get("placed", false) == true:
					print("[BuildPlacement] placement accepted")
					_occupied_slots[slot_id] = _active_selection_id
					if _battlefield_view != null and _battlefield_view.has_method("set_slot_available"):
						_battlefield_view.call("set_slot_available", slot_id, false)
					_schedule_cancel_active_placement()
					_schedule_post_release_probe()
					return true
		var failure_reason_code := str(placement_result.get("reason", ""))
		var failure_reason_key := _build_error_key_for_reason_code(failure_reason_code)
		_show_prompt(failure_reason_key, _build_error_fallback(failure_reason_key))
		_push_build_context_message(failure_reason_key)
		return true
	var reason_key := str(evaluation.get("reason_key", "battlemap.build_error.wrong_region"))
	_show_prompt(reason_key, _build_error_fallback(reason_key))
	_push_build_context_message(reason_key)
	_update_drag_preview()
	return true

func handle_pointer_release_without_slot() -> bool:
	if not _drag_active:
		return false
	cancel_active_placement()
	return true

func update_drag_pointer_position(pointer_position: Vector2) -> bool:
	if not _drag_active:
		return false
	_pointer_position = pointer_position
	if _hovered_slot_id.is_empty():
		_update_drag_preview()
	return true

func sync_drag_pointer(pointer_position: Vector2, hovered_slot_id: String) -> bool:
	if not _drag_active:
		return false
	_pointer_position = pointer_position
	var next_slot_id := hovered_slot_id.strip_edges()
	if _hovered_slot_id != next_slot_id:
		_hovered_slot_id = next_slot_id
		_apply_current_legality_overlay()
	_update_drag_preview()
	return true

func has_active_placement() -> bool:
	return not _active_selection_id.is_empty()

func reset_runtime_state() -> void:
	_occupied_slots.clear()
	if _battlefield_view != null and _battlefield_view.has_method("get_all_slot_ids") and _battlefield_view.has_method("set_slot_available"):
		var slot_ids: Array[String] = _string_array(_battlefield_view.call("get_all_slot_ids"))
		for slot_id in slot_ids:
			_battlefield_view.call("set_slot_available", slot_id, true)
	cancel_active_placement()
	_sync_occupied_slots_from_bridge()

func get_drag_preview_state() -> Dictionary:
	var preview_texture_path := ""
	var has_texture := false
	if _drag_preview_sprite != null and _drag_preview_sprite.texture != null:
		has_texture = true
		preview_texture_path = _drag_preview_texture_path
	return {
		"visible": _drag_preview != null and _drag_preview.visible,
		"selection_id": _active_selection_id,
		"slot_id": _hovered_slot_id,
		"visual_state": _current_preview_visual_state(),
		"invalid_badge_visible": false,
		"position": _drag_preview.position if _drag_preview != null else Vector2.ZERO,
		"tooltip_text": _drag_tooltip_label.text if _drag_tooltip_label != null else "",
		"preview_kind": _preview_kind_for_selection(_active_selection_id),
		"has_texture": has_texture,
		"texture_path": preview_texture_path,
	}

func cancel_active_placement() -> void:
	print("[BuildPlacement] cancel_active_placement")
	_pending_cancel_after_place = false
	_active_selection_id = ""
	_drag_active = false
	_hovered_slot_id = ""
	_pointer_position = Vector2.ZERO
	if _selection_controller != null and _selection_controller.has_method("set_placement_context_active"):
		print("[BuildPlacement] cancel -> set_placement_context_active(false)")
		_selection_controller.call("set_placement_context_active", false)
	if _selection_controller != null and _selection_controller.has_method("clear_building_selection"):
		print("[BuildPlacement] cancel -> clear_building_selection")
		_selection_controller.call("clear_building_selection")
	print("[BuildPlacement] cancel -> push build mode false")
	_push_build_mode_to_hud(false)
	print("[BuildPlacement] cancel -> clear active selection in hud")
	_push_active_build_selection_to_hud("")
	print("[BuildPlacement] cancel -> clear build context")
	_clear_build_context()
	print("[BuildPlacement] cancel -> update drag preview")
	_update_drag_preview()
	print("[BuildPlacement] cancel complete")

func _schedule_cancel_active_placement() -> void:
	if _pending_cancel_after_place:
		return
	_pending_cancel_after_place = true
	call_deferred("_finish_cancel_active_placement")

func _finish_cancel_active_placement() -> void:
	if not _pending_cancel_after_place:
		return
	_pending_cancel_after_place = false
	cancel_active_placement()

func _schedule_post_release_probe() -> void:
	_post_release_probe_id += 1
	var probe_id := _post_release_probe_id
	if _screen != null and _screen.has_method("debug_arm_post_place_probe"):
		_screen.call("debug_arm_post_place_probe")
	if _battle_hud != null and _battle_hud.has_method("DebugArmPostPlaceProbe"):
		_battle_hud.call("DebugArmPostPlaceProbe")
	call_deferred("_run_post_release_probe", probe_id, 0)

func _run_post_release_probe(probe_id: int, stage: int) -> void:
	if probe_id != _post_release_probe_id:
		return
	print("[BuildPlacement] post_release_probe stage=%d" % stage)
	if stage >= 3:
		return
	call_deferred("_run_post_release_probe", probe_id, stage + 1)

func _begin_placement(selection_id: String, drag_active: bool) -> bool:
	if _selection_data_provider == null or _battlefield_view == null:
		return false
	_resolve_drag_preview()
	var definition: Dictionary = _selection_data_provider.call("get_building_definition", selection_id)
	if definition.is_empty():
		return false
	_active_selection_id = selection_id
	_drag_active = drag_active
	_hovered_slot_id = ""
	_pointer_position = Vector2.ZERO
	_sync_occupied_slots_from_bridge()
	_push_build_mode_to_hud(true)
	if _selection_controller != null and _selection_controller.has_method("set_placement_context_active"):
		_selection_controller.call("set_placement_context_active", true)
	if _selection_controller != null and _selection_controller.has_method("clear_building_selection"):
		_selection_controller.call("clear_building_selection")
	_push_active_build_selection_to_hud(selection_id)
	_apply_current_legality_overlay()
	_push_build_context_message("")
	_update_drag_preview()
	return true

func _apply_current_legality_overlay() -> void:
	if _selection_controller == null or not _selection_controller.has_method("apply_legality_overlay"):
		return
	var legality_by_slot: Dictionary = _build_legality_overlay(_active_selection_id)
	if _hovered_slot_id.is_empty():
		_push_hover_reason_overlay(legality_by_slot, {})
	if _drag_active and not _hovered_slot_id.is_empty():
		var focused_overlay: Dictionary = {}
		if legality_by_slot.has(_hovered_slot_id):
			focused_overlay[_hovered_slot_id] = legality_by_slot[_hovered_slot_id]
		_push_hover_reason_overlay(focused_overlay, _evaluate_slot_legality(_active_selection_id, _hovered_slot_id))
		_selection_controller.call("apply_legality_overlay", focused_overlay)
		return
	_selection_controller.call("apply_legality_overlay", legality_by_slot)

func _update_drag_preview() -> void:
	_resolve_drag_preview()
	if _drag_preview == null:
		return
	if not _drag_active or _active_selection_id.is_empty():
		_drag_preview.visible = false
		if _drag_preview_sprite != null:
			_drag_preview_sprite.texture = null
		_drag_preview_texture_path = ""
		if _drag_tooltip != null:
			_drag_tooltip.visible = false
		return
	_drag_preview.visible = true
	_drag_preview.size = Vector2(48.0, 48.0)
	if _battlefield_view != null and not _hovered_slot_id.is_empty() and _battlefield_view.has_method("get_slot_position"):
		var pos: Variant = _battlefield_view.call("get_slot_position", _hovered_slot_id)
		if pos is Vector2:
			_drag_preview.position = pos
	elif not _pointer_position.is_zero_approx():
		_drag_preview.position = _pointer_position - (_drag_preview.size / 2.0)
	var state := _current_preview_visual_state()
	_apply_preview_texture()
	match state:
		"legal":
			_set_drag_preview_tint(Color(0.972549, 0.760784, 0.356863, 0.28), Color(1.0, 1.0, 1.0, 0.96))
		"illegal":
			_set_drag_preview_tint(Color(0.862745, 0.286275, 0.286275, 0.32), Color(1.0, 0.76, 0.76, 0.9))
		_:
			_set_drag_preview_tint(Color(0.372549, 0.666667, 0.94902, 0.22), Color(0.92, 0.97, 1.0, 0.92))
	_push_build_context_message("")
	_update_drag_tooltip(state)

func _resolve_drag_preview() -> void:
	if _drag_preview != null and _drag_preview_frame != null and _drag_preview_sprite != null and _drag_tooltip != null and _drag_tooltip_label != null:
		return
	if _screen != null:
		_drag_preview = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/DragPreview")
		_drag_preview_frame = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/DragPreview/PreviewFrame")
		_drag_preview_sprite = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/DragPreview/PreviewSprite")
		_drag_tooltip = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/DragTooltip")
		_drag_tooltip_label = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/DragTooltip/TooltipLabel")

func _current_preview_visual_state() -> String:
	if not _drag_active or _active_selection_id.is_empty():
		return "hidden"
	if _hovered_slot_id.is_empty():
		return "idle"
	var evaluation := _evaluate_slot_legality(_active_selection_id, _hovered_slot_id)
	var legality: String = str(evaluation.get("legality", ""))
	if legality == LEGALITY_VALID_INNER or legality == LEGALITY_VALID_OUTER:
		return "legal"
	return "illegal"

func _update_drag_tooltip(state: String) -> void:
	if _drag_tooltip == null or _drag_tooltip_label == null:
		return
	if not _drag_active or _active_selection_id.is_empty():
		_drag_tooltip.visible = false
		_drag_tooltip_label.text = ""
		return
	var building_name := _building_display_name()
	var status_text := ""
	match state:
		"legal":
			status_text = _t("battlemap.drag_status.valid", "Valid")
		"illegal":
			status_text = _t("battlemap.drag_status.invalid", "Invalid")
		_:
			status_text = _t("battlemap.drag_status.drag", "Drag")
	_drag_tooltip_label.text = "%s\n%s" % [building_name, status_text]
	_drag_tooltip.visible = true
	_drag_tooltip.position = _drag_preview.position + Vector2(56.0, 0.0)

func _building_display_name() -> String:
	match _active_selection_id:
		"tower_alpha":
			return _t("battlemap.building.tower", "Tower")
		"tower_beta":
			return _t("battlemap.building.sniper_tower", "Sniper Tower")
		"barracks_alpha":
			return _t("battlemap.building.barracks", "Barracks")
		"farm_alpha":
			return _t("battlemap.building.residence", "Residence")
		_:
			return _t("battlemap.building.generic", "Building")

func _apply_formal_selection_feedback(selection_id: String) -> void:
	if _selection_controller == null or not _selection_controller.has_method("apply_building_selection"):
		return
	var snapshot: Dictionary = _formal_selection_snapshot_for_selection(selection_id)
	if snapshot.is_empty():
		return
	_selection_controller.call("apply_building_selection", snapshot)

func _formal_selection_snapshot_for_selection(selection_id: String) -> Dictionary:
	if _selection_data_provider == null:
		return {}
	var definition: Dictionary = _selection_data_provider.call("get_building_definition", selection_id)
	if definition.is_empty():
		return {}
	var snapshot: Dictionary = definition.duplicate(true)
	match selection_id:
		"tower_alpha":
			snapshot["building_slots"] = ["InnerCastleRegionSlot_03_00"]
			snapshot["range_slots"] = ["InnerCastleRegionSlot_04_00"]
			snapshot["blocked_range_slots"] = ["InnerCastleRegionSlot_05_00"]
		"tower_beta":
			snapshot["building_slots"] = ["InnerCastleRegionSlot_06_05"]
			snapshot["range_slots"] = ["InnerCastleRegionSlot_05_00"]
			snapshot["blocked_range_slots"] = ["InnerCastleRegionSlot_06_00"]
		"barracks_alpha":
			snapshot["building_slots"] = ["InnerCastleRegionSlot_00_00"]
		"farm_alpha":
			snapshot["building_slots"] = ["InnerCastleRegionSlot_06_00"]
			snapshot["hidden_slots"] = ["InnerCastleRegionSlot_03_00"]
		_:
			snapshot["building_slots"] = []
	return snapshot

func _build_legality_overlay(selection_id: String) -> Dictionary:
	var legality_by_slot: Dictionary = {}
	if _selection_data_provider == null or _battlefield_view == null:
		return legality_by_slot
	var slot_ids: Array[String] = _string_array(_battlefield_view.call("get_all_slot_ids"))
	for slot_id in slot_ids:
		var evaluation := _evaluate_slot_legality(selection_id, slot_id)
		var legality: String = str(evaluation.get("legality", ""))
		if legality == LEGALITY_VALID_INNER or legality == LEGALITY_VALID_OUTER:
			legality_by_slot[slot_id] = legality
		elif not legality.is_empty():
			legality_by_slot[slot_id] = "wall"
	return legality_by_slot

func _evaluate_slot_legality(selection_id: String, slot_id: String) -> Dictionary:
	if _selection_data_provider == null or _battlefield_view == null or slot_id.is_empty():
		return {
			"legality": LEGALITY_FIXED_INVALID,
			"reason_code": "invalid_input",
			"reason_key": "battlemap.build_error.wrong_region",
		}
	var definition: Dictionary = _selection_data_provider.call("get_building_definition", selection_id)
	var allowed_regions: Array[String] = _string_array(definition.get("allowed_regions", []))
	var region_kind: String = str(_battlefield_view.call("get_slot_region_kind", slot_id))
	if region_kind == REGION_WALL or region_kind.is_empty():
		return {
			"legality": LEGALITY_FIXED_INVALID,
			"reason_code": "wall_blocked",
			"reason_key": "battlemap.build_error.wall_blocked",
		}
	var is_allowed_region: bool = allowed_regions.has(region_kind)
	if not is_allowed_region:
		return {
			"legality": LEGALITY_FIXED_INVALID,
			"reason_code": "wrong_region",
			"reason_key": "battlemap.build_error.wrong_region",
			"region_kind": region_kind,
		}
	var is_available: bool = _occupied_slots.has(slot_id) == false and _battlefield_view.call("is_slot_available", slot_id) == true
	if not is_available:
		return {
			"legality": LEGALITY_FIXED_INVALID,
			"reason_code": "slot_occupied",
			"reason_key": "battlemap.build_error.slot_occupied",
			"region_kind": region_kind,
		}
	return {
		"legality": LEGALITY_VALID_INNER if region_kind == REGION_INNER_CASTLE else LEGALITY_VALID_OUTER,
		"reason_code": "",
		"reason_key": "",
		"region_kind": region_kind,
	}

func _sync_occupied_slots_from_bridge() -> void:
	_occupied_slots.clear()
	var bridge: Node = _current_bridge()
	if bridge == null or not bridge.has_method("GetPlacedBuildingSlots"):
		return
	var result: Variant = bridge.call("GetPlacedBuildingSlots")
	if result is Dictionary:
		for slot_id_variant in (result as Dictionary).keys():
			var slot_id := str(slot_id_variant)
			_occupied_slots[slot_id] = str((result as Dictionary)[slot_id_variant])
			if _battlefield_view != null and _battlefield_view.has_method("set_slot_available"):
				_battlefield_view.call("set_slot_available", slot_id, false)

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null

func _show_prompt(key: String, fallback: String) -> void:
	if _feedback_controller != null and _feedback_controller.has_method("show_local_prompt"):
		_feedback_controller.call("show_local_prompt", _t(key, fallback))

func _t(key: String, fallback: String) -> String:
	if _translate.is_valid():
		var translated: String = str(_translate.call(key))
		if translated != key:
			return translated
	return fallback

func _string_array(values: Variant) -> Array[String]:
	var result: Array[String] = []
	if values is Array:
		for value in values:
			result.append(str(value))
	return result

func _apply_preview_texture() -> void:
	if _drag_preview_sprite == null:
		return
	var texture_path := str(BUILD_PREVIEW_TEXTURES.get(_active_selection_id, ""))
	if texture_path.is_empty():
		_drag_preview_sprite.texture = null
		_drag_preview_texture_path = ""
		return
	if _drag_preview_sprite.texture != null and _drag_preview_texture_path == texture_path:
		return
	var texture := _load_preview_texture(texture_path)
	_drag_preview_sprite.texture = texture
	_drag_preview_texture_path = texture_path if texture != null else ""

func _load_preview_texture(texture_path: String) -> Texture2D:
	var image_path := ProjectSettings.globalize_path(texture_path)
	if FileAccess.file_exists(image_path):
		var image := Image.load_from_file(image_path)
		if image != null and not image.is_empty():
			return ImageTexture.create_from_image(image)
	return _create_fallback_preview_texture(_preview_kind_for_selection(_active_selection_id))

func _create_fallback_preview_texture(preview_kind: String) -> Texture2D:
	var image := Image.create(48, 48, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.094118, 0.12549, 0.164706, 0.92))
	for x in range(48):
		for y in range(48):
			if x <= 1 or x >= 46 or y <= 1 or y >= 46:
				image.set_pixel(x, y, Color(0.905882, 0.807843, 0.603922, 0.92))
	var accent := Color(0.807843, 0.627451, 0.298039, 1.0)
	var fill := Color(0.556863, 0.658824, 0.772549, 1.0)
	match preview_kind:
		"barracks":
			fill = Color(0.517647, 0.639216, 0.737255, 1.0)
			accent = Color(0.721569, 0.32549, 0.286275, 1.0)
			_fill_rect(image, Rect2i(10, 31, 28, 7), Color(0.384314, 0.282353, 0.219608, 1.0))
			_fill_rect(image, Rect2i(12, 19, 24, 12), fill)
			_fill_triangle(image, Vector2i(8, 20), Vector2i(24, 9), Vector2i(40, 20), accent)
			_fill_rect(image, Rect2i(21, 22, 6, 9), Color(0.219608, 0.254902, 0.313726, 1.0))
		"residence":
			fill = Color(0.839216, 0.760784, 0.603922, 1.0)
			accent = Color(0.745098, 0.313726, 0.227451, 1.0)
			_fill_rect(image, Rect2i(11, 31, 26, 7), Color(0.517647, 0.372549, 0.243137, 1.0))
			_fill_rect(image, Rect2i(13, 20, 22, 12), fill)
			_fill_triangle(image, Vector2i(10, 21), Vector2i(24, 9), Vector2i(38, 21), accent)
			_fill_rect(image, Rect2i(21, 24, 6, 8), Color(0.4, 0.27451, 0.192157, 1.0))
		_:
			fill = Color(0.490196, 0.552941, 0.65098, 1.0)
			accent = Color(0.819608, 0.658824, 0.301961, 1.0)
			_fill_rect(image, Rect2i(16, 13, 16, 22), fill)
			_fill_rect(image, Rect2i(13, 34, 22, 6), Color(0.694118, 0.517647, 0.286275, 1.0))
			_fill_triangle(image, Vector2i(13, 16), Vector2i(24, 7), Vector2i(35, 16), accent)
			_fill_rect(image, Rect2i(22, 22, 4, 8), Color(0.219608, 0.254902, 0.313726, 1.0))
	return ImageTexture.create_from_image(image)

func _fill_rect(image: Image, rect: Rect2i, color: Color) -> void:
	for x in range(rect.position.x, rect.position.x + rect.size.x):
		for y in range(rect.position.y, rect.position.y + rect.size.y):
			if x >= 0 and x < image.get_width() and y >= 0 and y < image.get_height():
				image.set_pixel(x, y, color)

func _fill_triangle(image: Image, a: Vector2i, b: Vector2i, c: Vector2i, color: Color) -> void:
	var min_x := mini(a.x, mini(b.x, c.x))
	var max_x := maxi(a.x, maxi(b.x, c.x))
	var min_y := mini(a.y, mini(b.y, c.y))
	var max_y := maxi(a.y, maxi(b.y, c.y))
	for x in range(min_x, max_x + 1):
		for y in range(min_y, max_y + 1):
			if _point_in_triangle(Vector2(x + 0.5, y + 0.5), Vector2(a), Vector2(b), Vector2(c)):
				if x >= 0 and x < image.get_width() and y >= 0 and y < image.get_height():
					image.set_pixel(x, y, color)

func _point_in_triangle(point: Vector2, a: Vector2, b: Vector2, c: Vector2) -> bool:
	var denominator := ((b.y - c.y) * (a.x - c.x)) + ((c.x - b.x) * (a.y - c.y))
	if is_zero_approx(denominator):
		return false
	var w1 := (((b.y - c.y) * (point.x - c.x)) + ((c.x - b.x) * (point.y - c.y))) / denominator
	var w2 := (((c.y - a.y) * (point.x - c.x)) + ((a.x - c.x) * (point.y - c.y))) / denominator
	var w3 := 1.0 - w1 - w2
	return w1 >= 0.0 and w2 >= 0.0 and w3 >= 0.0

func _set_drag_preview_tint(frame_color: Color, sprite_modulate: Color) -> void:
	if _drag_preview_frame != null:
		_drag_preview_frame.color = frame_color
	if _drag_preview_sprite != null:
		_drag_preview_sprite.modulate = sprite_modulate

func _preview_kind_for_selection(selection_id: String) -> String:
	if _selection_data_provider != null and not selection_id.is_empty():
		var definition: Dictionary = _selection_data_provider.call("get_building_definition", selection_id)
		var preview_kind := str(definition.get("preview_kind", ""))
		if not preview_kind.is_empty():
			return preview_kind
	match selection_id:
		"tower_alpha":
			return "tower"
		"tower_beta":
			return "tower_beta"
		"barracks_alpha":
			return "barracks"
		"farm_alpha":
			return "residence"
		_:
			return "building"

func _push_active_build_selection_to_hud(selection_id: String) -> void:
	_apply_build_palette_visual_state(selection_id)

func _push_build_mode_to_hud(active: bool) -> void:
	if _battle_hud != null:
		_battle_hud.call("SetBuildPlacementMode", active)
	_set_build_action_label(active)

func _apply_build_palette_visual_state(selection_id: String) -> void:
	if _battle_hud == null:
		return
	_apply_build_button_state(_battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot"), selection_id, "tower_alpha")
	_apply_build_button_state(_battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/SniperTowerSlot"), selection_id, "tower_beta")
	_apply_build_button_state(_battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BarracksSlot"), selection_id, "barracks_alpha")
	_apply_build_button_state(_battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot"), selection_id, "farm_alpha")

func _apply_build_button_state(button_node: Node, active_selection_id: String, button_selection_id: String) -> void:
	var button := button_node as BaseButton
	if button == null:
		return
	var has_active_selection := not active_selection_id.is_empty()
	var is_active := has_active_selection and active_selection_id == button_selection_id
	if is_active:
		button.modulate = Color(1.0, 1.0, 1.0, 1.0)
		button.self_modulate = Color(1.08, 1.02, 0.9, 1.0)
		button.scale = Vector2.ONE
		return
	button.modulate = Color(1.0, 1.0, 1.0, 0.56 if has_active_selection else 1.0)
	button.self_modulate = Color(1.0, 1.0, 1.0, 1.0)
	button.scale = Vector2.ONE

func _push_hover_reason_overlay(legality_by_slot: Dictionary, evaluation: Dictionary) -> void:
	if _selection_controller == null or not _selection_controller.has_method("apply_hover_overlay_context"):
		return
	var payload := {
		"selection_id": _active_selection_id,
		"hovered_slot_id": _hovered_slot_id,
		"reason_text": "",
		"visual_state": _current_preview_visual_state(),
	}
	if not evaluation.is_empty():
		var legality := str(evaluation.get("legality", ""))
		if legality != LEGALITY_VALID_INNER and legality != LEGALITY_VALID_OUTER:
			var reason_key := str(evaluation.get("reason_key", "battlemap.build_error.wrong_region"))
			payload["reason_text"] = _t(reason_key, _build_error_fallback(reason_key))
	_selection_controller.call("apply_hover_overlay_context", legality_by_slot, payload)

func _clear_build_context() -> void:
	_set_build_context_labels("", "")

func _push_build_context_message(reason_key: String) -> void:
	if _battle_hud == null or _active_selection_id.is_empty():
		return
	var title_text := "%s | %s" % [
		_building_display_name(),
		_allowed_region_text(_active_selection_id),
	]
	var detail_text := _build_context_detail_text(reason_key)
	_set_build_context_labels(title_text, detail_text)

func _set_build_action_label(active: bool) -> void:
	if _battle_hud == null:
		return
	var build_action_node := _battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BuildAction")
	var build_button := build_action_node as Button
	if build_button == null:
		return
	build_button.text = _t("hud.cancel_build", "Cancel Build") if active else _t("hud.build", "Build")

func _set_build_context_labels(title_text: String, detail_text: String) -> void:
	if _battle_hud == null:
		return
	var production_label := _battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/ProductionLabel") as Label
	var build_status_label := _battle_hud.get_node_or_null("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildStatusLabel") as Label
	if production_label != null:
		production_label.text = title_text if not title_text.is_empty() else _t("hud.production_ready", "Production Ready")
	if build_status_label != null:
		build_status_label.text = detail_text if not detail_text.is_empty() else _t("hud.build_status_default", "Choose a building to start placement.")

func _allowed_region_text(selection_id: String) -> String:
	if _selection_data_provider == null:
		return _t("battlemap.build_region.inner_castle", "Build Region: Inner Castle")
	var definition: Dictionary = _selection_data_provider.call("get_building_definition", selection_id)
	var allowed_regions: Array[String] = _string_array(definition.get("allowed_regions", []))
	if allowed_regions.has(REGION_OUTER_FIELD):
		return _t("battlemap.build_region.outer_field", "Build Region: Outer Field")
	return _t("battlemap.build_region.inner_castle", "Build Region: Inner Castle")

func _build_context_detail_text(reason_override_key: String = "") -> String:
	if not reason_override_key.is_empty():
		return _t(reason_override_key, _build_error_fallback(reason_override_key))
	if _hovered_slot_id.is_empty():
		return _t("battlemap.build_status.drag_hint", "Drag onto a highlighted slot to place this building.")
	var evaluation := _evaluate_slot_legality(_active_selection_id, _hovered_slot_id)
	var legality: String = str(evaluation.get("legality", ""))
	if legality == LEGALITY_VALID_INNER or legality == LEGALITY_VALID_OUTER:
		return _t("battlemap.build_status.valid_hint", "Release now to place this building.")
	var reason_key := str(evaluation.get("reason_key", "battlemap.build_error.wrong_region"))
	return _t(reason_key, _build_error_fallback(reason_key))

func _build_error_key_for_reason_code(reason_code: String) -> String:
	match reason_code:
		"tile_occupied", "blocked_tile":
			return "battlemap.build_error.slot_occupied"
		"insufficient_resources":
			return "battlemap.build_error.insufficient_resources"
		"wall_blocked":
			return "battlemap.build_error.wall_blocked"
		"invalid_target", "invalid_input", "invalid_terrain":
			return "battlemap.build_error.wrong_region"
		_:
			return "battlemap.prompt.build_place_failed"

func _build_error_fallback(reason_key: String) -> String:
	match reason_key:
		"battlemap.build_error.slot_occupied":
			return "That slot is already occupied."
		"battlemap.build_error.insufficient_resources":
			return "Not enough resources to place this building."
		"battlemap.build_error.wall_blocked":
			return "Buildings cannot be placed on wall tiles."
		"battlemap.build_error.wrong_region":
			return "This building cannot be placed in that region."
		"battlemap.prompt.build_place_failed":
			return "Building placement failed."
		_:
			return "Building placement failed."
