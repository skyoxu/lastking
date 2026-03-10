extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

func _add_navigation_region(outline: PackedVector2Array) -> NavigationRegion2D:
	for child in get_children():
		if child is NavigationRegion2D:
			remove_child(child)
			child.free()

	var region := NavigationRegion2D.new()
	var polygon := NavigationPolygon.new()
	polygon.add_outline(outline)
	polygon.make_polygons_from_outlines()
	region.navigation_polygon = polygon
	add_child(region)
	return region

func _build_navigation_grid(blocked_cells: Array[Vector2i]) -> AStarGrid2D:
	var grid := AStarGrid2D.new()
	grid.region = Rect2i(0, 0, 6, 6)
	grid.cell_size = Vector2(1, 1)
	grid.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_NEVER
	grid.update()
	for blocked in blocked_cells:
		grid.set_point_solid(blocked, true)
	return grid

func _candidate_from_navigation(id: String, target_class: String, start: Vector2i, goal: Vector2i, grid: AStarGrid2D, distance: int) -> Dictionary:
	var path: PackedVector2Array = grid.get_point_path(start, goal)
	var path_points := path.size()
	return {
		"id": id,
		"class": target_class,
		"reachable": path_points > 0,
		"blocked": path_points == 0,
		"path_points": path_points,
		"distance": distance,
		"blocks_route_to_higher_priority": false,
		"nav_position": Vector2(goal.x, goal.y)
	}

# ACC:T6.4
# ACC:T6.20
func test_selects_highest_reachable_target_in_strict_priority_order() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(8, 0), Vector2(8, 8), Vector2(0, 8)]))
	var grid := _build_navigation_grid([])
	var start := Vector2i(0, 0)
	var candidates: Array = [
		_candidate_from_navigation("w1", "wall", start, Vector2i(1, 0), grid, 3),
		_candidate_from_navigation("a1", "armed_defense", start, Vector2i(2, 0), grid, 4),
		_candidate_from_navigation("c1", "castle", start, Vector2i(3, 0), grid, 5),
		_candidate_from_navigation("u1", "unit", start, Vector2i(4, 0), grid, 6)
	]
	var nav_probe: Dictionary = probe.call("ProbeNavigationPath", Vector2(start.x, start.y), Vector2(4, 0))
	assert_bool(bool(nav_probe.get("navigation_api_used", false))).is_true()

	var selected: Dictionary = probe.call("SelectTarget", candidates)
	assert_str(str(selected.get("target_class", ""))).is_equal("Unit")
	assert_str(str(selected.get("target_id", ""))).is_equal("u1")
	assert_bool(bool(selected.get("is_fallback_attack", false))).is_false()

# ACC:T6.7
func test_navigation_constraints_exclude_blocked_or_non_navigable_targets() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(2, 0), Vector2(2, 6), Vector2(0, 6)]))
	var grid := _build_navigation_grid([
		Vector2i(1, 0), Vector2i(1, 1), Vector2i(1, 2), Vector2i(1, 3), Vector2i(1, 4), Vector2i(1, 5)
	])
	var start := Vector2i(0, 0)
	var blocked_unit := _candidate_from_navigation("u_blocked", "unit", start, Vector2i(3, 0), grid, 1)
	var valid_wall := _candidate_from_navigation("w_ok", "wall", start, Vector2i(0, 1), grid, 2)

	assert_bool(bool(blocked_unit["reachable"])).is_false()
	assert_bool(bool(valid_wall["reachable"])).is_true()
	var nav_probe: Dictionary = probe.call("ProbeNavigationPath", Vector2(start.x, start.y), Vector2(3, 0))
	assert_bool(bool(nav_probe.get("navigation_api_used", false))).is_true()
	assert_bool(nav_probe.has("path_points")).is_true()
	assert_bool(int(nav_probe.get("path_points", 0)) >= 0).is_true()

	var selected: Dictionary = probe.call("SelectTarget", [blocked_unit, valid_wall])
	assert_str(str(selected.get("target_id", ""))).is_equal("w_ok")

