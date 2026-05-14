extends Node

var _screen: Control = null
var _bridge: Node = null
var _bridge_provider: Callable
var _status_label: Label = null
var _summary_label: Label = null
var _background: ColorRect = null
var _enemy_spawn_a: ColorRect = null
var _enemy_spawn_b: ColorRect = null
var _enemy_tokens := {}
var _path_points: PackedVector2Array = PackedVector2Array()
var _spawn_pulse_time_left := 0.0
var _translate: Callable

func configure(screen: Control, bridge: Node, refs: Dictionary) -> void:
	_screen = screen
	_bridge = bridge
	_bridge_provider = refs["bridge_provider"]
	_status_label = refs["status"]
	_summary_label = refs["summary"]
	_background = refs["background"]
	_enemy_spawn_a = refs["enemy_spawn_a"]
	_enemy_spawn_b = refs["enemy_spawn_b"]
	_path_points = refs["path_points"]
	_translate = refs["translate"]

func process_frame(delta: float) -> void:
	_update_spawn_cues(delta)
	_render_actor_tokens()

func mark_spawn_pulse(duration_sec: float) -> void:
	_spawn_pulse_time_left = duration_sec
	_apply_spawn_cues()

func clear_spawn_pulse() -> void:
	_spawn_pulse_time_left = 0.0
	_apply_spawn_cues()

func render_summary(result: Dictionary, status_text: String) -> void:
	var friendly := int(result.get("friendly_units_deployed", 0))
	var enemies := int(result.get("enemy_units_spawned", 0))
	var exchanges := int(result.get("combat_exchanges", 0))
	var retired := int(result.get("dead_units_retired", 0))
	var active := int(result.get("active_combat_nodes_after_cleanup", 0))
	var castle_hp := int(result.get("castle_hp", 0))
	_status_label.text = status_text
	_summary_label.text = "%s\n" % _t("battlemap.summary.title") \
		+ "%s: %d\n" % [_t("battlemap.summary.castle_hp"), castle_hp] \
		+ "%s: %d\n" % [_t("battlemap.summary.friendly_units"), friendly] \
		+ "%s: %d\n" % [_t("battlemap.summary.enemy_units_spawned"), enemies] \
		+ "%s: %d\n" % [_t("battlemap.summary.combat_exchanges"), exchanges] \
		+ "%s: %d\n" % [_t("battlemap.summary.dead_units_retired"), retired] \
		+ "%s: %d" % [_t("battlemap.summary.active_nodes"), active]

func render_loaded_summary(status_text: String) -> void:
	render_summary(_try_bridge_summary(), status_text)

func _render_actor_tokens() -> void:
	var bridge := _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		return
	var snapshots: Array = bridge.call("GetActorSnapshots")
	var alive_names := {}
	for item in snapshots:
		var d := item as Dictionary
		if d == null:
			continue
		if not bool(d.get("is_moving_enemy", false)):
			continue
		var actor_name := String(d.get("name", ""))
		if actor_name.is_empty():
			continue
		if not bool(d.get("active", false)):
			continue
		alive_names[actor_name] = true
		var token: ColorRect = _enemy_tokens.get(actor_name, null)
		if token == null:
			token = ColorRect.new()
			token.color = Color(0.95, 0.35, 0.35, 1.0)
			token.custom_minimum_size = Vector2(14, 14)
			token.size = Vector2(14, 14)
			_background.add_child(token)
			_enemy_tokens[actor_name] = token
		token.position = _sample_path(float(d.get("path_progress", 0.0)))

	for key in _enemy_tokens.keys():
		if alive_names.has(key):
			continue
		var stale: ColorRect = _enemy_tokens[key]
		if stale != null and is_instance_valid(stale):
			stale.queue_free()
		_enemy_tokens.erase(key)

func _update_spawn_cues(delta: float) -> void:
	if _spawn_pulse_time_left > 0.0:
		_spawn_pulse_time_left = maxf(0.0, _spawn_pulse_time_left - delta)
	_apply_spawn_cues()

func _apply_spawn_cues() -> void:
	var pulse_active := _spawn_pulse_time_left > 0.0
	var weak_color := Color(0.847059, 0.286275, 0.286275, 0.55)
	var pulse_color := Color(0.996078, 0.505882, 0.505882, 0.95)
	var spawn_color := pulse_color if pulse_active else weak_color
	_enemy_spawn_a.color = spawn_color
	_enemy_spawn_b.color = spawn_color

func _sample_path(progress: float) -> Vector2:
	var p := clampf(progress, 0.0, 1.0)
	var rev: PackedVector2Array = PackedVector2Array()
	for i in range(_path_points.size() - 1, -1, -1):
		rev.append(_path_points[i])
	var seg_count := rev.size() - 1
	if seg_count <= 0:
		return Vector2.ZERO
	var scaled := p * float(seg_count)
	var idx := mini(int(floor(scaled)), seg_count - 1)
	var local_t := scaled - float(idx)
	return rev[idx].lerp(rev[idx + 1], local_t)

func _t(key: String) -> String:
	if _translate.is_valid():
		return String(_translate.call(key))
	return key

func _try_bridge_summary() -> Dictionary:
	var bridge := _current_bridge()
	if bridge == null:
		return {}
	if bridge.has_method("GetSummary"):
		var result = bridge.call("GetSummary")
		if result is Dictionary:
			return result
	if bridge.has_method("RunCompleteCombatExperienceForTest"):
		var full = bridge.call("RunCompleteCombatExperienceForTest")
		if full is Dictionary:
			return full
	return {}

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided = _bridge_provider.call()
		if provided is Node:
			return provided
	return _bridge
