extends Node
class_name BuildingModeRuntime

const GRID_SIZE := 4
const GRID_BOUNDS := Rect2i(Vector2i.ZERO, Vector2i(10, 10))
const BUILDING_CORE_BRIDGE := preload("res://Game.Godot/Scripts/Building/BuildingModeCoreBridge.cs")

const TILE_WALL := "wall"
const TILE_GATE := "gate"

var resources: int = 200
var selected_type: String = ""
var placements: Array[Dictionary] = []
var gates: Array[Dictionary] = []

var _blocked := {}
var _occupied := {}
var _gate_allowed_by_cell := {}
var _bridge = BUILDING_CORE_BRIDGE.new()

func _ready() -> void:
	_bridge.ResetState(GRID_BOUNDS.size.x, GRID_BOUNDS.size.y, resources)
	_pull_runtime_state_from_bridge()

func list_building_types() -> Array[String]:
	var names: Array[String] = []
	for item in _bridge.ListBuildingTypes():
		names.append(String(item))
	return names

func select_building_type(building_type: String) -> bool:
	if not _bridge.HasBuildingType(building_type):
		return false
	selected_type = building_type
	return true

func get_cost(building_type: String) -> int:
	return int(_bridge.GetCost(building_type))

func get_footprint_size(building_type: String) -> int:
	return int(_bridge.GetFootprintSize(building_type))

func get_footprint_offsets(building_type: String) -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []
	for item in _bridge.GetFootprintOffsets(building_type):
		offsets.append(item)
	return offsets

func preview_at(world_position: Vector2) -> Dictionary:
	_push_runtime_state_to_bridge()
	var snapped_cell := _snap_to_cell(world_position)
	var covered_cells: Array = []
	var is_valid := false
	if not selected_type.is_empty() and _bridge.HasBuildingType(selected_type):
		var preview = _bridge.Preview(selected_type, snapped_cell)
		covered_cells = preview.get("covered_cells", [])
		is_valid = bool(preview.get("is_valid", false))
	var on_grid := _is_world_on_grid(world_position)
	return {
		"selected_type": selected_type,
		"snapped_cell": snapped_cell,
		"covered_cells": covered_cells,
		"on_grid": on_grid,
		"is_valid": is_valid,
	}

func confirm_at(world_position: Vector2) -> Dictionary:
	var resources_before := resources
	var placements_before := placements.size()

	if selected_type.is_empty() or not _bridge.HasBuildingType(selected_type):
		return _rejected(resources_before, placements_before)
	if not _is_world_on_grid(world_position):
		return _rejected(resources_before, placements_before)

	_push_runtime_state_to_bridge()
	var snapped_cell := _snap_to_cell(world_position)
	var result: Dictionary = _bridge.Confirm(selected_type, snapped_cell)
	_pull_runtime_state_from_bridge()
	return result

func set_blocked(cell: Vector2i) -> void:
	_blocked[_key(cell)] = true
	_bridge.SetBlocked(cell)

func set_occupied(cell: Vector2i) -> void:
	_occupied[_key(cell)] = true
	_bridge.SetOccupied(cell)

func drag_place_wall(path: Array[Vector2i]) -> Dictionary:
	_push_runtime_state_to_bridge()
	var result: Dictionary = _bridge.DragPlaceWall(path)
	_pull_runtime_state_from_bridge()
	return result

func create_gate_segments(from_cell: Vector2i, to_cell: Vector2i) -> Array[Dictionary]:
	var segments: Array[Dictionary] = []
	if from_cell.x != to_cell.x and from_cell.y != to_cell.y:
		return segments

	var cell_size := 16.0
	if from_cell.y == to_cell.y:
		var step_x := 1 if to_cell.x >= from_cell.x else -1
		for x in range(from_cell.x, to_cell.x + step_x, step_x):
			segments.append({
				"position": Vector2(x * cell_size, from_cell.y * cell_size),
				"direction": Vector2.RIGHT if step_x > 0 else Vector2.LEFT
			})
		return segments

	var step_y := 1 if to_cell.y >= from_cell.y else -1
	for y in range(from_cell.y, to_cell.y + step_y, step_y):
		segments.append({
			"position": Vector2(from_cell.x * cell_size, y * cell_size),
			"direction": Vector2.DOWN if step_y > 0 else Vector2.UP
		})
	return segments