# ACC:T6.10
func test_lower_priority_selection_is_prevented_when_higher_reachable_exists() -> void:
	var probe := _new_probe()
	var grid := _build_navigation_grid([])
	var start := Vector2i(0, 0)
	var candidates: Array = [
		_candidate_from_navigation("c1", "castle", start, Vector2i(1, 0), grid, 2),
		_candidate_from_navigation("a1", "armed_defense", start, Vector2i(2, 0), grid, 2),
		_candidate_from_navigation("u1", "unit", start, Vector2i(3, 0), grid, 2)
	]

	var selected: Dictionary = probe.call("SelectTarget", candidates)
	assert_str(str(selected.get("target_class", ""))).is_equal("Unit")
	assert_str(str(selected.get("target_id", ""))).is_equal("u1")

# ACC:T6.13
func test_reachability_filter_runs_before_priority_order() -> void:
	var probe := _new_probe()
	var grid := _build_navigation_grid([
		Vector2i(1, 0), Vector2i(1, 1), Vector2i(1, 2), Vector2i(1, 3), Vector2i(1, 4), Vector2i(1, 5)
	])
	var start := Vector2i(0, 0)
	var unreachable_unit := _candidate_from_navigation("u_unreachable", "unit", start, Vector2i(3, 0), grid, 1)
	var reachable_castle := _candidate_from_navigation("c_reachable", "castle", start, Vector2i(0, 1), grid, 3)
	var reachable_armed := _candidate_from_navigation("a_reachable", "armed_defense", start, Vector2i(0, 2), grid, 3)

	var selected: Dictionary = probe.call("SelectTarget", [unreachable_unit, reachable_castle, reachable_armed])
	assert_str(str(selected.get("target_id", ""))).is_equal("c_reachable")
	assert_str(str(selected.get("target_class", ""))).is_equal("Castle")

# ACC:T6.7
func test_pathfinding_does_not_traverse_blocked_cells_and_uses_fallback_when_no_path_exists() -> void:
	var probe := _new_probe()
	_add_navigation_region(PackedVector2Array([Vector2(0, 0), Vector2(2, 0), Vector2(2, 6), Vector2(0, 6)]))
	var blocked_cells: Array[Vector2i] = [
		Vector2i(1, 0), Vector2i(1, 1), Vector2i(1, 2), Vector2i(1, 3), Vector2i(1, 4), Vector2i(1, 5)
	]
	var grid := _build_navigation_grid(blocked_cells)
	var start := Vector2i(0, 0)
	var goal := Vector2i(3, 0)
	var path: PackedVector2Array = grid.get_point_path(start, goal)

	assert_int(path.size()).is_equal(0)

	var fallback_candidates: Array = [
		{"id": "unit_blocked", "class": "unit", "reachable": false, "blocked": true, "path_points": 0, "distance": 1, "blocks_route_to_higher_priority": false, "nav_position": Vector2(5, 3)},
		{"id": "blocker_near", "class": "blocking_structure", "reachable": false, "blocked": true, "path_points": 0, "distance": 2, "blocks_route_to_higher_priority": true, "nav_position": Vector2(1, 1)},
		{"id": "blocker_far", "class": "blocking_structure", "reachable": false, "blocked": true, "path_points": 0, "distance": 6, "blocks_route_to_higher_priority": true, "nav_position": Vector2(1, 5)}
	]
	var nav_probe: Dictionary = probe.call("ProbeNavigationPath", Vector2(start.x, start.y), Vector2(3, 0))
	assert_bool(bool(nav_probe.get("navigation_api_used", false))).is_true()
	assert_int(int(nav_probe.get("path_points", 0))).is_equal(0)

	var selected: Dictionary = probe.call("SelectTarget", fallback_candidates)
	assert_str(str(selected.get("target_id", ""))).is_equal("blocker_near")
	assert_bool(bool(selected.get("is_fallback_attack", false))).is_true()