func create_gate(from_cell: Vector2i, to_cell: Vector2i, gate_cost: int = 3) -> Dictionary:
	var invalid_input := gate_cost <= 0 \
		or from_cell == to_cell \
		or (from_cell.x != to_cell.x and from_cell.y != to_cell.y)
	if invalid_input:
		return {"accepted": false, "reason": "invalid_input"}
	if resources < gate_cost:
		return {"accepted": false, "reason": "insufficient_resources"}

	var segments = create_gate_segments(from_cell, to_cell)
	if segments.is_empty():
		return {"accepted": false, "reason": "invalid_input"}

	resources -= gate_cost
	var covered_cells: Array[Vector2i] = []
	for segment in segments:
		var pos: Vector2 = segment["position"]
		var direction: Vector2 = segment["direction"]
		var cell := Vector2i(int(round(pos.x / 16.0)), int(round(pos.y / 16.0)))
		covered_cells.append(cell)
		_gate_allowed_by_cell[_key(cell)] = Vector2i(int(direction.x), int(direction.y))

	gates.append({
		"from": from_cell,
		"to": to_cell,
		"kind": "gate",
		"cells": covered_cells,
		"direction": segments[0]["direction"]
	})
	return {"accepted": true, "reason": "ok"}

func can_traverse(tile_type: String, movement_direction: Vector2i, gate_allowed_direction: Vector2i) -> bool:
	if tile_type == TILE_WALL:
		return false
	if tile_type == TILE_GATE:
		return movement_direction == gate_allowed_direction
	return true

func can_traverse_gate(cell: Vector2i, movement_direction: Vector2i) -> bool:
	var key := _key(cell)
	if not _gate_allowed_by_cell.has(key):
		return false
	return movement_direction == _gate_allowed_by_cell[key]

func resolve_gate_fallback(seed_value: int, forced_gate_order: Array = []) -> String:
	return String(_bridge.ResolveGateFallback(seed_value, forced_gate_order))

func preview_overlay_for(origin: Vector2i, footprint: Array[Vector2i]) -> Dictionary:
	var occupied_cells: Array[Vector2i] = []
	for local_cell in footprint:
		occupied_cells.append(origin + local_cell)
	return {
		"occupied_cells": occupied_cells,
		"footprint_boundary": _bounds_from_cells(occupied_cells),
	}

func commit_cells(origin: Vector2i, footprint: Array[Vector2i]) -> Array[Vector2i]:
	var committed: Array[Vector2i] = []
	for local_cell in footprint:
		committed.append(origin + local_cell)
	return committed

func _rejected(resources_before: int, placements_before: int) -> Dictionary:
	return {
		"accepted": false,
		"resources_before": resources_before,
		"resources_after": resources,
		"placements_before": placements_before,
		"placements_after": placements.size(),
	}

func _push_runtime_state_to_bridge() -> void:
	_bridge.SetResources(resources)

func _pull_runtime_state_from_bridge() -> void:
	resources = int(_bridge.GetResources())
	placements.clear()
	var snapshot: Array = _bridge.GetPlacementsSnapshot()
	for item in snapshot:
		placements.append(item)

func _snap_to_cell(world_position: Vector2) -> Vector2i:
	return Vector2i(
		int(round(world_position.x / float(GRID_SIZE))),
		int(round(world_position.y / float(GRID_SIZE)))
	)

func _is_world_on_grid(world_position: Vector2) -> bool:
	return is_equal_approx(fmod(world_position.x, float(GRID_SIZE)), 0.0) \
		and is_equal_approx(fmod(world_position.y, float(GRID_SIZE)), 0.0)

func _cover_cells(origin_cell: Vector2i, building_type: String) -> Array[Vector2i]:
	var covered: Array[Vector2i] = []
	for offset in get_footprint_offsets(building_type):
		covered.append(origin_cell + offset)
	return covered

func _are_cells_placeable(cells: Array[Vector2i]) -> bool:
	if cells.is_empty():
		return false
	for cell in cells:
		if not _is_cell_placeable(cell):
			return false
	return true

func _is_cell_placeable(cell: Vector2i) -> bool:
	if not GRID_BOUNDS.has_point(cell):
		return false
	var key := _key(cell)
	if _blocked.has(key):
		return false
	if _occupied.has(key):
		return false
	return true

func _key(cell: Vector2i) -> String:
	return "%d:%d" % [cell.x, cell.y]

func _bounds_from_cells(cells: Array[Vector2i]) -> Rect2i:
	if cells.is_empty():
		return Rect2i()
	var min_x := cells[0].x
	var max_x := cells[0].x
	var min_y := cells[0].y
	var max_y := cells[0].y
	for cell in cells:
		min_x = min(min_x, cell.x)
		max_x = max(max_x, cell.x)
		min_y = min(min_y, cell.y)
		max_y = max(max_y, cell.y)
	return Rect2i(Vector2i(min_x, min_y), Vector2i(max_x - min_x + 1, max_y - min_y + 1))
